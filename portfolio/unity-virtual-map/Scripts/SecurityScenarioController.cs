using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class SecurityScenarioController : MonoBehaviour
{
    public enum SecurityScenarioAction
    {
        FenceClimb,
        FenceDamage,
        FacilityFilming,
        FacilityDamage,
    }

    [Serializable]
    public sealed class ScenarioConfig
    {
        public string scenarioId = "fence_climb";
        public string incidentId = "INC-UNITY-FENCE-CLIMB";
        public string cameraId = "cam_01";
        public string zoneId = "fence_north";
        public SecurityScenarioAction action = SecurityScenarioAction.FenceClimb;
        public GameObject actor;
        public GameObject fallbackPrefab;
        public Transform startPoint;
        public Transform actionPoint;
        public Transform exitPoint;
        [HideInInspector]
        public Transform focusPoint;
        public Transform targetProp;
        public float moveSeconds = 3f;
        public float actionSeconds = 2.4f;
        public float climbHeight = 3.2f;
        public float walkBlendSpeed = 2f;
        public float runBlendSpeed = 6f;
        public float runUpSeconds = 0.45f;
        public float runUpDistance = 1.4f;
        public float landingSeconds = 0.35f;
        public string idleStateName = "Idle";
        public string moveStateName = "Walk";
        public string actionStateName = "";
        public string airborneStateName = "InAir";
        public string landingStateName = "JumpLand";
        public string moveBoolName = "";
        public string actionBoolName = "";
        public bool hideActorAtSceneStart;
    }

    [SerializeField] private SecurityShowcaseController showcase;
    [SerializeField] private Transform actorRoot;
    [SerializeField] private Transform anchorRoot;
    [SerializeField] private GameObject intrusionRobotPrefab;
    [SerializeField] private GameObject attackRobotPrefab;
    [SerializeField] private bool keyboardShortcuts = true;
    [SerializeField] private ScenarioConfig[] scenarios = Array.Empty<ScenarioConfig>();
    [Header("Animator Controllers")]
    [SerializeField] private bool switchMechControllerForMovement = true;
    [SerializeField] private bool useMechInPlaceMovementClips = true;
    [SerializeField] private bool useDirectMechWalkClip = false;
    [SerializeField] private bool raiseIncidentAtActionStart = true;
    [SerializeField] private bool showDamageTargetPulse;
    [SerializeField] private RuntimeAnimatorController mechMovingController;
    [SerializeField] private RuntimeAnimatorController mechDemoController;
    [SerializeField] private AnimationClip mechWalkInPlaceClip;
    [SerializeField] private AnimationClip mechRunInPlaceClip;
    [Header("Actor Grounding")]
    [SerializeField] private bool snapScenarioActorsToGround = true;
    [SerializeField] private LayerMask scenarioGroundLayers = ~0;
    [SerializeField] private float groundProbeHeight = 120f;
    [SerializeField] private float groundProbeDepth = 280f;
    [SerializeField] private float groundOffset = 0f;

    private Bounds sceneBounds = new Bounds(Vector3.zero, new Vector3(120f, 20f, 80f));
    private readonly Dictionary<string, Coroutine> activeRoutines = new Dictionary<string, Coroutine>(StringComparer.OrdinalIgnoreCase);
    private Material filmingBeamMaterial;
    private Material damagePulseMaterial;
    private RuntimeAnimatorController mechMovementControllerInstance;

    public void Configure(
        SecurityShowcaseController owner,
        Transform nextActorRoot,
        Transform nextAnchorRoot,
        Bounds nextSceneBounds,
        GameObject nextIntrusionRobotPrefab,
        GameObject nextAttackRobotPrefab)
    {
        showcase = showcase != null ? showcase : owner;
        actorRoot = actorRoot != null ? actorRoot : nextActorRoot;
        anchorRoot = anchorRoot != null ? anchorRoot : nextAnchorRoot;
        sceneBounds = nextSceneBounds.size.sqrMagnitude > 0.01f ? nextSceneBounds : sceneBounds;
        intrusionRobotPrefab = intrusionRobotPrefab != null ? intrusionRobotPrefab : nextIntrusionRobotPrefab;
        attackRobotPrefab = attackRobotPrefab != null ? attackRobotPrefab : nextAttackRobotPrefab;

        EnsureDefaultScenarios();
        ResolveDefaultAnchors();
    }

    private void Start()
    {
#if UNITY_EDITOR
        AutoAssignEditorAnimatorControllers();
#endif
        if (showcase == null) showcase = FindObjectOfType<SecurityShowcaseController>(true);
        EnsureDefaultScenarios();
        ResolveDefaultAnchors();

        foreach (ScenarioConfig scenario in scenarios)
        {
            if (scenario?.actor != null && scenario.hideActorAtSceneStart) scenario.actor.SetActive(false);
        }
    }

    private void Update()
    {
        if (!keyboardShortcuts) return;
        if (WasPressed(KeyCode.Alpha1)) PlayScenario("fence_climb");
        if (WasPressed(KeyCode.Alpha2)) PlayScenario("fence_damage");
        if (WasPressed(KeyCode.Alpha3)) PlayScenario("facility_filming");
        if (WasPressed(KeyCode.Alpha4)) PlayScenario("facility_damage");
    }

    public bool PlayScenario(string scenarioId)
    {
        EnsureDefaultScenarios();
        ScenarioConfig scenario = FindScenario(scenarioId);
        if (scenario == null) return false;

        string key = NormalizeScenarioId(scenario.scenarioId);
        if (activeRoutines.TryGetValue(key, out Coroutine activeRoutine) && activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
        }

        activeRoutines[key] = StartCoroutine(RunScenario(scenario, key));
        return true;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoAssignEditorAnimatorControllers();
    }

    public void EnsureDefaultsForEditor(Bounds nextSceneBounds, GameObject nextIntrusionRobotPrefab, GameObject nextAttackRobotPrefab)
    {
        AutoAssignEditorAnimatorControllers();
        sceneBounds = nextSceneBounds.size.sqrMagnitude > 0.01f ? nextSceneBounds : sceneBounds;
        intrusionRobotPrefab = intrusionRobotPrefab != null ? intrusionRobotPrefab : nextIntrusionRobotPrefab;
        attackRobotPrefab = attackRobotPrefab != null ? attackRobotPrefab : nextAttackRobotPrefab;
        EnsureDefaultScenarios();
        ResolveDefaultAnchors();
        UnityEditor.EditorUtility.SetDirty(this);
    }

    private void AutoAssignEditorAnimatorControllers()
    {
        bool changed = false;
        if (mechMovingController == null)
        {
            mechMovingController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Mech/Controller/Mech_Moving.controller");
            changed = mechMovingController != null;
        }

        if (mechDemoController == null)
        {
            mechDemoController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Mech/Controller/Mech_Demo.controller");
            changed = changed || mechDemoController != null;
        }

        if (mechWalkInPlaceClip == null || mechRunInPlaceClip == null)
        {
            UnityEngine.Object[] mechAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Mech/Models/mech.fbx");
            foreach (UnityEngine.Object asset in mechAssets)
            {
                AnimationClip clip = asset as AnimationClip;
                if (clip == null) continue;
                if (mechWalkInPlaceClip == null && clip.name == "WalkInPlace")
                {
                    mechWalkInPlaceClip = clip;
                    changed = true;
                }
                if (mechRunInPlaceClip == null && clip.name == "Run_InPlace")
                {
                    mechRunInPlaceClip = clip;
                    changed = true;
                }
            }
        }

        if (changed)
        {
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif

    private IEnumerator RunScenario(ScenarioConfig scenario, string routineKey)
    {
        GameObject actor = ResolveActor(scenario);
        if (actor == null)
        {
            activeRoutines.Remove(routineKey);
            yield break;
        }

        ApplyAnimatorProfileFallback(scenario, actor);
        ApplyMechDemoControllerIfNeeded(scenario, actor);
        NormalizeScenarioAnimationDefaults(scenario);
        actor.SetActive(true);
        if (scenario.startPoint != null) actor.transform.position = ResolveActorGroundPosition(scenario.startPoint.position);
        ResetJumpParameters(actor.GetComponentInChildren<Animator>(true), true);

        Transform actionPoint = scenario.actionPoint;
        if (actionPoint != null)
        {
            yield return MoveActor(actor.transform, ResolveActorGroundPosition(actionPoint.position), Mathf.Max(0.1f, scenario.moveSeconds), scenario);
        }

        bool incidentRaised = false;
        if (raiseIncidentAtActionStart)
        {
            showcase?.RaiseScenarioIncident(scenario.incidentId, scenario.cameraId, scenario.zoneId, "detected");
            incidentRaised = true;
        }

        switch (scenario.action)
        {
            case SecurityScenarioAction.FenceClimb:
                yield return FenceClimb(actor.transform, scenario);
                break;
            case SecurityScenarioAction.FenceDamage:
            case SecurityScenarioAction.FacilityDamage:
                yield return DamageAction(actor.transform, scenario);
                break;
            case SecurityScenarioAction.FacilityFilming:
                yield return FilmingAction(actor.transform, scenario);
                break;
        }

        if (!incidentRaised)
        {
            showcase?.RaiseScenarioIncident(scenario.incidentId, scenario.cameraId, scenario.zoneId, "detected");
        }
        activeRoutines.Remove(routineKey);
    }

    private IEnumerator MoveActor(Transform actor, Vector3 target, float seconds, ScenarioConfig scenario)
    {
        if (IsMechDamageScenario(scenario))
        {
            yield return MoveMechActorWithDemoController(actor, target, seconds, scenario);
            yield break;
        }

        Animator animator = actor.GetComponentInChildren<Animator>(true);
        string moveStateName = ResolveSafeMoveState(animator, scenario);
        SecurityMechAttackAdapter mechMoveLock = null;
        bool usingDirectMechWalkClip = TryBeginDirectMechWalkClip(mechMoveLock, scenario);
        RuntimeAnimatorController restoreController = usingDirectMechWalkClip
            ? null
            : SwitchToMovementControllerIfNeeded(animator, scenario, moveStateName);
        if (!usingDirectMechWalkClip && restoreController != null)
        {
            if (mechMoveLock == null) mechMoveLock = actor.GetComponentInChildren<SecurityMechAttackAdapter>(true);
            mechMoveLock?.BeginScenarioMoveLock();
        }

        SetAnimatorBool(animator, scenario.moveBoolName, true);
        float moveBlendSpeed = ResolveLocomotionBlendSpeed(animator, moveStateName, scenario.walkBlendSpeed, scenario);
        if (!usingDirectMechWalkClip)
        {
            ApplyMoveAnimatorState(animator, moveBlendSpeed, 1.0f);
            CrossFadeIfStateExists(animator, moveStateName, 0.08f);
        }

        Vector3 start = actor.position;
        mechMoveLock?.SetScenarioMoveLockPose(actor.position, actor.rotation);
        for (float elapsed = 0f; elapsed < seconds; elapsed += Time.deltaTime)
        {
            if (!usingDirectMechWalkClip) ApplyMoveAnimatorState(animator, moveBlendSpeed, 1.0f);
            float t = Mathf.SmoothStep(0f, 1f, elapsed / seconds);
            actor.position = Vector3.Lerp(start, target, t);
            Face(actor, target);
            mechMoveLock?.SetScenarioMoveLockPose(actor.position, actor.rotation);
            yield return null;
        }

        actor.position = target;
        mechMoveLock?.SetScenarioMoveLockPose(actor.position, actor.rotation);
        SetAnimatorBool(animator, scenario.moveBoolName, false);
        ResetJumpParameters(animator, true);
        SetAnimatorFloatIfExists(animator, "Speed", 0f);
        SetAnimatorFloatIfExists(animator, "MotionSpeed", 0f);
        if (usingDirectMechWalkClip)
        {
            mechMoveLock?.EndScenarioWalkClip();
            mechMoveLock?.SetScenarioMoveLockPose(actor.position, actor.rotation);
        }
        else if (restoreController != null)
        {
            Quaternion finalRotation = actor.rotation;
            RestoreAnimatorController(animator, restoreController, actor);
            actor.position = target;
            actor.rotation = finalRotation;
            mechMoveLock?.SetScenarioMoveLockPose(actor.position, actor.rotation);
        }
        else
        {
            CrossFadeIfStateExists(animator, scenario.idleStateName, 0.12f);
        }
    }

    private IEnumerator MoveMechActorWithDemoController(Transform actor, Vector3 target, float seconds, ScenarioConfig scenario)
    {
        Animator animator = actor.GetComponentInChildren<Animator>(true);
        SecurityMechAttackAdapter mechAdapter = actor.GetComponentInChildren<SecurityMechAttackAdapter>(true);

        mechAdapter?.EndAttack();
        mechAdapter?.EndScenarioWalkClip();
        mechAdapter?.BeginScenarioMoveLock();
        if (mechDemoController != null && animator != null && animator.runtimeAnimatorController != mechDemoController)
        {
            animator.runtimeAnimatorController = mechDemoController;
            animator.Rebind();
            animator.Update(0f);
        }

        SetAnimatorBool(animator, scenario.moveBoolName, false);
        SetAnimatorBoolIfExists(animator, "Move", true);
        ResetJumpParameters(animator, true);
        SetAnimatorFloatIfExists(animator, "Speed", 0f);
        SetAnimatorFloatIfExists(animator, "MotionSpeed", 0f);
        CrossFadeIfStateExists(animator, "Walk", 0.08f);

        Vector3 start = actor.position;
        mechAdapter?.SetScenarioMoveLockPose(actor.position, actor.rotation);
        for (float elapsed = 0f; elapsed < seconds; elapsed += Time.deltaTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / seconds);
            actor.position = Vector3.Lerp(start, target, t);
            Face(actor, target);
            mechAdapter?.SetScenarioMoveLockPose(actor.position, actor.rotation);
            yield return null;
        }

        actor.position = target;
        if (scenario.targetProp != null)
        {
            Face(actor, scenario.targetProp.position);
        }
        else
        {
            Face(actor, target);
        }

        SetAnimatorBoolIfExists(animator, "Move", false);
        mechAdapter?.SetScenarioMoveLockPose(actor.position, actor.rotation);
    }

    private IEnumerator FenceClimb(Transform actor, ScenarioConfig scenario)
    {
        Vector3 end = scenario.exitPoint != null ? ResolveActorGroundPosition(scenario.exitPoint.position) : ResolveActorGroundPosition(actor.position + actor.forward * 4f);
        Vector3 start = actor.position;
        Animator animator = actor.GetComponentInChildren<Animator>(true);

        Vector3 horizontal = end - start;
        horizontal.y = 0f;
        float horizontalDistance = horizontal.magnitude;
        if (horizontalDistance > 0.05f && scenario.runUpSeconds > 0.01f && scenario.runUpDistance > 0.01f)
        {
            Vector3 runTarget = start + horizontal.normalized * Mathf.Min(scenario.runUpDistance, horizontalDistance * 0.45f);
            ApplyMoveAnimatorState(animator, Mathf.Max(0.01f, scenario.runBlendSpeed), 1.15f);
            CrossFadeIfStateExists(animator, "Run", 0.08f);

            float runSeconds = Mathf.Max(0.05f, scenario.runUpSeconds);
            for (float elapsed = 0f; elapsed < runSeconds; elapsed += Time.deltaTime)
            {
                ApplyMoveAnimatorState(animator, Mathf.Max(0.01f, scenario.runBlendSpeed), 1.15f);
                float t = Mathf.SmoothStep(0f, 1f, elapsed / runSeconds);
                actor.position = Vector3.Lerp(start, runTarget, t);
                Face(actor, end);
                yield return null;
            }

            actor.position = runTarget;
            start = runTarget;
        }

        SetAnimatorBoolIfExists(animator, "Jump", true);
        SetAnimatorBoolIfExists(animator, "Grounded", false);
        SetAnimatorBoolIfExists(animator, "FreeFall", false);
        SetAnimatorFloatIfExists(animator, "Speed", Mathf.Max(0.01f, scenario.runBlendSpeed));
        SetAnimatorFloatIfExists(animator, "MotionSpeed", 1.0f);
        CrossFadeIfStateExists(animator, string.IsNullOrWhiteSpace(scenario.actionStateName) ? "JumpStart" : scenario.actionStateName, 0.06f);

        float seconds = Mathf.Max(0.6f, scenario.actionSeconds);
        float height = Mathf.Max(0.5f, scenario.climbHeight);
        bool switchedToAirborne = false;
        bool switchedToLanding = false;
        for (float elapsed = 0f; elapsed < seconds; elapsed += Time.deltaTime)
        {
            float t = Mathf.Clamp01(elapsed / seconds);
            if (!switchedToAirborne)
            {
                SetAnimatorBoolIfExists(animator, "Jump", true);
                SetAnimatorBoolIfExists(animator, "Grounded", false);
                SetAnimatorBoolIfExists(animator, "FreeFall", false);
            }
            SetAnimatorFloatIfExists(animator, "Speed", Mathf.Max(0.01f, scenario.runBlendSpeed));
            SetAnimatorFloatIfExists(animator, "MotionSpeed", 1.0f);
            Vector3 position = Vector3.Lerp(start, end, t);
            position.y += Mathf.Sin(t * Mathf.PI) * height;
            actor.position = position;
            Face(actor, end);

            if (!switchedToAirborne && t >= 0.28f)
            {
                switchedToAirborne = true;
                SetAnimatorBoolIfExists(animator, "Jump", false);
                SetAnimatorBoolIfExists(animator, "FreeFall", true);
                SetAnimatorBoolIfExists(animator, "Grounded", false);
                CrossFadeIfStateExists(animator, string.IsNullOrWhiteSpace(scenario.airborneStateName) ? "InAir" : scenario.airborneStateName, 0.10f);
            }

            if (!switchedToLanding && t >= 0.82f)
            {
                switchedToLanding = true;
                SetAnimatorBoolIfExists(animator, "FreeFall", false);
                SetAnimatorBoolIfExists(animator, "Grounded", true);
                CrossFadeIfStateExists(animator, string.IsNullOrWhiteSpace(scenario.landingStateName) ? "JumpLand" : scenario.landingStateName, 0.08f);
            }

            yield return null;
        }

        actor.position = end;
        SetAnimatorBoolIfExists(animator, "Jump", false);
        SetAnimatorBoolIfExists(animator, "FreeFall", false);
        SetAnimatorBoolIfExists(animator, "Grounded", true);
        if (scenario.landingSeconds > 0f)
        {
            yield return new WaitForSeconds(scenario.landingSeconds);
        }
        SetAnimatorFloatIfExists(animator, "Speed", 0f);
        SetAnimatorFloatIfExists(animator, "MotionSpeed", 0f);
        CrossFadeIfStateExists(animator, scenario.idleStateName, 0.12f);
    }

    private IEnumerator DamageAction(Transform actor, ScenarioConfig scenario)
    {
        Animator animator = actor.GetComponentInChildren<Animator>(true);
        SecurityMechAttackAdapter mechAttack = actor.GetComponentInChildren<SecurityMechAttackAdapter>(true);
        mechAttack?.EndScenarioWalkClip();
        mechAttack?.SetScenarioMoveLockPose(actor.position, actor.rotation);
        StopMechLocomotion(animator);
        SetAnimatorBool(animator, scenario.actionBoolName, true);
        CrossFadeIfStateExists(animator, string.IsNullOrWhiteSpace(scenario.actionStateName) ? "Run_Aim" : scenario.actionStateName, 0.06f);
        mechAttack?.SetScenarioMoveLockPose(actor.position, actor.rotation);

        Transform target = scenario.targetProp;
        Vector3 propBasePosition = target != null ? target.position : Vector3.zero;
        Vector3 propBaseScale = target != null ? target.localScale : Vector3.one;
        GameObject pulse = showDamageTargetPulse && target != null ? EnsurePulseObject(target) : null;
        mechAttack?.BeginAttack(target);
        mechAttack?.SetScenarioMoveLockPose(actor.position, actor.rotation);

        float seconds = Mathf.Max(0.5f, scenario.actionSeconds);
        for (float elapsed = 0f; elapsed < seconds; elapsed += Time.deltaTime)
        {
            StopMechLocomotion(animator);
            mechAttack?.TickAttack(target);
            if (target != null)
            {
                Face(actor, target.position);
                mechAttack?.SetScenarioMoveLockPose(actor.position, actor.rotation);
                float hit = Mathf.Abs(Mathf.Sin(elapsed * 12f));
                target.position = propBasePosition + new Vector3(Mathf.Sin(elapsed * 30f) * 0.04f, 0f, Mathf.Cos(elapsed * 26f) * 0.04f) * hit;
                target.localScale = propBaseScale * (1f + hit * 0.035f);
            }
            else
            {
                mechAttack?.SetScenarioMoveLockPose(actor.position, actor.rotation);
            }

            mechAttack?.SetScenarioMoveLockPose(actor.position, actor.rotation);
            if (pulse != null) pulse.SetActive(Mathf.Sin(elapsed * 18f) > 0f);
            yield return null;
        }

        mechAttack?.SetScenarioMoveLockPose(actor.position, actor.rotation);
        if (target != null)
        {
            target.position = propBasePosition;
            target.localScale = propBaseScale;
        }
        if (pulse != null) pulse.SetActive(false);
        SetAnimatorBool(animator, scenario.actionBoolName, false);
        StopMechLocomotion(animator);
        mechAttack?.EndAttack();
        CrossFadeIfStateExists(animator, scenario.idleStateName, 0.12f);
        mechAttack?.SetScenarioMoveLockPose(actor.position, actor.rotation);
        mechAttack?.EndScenarioMoveLock();
    }

    private IEnumerator FilmingAction(Transform actor, ScenarioConfig scenario)
    {
        Animator animator = actor.GetComponentInChildren<Animator>(true);
        SetAnimatorBool(animator, scenario.moveBoolName, false);
        SetAnimatorBool(animator, scenario.actionBoolName, true);
        SetAnimatorBoolIfExists(animator, "Jump", false);
        SetAnimatorBoolIfExists(animator, "FreeFall", false);
        SetAnimatorBoolIfExists(animator, "Grounded", true);
        SetAnimatorFloatIfExists(animator, "Speed", 0f);
        SetAnimatorFloatIfExists(animator, "MotionSpeed", 0f);
        string filmingStateName = IsFilmingUnsafeActionState(scenario.actionStateName) || string.IsNullOrWhiteSpace(scenario.actionStateName)
            ? scenario.idleStateName
            : scenario.actionStateName;
        CrossFadeIfStateExists(animator, filmingStateName, 0.12f);

        Transform target = scenario.targetProp;
        LineRenderer beam = EnsureFilmingBeam(actor);
        float seconds = Mathf.Max(0.5f, scenario.actionSeconds);
        for (float elapsed = 0f; elapsed < seconds; elapsed += Time.deltaTime)
        {
            Vector3 targetPosition = target != null ? target.position : actor.position + actor.forward * 5f;
            Face(actor, targetPosition);
            actor.rotation *= Quaternion.Euler(0f, Mathf.Sin(elapsed * 5.0f) * 9f, Mathf.Sin(elapsed * 6.5f) * 3f);
            beam.enabled = true;
            beam.SetPosition(0, actor.position + Vector3.up * 1.4f);
            beam.SetPosition(1, targetPosition + Vector3.up * 1.0f);
            beam.startWidth = 0.035f + Mathf.Abs(Mathf.Sin(Time.time * 7f)) * 0.035f;
            beam.endWidth = 0.015f;
            yield return null;
        }

        beam.enabled = false;
        Vector3 finalTargetPosition = target != null ? target.position : actor.position + actor.forward * 5f;
        Face(actor, finalTargetPosition);
        SetAnimatorBool(animator, scenario.actionBoolName, false);
        SetAnimatorBoolIfExists(animator, "Jump", false);
        SetAnimatorBoolIfExists(animator, "FreeFall", false);
        SetAnimatorBoolIfExists(animator, "Grounded", true);
        SetAnimatorFloatIfExists(animator, "Speed", 0f);
        SetAnimatorFloatIfExists(animator, "MotionSpeed", 0f);
        CrossFadeIfStateExists(animator, scenario.idleStateName, 0.12f);
    }

    private GameObject ResolveActor(ScenarioConfig scenario)
    {
        if (scenario.actor != null) return scenario.actor;

        GameObject discovered = DiscoverSceneActor(scenario.action);
        if (discovered != null)
        {
            scenario.actor = discovered;
            return discovered;
        }

        GameObject prefab = scenario.fallbackPrefab != null ? scenario.fallbackPrefab :
            (scenario.action == SecurityScenarioAction.FenceDamage || scenario.action == SecurityScenarioAction.FacilityDamage ? attackRobotPrefab : intrusionRobotPrefab);
        GameObject actor = prefab != null ? Instantiate(prefab) : GameObject.CreatePrimitive(PrimitiveType.Capsule);
        actor.name = scenario.scenarioId + "_actor";
        if (actorRoot != null) actor.transform.SetParent(actorRoot, true);
        scenario.actor = actor;
        return actor;
    }

    private GameObject DiscoverSceneActor(SecurityScenarioAction action)
    {
        string[] hints = action == SecurityScenarioAction.FenceDamage || action == SecurityScenarioAction.FacilityDamage
            ? new[] { "AttackRobot", "Attack Robot", "Mech", "Robot_Soldier", "Robot Soldier", "Soldier" }
            : new[] { "Intrusion Robot", "RobotKyle", "Kyle" };

        Animator[] animators = FindObjectsOfType<Animator>(true);
        foreach (string hint in hints)
        {
            Animator match = animators.FirstOrDefault(item => item != null && item.gameObject.name.IndexOf(hint, StringComparison.OrdinalIgnoreCase) >= 0);
            if (match != null) return match.gameObject;
        }

        Transform[] transforms = FindObjectsOfType<Transform>(true);
        foreach (string hint in hints)
        {
            Transform match = transforms.FirstOrDefault(item => item != null && item.parent == null && item.name.IndexOf(hint, StringComparison.OrdinalIgnoreCase) >= 0);
            if (match != null) return match.gameObject;
        }

        return null;
    }

    private void ApplyAnimatorProfileFallback(ScenarioConfig scenario, GameObject actor)
    {
        if (scenario == null || actor == null) return;

        Animator animator = actor.GetComponentInChildren<Animator>(true);
        string actorName = actor.name;
        string controllerName = animator != null && animator.runtimeAnimatorController != null
            ? animator.runtimeAnimatorController.name
            : "";

        bool isRobotKyle = ContainsIgnoreCase(actorName, "RobotKyle") ||
            ContainsIgnoreCase(actorName, "Intrusion Robot") ||
            ContainsIgnoreCase(controllerName, "StarterAssetsThirdPerson");
        bool isMech = ContainsIgnoreCase(actorName, "Mech") ||
            ContainsIgnoreCase(actorName, "AttackRobot") ||
            ContainsIgnoreCase(controllerName, "Mech_");

        if (isRobotKyle && (scenario.action == SecurityScenarioAction.FenceClimb || scenario.action == SecurityScenarioAction.FacilityFilming))
        {
            if (IsBlankOrGenericHumanoidState(scenario.idleStateName)) scenario.idleStateName = "Idle Walk Run Blend";
            if (IsBlankOrGenericHumanoidState(scenario.moveStateName) || IsUnsafeLocomotionState(scenario.moveStateName))
            {
                scenario.moveStateName = "Idle Walk Run Blend";
            }
            if (scenario.action == SecurityScenarioAction.FenceClimb && string.IsNullOrWhiteSpace(scenario.actionStateName))
            {
                scenario.actionStateName = "JumpStart";
            }
        }

        if (isMech && (scenario.action == SecurityScenarioAction.FenceDamage || scenario.action == SecurityScenarioAction.FacilityDamage))
        {
            if (string.IsNullOrWhiteSpace(scenario.idleStateName) || scenario.idleStateName == "Idle Walk Run Blend") scenario.idleStateName = "DefaultToWalk";
            if (string.IsNullOrWhiteSpace(scenario.moveStateName) || scenario.moveStateName == "Idle Walk Run Blend") scenario.moveStateName = "Walk";
            if (string.IsNullOrWhiteSpace(scenario.actionStateName)) scenario.actionStateName = "ShootBigCanon_A";
        }
    }

    private void ApplyMechDemoControllerIfNeeded(ScenarioConfig scenario, GameObject actor)
    {
        if (!IsMechDamageScenario(scenario) || actor == null || mechDemoController == null) return;

        Animator animator = actor.GetComponentInChildren<Animator>(true);
        if (animator == null || animator.runtimeAnimatorController == mechDemoController) return;

        Vector3 position = actor.transform.position;
        Quaternion rotation = actor.transform.rotation;
        Vector3 localScale = actor.transform.localScale;
        animator.runtimeAnimatorController = mechDemoController;
        if (actor.activeInHierarchy && animator.isActiveAndEnabled)
        {
            animator.Rebind();
            animator.Update(0f);
        }

        actor.transform.position = position;
        actor.transform.rotation = rotation;
        actor.transform.localScale = localScale;
    }

    private static bool IsBlankOrGenericHumanoidState(string stateName)
    {
        return string.IsNullOrWhiteSpace(stateName) ||
            stateName == "Idle" ||
            stateName == "Walk" ||
            stateName == "Run";
    }

    private static bool IsFilmingUnsafeActionState(string stateName)
    {
        if (string.IsNullOrWhiteSpace(stateName)) return false;
        string normalized = stateName.Trim();
        return normalized.Equals("Run", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Jump", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("JumpStart", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("InAir", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Air", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Airborne", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("JumpLand", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Land", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Landing", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Fly", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsUnsafeLocomotionState(string stateName)
    {
        if (string.IsNullOrWhiteSpace(stateName)) return false;
        string normalized = stateName.Trim();
        return normalized.Equals("Jump", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("JumpStart", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("InAir", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Air", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Airborne", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("JumpLand", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Land", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Landing", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Fly", StringComparison.OrdinalIgnoreCase);
    }

    private static void NormalizeScenarioAnimationDefaults(ScenarioConfig scenario)
    {
        if (scenario == null) return;

        if (scenario.walkBlendSpeed <= 0f) scenario.walkBlendSpeed = 2f;
        if (scenario.runBlendSpeed <= 0f) scenario.runBlendSpeed = 6f;
        if (scenario.action == SecurityScenarioAction.FenceClimb)
        {
            if (scenario.runUpSeconds <= 0f) scenario.runUpSeconds = 0.45f;
            if (scenario.runUpDistance <= 0f) scenario.runUpDistance = 1.4f;
            if (scenario.landingSeconds <= 0f) scenario.landingSeconds = 0.35f;
            if (string.IsNullOrWhiteSpace(scenario.airborneStateName)) scenario.airborneStateName = "InAir";
            if (string.IsNullOrWhiteSpace(scenario.landingStateName)) scenario.landingStateName = "JumpLand";
        }
    }

    private static float ResolveLocomotionBlendSpeed(Animator animator, string stateName, float fallbackSpeed, ScenarioConfig scenario)
    {
        string normalized = string.IsNullOrWhiteSpace(stateName) ? "" : stateName.Trim();
        if (normalized.Equals("Idle", StringComparison.OrdinalIgnoreCase)) return 0f;
        if (normalized.Equals("Walk", StringComparison.OrdinalIgnoreCase)) return Mathf.Max(0.01f, scenario.walkBlendSpeed > 0f ? scenario.walkBlendSpeed : 2f);
        if (normalized.Equals("Run", StringComparison.OrdinalIgnoreCase)) return Mathf.Max(0.01f, scenario.runBlendSpeed > 0f ? scenario.runBlendSpeed : 6f);
        if (UsesRobotKyleBlendTree(animator)) return Mathf.Max(0.01f, fallbackSpeed > 0f ? fallbackSpeed : 2f);
        return Mathf.Max(0.01f, fallbackSpeed > 0f ? fallbackSpeed : 1f);
    }

    private static bool UsesRobotKyleBlendTree(Animator animator)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;
        string controllerName = animator.runtimeAnimatorController.name;
        if (!string.IsNullOrWhiteSpace(controllerName) &&
            controllerName.IndexOf("StarterAssetsThirdPerson", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        return animator.HasState(0, Animator.StringToHash("Idle Walk Run Blend"));
    }

    private static string ResolveSafeMoveState(Animator animator, ScenarioConfig scenario)
    {
        string moveStateName = scenario != null ? scenario.moveStateName : "";
        bool usesRobotKyleBlend = UsesRobotKyleBlendTree(animator);
        if (IsUnsafeLocomotionState(moveStateName)) return usesRobotKyleBlend ? "Idle Walk Run Blend" : "Walk";
        if (usesRobotKyleBlend && !AnimatorHasAnyState(animator, moveStateName)) return "Idle Walk Run Blend";
        return moveStateName;
    }

    private void ApplyMoveAnimatorState(Animator animator, float speed, float motionSpeed)
    {
        if (animator == null) return;
        if (animator.speed <= 0.001f) animator.speed = 1f;
        ResetJumpParameters(animator, true);
        SetAnimatorFloatIfExists(animator, "Speed", Mathf.Max(0.01f, speed));
        SetAnimatorFloatIfExists(animator, "MotionSpeed", Mathf.Max(0.01f, motionSpeed));
    }

    private void StopMechLocomotion(Animator animator)
    {
        if (animator == null) return;
        SetAnimatorBoolIfExists(animator, "StartRun", false);
        SetAnimatorBoolIfExists(animator, "Jump", false);
        SetAnimatorBoolIfExists(animator, "Grounded", true);
        SetAnimatorBoolIfExists(animator, "FreeFall", false);
        SetAnimatorFloatIfExists(animator, "Speed", 0f);
        SetAnimatorFloatIfExists(animator, "MotionSpeed", 0f);
    }

    private bool TryBeginDirectMechWalkClip(SecurityMechAttackAdapter mechAdapter, ScenarioConfig scenario)
    {
        if (!useDirectMechWalkClip || mechAdapter == null || !IsMechDamageScenario(scenario)) return false;

        AnimationClip clip = mechWalkInPlaceClip != null ? mechWalkInPlaceClip : mechRunInPlaceClip;
        if (clip == null) return false;

        mechAdapter.BeginScenarioMoveLock();
        if (mechAdapter.BeginScenarioWalkClip(clip, ResolveMechWalkClipPlaybackSpeed(scenario)))
        {
            return true;
        }

        mechAdapter.EndScenarioMoveLock();
        return false;
    }

    private static float ResolveMechWalkClipPlaybackSpeed(ScenarioConfig scenario)
    {
        float visualSpeed = scenario != null && scenario.walkBlendSpeed > 0f ? scenario.walkBlendSpeed : 2f;
        return Mathf.Clamp(visualSpeed / 2f, 0.35f, 1.8f);
    }

    private static bool IsMechDamageScenario(ScenarioConfig scenario)
    {
        return scenario != null &&
            (scenario.action == SecurityScenarioAction.FenceDamage || scenario.action == SecurityScenarioAction.FacilityDamage);
    }

    private RuntimeAnimatorController SwitchToMovementControllerIfNeeded(Animator animator, ScenarioConfig scenario, string moveStateName)
    {
        if (!switchMechControllerForMovement) return null;
        if (scenario == null || animator == null || animator.runtimeAnimatorController == null) return null;
        if (mechMovingController == null) return null;
        if (scenario.action != SecurityScenarioAction.FenceDamage && scenario.action != SecurityScenarioAction.FacilityDamage) return null;
        if (AnimatorHasAnyState(animator, moveStateName)) return null;

        RuntimeAnimatorController currentController = animator.runtimeAnimatorController;
        string controllerName = currentController != null ? currentController.name : "";
        if (!ContainsIgnoreCase(controllerName, "Mech_")) return null;

        animator.runtimeAnimatorController = GetMechMovementController();
        animator.Rebind();
        animator.Update(0f);
        return currentController;
    }

    private RuntimeAnimatorController GetMechMovementController()
    {
        if (mechMovingController == null) return null;
        if (!useMechInPlaceMovementClips) return mechMovingController;
        if (mechWalkInPlaceClip == null && mechRunInPlaceClip == null) return mechMovingController;
        if (mechMovementControllerInstance != null) return mechMovementControllerInstance;

        AnimatorOverrideController overrideController = new AnimatorOverrideController(mechMovingController);
        List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        overrideController.GetOverrides(overrides);
        for (int i = 0; i < overrides.Count; i++)
        {
            AnimationClip original = overrides[i].Key;
            if (original == null) continue;

            if (mechWalkInPlaceClip != null && original.name == "Walk")
            {
                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(original, mechWalkInPlaceClip);
            }
            else if (mechRunInPlaceClip != null && original.name == "Run")
            {
                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(original, mechRunInPlaceClip);
            }
        }

        overrideController.ApplyOverrides(overrides);
        mechMovementControllerInstance = overrideController;
        return mechMovementControllerInstance;
    }

    private static bool AnimatorHasAnyState(Animator animator, string stateName)
    {
        if (animator == null || animator.runtimeAnimatorController == null || string.IsNullOrWhiteSpace(stateName)) return false;
        foreach (string candidate in StateCandidates(stateName))
        {
            if (animator.HasState(0, Animator.StringToHash(candidate))) return true;
        }

        return false;
    }

    private static void RestoreAnimatorController(Animator animator, RuntimeAnimatorController restoreController, Transform preserveTransform)
    {
        if (animator == null || restoreController == null) return;
        if (animator.runtimeAnimatorController == restoreController) return;

        Vector3 position = preserveTransform != null ? preserveTransform.position : animator.transform.position;
        Quaternion rotation = preserveTransform != null ? preserveTransform.rotation : animator.transform.rotation;
        Vector3 localScale = preserveTransform != null ? preserveTransform.localScale : animator.transform.localScale;

        animator.runtimeAnimatorController = restoreController;
        animator.Rebind();
        animator.Update(0f);

        Transform targetTransform = preserveTransform != null ? preserveTransform : animator.transform;
        targetTransform.position = position;
        targetTransform.rotation = rotation;
        targetTransform.localScale = localScale;
    }

    private void ResetJumpParameters(Animator animator, bool grounded)
    {
        SetAnimatorBoolIfExists(animator, "Jump", false);
        SetAnimatorBoolIfExists(animator, "FreeFall", false);
        SetAnimatorBoolIfExists(animator, "Grounded", grounded);
    }

    private static bool ContainsIgnoreCase(string value, string token)
    {
        return !string.IsNullOrWhiteSpace(value) &&
            !string.IsNullOrWhiteSpace(token) &&
            value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private ScenarioConfig FindScenario(string scenarioId)
    {
        if (string.IsNullOrWhiteSpace(scenarioId)) return null;
        string normalized = NormalizeScenarioId(scenarioId);
        return scenarios.FirstOrDefault(item => item != null && NormalizeScenarioId(item.scenarioId) == normalized);
    }

    private void EnsureDefaultScenarios()
    {
        if (scenarios != null && scenarios.Length >= 4) return;

        scenarios = new[]
        {
            new ScenarioConfig
            {
                scenarioId = "fence_climb",
                incidentId = "INC-UNITY-FENCE-CLIMB",
                cameraId = "cam_01",
                zoneId = "fence_north",
                action = SecurityScenarioAction.FenceClimb,
                fallbackPrefab = intrusionRobotPrefab,
                moveStateName = "Idle Walk Run Blend",
                idleStateName = "Idle Walk Run Blend",
                actionStateName = "JumpStart",
                moveSeconds = 2.8f,
                actionSeconds = 1.7f,
                climbHeight = 3.2f,
            },
            new ScenarioConfig
            {
                scenarioId = "fence_damage",
                incidentId = "INC-UNITY-FENCE-DAMAGE",
                cameraId = "cam_02",
                zoneId = "fence_north",
                action = SecurityScenarioAction.FenceDamage,
                fallbackPrefab = attackRobotPrefab,
                idleStateName = "DefaultToWalk",
                moveStateName = "Walk",
                actionStateName = "ShootBigCanon_A",
                moveSeconds = 2.4f,
                actionSeconds = 2.8f,
            },
            new ScenarioConfig
            {
                scenarioId = "facility_filming",
                incidentId = "INC-UNITY-FACILITY-FILMING",
                cameraId = "cam_03",
                zoneId = "facility_internal",
                action = SecurityScenarioAction.FacilityFilming,
                fallbackPrefab = intrusionRobotPrefab,
                moveStateName = "Idle Walk Run Blend",
                idleStateName = "Idle Walk Run Blend",
                moveSeconds = 3.2f,
                actionSeconds = 4.0f,
            },
            new ScenarioConfig
            {
                scenarioId = "facility_damage",
                incidentId = "INC-UNITY-FACILITY-DAMAGE",
                cameraId = "cam_04",
                zoneId = "facility_internal",
                action = SecurityScenarioAction.FacilityDamage,
                fallbackPrefab = attackRobotPrefab,
                idleStateName = "DefaultToWalk",
                moveStateName = "Walk",
                actionStateName = "ShootBigCanon_A",
                moveSeconds = 2.6f,
                actionSeconds = 3.0f,
            },
        };
    }

    private void ResolveDefaultAnchors()
    {
        if (anchorRoot == null)
        {
            GameObject anchors = GameObject.Find("ScenarioAnchors") ?? new GameObject("ScenarioAnchors");
            anchorRoot = anchors.transform;
        }

        AssignAnchors("fence_climb", 0.12f, 0.20f, 0.20f, 0.20f, 0.30f, 0.20f, 2.6f);
        AssignAnchors("fence_damage", 0.18f, 0.24f, 0.26f, 0.24f, 0.26f, 0.24f, 0.4f);
        AssignAnchors("facility_filming", 0.55f, 0.55f, 0.64f, 0.58f, 0.64f, 0.58f, 0.8f);
        AssignAnchors("facility_damage", 0.62f, 0.50f, 0.70f, 0.54f, 0.70f, 0.54f, 0.8f);
    }

    private void AssignAnchors(string scenarioId, float startU, float startV, float actionU, float actionV, float exitU, float exitV, float targetYOffset)
    {
        ScenarioConfig scenario = FindScenario(scenarioId);
        if (scenario == null) return;

        scenario.startPoint = scenario.startPoint != null ? scenario.startPoint : EnsureAnchor(scenarioId + "_start", AnchorPosition(startU, startV, 0f));
        scenario.actionPoint = scenario.actionPoint != null ? scenario.actionPoint : EnsureAnchor(scenarioId + "_action", AnchorPosition(actionU, actionV, 0f));
        scenario.exitPoint = scenario.exitPoint != null ? scenario.exitPoint : EnsureAnchor(scenarioId + "_exit", AnchorPosition(exitU, exitV, 0f));

        if (scenario.action != SecurityScenarioAction.FenceClimb)
        {
            scenario.targetProp = scenario.targetProp != null ? scenario.targetProp : EnsureAnchor(scenarioId + "_target_prop", AnchorPosition(actionU, actionV, targetYOffset));
        }
    }

    private Transform EnsureAnchor(string anchorName, Vector3 position)
    {
        Transform existing = anchorRoot.Find(anchorName);
        if (existing != null) return existing;
        GameObject go = new GameObject(anchorName);
        go.transform.SetParent(anchorRoot, true);
        go.transform.position = position;
        return go.transform;
    }

    private Vector3 AnchorPosition(float u, float v, float yOffset)
    {
        Bounds bounds = sceneBounds.size.sqrMagnitude > 0.01f ? sceneBounds : new Bounds(Vector3.zero, new Vector3(120f, 20f, 80f));
        return new Vector3(Mathf.Lerp(bounds.min.x, bounds.max.x, u), bounds.min.y + yOffset, Mathf.Lerp(bounds.min.z, bounds.max.z, v));
    }

    private Vector3 ResolveActorGroundPosition(Vector3 position)
    {
        if (!snapScenarioActorsToGround) return position;

        Vector3 resolved = position;
        if (TrySampleTerrainHeight(position, out float terrainHeight))
        {
            resolved.y = terrainHeight + groundOffset;
            return resolved;
        }

        float up = Mathf.Max(1f, groundProbeHeight);
        float down = Mathf.Max(1f, groundProbeDepth);
        Vector3 origin = new Vector3(position.x, position.y + up, position.z);
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, up + down, scenarioGroundLayers, QueryTriggerInteraction.Ignore))
        {
            resolved.y = hit.point.y + groundOffset;
        }

        return resolved;
    }

    private static bool TrySampleTerrainHeight(Vector3 position, out float height)
    {
        Terrain[] terrains = Terrain.activeTerrains;
        foreach (Terrain terrain in terrains)
        {
            if (terrain == null || terrain.terrainData == null) continue;

            Vector3 terrainPosition = terrain.transform.position;
            Vector3 terrainSize = terrain.terrainData.size;
            bool insideX = position.x >= terrainPosition.x && position.x <= terrainPosition.x + terrainSize.x;
            bool insideZ = position.z >= terrainPosition.z && position.z <= terrainPosition.z + terrainSize.z;
            if (!insideX || !insideZ) continue;

            height = terrain.SampleHeight(position) + terrainPosition.y;
            return true;
        }

        height = position.y;
        return false;
    }

    private void Face(Transform actor, Vector3 target)
    {
        Vector3 direction = target - actor.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
        {
            actor.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }

    private void CrossFadeIfStateExists(Animator animator, string stateName, float transitionSeconds)
    {
        if (animator == null || string.IsNullOrWhiteSpace(stateName)) return;
        if (animator.runtimeAnimatorController == null) return;
        if (!animator.isActiveAndEnabled) return;

        foreach (string candidate in StateCandidates(stateName))
        {
            int stateHash = Animator.StringToHash(candidate);
            if (!animator.HasState(0, stateHash)) continue;
            animator.CrossFade(stateHash, transitionSeconds);
            return;
        }
    }

    private static string[] StateCandidates(string stateName)
    {
        if (string.IsNullOrWhiteSpace(stateName)) return Array.Empty<string>();

        string normalized = stateName.Trim();
        if (normalized == "Idle" || normalized == "Walk" || normalized == "Run")
        {
            return new[] { normalized, "Idle Walk Run Blend" };
        }

        if (normalized == "Jump") return new[] { normalized, "JumpStart" };
        if (normalized == "Airborne" || normalized == "Air") return new[] { normalized, "InAir" };
        if (normalized == "Land" || normalized == "Landing") return new[] { normalized, "JumpLand" };

        return new[] { normalized, "Base Layer." + normalized };
    }

    private void SetAnimatorBool(Animator animator, string parameterName, bool value)
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

    private void SetAnimatorBoolIfExists(Animator animator, string parameterName, bool value)
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

    private void SetAnimatorFloatIfExists(Animator animator, string parameterName, float value)
    {
        if (animator == null || string.IsNullOrWhiteSpace(parameterName)) return;
        if (animator.runtimeAnimatorController == null) return;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Float && parameter.name == parameterName)
            {
                animator.SetFloat(parameterName, value);
                return;
            }
        }
    }

    private LineRenderer EnsureFilmingBeam(Transform actor)
    {
        Transform existing = actor.Find("security_filming_beam");
        GameObject beamObject = existing != null ? existing.gameObject : null;
        if (beamObject != null && beamObject.GetComponent<LineRenderer>() == null)
        {
            Destroy(beamObject);
            beamObject = null;
        }

        if (beamObject == null)
        {
            beamObject = new GameObject("security_filming_beam");
            beamObject.transform.SetParent(actor, false);
        }

        LineRenderer line = beamObject.GetComponent<LineRenderer>();
        if (line == null) line = beamObject.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.material = filmingBeamMaterial != null ? filmingBeamMaterial : CreateRuntimeMaterial("Security Filming Beam", new Color(0.20f, 0.80f, 1f, 1f));
        filmingBeamMaterial = line.material;
        line.enabled = false;
        return line;
    }

    private GameObject EnsurePulseObject(Transform parent)
    {
        Transform existing = parent.Find("security_damage_pulse");
        if (existing != null) return existing.gameObject;

        GameObject pulse = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pulse.name = "security_damage_pulse";
        pulse.transform.SetParent(parent, false);
        pulse.transform.localPosition = Vector3.up * 0.8f;
        pulse.transform.localScale = Vector3.one * 0.35f;
        pulse.GetComponent<Renderer>().sharedMaterial = damagePulseMaterial != null ? damagePulseMaterial : CreateRuntimeMaterial("Security Damage Pulse", Color.red);
        damagePulseMaterial = pulse.GetComponent<Renderer>().sharedMaterial;
        pulse.SetActive(false);
        return pulse;
    }

    private Material CreateRuntimeMaterial(string materialName, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ??
            Shader.Find("Universal Render Pipeline/Lit") ??
            Shader.Find("Standard") ??
            Shader.Find("Sprites/Default");
        Material material = new Material(shader);
        material.name = materialName;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        else material.color = color;
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color);
        }
        return material;
    }

    private string NormalizeScenarioId(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        string normalized = value.Trim().ToLowerInvariant().Replace("-", "_").Replace(" ", "_");
        if (normalized.Contains("climb")) return "fence_climb";
        if (normalized.Contains("fence") && (normalized.Contains("damage") || normalized.Contains("break"))) return "fence_damage";
        if (normalized.Contains("film") || normalized.Contains("photo") || normalized.Contains("camera")) return "facility_filming";
        if (normalized.Contains("facility") && (normalized.Contains("damage") || normalized.Contains("break"))) return "facility_damage";
        return normalized;
    }

    private bool WasPressed(KeyCode keyCode)
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (keyCode == KeyCode.Alpha1) return Keyboard.current.digit1Key.wasPressedThisFrame;
            if (keyCode == KeyCode.Alpha2) return Keyboard.current.digit2Key.wasPressedThisFrame;
            if (keyCode == KeyCode.Alpha3) return Keyboard.current.digit3Key.wasPressedThisFrame;
            if (keyCode == KeyCode.Alpha4) return Keyboard.current.digit4Key.wasPressedThisFrame;
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
}
