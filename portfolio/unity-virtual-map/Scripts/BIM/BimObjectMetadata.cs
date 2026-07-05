using UnityEngine;

public sealed class BimObjectMetadata : MonoBehaviour
{
    [Header("Source Identity")]
    public string sourceGlobalId;
    public string sourceName;
    public string sourceType;

    [Header("Virtual Map Mapping")]
    public string objectId;
    public string zoneId;
    public string floorName;
    public string riskClass;

    [TextArea(2, 4)]
    public string notes;
}
