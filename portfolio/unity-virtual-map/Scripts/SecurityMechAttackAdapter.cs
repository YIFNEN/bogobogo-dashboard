using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public sealed class SecurityMechAttackAdapter : MonoBehaviour
{
    [Header("Animation")]
    public Animator animator;
    public string attackStateName = "ShootBigCanon_A";
    public string attackBoolName = "";
    public string attackTriggerName = "";
    public bool useAttackStateCycle = true;
    public string[] attackStateCycle =
    {
        "ShootBigCanon_A",
        "ShootBigCanon_B",
        "ShootSmallCanon_A",
        "ShootSmallCanon_B",
    };
    public float attackStateCycleSeconds = 0.72f;
    public bool flashBeamOnCycleState = true;
    public bool lockRootMotionDuringScenarioMove = true;
    public Transform visualRootToLock;
    public bool allowDirectScenarioWalkClip = true;

    [Header("Beam")]
    public Transform beamOrigin;
    public bool usePerBeamMuzzleOrigin = true;
    public LineRenderer[] bigCanonBeams = new LineRenderer[0];
    public LineRenderer[] smallCanonBeams = new LineRenderer[0];
    public LineRenderer[] bigCanonABeams = new LineRenderer[0];
    public LineRenderer[] bigCanonBBeams = new LineRenderer[0];
    public LineRenderer[] smallCanonABeams = new LineRenderer[0];
    public LineRenderer[] smallCanonBBeams = new LineRenderer[0];
    public bool continuousBeamWhileAttacking = true;
    public float eventFlashSeconds = 0.22f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip bigCanonClip;
    public AudioClip smallCanonClip;
    public AudioClip footstepClip;
    [Range(0f, 1f)]
    public float attackVolume = 0.65f;
    [Range(0f, 1f)]
    public float masterVolumeMultiplier = 0.35f;
    public bool enableFootstepAudio;
    public float footstepVolume = 0.35f;

    [Header("Imported Demo Script Control")]
    public bool disableImportedDemoScripts = true;

    private Transform activeTarget;
    private bool attacking;
    private int nextCycleIndex;
    private float nextCycleAt;
    private bool movementLockActive;
    private bool restoreApplyRootMotion;
    private bool hasLockedWorldPose;
    private Vector3 lockedWorldPosition;
    private Quaternion lockedWorldRotation;
    private Vector3 lockedVisualRootLocalPosition;
    private Quaternion lockedVisualRootLocalRotation;
    private Transform[] lockedLocalTransforms = new Transform[0];
    private Vector3[] lockedLocalPositions = new Vector3[0];
    private Quaternion[] lockedLocalRotations = new Quaternion[0];
    private PlayableGraph scenarioWalkGraph;
    private AnimationClipPlayable scenarioWalkPlayable;
    private AnimationClip scenarioWalkClip;
    private double scenarioWalkStartedAt;
    private float scenarioWalkPlaybackSpeed = 1f;
    private bool scenarioWalkActive;
    private string currentAttackStateName = "";

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = GetComponentInChildren<AudioSource>(true);
        if (beamOrigin == null) beamOrigin = FindBeamOrigin();
        if (visualRootToLock == null) visualRootToLock = FindVisualRoot();
        AutoCollectBeamsIfEmpty();
        DisableImportedDemoScriptsIfNeeded();
        ApplyAudioSourceVolume();
        DisableAllBeams();
    }

    private void LateUpdate()
    {
        UpdateScenarioWalkClip();
        ApplyMovementRootLock();
    }

    private void OnDisable()
    {
        EndScenarioWalkClip();
    }

    private void OnDestroy()
    {
        EndScenarioWalkClip();
    }

    public void BeginAttack(Transform target)
    {
        activeTarget = target;
        attacking = true;
        nextCycleIndex = 0;
        nextCycleAt = 0f;
        currentAttackStateName = "";
        AutoCollectBeamsIfEmpty();
        DisableImportedDemoScriptsIfNeeded();
        ApplyAudioSourceVolume();
        SetAnimatorParameter("StartRun", false);
        SetAnimatorParameter("Jump", false);
        SetAnimatorParameter(attackBoolName, true);
        SetAnimatorTrigger(attackTriggerName);
        if (useAttackStateCycle && attackStateCycle != null && attackStateCycle.Length > 0)
        {
            PlayCycleState(0);
        }
        else
        {
            CrossFadeIfStateExists(attackStateName, 0.06f);
        }
        TickAttack(target);
    }

    public void TickAttack(Transform target)
    {
        if (!attacking) return;
        if (target != null) activeTarget = target;
        SetAnimatorParameter("StartRun", false);
        SetAnimatorParameter("Jump", false);

        if (useAttackStateCycle && attackStateCycle != null && attackStateCycle.Length > 0 && Time.time >= nextCycleAt)
        {
            PlayCycleState(nextCycleIndex);
        }
        else if (useAttackStateCycle)
        {
            KeepAttackStateActive();
        }

        if (!continuousBeamWhileAttacking || useAttackStateCycle || activeTarget == null) return;

        UpdateBeamGroup(bigCanonBeams, activeTarget, true);
    }

    public void EndAttack()
    {
        attacking = false;
        SetAnimatorParameter(attackBoolName, false);
        nextCycleIndex = 0;
        nextCycleAt = 0f;
        currentAttackStateName = "";
        DisableAllBeams();
    }

    public void ShootBigCanonA()
    {
        FlashBeamGroup(SelectBeamGroup(bigCanonABeams, bigCanonBeams), bigCanonClip);
    }

    public void ShootBigCanonB()
    {
        FlashBeamGroup(SelectBeamGroup(bigCanonBBeams, bigCanonBeams), bigCanonClip);
    }

    public void ShootSmallCanonA()
    {
        FlashBeamGroup(SelectBeamGroup(smallCanonABeams, smallCanonBeams), smallCanonClip);
    }

    public void ShootSmallCanonB()
    {
        FlashBeamGroup(SelectBeamGroup(smallCanonBBeams, smallCanonBeams), smallCanonClip);
    }

    public void FootStep()
    {
        if (!enableFootstepAudio || audioSource == null || footstepClip == null) return;
        audioSource.PlayOneShot(footstepClip, Mathf.Clamp01(footstepVolume));
    }

    public void EndOfWalk()
    {
        // Imported demo event; movement is driven by SecurityScenarioController.
    }

    public void EndOfRun()
    {
        // Imported demo event; movement is driven by SecurityScenarioController.
    }

    public void EndOfRunJump()
    {
        // Imported demo event; movement is driven by SecurityScenarioController.
    }

    public void BeginScenarioMoveLock()
    {
        if (!lockRootMotionDuringScenarioMove) return;
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        if (visualRootToLock == null) visualRootToLock = FindVisualRoot();

        restoreApplyRootMotion = animator != null && animator.applyRootMotion;
        if (animator != null) animator.applyRootMotion = false;

        hasLockedWorldPose = false;
        lockedWorldPosition = transform.position;
        lockedWorldRotation = transform.rotation;
        if (visualRootToLock != null)
        {
            lockedVisualRootLocalPosition = visualRootToLock.localPosition;
            lockedVisualRootLocalRotation = visualRootToLock.localRotation;
        }
        CaptureRootMotionLockTransforms();

        movementLockActive = true;
    }

    public void SetScenarioMoveLockPose(Vector3 worldPosition, Quaternion worldRotation)
    {
        if (!movementLockActive) return;
        hasLockedWorldPose = true;
        lockedWorldPosition = worldPosition;
        lockedWorldRotation = worldRotation;
    }

    public bool BeginScenarioWalkClip(AnimationClip clip, float playbackSpeed)
    {
        if (!allowDirectScenarioWalkClip || clip == null) return false;
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        if (animator == null) return false;

        EndScenarioWalkClip();
        scenarioWalkClip = clip;
        scenarioWalkPlaybackSpeed = Mathf.Clamp(playbackSpeed, 0.1f, 3f);
        scenarioWalkStartedAt = Time.timeAsDouble;
        scenarioWalkGraph = PlayableGraph.Create(gameObject.name + "_ScenarioWalk");
        scenarioWalkGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        scenarioWalkPlayable = AnimationClipPlayable.Create(scenarioWalkGraph, scenarioWalkClip);
        scenarioWalkPlayable.SetApplyFootIK(false);
        scenarioWalkPlayable.SetApplyPlayableIK(false);
        scenarioWalkPlayable.SetSpeed(0f);

        AnimationPlayableOutput output = AnimationPlayableOutput.Create(scenarioWalkGraph, "ScenarioWalk", animator);
        output.SetSourcePlayable(scenarioWalkPlayable);
        scenarioWalkGraph.Play();
        scenarioWalkActive = true;
        UpdateScenarioWalkClip();
        return true;
    }

    public void EndScenarioWalkClip()
    {
        scenarioWalkActive = false;
        scenarioWalkClip = null;
        if (scenarioWalkGraph.IsValid())
        {
            scenarioWalkGraph.Destroy();
        }
    }

    public void EndScenarioMoveLock()
    {
        if (!movementLockActive) return;
        EndScenarioWalkClip();
        ApplyMovementRootLock();
        movementLockActive = false;
        hasLockedWorldPose = false;
        if (animator != null) animator.applyRootMotion = restoreApplyRootMotion;
    }

    private void PlayCycleState(int index)
    {
        if (attackStateCycle == null || attackStateCycle.Length == 0) return;

        int clamped = Mathf.Abs(index) % attackStateCycle.Length;
        string stateName = attackStateCycle[clamped];
        currentAttackStateName = stateName;
        CrossFadeIfStateExists(stateName, 0.06f);
        if (flashBeamOnCycleState) FlashForState(stateName);

        nextCycleIndex = (clamped + 1) % attackStateCycle.Length;
        nextCycleAt = Time.time + Mathf.Clamp(attackStateCycleSeconds, 0.12f, 0.42f);
    }

    private void KeepAttackStateActive()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        if (string.IsNullOrWhiteSpace(currentAttackStateName))
        {
            PlayCycleState(nextCycleIndex);
            return;
        }

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        foreach (string candidate in StateCandidates(currentAttackStateName))
        {
            if (state.shortNameHash == Animator.StringToHash(candidate) ||
                state.fullPathHash == Animator.StringToHash(candidate))
            {
                return;
            }
        }

        CrossFadeIfStateExists(currentAttackStateName, 0.02f);
    }

    private void FlashForState(string stateName)
    {
        string normalized = string.IsNullOrWhiteSpace(stateName) ? "" : stateName.ToLowerInvariant();
        if (normalized.Contains("bigcanon_a") || normalized.Contains("bigcanon01"))
        {
            ShootBigCanonA();
        }
        else if (normalized.Contains("bigcanon_b") || normalized.Contains("bigcanon02"))
        {
            ShootBigCanonB();
        }
        else if (normalized.Contains("smallcanon_a") || normalized.Contains("smallcanon01"))
        {
            ShootSmallCanonA();
        }
        else if (normalized.Contains("smallcanon_b") || normalized.Contains("smallcanon02"))
        {
            ShootSmallCanonB();
        }
    }

    private Transform FindBeamOrigin()
    {
        Transform body = transform.Find("Mech/Root/Pelvis/Body");
        if (body != null) return body;

        Animator foundAnimator = animator != null ? animator : GetComponentInChildren<Animator>(true);
        if (foundAnimator != null) return foundAnimator.transform;
        return transform;
    }

    private void ApplyMovementRootLock()
    {
        if (!movementLockActive) return;
        if (hasLockedWorldPose)
        {
            transform.position = lockedWorldPosition;
            transform.rotation = lockedWorldRotation;
        }

        if (visualRootToLock != null)
        {
            visualRootToLock.localPosition = lockedVisualRootLocalPosition;
            visualRootToLock.localRotation = lockedVisualRootLocalRotation;
        }

        for (int i = 0; i < lockedLocalTransforms.Length; i++)
        {
            Transform item = lockedLocalTransforms[i];
            if (item == null) continue;
            item.localPosition = lockedLocalPositions[i];
            item.localRotation = lockedLocalRotations[i];
        }
    }

    private void UpdateScenarioWalkClip()
    {
        if (!scenarioWalkActive || scenarioWalkClip == null || !scenarioWalkGraph.IsValid() || !scenarioWalkPlayable.IsValid()) return;

        double clipLength = Mathf.Max(0.01f, scenarioWalkClip.length);
        double elapsed = (Time.timeAsDouble - scenarioWalkStartedAt) * scenarioWalkPlaybackSpeed;
        scenarioWalkPlayable.SetTime(elapsed % clipLength);
        scenarioWalkGraph.Evaluate(0f);
    }

    private Transform FindVisualRoot()
    {
        string[] candidates =
        {
            "mech/Mech/Root",
            "mech/Mech",
            "mech",
            "Mech/Root",
            "Mech",
            "Root",
        };

        foreach (string candidate in candidates)
        {
            Transform match = transform.Find(candidate);
            if (match != null) return match;
        }

        Animator foundAnimator = animator != null ? animator : GetComponentInChildren<Animator>(true);
        return foundAnimator != null ? foundAnimator.transform : transform;
    }

    private void CaptureRootMotionLockTransforms()
    {
        List<Transform> transforms = new List<Transform>();
        AddUniqueTransform(transforms, transform.Find("mech"));
        AddUniqueTransform(transforms, transform.Find("Mech"));
        AddUniqueTransform(transforms, transform.Find("Mech/Root"));
        AddUniqueTransform(transforms, transform.Find("mech/Mech"));
        AddUniqueTransform(transforms, transform.Find("mech/Mech/Root"));
        AddUniqueTransform(transforms, visualRootToLock);

        lockedLocalTransforms = transforms.ToArray();
        lockedLocalPositions = new Vector3[lockedLocalTransforms.Length];
        lockedLocalRotations = new Quaternion[lockedLocalTransforms.Length];
        for (int i = 0; i < lockedLocalTransforms.Length; i++)
        {
            Transform item = lockedLocalTransforms[i];
            lockedLocalPositions[i] = item != null ? item.localPosition : Vector3.zero;
            lockedLocalRotations[i] = item != null ? item.localRotation : Quaternion.identity;
        }
    }

    private static void AddUniqueTransform(List<Transform> transforms, Transform item)
    {
        if (item == null || transforms.Contains(item)) return;
        transforms.Add(item);
    }

    private void AutoCollectBeamsIfEmpty()
    {
        MechShoot sourceShoot = GetComponentInChildren<MechShoot>(true);
        if (sourceShoot != null)
        {
            if (bigCanonABeams == null || bigCanonABeams.Length == 0)
            {
                bigCanonABeams = Compact(sourceShoot.BigCanon01L, sourceShoot.BigCanon01R);
            }

            if (bigCanonBBeams == null || bigCanonBBeams.Length == 0)
            {
                bigCanonBBeams = Compact(sourceShoot.BigCanon02L, sourceShoot.BigCanon02R);
            }

            if (smallCanonABeams == null || smallCanonABeams.Length == 0)
            {
                smallCanonABeams = Compact(sourceShoot.SmallCanon01L, sourceShoot.SmallCanon01R);
            }

            if (smallCanonBBeams == null || smallCanonBBeams.Length == 0)
            {
                smallCanonBBeams = Compact(sourceShoot.SmallCanon02L, sourceShoot.SmallCanon02R);
            }

            if (bigCanonClip == null) bigCanonClip = sourceShoot.audioBigCanon;
            if (smallCanonClip == null) smallCanonClip = sourceShoot.audioSmallCanon;
        }

        LineRenderer[] renderers = GetComponentsInChildren<LineRenderer>(true);
        if ((bigCanonABeams == null || bigCanonABeams.Length == 0) && renderers.Length > 0)
        {
            bigCanonABeams = System.Array.FindAll(renderers, item => NameContains(item, "bigcanon01"));
        }

        if ((bigCanonBBeams == null || bigCanonBBeams.Length == 0) && renderers.Length > 0)
        {
            bigCanonBBeams = System.Array.FindAll(renderers, item => NameContains(item, "bigcanon02"));
        }

        if ((smallCanonABeams == null || smallCanonABeams.Length == 0) && renderers.Length > 0)
        {
            smallCanonABeams = System.Array.FindAll(renderers, item => NameContains(item, "smallcanon01"));
        }

        if ((smallCanonBBeams == null || smallCanonBBeams.Length == 0) && renderers.Length > 0)
        {
            smallCanonBBeams = System.Array.FindAll(renderers, item => NameContains(item, "smallcanon02"));
        }

        if ((bigCanonBeams == null || bigCanonBeams.Length == 0) && renderers.Length > 0)
        {
            bigCanonBeams = System.Array.FindAll(renderers, item => NameContains(item, "bigcanon"));
        }

        if ((smallCanonBeams == null || smallCanonBeams.Length == 0) && renderers.Length > 0)
        {
            smallCanonBeams = System.Array.FindAll(renderers, item => NameContains(item, "smallcanon"));
        }
    }

    private void FlashBeamGroup(LineRenderer[] beams, AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            ApplyAudioSourceVolume();
            audioSource.PlayOneShot(clip, Mathf.Clamp01(attackVolume * masterVolumeMultiplier));
        }

        if (beams == null || beams.Length == 0) return;
        UpdateBeamGroup(beams, activeTarget, true);
        StartCoroutine(FadeBeamGroup(beams, eventFlashSeconds));
    }

    private void ApplyAudioSourceVolume()
    {
        if (audioSource == null) return;
        audioSource.volume = Mathf.Clamp01(masterVolumeMultiplier);
    }

    private void DisableImportedDemoScriptsIfNeeded()
    {
        if (!disableImportedDemoScripts) return;

        foreach (MechShoot script in GetComponentsInChildren<MechShoot>(true))
        {
            RemoveImportedDemoScript(script);
        }

        foreach (MechWalk script in GetComponentsInChildren<MechWalk>(true))
        {
            RemoveImportedDemoScript(script);
        }

        foreach (MechHit script in GetComponentsInChildren<MechHit>(true))
        {
            RemoveImportedDemoScript(script);
        }

        foreach (FootSteps script in GetComponentsInChildren<FootSteps>(true))
        {
            RemoveImportedDemoScript(script);
        }
    }

    private static void RemoveImportedDemoScript(MonoBehaviour script)
    {
        if (script == null) return;
        script.enabled = false;
        if (Application.isPlaying)
        {
            Destroy(script);
        }
#if UNITY_EDITOR
        else
        {
            DestroyImmediate(script);
        }
#endif
    }

    private IEnumerator FadeBeamGroup(LineRenderer[] beams, float seconds)
    {
        float duration = Mathf.Max(0.05f, seconds);
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            float alpha = 1f - Mathf.Clamp01(elapsed / duration);
            SetBeamAlpha(beams, alpha);
            yield return null;
        }

        UpdateBeamGroup(beams, Vector3.zero, Vector3.zero, false);
    }

    private void UpdateBeamGroup(LineRenderer[] beams, Vector3 start, Vector3 end, bool enabled)
    {
        if (beams == null) return;
        foreach (LineRenderer beam in beams)
        {
            if (beam == null) continue;
            UpdateBeam(beam, start, end, enabled);
        }
    }

    private void UpdateBeamGroup(LineRenderer[] beams, Transform target, bool enabled)
    {
        if (beams == null) return;
        foreach (LineRenderer beam in beams)
        {
            if (beam == null) continue;
            if (!enabled)
            {
                UpdateBeam(beam, Vector3.zero, Vector3.zero, false);
                continue;
            }

            Vector3 start = BeamStart(beam);
            Vector3 end = target != null
                ? target.position + Vector3.up * 0.8f
                : start + BeamDirection(beam) * 8f;
            UpdateBeam(beam, start, end, true);
        }
    }

    private void UpdateBeam(LineRenderer beam, Vector3 start, Vector3 end, bool enabled)
    {
        if (beam == null) return;
        beam.enabled = enabled;
        if (!enabled) return;

        beam.useWorldSpace = true;
        beam.positionCount = 2;
        beam.SetPosition(0, start);
        beam.SetPosition(1, end);
        if (beam.startWidth <= 0.001f) beam.startWidth = 0.05f;
        if (beam.endWidth <= 0.001f) beam.endWidth = 0.015f;
        SetBeamAlpha(beam, 1f);
    }

    private void DisableAllBeams()
    {
        UpdateBeamGroup(bigCanonBeams, Vector3.zero, Vector3.zero, false);
        UpdateBeamGroup(smallCanonBeams, Vector3.zero, Vector3.zero, false);
        UpdateBeamGroup(bigCanonABeams, Vector3.zero, Vector3.zero, false);
        UpdateBeamGroup(bigCanonBBeams, Vector3.zero, Vector3.zero, false);
        UpdateBeamGroup(smallCanonABeams, Vector3.zero, Vector3.zero, false);
        UpdateBeamGroup(smallCanonBBeams, Vector3.zero, Vector3.zero, false);
    }

    private Vector3 BeamStart()
    {
        return (beamOrigin != null ? beamOrigin.position : transform.position) + Vector3.up * 0.4f;
    }

    private Vector3 BeamStart(LineRenderer beam)
    {
        if (!usePerBeamMuzzleOrigin || beam == null) return BeamStart();

        if (!beam.useWorldSpace && beam.positionCount > 0)
        {
            return beam.transform.TransformPoint(beam.GetPosition(0));
        }

        return beam.transform.position;
    }

    private Vector3 BeamDirection(LineRenderer beam)
    {
        if (usePerBeamMuzzleOrigin && beam != null)
        {
            if (!beam.useWorldSpace && beam.positionCount > 1)
            {
                Vector3 start = beam.transform.TransformPoint(beam.GetPosition(0));
                Vector3 end = beam.transform.TransformPoint(beam.GetPosition(1));
                Vector3 localDirection = end - start;
                if (localDirection.sqrMagnitude > 0.0001f) return localDirection.normalized;
            }

            if (beam.transform.forward.sqrMagnitude > 0.0001f) return beam.transform.forward;
        }

        if (transform.forward.sqrMagnitude > 0.0001f) return transform.forward;
        return Vector3.forward;
    }

    private void SetBeamAlpha(LineRenderer[] beams, float alpha)
    {
        if (beams == null) return;
        foreach (LineRenderer beam in beams) SetBeamAlpha(beam, alpha);
    }

    private void SetBeamAlpha(LineRenderer beam, float alpha)
    {
        if (beam == null || beam.material == null) return;
        Color color = Color.white;
        if (beam.material.HasProperty("_TintColor")) color = beam.material.GetColor("_TintColor");
        else if (beam.material.HasProperty("_BaseColor")) color = beam.material.GetColor("_BaseColor");
        else if (beam.material.HasProperty("_Color")) color = beam.material.GetColor("_Color");

        color.a = alpha;
        if (beam.material.HasProperty("_TintColor")) beam.material.SetColor("_TintColor", color);
        if (beam.material.HasProperty("_BaseColor")) beam.material.SetColor("_BaseColor", color);
        if (beam.material.HasProperty("_Color")) beam.material.SetColor("_Color", color);
    }

    private void CrossFadeIfStateExists(string stateName, float transitionSeconds)
    {
        if (animator == null || animator.runtimeAnimatorController == null || string.IsNullOrWhiteSpace(stateName)) return;
        foreach (string candidate in StateCandidates(stateName))
        {
            int hash = Animator.StringToHash(candidate);
            if (!animator.HasState(0, hash)) continue;
            animator.CrossFade(hash, transitionSeconds);
            return;
        }
    }

    private static string[] StateCandidates(string stateName)
    {
        if (string.IsNullOrWhiteSpace(stateName)) return new string[0];
        return stateName.Contains(".")
            ? new[] { stateName, "Base Layer." + stateName }
            : new[] { stateName, "Base Layer." + stateName, "ShootBigCanon." + stateName, "Base Layer.ShootBigCanon." + stateName };
    }

    private void SetAnimatorParameter(string parameterName, bool value)
    {
        if (animator == null || animator.runtimeAnimatorController == null || string.IsNullOrWhiteSpace(parameterName)) return;
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName && parameter.type == AnimatorControllerParameterType.Bool)
            {
                animator.SetBool(parameterName, value);
                return;
            }
        }
    }

    private void SetAnimatorTrigger(string parameterName)
    {
        if (animator == null || animator.runtimeAnimatorController == null || string.IsNullOrWhiteSpace(parameterName)) return;
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName && parameter.type == AnimatorControllerParameterType.Trigger)
            {
                animator.SetTrigger(parameterName);
                return;
            }
        }
    }

    private static bool NameContains(LineRenderer item, string token)
    {
        return item != null && item.name.ToLowerInvariant().Contains(token);
    }

    private static LineRenderer[] SelectBeamGroup(LineRenderer[] preferred, LineRenderer[] fallback)
    {
        return preferred != null && preferred.Length > 0 ? preferred : fallback;
    }

    private static LineRenderer[] Compact(params LineRenderer[] items)
    {
        int count = 0;
        if (items != null)
        {
            foreach (LineRenderer item in items)
            {
                if (item != null) count++;
            }
        }

        if (count == 0) return new LineRenderer[0];
        LineRenderer[] result = new LineRenderer[count];
        int index = 0;
        if (items == null) return result;
        foreach (LineRenderer item in items)
        {
            if (item != null) result[index++] = item;
        }

        return result;
    }
}
