using UnityEngine;

public sealed class BimZoneAnchor : MonoBehaviour
{
    public string zoneId = "zone_unassigned";
    public string displayName = "Zone";
    public string riskClass = "normal";
    public string sourceTypesSummary;
    public Bounds sourceBounds;

    [Header("Review Flags")]
    public bool requiresManualReview;
    public string reviewNote;

    private void OnDrawGizmos()
    {
        Color color = new Color(0.2f, 0.7f, 1f, 0.25f);
        if (riskClass == "high" || riskClass == "restricted" || riskClass == "hazard")
        {
            color = new Color(1f, 0.25f, 0.15f, 0.28f);
        }
        else if (riskClass == "medium" || riskClass == "review")
        {
            color = new Color(1f, 0.7f, 0.15f, 0.25f);
        }

        Gizmos.color = color;
        Vector3 size = sourceBounds.size;
        if (size == Vector3.zero)
        {
            size = new Vector3(4f, 0.2f, 4f);
        }

        Gizmos.DrawCube(transform.position, size);
        Gizmos.color = new Color(color.r, color.g, color.b, 0.9f);
        Gizmos.DrawWireCube(transform.position, size);
    }
}
