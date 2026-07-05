using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class SecurityCameraRig : MonoBehaviour
{
    public string cameraId = "cam_01";
    public string zoneId = "security_zone";
    public Transform lookTarget;
    public Transform viewPoint;

    [SerializeField] private bool enablePanMotion;
    [SerializeField] private float panDegrees = 24f;
    [SerializeField] private float panSeconds = 4.5f;
    [SerializeField] private float panSecondsMultiplier = 2f;
    [SerializeField] private float viewHeightOffset = 0.35f;
    [SerializeField] private float viewForwardOffset = 0.25f;
    [SerializeField] private bool createStatusMarker = true;
    [SerializeField] private float markerHeight = 18f;
    [SerializeField] private float markerScale = 10f;
    [SerializeField] private bool createMarkerRing = true;
    [SerializeField] private float markerRingRadiusMultiplier = 1.35f;
    [SerializeField] private float markerRingLineWidth = 0.08f;
    [SerializeField] private float markerRingPulseAmount = 0.18f;
    [SerializeField] private bool markerRingBillboardToCamera = true;
    [SerializeField] private Renderer markerRenderer;
    [SerializeField] private Transform markerTransform;
    [SerializeField] private Renderer[] statusRenderers = new Renderer[0];

    private SecurityShowcaseController controller;
    private Quaternion baseRotation;
    private Material normalMaterial;
    private Material detectedMaterial;
    private Material reviewedMaterial;
    private string currentStatus = "normal";
    private Vector3 baseScale;
    private Vector3 markerBaseScale = Vector3.one * 10f;
    private Transform markerRingTransform;
    private LineRenderer markerRingRenderer;
    private Material markerRingMaterial;
    private const int MarkerRingSegments = 80;

    private void Awake()
    {
        baseRotation = transform.rotation;
        baseScale = transform.localScale;
        EnsureRenderers();
        EnsureStatusMarker();
        ApplyMarkerMinimums();
        EnsureMarkerRing();
        EnsureClickTargets();
    }

    public void Configure(string nextCameraId, string nextZoneId, Transform nextLookTarget, Material normal, Material detected, Material reviewed)
    {
        cameraId = nextCameraId;
        zoneId = nextZoneId;
        lookTarget = nextLookTarget;
        normalMaterial = normal;
        detectedMaterial = detected;
        reviewedMaterial = reviewed;
        baseRotation = transform.rotation;
        baseScale = transform.localScale;
        EnsureRenderers();
        EnsureStatusMarker();
        ApplyMarkerMinimums();
        EnsureMarkerRing();
        EnsureClickTargets();
        SetStatus("normal");
    }

    public void BindController(SecurityShowcaseController owner, Material normal, Material detected, Material reviewed)
    {
        controller = owner;
        normalMaterial = normal;
        detectedMaterial = detected;
        reviewedMaterial = reviewed;
        EnsureRenderers();
        EnsureStatusMarker();
        ApplyMarkerMinimums();
        EnsureMarkerRing();
        EnsureClickTargets();
        SetStatus(currentStatus);
    }

    public Vector3 FocusPoint()
    {
        if (lookTarget != null) return lookTarget.position;
        return transform.position;
    }

    public Vector3 ViewPosition()
    {
        if (viewPoint != null) return viewPoint.position;

        if (TryGetVisualBounds(out Bounds bounds))
        {
            return bounds.center + transform.forward * viewForwardOffset + Vector3.up * viewHeightOffset;
        }

        return transform.position + Vector3.up * viewHeightOffset + transform.forward * viewForwardOffset;
    }

    public Quaternion ViewRotation()
    {
        if (lookTarget == null) return transform.rotation;

        Vector3 direction = lookTarget.position - ViewPosition();
        if (direction.sqrMagnitude < 0.001f) return transform.rotation;
        return Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    public void SetStatus(string status)
    {
        currentStatus = string.IsNullOrWhiteSpace(status) ? "normal" : status;
        if (baseScale == Vector3.zero) baseScale = transform.localScale;
        Material material = normalMaterial;
        if (currentStatus == "detected") material = detectedMaterial;
        else if (currentStatus == "reviewed") material = reviewedMaterial;

        foreach (Renderer item in statusRenderers)
        {
            if (item != null && material != null) item.sharedMaterial = material;
        }

        UpdateMarkerRing();
    }

    private void Update()
    {
        if (enablePanMotion)
        {
            float seconds = Mathf.Max(0.1f, panSeconds * Mathf.Max(0.1f, panSecondsMultiplier));
            float t = Mathf.Sin(Time.time / seconds * Mathf.PI * 2f);
            transform.rotation = baseRotation * Quaternion.Euler(0f, t * panDegrees, 0f);
        }

        if (currentStatus == "detected")
        {
            float pulse = 1f + Mathf.Sin(Time.time * 8f) * 0.16f;
            if (markerTransform != null) markerTransform.localScale = markerBaseScale * pulse;
            else transform.localScale = baseScale * pulse;
        }
        else
        {
            if (markerTransform != null) markerTransform.localScale = markerBaseScale;
            transform.localScale = baseScale;
        }

        UpdateMarkerRing();
    }

    private void EnsureRenderers()
    {
        if (statusRenderers == null || statusRenderers.Length == 0 || statusRenderers[0] == null)
        {
            EnsureStatusMarker();
            statusRenderers = markerRenderer != null ? new[] { markerRenderer } : GetComponentsInChildren<Renderer>(true);
        }
    }

    private void EnsureStatusMarker()
    {
        if (!createStatusMarker || markerRenderer != null) return;

        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = cameraId + "_status_marker";
        marker.transform.SetParent(transform, false);
        marker.transform.localPosition = Vector3.up * markerHeight;
        marker.transform.localScale = Vector3.one * markerScale;

        markerTransform = marker.transform;
        markerRenderer = marker.GetComponent<Renderer>();

        Collider markerCollider = marker.GetComponent<Collider>();
        if (markerCollider != null) markerCollider.isTrigger = true;
        SecurityCameraMarkerClickTarget clickTarget = marker.AddComponent<SecurityCameraMarkerClickTarget>();
        clickTarget.Configure(this);
    }

    private void EnsureMarkerRing()
    {
        if (!createMarkerRing || markerTransform == null) return;

        if (markerRingTransform == null)
        {
            Transform existing = markerTransform.Find(cameraId + "_status_ring");
            if (existing == null) existing = markerTransform.Find("status_ring");

            if (existing != null)
            {
                markerRingTransform = existing;
                markerRingRenderer = existing.GetComponent<LineRenderer>();
            }
        }

        if (markerRingTransform == null)
        {
            GameObject ring = new GameObject(cameraId + "_status_ring");
            ring.transform.SetParent(markerTransform, false);
            ring.transform.localPosition = Vector3.zero;
            ring.transform.localRotation = Quaternion.identity;
            ring.transform.localScale = Vector3.one;
            markerRingTransform = ring.transform;
            markerRingRenderer = ring.AddComponent<LineRenderer>();
        }

        if (markerRingRenderer == null) markerRingRenderer = markerRingTransform.GetComponent<LineRenderer>();
        if (markerRingRenderer == null) markerRingRenderer = markerRingTransform.gameObject.AddComponent<LineRenderer>();

        markerRingRenderer.useWorldSpace = false;
        markerRingRenderer.loop = true;
        markerRingRenderer.positionCount = MarkerRingSegments;
        markerRingRenderer.widthMultiplier = markerRingLineWidth;
        markerRingRenderer.alignment = LineAlignment.View;
        markerRingRenderer.numCornerVertices = 4;
        markerRingRenderer.numCapVertices = 4;
        markerRingRenderer.shadowCastingMode = ShadowCastingMode.Off;
        markerRingRenderer.receiveShadows = false;
        markerRingRenderer.sharedMaterial = markerRingMaterial != null ? markerRingMaterial : CreateRingMaterial();
        markerRingMaterial = markerRingRenderer.sharedMaterial;

        WriteRingPositions(1f);
        UpdateMarkerRing();
    }

    private void UpdateMarkerRing()
    {
        if (!createMarkerRing || markerRingRenderer == null || markerRingTransform == null) return;

        bool active = currentStatus == "detected" || currentStatus == "reviewed";
        markerRingRenderer.enabled = active;
        if (!active) return;

        float speed = currentStatus == "detected" ? 8f : 4f;
        float pulse01 = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f;
        float pulse = 1f + pulse01 * Mathf.Max(0f, markerRingPulseAmount);
        Color color = currentStatus == "detected"
            ? new Color(1f, 0.05f, 0.03f, 0.95f)
            : new Color(1f, 0.58f, 0.08f, 0.85f);

        markerRingRenderer.widthMultiplier = markerRingLineWidth * (currentStatus == "detected" ? 1.2f : 1f);
        markerRingRenderer.startColor = color;
        markerRingRenderer.endColor = color;
        if (markerRingMaterial != null)
        {
            SetMaterialColor(markerRingMaterial, color);
        }

        WriteRingPositions(pulse);

        if (markerRingBillboardToCamera && Camera.main != null)
        {
            Vector3 direction = markerRingTransform.position - Camera.main.transform.position;
            if (direction.sqrMagnitude > 0.001f)
            {
                markerRingTransform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }
        }
    }

    private void WriteRingPositions(float pulse)
    {
        if (markerRingRenderer == null) return;

        float radius = Mathf.Max(0.05f, 0.5f * Mathf.Max(1f, markerRingRadiusMultiplier) * pulse);
        for (int i = 0; i < MarkerRingSegments; i++)
        {
            float angle = (Mathf.PI * 2f * i) / MarkerRingSegments;
            markerRingRenderer.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
        }
    }

    private static Material CreateRingMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ??
            Shader.Find("Sprites/Default") ??
            Shader.Find("Unlit/Color") ??
            Shader.Find("Standard");
        Material material = new Material(shader);
        material.name = "Security Marker Ring";
        SetMaterialColor(material, Color.red);
        if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
        material.renderQueue = 3000;
        return material;
    }

    private static void SetMaterialColor(Material material, Color color)
    {
        if (material == null) return;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 0.75f);
        }
    }

    private void EnsureClickTargets()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        bool hasBodyCollider = colliders.Any(item => item != null && (markerTransform == null || !item.transform.IsChildOf(markerTransform)));
        if (!hasBodyCollider && TryGetVisualBounds(out Bounds bounds))
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = transform.InverseTransformPoint(bounds.center);
            Vector3 scale = transform.lossyScale;
            box.size = new Vector3(
                SafeSize(bounds.size.x, scale.x),
                SafeSize(bounds.size.y, scale.y),
                SafeSize(bounds.size.z, scale.z));
            colliders = new Collider[] { box };
        }

        foreach (Collider item in colliders)
        {
            if (item == null) continue;
            if (item.gameObject == gameObject) continue;
            SecurityCameraMarkerClickTarget clickTarget = item.GetComponent<SecurityCameraMarkerClickTarget>();
            if (clickTarget == null) clickTarget = item.gameObject.AddComponent<SecurityCameraMarkerClickTarget>();
            clickTarget.Configure(this);
        }
    }

    private static float SafeSize(float worldSize, float scale)
    {
        float denominator = Mathf.Abs(scale);
        return denominator > 0.0001f ? Mathf.Max(0.01f, worldSize / denominator) : Mathf.Max(0.01f, worldSize);
    }

    private void ApplyMarkerMinimums()
    {
        if (markerTransform == null) return;

        Vector3 localPosition = markerTransform.localPosition;
        localPosition.y = Mathf.Max(localPosition.y, markerHeight);
        markerTransform.localPosition = localPosition;

        float minScale = Mathf.Max(1f, markerScale);
        float currentMax = Mathf.Max(markerTransform.localScale.x, markerTransform.localScale.y, markerTransform.localScale.z);
        markerBaseScale = currentMax < minScale ? Vector3.one * minScale : markerTransform.localScale;
        markerTransform.localScale = markerBaseScale;
    }

    private bool TryGetVisualBounds(out Bounds bounds)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        bounds = new Bounds(transform.position, Vector3.zero);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null) continue;
            if (markerTransform != null && renderer.transform.IsChildOf(markerTransform)) continue;
            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private void OnMouseDown()
    {
        NotifySelected();
    }

    public void NotifySelected()
    {
        if (controller != null)
        {
            controller.OpenCameraView(cameraId);
            controller.HandleMapClick("camera_selected", "", cameraId, zoneId);
        }
    }
}

public sealed class SecurityCameraMarkerClickTarget : MonoBehaviour
{
    private SecurityCameraRig rig;

    public void Configure(SecurityCameraRig owner)
    {
        rig = owner;
    }

    private void OnMouseDown()
    {
        if (rig != null) rig.NotifySelected();
    }
}
