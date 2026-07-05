using System;

[Serializable]
public sealed class UnityDashboardState
{
    public string schema_version;
    public string site_id;
    public string timestamp;
    public UnityIncidentState[] incidents;
    public UnityCameraState[] cameras;
    public UnityRejectedCandidateState[] rejected_candidates;
    public string selected_incident_id;
}

[Serializable]
public sealed class UnityIncidentState
{
    public string incident_id;
    public string event_type;
    public string camera_id;
    public string source_camera_id;
    public string zone_id;
    public string status;
    public float severity;
    public string detected_at;
    public string source_type;
}

[Serializable]
public sealed class UnityCameraState
{
    public string camera_id;
    public string source_camera_id;
    public string zone_id;
    public string status;
}

[Serializable]
public sealed class UnityRejectedCandidateState
{
    public string candidate_id;
    public string camera_id;
    public string source_camera_id;
    public string zone_id;
    public string rejected_reason;
}

[Serializable]
public sealed class BackendSimulatorWebSocketEvent
{
    public string type;
    public string schema_version;
    public int seq;
    public BackendSimulatorWebSocketData data;

    // Backward-compatible flat fields, in case a test sender omits the data wrapper.
    public string incident_id;
    public string camera_id;
    public string status;
    public string updated_at;
}

[Serializable]
public sealed class BackendSimulatorWebSocketData
{
    public string incident_id;
    public string camera_id;
    public string status;
    public string updated_at;
    public string location;
}
