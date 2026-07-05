using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Camera))]
public sealed class FactoryMapCameraController : MonoBehaviour
{
    [SerializeField] private bool resetViewOnAwake;
    [SerializeField] private float defaultYaw = 42f;
    [SerializeField] private float defaultPitch = 58f;
    [SerializeField] private float defaultOrthographicSize = 55f;
    [SerializeField] private float minOrthographicSize = 12f;
    [SerializeField] private float maxOrthographicSize = 78f;
    [SerializeField] private float focusOrthographicSize = 24f;
    [SerializeField] private bool autoExpandZoomBoundsToScene = true;
    [SerializeField] private float orbitSensitivity = 0.18f;
    [SerializeField] private float panSensitivity = 1.1f;
    [SerializeField] private float zoomSensitivity = 5.5f;
    [SerializeField] private float smoothing = 12f;

    private Camera mapCamera;
    private Vector3 targetPivot;
    private Vector3 currentPivot;
    private float targetYaw;
    private float currentYaw;
    private float targetPitch;
    private float currentPitch;
    private float targetSize;
    private float currentSize;
    private Vector3 siteCenter;
    private float siteHalfWidth = 60f;
    private float siteHalfDepth = 40f;
    private float cameraDistance = 90f;
    private bool configured;

    private void Awake()
    {
        mapCamera = GetComponent<Camera>();
        if (resetViewOnAwake)
        {
            mapCamera.orthographic = true;
            ResetViewImmediate();
        }
        else
        {
            CaptureCurrentCameraView();
        }
    }

    public void ConfigureSiteBounds(float widthMeters, float depthMeters)
    {
        ConfigureSiteBounds(widthMeters, depthMeters, Vector3.zero);
    }

    public void ConfigureSiteBounds(float widthMeters, float depthMeters, Vector3 center)
    {
        ConfigureSiteBounds(widthMeters, depthMeters, center, true);
    }

    public void ConfigureSiteBounds(float widthMeters, float depthMeters, Vector3 center, bool applyInitialView)
    {
        siteCenter = new Vector3(center.x, 0f, center.z);
        siteHalfWidth = Mathf.Max(10f, widthMeters * 0.5f);
        siteHalfDepth = Mathf.Max(10f, depthMeters * 0.5f);
        if (autoExpandZoomBoundsToScene)
        {
            float siteSize = Mathf.Max(siteHalfWidth, siteHalfDepth);
            defaultOrthographicSize = Mathf.Max(defaultOrthographicSize, siteSize * 0.45f);
            maxOrthographicSize = Mathf.Max(maxOrthographicSize, siteSize * 0.95f);
        }
        configured = true;

        if (applyInitialView)
        {
            targetPivot = siteCenter;
            currentPivot = siteCenter;
            ClampTargetPivot();
            ApplyCameraTransform(true);
        }
        else
        {
            CaptureCurrentCameraView();
            ClampTargetPivot();
            currentPivot = targetPivot;
        }
    }

    public void ResetView()
    {
        targetPivot = siteCenter;
        targetYaw = defaultYaw;
        targetPitch = defaultPitch;
        targetSize = defaultOrthographicSize;
        ClampTargetPivot();
    }

    public void SetZoomLevel(float normalizedZoom)
    {
        float zoom = Mathf.Clamp(normalizedZoom, 0.65f, 1.7f);
        targetSize = Mathf.Clamp(defaultOrthographicSize / zoom, minOrthographicSize, maxOrthographicSize);
    }

    public void FocusOn(Vector3 worldPosition, float preferredSize = -1f)
    {
        targetPivot = new Vector3(worldPosition.x, 0f, worldPosition.z);
        targetSize = Mathf.Clamp(preferredSize > 0f ? preferredSize : focusOrthographicSize, minOrthographicSize, maxOrthographicSize);
        ClampTargetPivot();
    }

    public void CaptureCurrentCameraView()
    {
        if (mapCamera == null) mapCamera = GetComponent<Camera>();
        Vector3 pivot = EstimateCurrentPivot();
        targetPivot = pivot;
        currentPivot = pivot;

        Vector3 euler = transform.rotation.eulerAngles;
        targetPitch = NormalizeAngle(euler.x);
        currentPitch = targetPitch;
        targetYaw = euler.y;
        currentYaw = targetYaw;

        float size = mapCamera != null && mapCamera.orthographic ? mapCamera.orthographicSize : defaultOrthographicSize;
        targetSize = size;
        currentSize = size;
    }

    private void Update()
    {
        if (!configured) return;

        HandleMouseInput();
        ApplyCameraTransform(false);
    }

    private void HandleMouseInput()
    {
        Vector2 delta = ReadMouseDelta();
        bool shiftHeld = IsShiftHeld();

        if (IsMouseButtonPressed(0) && !shiftHeld)
        {
            Pan(delta);
        }

        if (IsMouseButtonPressed(1) || (shiftHeld && IsMouseButtonPressed(0)))
        {
            targetYaw += delta.x * orbitSensitivity * 10f;
            targetPitch = Mathf.Clamp(targetPitch - delta.y * orbitSensitivity * 10f, 38f, 72f);
        }

        float scroll = ReadMouseScroll();
        if (Mathf.Abs(scroll) > 0.01f)
        {
            targetSize = Mathf.Clamp(targetSize - scroll * zoomSensitivity, minOrthographicSize, maxOrthographicSize);
        }
    }

    private static Vector2 ReadMouseDelta()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            return Mouse.current.delta.ReadValue() * 0.1f;
        }
#endif
        try
        {
            return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        }
        catch
        {
            return Vector2.zero;
        }
    }

    private static float ReadMouseScroll()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            return Mathf.Abs(scroll) > 10f ? scroll / 120f : scroll;
        }
#endif
        try
        {
            return Input.mouseScrollDelta.y;
        }
        catch
        {
            return 0f;
        }
    }

    private static bool IsMouseButtonPressed(int button)
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            if (button == 0) return Mouse.current.leftButton.isPressed;
            if (button == 1) return Mouse.current.rightButton.isPressed;
            if (button == 2) return Mouse.current.middleButton.isPressed;
        }
#endif
        try
        {
            return Input.GetMouseButton(button);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsShiftHeld()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            return Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
        }
#endif
        try
        {
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        }
        catch
        {
            return false;
        }
    }

    private void Pan(Vector2 delta)
    {
        if (delta.sqrMagnitude < 0.0001f) return;

        Quaternion rotation = Quaternion.Euler(0f, currentYaw, 0f);
        Vector3 right = rotation * Vector3.right;
        Vector3 forward = rotation * Vector3.forward;
        right.y = 0f;
        forward.y = 0f;
        right.Normalize();
        forward.Normalize();

        float scale = (targetSize / Mathf.Max(1f, Screen.height)) * panSensitivity;
        targetPivot -= (right * delta.x + forward * delta.y) * scale * 2f;
        ClampTargetPivot();
    }

    private void ClampTargetPivot()
    {
        targetPivot.x = Mathf.Clamp(targetPivot.x, siteCenter.x - siteHalfWidth, siteCenter.x + siteHalfWidth);
        targetPivot.z = Mathf.Clamp(targetPivot.z, siteCenter.z - siteHalfDepth, siteCenter.z + siteHalfDepth);
        targetPivot.y = 0f;
    }

    private void ApplyCameraTransform(bool immediate)
    {
        float factor = immediate ? 1f : 1f - Mathf.Exp(-smoothing * Time.deltaTime);
        currentPivot = Vector3.Lerp(currentPivot, targetPivot, factor);
        currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, factor);
        currentPitch = Mathf.Lerp(currentPitch, targetPitch, factor);
        currentSize = Mathf.Lerp(currentSize, targetSize, factor);

        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        mapCamera.orthographic = true;
        mapCamera.orthographicSize = currentSize;
        transform.position = currentPivot - rotation * Vector3.forward * cameraDistance;
        transform.rotation = rotation;
    }

    private void ResetViewImmediate()
    {
        targetPivot = Vector3.zero;
        currentPivot = Vector3.zero;
        targetYaw = defaultYaw;
        currentYaw = defaultYaw;
        targetPitch = defaultPitch;
        currentPitch = defaultPitch;
        targetSize = defaultOrthographicSize;
        currentSize = defaultOrthographicSize;
        ApplyCameraTransform(true);
    }

    private Vector3 EstimateCurrentPivot()
    {
        Vector3 position = transform.position;
        Vector3 forward = transform.forward;
        if (Mathf.Abs(forward.y) > 0.001f)
        {
            float t = -position.y / forward.y;
            if (t > 0f)
            {
                Vector3 hit = position + forward * t;
                return new Vector3(hit.x, 0f, hit.z);
            }
        }

        return new Vector3(position.x, 0f, position.z);
    }

    private static float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }
}
