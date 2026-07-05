using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class SecurityPlayerRobotController : MonoBehaviour
{
    [SerializeField] private Transform playerRoot;
    [SerializeField] private GameObject playerRobot;
    [SerializeField] private GameObject playerRobotPrefab;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private FactoryMapCameraController mapCameraController;
    [SerializeField] private KeyCode toggleKey = KeyCode.P;
    [SerializeField] private KeyCode rollKey = KeyCode.Space;
    [SerializeField] private bool startInPlayerMode;
    [SerializeField] private bool enableMapCameraControlsWhenPlayerModeOff = true;
    [SerializeField] private bool holdShiftToRoll = true;
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float rollSpeed = 10f;
    [SerializeField] private float turnSpeed = 540f;
    [SerializeField] private float cameraDistance = 8f;
    [SerializeField] private float cameraHeight = 3.2f;
    [SerializeField] private float cameraLookHeight = 1.4f;
    [SerializeField] private float cameraFollowSmoothing = 12f;
    [SerializeField] private bool keepGrounded = true;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private string openBoolName = "Open_Anim";
    [SerializeField] private string walkBoolName = "Walk_Anim";
    [SerializeField] private string rollBoolName = "Roll_Anim";

    private Animator animator;
    private bool playerModeEnabled;
    private bool rollMode;
    private CameraViewState cameraViewBeforePlayer;
    private bool hasCameraViewBeforePlayer;
    private bool hasAppliedInitialMode;

    public void Configure(Transform nextPlayerRoot, GameObject nextPlayerRobotPrefab, FactoryMapCameraController nextMapCameraController)
    {
        playerRoot = playerRoot != null ? playerRoot : nextPlayerRoot;
        playerRobotPrefab = playerRobotPrefab != null ? playerRobotPrefab : nextPlayerRobotPrefab;
        mapCameraController = mapCameraController != null ? mapCameraController : nextMapCameraController;
    }

    private void Start()
    {
        mainCamera = mainCamera != null ? mainCamera : Camera.main;
        if (mapCameraController == null && mainCamera != null) mapCameraController = mainCamera.GetComponent<FactoryMapCameraController>();
        EnsurePlayerRobot();
        SetPlayerMode(startInPlayerMode);
    }

    private void Update()
    {
        if (WasPressed(toggleKey)) SetPlayerMode(!playerModeEnabled);
        if (!playerModeEnabled || playerRobot == null) return;

        if (WasPressed(rollKey)) rollMode = !rollMode;
        Vector2 input = ReadMoveInput();
        bool moving = input.sqrMagnitude > 0.01f;
        bool rolling = rollMode || (holdShiftToRoll && IsShiftHeld());

        MovePlayer(input, rolling);
        SetAnimatorBool(openBoolName, true);
        SetAnimatorBool(walkBoolName, moving && !rolling);
        SetAnimatorBool(rollBoolName, moving && rolling);
    }

    private void LateUpdate()
    {
        if (!playerModeEnabled || playerRobot == null || mainCamera == null) return;

        Vector3 lookAt = playerRobot.transform.position + Vector3.up * cameraLookHeight;
        Vector3 desired = lookAt - playerRobot.transform.forward * cameraDistance + Vector3.up * cameraHeight;
        float factor = 1f - Mathf.Exp(-cameraFollowSmoothing * Time.deltaTime);
        mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, desired, factor);
        mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, Quaternion.LookRotation(lookAt - mainCamera.transform.position, Vector3.up), factor);
    }

    private void SetPlayerMode(bool enabled)
    {
        bool wasEnabled = playerModeEnabled;
        playerModeEnabled = enabled;
        if (mainCamera == null) mainCamera = Camera.main;

        if (mapCameraController != null)
        {
            if (enabled)
            {
                if (!wasEnabled && mainCamera != null)
                {
                    cameraViewBeforePlayer = CameraViewState.Capture(mainCamera);
                    hasCameraViewBeforePlayer = true;
                }
                mapCameraController.enabled = false;
            }
            else if (wasEnabled && hasCameraViewBeforePlayer)
            {
                cameraViewBeforePlayer.Apply(mainCamera);
                hasCameraViewBeforePlayer = false;
                mapCameraController.CaptureCurrentCameraView();
                mapCameraController.enabled = enableMapCameraControlsWhenPlayerModeOff;
            }
            else if (!hasAppliedInitialMode)
            {
                mapCameraController.CaptureCurrentCameraView();
                mapCameraController.enabled = enableMapCameraControlsWhenPlayerModeOff;
            }
        }

        if (mainCamera != null && enabled)
        {
            mainCamera.orthographic = false;
        }

        SetAnimatorBool(openBoolName, enabled);
        if (!enabled)
        {
            SetAnimatorBool(walkBoolName, false);
            SetAnimatorBool(rollBoolName, false);
            rollMode = false;
        }

        hasAppliedInitialMode = true;
    }

    private struct CameraViewState
    {
        private Vector3 position;
        private Quaternion rotation;
        private bool orthographic;
        private float fieldOfView;
        private float orthographicSize;

        public static CameraViewState Capture(Camera camera)
        {
            if (camera == null) return default;
            return new CameraViewState
            {
                position = camera.transform.position,
                rotation = camera.transform.rotation,
                orthographic = camera.orthographic,
                fieldOfView = camera.fieldOfView,
                orthographicSize = camera.orthographicSize,
            };
        }

        public void Apply(Camera camera)
        {
            if (camera == null) return;
            camera.transform.position = position;
            camera.transform.rotation = rotation;
            camera.orthographic = orthographic;
            camera.fieldOfView = fieldOfView;
            camera.orthographicSize = orthographicSize;
        }
    }

    private void EnsurePlayerRobot()
    {
        if (playerRobot == null)
        {
            GameObject found = GameObject.Find("robotSphere") ?? GameObject.Find("RobotSphere");
            if (found == null)
            {
                Animator[] animators = FindObjectsOfType<Animator>(true);
                foreach (Animator item in animators)
                {
                    if (item != null && item.gameObject.name.IndexOf("robotSphere", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        found = item.gameObject;
                        break;
                    }
                }
            }
            playerRobot = found;
        }

        if (playerRobot == null && playerRobotPrefab != null)
        {
            playerRobot = Instantiate(playerRobotPrefab);
            playerRobot.name = "robotSphere_Player";
            if (playerRoot != null) playerRobot.transform.SetParent(playerRoot, true);
        }

        if (playerRobot != null)
        {
            animator = playerRobot.GetComponentInChildren<Animator>(true);
        }
    }

    private void MovePlayer(Vector2 input, bool rolling)
    {
        if (input.sqrMagnitude < 0.01f) return;

        Transform playerTransform = playerRobot.transform;
        Transform reference = mainCamera != null ? mainCamera.transform : playerTransform;
        Vector3 forward = reference.forward;
        Vector3 right = reference.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 direction = (forward * input.y + right * input.x).normalized;
        float speed = rolling ? rollSpeed : moveSpeed;
        playerTransform.position += direction * speed * Time.deltaTime;

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        playerTransform.rotation = Quaternion.RotateTowards(playerTransform.rotation, targetRotation, turnSpeed * Time.deltaTime);

        if (keepGrounded)
        {
            Vector3 origin = playerTransform.position + Vector3.up * 6f;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 20f, groundMask, QueryTriggerInteraction.Ignore))
            {
                playerTransform.position = new Vector3(playerTransform.position.x, hit.point.y, playerTransform.position.z);
            }
        }
    }

    private void SetAnimatorBool(string parameterName, bool value)
    {
        if (animator == null || string.IsNullOrWhiteSpace(parameterName)) return;
        if (animator.runtimeAnimatorController == null) return;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Bool && parameter.name == parameterName)
            {
                animator.SetBool(parameterName, value);
                return;
            }
        }
    }

    private Vector2 ReadMoveInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            Vector2 value = Vector2.zero;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) value.x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) value.x += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) value.y -= 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) value.y += 1f;
            return Vector2.ClampMagnitude(value, 1f);
        }
#endif
        try
        {
            return Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);
        }
        catch (InvalidOperationException)
        {
            return Vector2.zero;
        }
    }

    private bool WasPressed(KeyCode keyCode)
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (keyCode == KeyCode.P) return Keyboard.current.pKey.wasPressedThisFrame;
            if (keyCode == KeyCode.Space) return Keyboard.current.spaceKey.wasPressedThisFrame;
        }
#endif
        try
        {
            return Input.GetKeyDown(keyCode);
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private bool IsShiftHeld()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null) return Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
#endif
        try
        {
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
