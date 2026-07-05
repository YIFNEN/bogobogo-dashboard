using System.Linq;
using UnityEngine;

public sealed class SecurityDroneRig : MonoBehaviour
{
    public string droneId = "drone_01";
    public string zoneId = "security_zone";
    public Transform orbitCenter;
    public Transform[] waypoints = new Transform[0];

    [SerializeField] private float orbitRadius = 8f;
    [SerializeField] private float orbitRadiusMultiplier = 1f;
    [SerializeField] private float orbitSeconds = 36f;
    [SerializeField] private float bobHeight = 0.55f;
    [SerializeField] private float waypointSpeed = 2f;
    [SerializeField] private float waypointArriveDistance = 0.4f;
    [SerializeField] private Renderer[] pulseRenderers = new Renderer[0];
    [SerializeField] private Transform[] rotorRoots = new Transform[0];
    [SerializeField] private bool enableProceduralRotorSpin;
    [SerializeField] private float rotorSpinDegreesPerSecond;
    [SerializeField] private Transform visualRoot;

    private Vector3 basePosition;
    private float phase;
    private int waypointIndex;
    private MaterialPropertyBlock propertyBlock;
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        basePosition = transform.position;
        phase = Random.value * Mathf.PI * 2f;
        if (visualRoot == null) visualRoot = transform;
        if (pulseRenderers == null || pulseRenderers.Length == 0) pulseRenderers = GetComponentsInChildren<Renderer>(true);
        if (rotorRoots == null || rotorRoots.Length == 0) rotorRoots = FindRotorRoots();
        propertyBlock = new MaterialPropertyBlock();
    }

    public void Configure(string nextDroneId, string nextZoneId, Transform center, float radius)
    {
        droneId = nextDroneId;
        zoneId = nextZoneId;
        orbitCenter = center;
        orbitRadius = Mathf.Max(1f, radius);
        basePosition = transform.position;
    }

    private void Update()
    {
        if (waypoints != null && waypoints.Length > 0) PatrolWaypoints();
        else OrbitCenter();

        SpinRotors();
        PulseEmission();
    }

    private void OrbitCenter()
    {
        Vector3 center = orbitCenter != null ? orbitCenter.position : basePosition;
        float angle = phase + Time.time / Mathf.Max(0.1f, orbitSeconds) * Mathf.PI * 2f;
        float effectiveRadius = orbitRadius * Mathf.Max(1f, orbitRadiusMultiplier);
        Vector3 offset = new Vector3(Mathf.Cos(angle) * effectiveRadius, Mathf.Sin(Time.time * 2.2f + phase) * bobHeight, Mathf.Sin(angle) * effectiveRadius);
        transform.position = center + offset;
        transform.rotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg + 90f, 0f);
    }

    private void PatrolWaypoints()
    {
        Transform target = waypoints[Mathf.Clamp(waypointIndex, 0, waypoints.Length - 1)];
        if (target == null)
        {
            waypointIndex = (waypointIndex + 1) % waypoints.Length;
            return;
        }

        Vector3 targetPosition = target.position + Vector3.up * Mathf.Sin(Time.time * 2.2f + phase) * bobHeight;
        Vector3 direction = targetPosition - transform.position;
        float step = waypointSpeed * Time.deltaTime;
        if (direction.magnitude <= Mathf.Max(step, waypointArriveDistance))
        {
            waypointIndex = (waypointIndex + 1) % waypoints.Length;
        }
        else
        {
            transform.position += direction.normalized * step;
            Vector3 flatDirection = direction;
            flatDirection.y = 0f;
            if (flatDirection.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(flatDirection.normalized, Vector3.up);
        }
    }

    private void SpinRotors()
    {
        if (!enableProceduralRotorSpin) return;
        if (rotorRoots == null) return;
        float amount = rotorSpinDegreesPerSecond * Time.deltaTime;
        foreach (Transform rotor in rotorRoots)
        {
            if (rotor != null) rotor.Rotate(Vector3.up, amount, Space.Self);
        }
    }

    private void PulseEmission()
    {
        if (pulseRenderers == null || propertyBlock == null) return;
        Color color = Color.cyan * (0.35f + Mathf.Abs(Mathf.Sin(Time.time * 3.5f + phase)) * 0.8f);
        foreach (Renderer renderer in pulseRenderers)
        {
            if (renderer == null) continue;
            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(EmissionColorId, color);
            renderer.SetPropertyBlock(propertyBlock);
        }
    }

    private Transform[] FindRotorRoots()
    {
        return GetComponentsInChildren<Transform>(true)
            .Where(item => item != transform && item.name.ToLowerInvariant().Contains("fan"))
            .ToArray();
    }
}
