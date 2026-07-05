using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

#if !UNITY_WEBGL || UNITY_EDITOR
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#endif

public sealed class SecurityShowcaseController : MonoBehaviour
{
    private const int SecurityCameraCount = 8;
    private const int SecurityDroneCount = 8;

    [SerializeField] private Transform securityRoot;
    [SerializeField] private Transform cctvRoot;
    [SerializeField] private Transform droneRoot;
    [SerializeField] private Transform intruderRoot;
    [SerializeField] private Transform playerRoot;
    [SerializeField] private Transform markerRoot;
    [SerializeField] private Transform anchorRoot;
    [SerializeField] private Transform dashboardBridgeRoot;
    [SerializeField] private Transform importedSetpiecesRoot;
    [SerializeField] private GameObject cctvPrefab;
    [SerializeField] private GameObject dronePrefab;
    [SerializeField] private GameObject intruderPrefab;
    [SerializeField] private GameObject intrusionRobotPrefab;
    [SerializeField] private GameObject attackRobotPrefab;
    [SerializeField] private GameObject playerRobotPrefab;
    [SerializeField] private bool adoptExistingSurveillanceCameras = true;
    [SerializeField] private bool generateMissingRigs;
    [SerializeField] private bool showIncidentPins;
    [SerializeField] private bool forceVisibleCursor = true;
    [SerializeField] private bool preserveMainCameraOnStart = true;
    [SerializeField] private bool enableMapCameraControls = true;
    [SerializeField] private bool writeDemoBridgeFile = true;
    [Header("Backend WebSocket")]
    [SerializeField] private bool enableBackendWebSocket = true;
    [SerializeField] private string backendWebSocketUrl = "ws://127.0.0.1:8000/ws";
    [SerializeField] private bool backendWebSocketPlaysScenarios = true;
    [SerializeField] private bool backendWebSocketLogs = true;
    [SerializeField] private float backendWebSocketReconnectSeconds = 3f;
    [SerializeField] private float backendScenarioIncidentSuppressSeconds = 30f;
    [SerializeField] private bool showQuitButton = true;
    [SerializeField] private Rect quitButtonRect = new Rect(18f, 18f, 112f, 38f);
    [SerializeField] private string quitButtonLabel = "EXIT";
    [SerializeField] private bool showWindowModeButton = true;
    [SerializeField] private Vector2Int windowedResolution = new Vector2Int(1280, 720);
    [SerializeField] private float windowModeButtonGap = 8f;
    [SerializeField] private bool showCctvQuadViewButton = true;
    [SerializeField] private string cctvQuadViewButtonLabel = "CCTV x4";
    [SerializeField] private KeyCode cctvQuadViewKey = KeyCode.V;
    [SerializeField] private string[] cctvQuadCameraIds = { "cam_01", "cam_02", "cam_03", "cam_04" };
    [SerializeField] private Color cctvQuadBackgroundColor = new Color(0.02f, 0.025f, 0.03f, 1f);
    [SerializeField] private string demoBridgeOutputPath = "./apps/dashboard/public/simulator/demo-bridge.json";
    [SerializeField] private string demoBridgeMirrorOutputPath = "";
    [SerializeField] private KeyCode cctvViewKey = KeyCode.C;
    [SerializeField] private KeyCode exitCctvViewKey = KeyCode.Escape;
    [SerializeField] private float cctvViewFieldOfView = 46f;
    [SerializeField] private SecurityScenarioController scenarioController;
    [SerializeField] private SecurityPlayerRobotController playerController;

    private readonly Dictionary<string, SecurityCameraRig> cameras = new Dictionary<string, SecurityCameraRig>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SecurityIncidentMarker> incidentMarkers = new Dictionary<string, SecurityIncidentMarker>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> incidentCameraIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> incidentStatuses = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> knownIncidentIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> playedBackendIncidentIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> processedBackendEventKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly Queue<string> backendWebSocketMessages = new Queue<string>();
    private readonly Queue<string> backendWebSocketLogMessages = new Queue<string>();
    private readonly object backendWebSocketLock = new object();

    private FactoryMapCameraController mapCameraController;
    private Bounds sceneBounds;
    private Material matCameraNormal;
    private Material matDetected;
    private Material matReviewed;
    private Material matDrone;
    private Material matIntruder;
    private Material matMarker;
    private Coroutine scenarioRoutine;
    private string selectedCameraId = "cam_01";
    private bool cctvViewActive;
    private bool cctvQuadViewActive;
    private Camera mainCamera;
    private CameraViewState cameraViewBeforeCctv;
    private bool hasCameraViewBeforeCctv;
    private CameraViewState cameraViewBeforeQuad;
    private bool hasCameraViewBeforeQuad;
    private bool mainCameraEnabledBeforeQuad = true;
    private readonly List<Camera> cctvQuadCameras = new List<Camera>();
    private int demoBridgeSeq;
    private float suppressScenarioIncidentUntil;

#if !UNITY_WEBGL || UNITY_EDITOR
    private CancellationTokenSource backendWebSocketCts;
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void BogobogoUnityMapEvent(string json);
#endif

    private void Start()
    {
        gameObject.name = "FactorySimulator";
        CreateMaterials();
        EnsureRoots();
        CalculateSceneBounds();
        EnsureMapCamera();
        if (adoptExistingSurveillanceCameras) AdoptExistingSurveillanceCameras();
        if (generateMissingRigs) EnsureSecurityRigs();
        BindSecurityRigs();
        EnsureLocalRuntimeControllers();
        ApplyCursorPolicy();
        StartBackendWebSocket();
    }

    private void Update()
    {
        ApplyCursorPolicy();
        DrainBackendWebSocketMessages();

        if (WasPressed(cctvViewKey))
        {
            if (cctvQuadViewActive) ExitCctvQuadView();
            if (cctvViewActive) ExitCctvView();
            else EnterSelectedCctvView();
        }

        if (WasPressed(cctvQuadViewKey))
        {
            ToggleCctvQuadView();
        }

        if (cctvViewActive && WasPressed(exitCctvViewKey))
        {
            ExitCctvView();
        }

        if (cctvQuadViewActive && WasPressed(exitCctvViewKey))
        {
            ExitCctvQuadView();
        }

        if (cctvViewActive)
        {
            UpdateCctvViewCamera();
        }

        if (cctvQuadViewActive)
        {
            UpdateCctvQuadCameras();
        }
    }

    private void OnApplicationQuit()
    {
        WriteDemoBridge("all", "normal");
        StopBackendWebSocket();
    }

    private void OnDestroy()
    {
        StopBackendWebSocket();
    }

    private void OnGUI()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        GUI.depth = -100;
        if (showQuitButton && GUI.Button(quitButtonRect, quitButtonLabel))
        {
            QuitShowcase();
        }

        if (showWindowModeButton && GUI.Button(WindowModeButtonRect(), WindowModeButtonLabel()))
        {
            ToggleWindowMode();
        }

        if (showCctvQuadViewButton && GUI.Button(CctvQuadButtonRect(), CctvQuadButtonLabel()))
        {
            ToggleCctvQuadView();
        }

        if (cctvQuadViewActive)
        {
            DrawCctvQuadOverlay();
        }
#endif
    }

    public void QuitShowcase()
    {
        WriteDemoBridge("all", "normal");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ToggleWindowMode()
    {
        if (Screen.fullScreen)
        {
            int width = Mathf.Max(640, windowedResolution.x);
            int height = Mathf.Max(360, windowedResolution.y);
            Screen.SetResolution(width, height, FullScreenMode.Windowed);
        }
        else
        {
            Resolution resolution = Screen.currentResolution;
            Screen.SetResolution(resolution.width, resolution.height, FullScreenMode.FullScreenWindow);
        }

        ApplyCursorPolicy();
    }

    private Rect WindowModeButtonRect()
    {
        return new Rect(
            quitButtonRect.x,
            quitButtonRect.y + quitButtonRect.height + windowModeButtonGap,
            quitButtonRect.width,
            quitButtonRect.height);
    }

    private static string WindowModeButtonLabel()
    {
        return Screen.fullScreen ? "WINDOW" : "FULLSCREEN";
    }

    private Rect CctvQuadButtonRect()
    {
        float y = quitButtonRect.y + quitButtonRect.height + windowModeButtonGap;
        if (showWindowModeButton) y += quitButtonRect.height + windowModeButtonGap;
        return new Rect(quitButtonRect.x, y, quitButtonRect.width, quitButtonRect.height);
    }

    private string CctvQuadButtonLabel()
    {
        return cctvQuadViewActive ? "CCTV OFF" : cctvQuadViewButtonLabel;
    }

    private void StartBackendWebSocket()
    {
        if (!enableBackendWebSocket || string.IsNullOrWhiteSpace(backendWebSocketUrl)) return;

#if !UNITY_WEBGL || UNITY_EDITOR
        StopBackendWebSocket();
        backendWebSocketCts = new CancellationTokenSource();
        _ = RunBackendWebSocketLoop(backendWebSocketCts.Token);
        if (backendWebSocketLogs) Debug.Log("Backend WebSocket listener starting: " + backendWebSocketUrl);
#else
        if (backendWebSocketLogs) Debug.LogWarning("Backend WebSocket listener is disabled in WebGL builds. Use dashboard bridge messages instead.");
#endif
    }

    private void StopBackendWebSocket()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (backendWebSocketCts != null)
        {
            backendWebSocketCts.Cancel();
            backendWebSocketCts.Dispose();
            backendWebSocketCts = null;
        }
#endif
    }

    private void DrainBackendWebSocketMessages()
    {
        for (int i = 0; i < 8; i++)
        {
            string log = null;
            lock (backendWebSocketLock)
            {
                if (backendWebSocketLogMessages.Count > 0) log = backendWebSocketLogMessages.Dequeue();
            }

            if (string.IsNullOrWhiteSpace(log)) break;
            if (log.StartsWith("WARN|", StringComparison.Ordinal)) Debug.LogWarning(log.Substring(5));
            else Debug.Log(log.StartsWith("INFO|", StringComparison.Ordinal) ? log.Substring(5) : log);
        }

        for (int i = 0; i < 12; i++)
        {
            string json = null;
            lock (backendWebSocketLock)
            {
                if (backendWebSocketMessages.Count > 0) json = backendWebSocketMessages.Dequeue();
            }

            if (string.IsNullOrWhiteSpace(json)) break;
            ApplyBackendWebSocketEvent(json);
        }
    }

    private void EnqueueBackendWebSocketLog(string level, string message)
    {
        if (!backendWebSocketLogs) return;
        lock (backendWebSocketLock)
        {
            backendWebSocketLogMessages.Enqueue(level + "|" + message);
        }
    }

    public void ApplyBackendWebSocketEvent(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return;

        BackendSimulatorWebSocketEvent packet;
        try
        {
            packet = JsonUtility.FromJson<BackendSimulatorWebSocketEvent>(json);
        }
        catch (Exception ex)
        {
            if (backendWebSocketLogs) Debug.LogWarning("Failed to parse backend WebSocket event: " + ex.Message + "\n" + json);
            return;
        }

        if (packet == null) return;

        BackendSimulatorWebSocketData data = packet.data;
        string incidentId = data != null && !string.IsNullOrWhiteSpace(data.incident_id) ? data.incident_id : packet.incident_id;
        string cameraId = data != null && !string.IsNullOrWhiteSpace(data.camera_id) ? data.camera_id : packet.camera_id;
        string rawStatus = data != null && !string.IsNullOrWhiteSpace(data.status) ? data.status : packet.status;
        string updatedAt = data != null && !string.IsNullOrWhiteSpace(data.updated_at) ? data.updated_at : packet.updated_at;

        if (string.IsNullOrWhiteSpace(cameraId)) return;
        if (string.IsNullOrWhiteSpace(incidentId))
        {
            incidentId = "BACKEND-" + cameraId.ToUpperInvariant().Replace("_", "-") + "-" + Mathf.Max(0, packet.seq).ToString(CultureInfo.InvariantCulture);
        }

        string eventKey = packet.type + "|" + incidentId + "|" + cameraId + "|" + rawStatus + "|" + updatedAt;
        if (!processedBackendEventKeys.Add(eventKey)) return;

        string status = NormalizeBackendIncidentStatus(rawStatus);
        if (status == "normal")
        {
            ClearIncident(incidentId, cameraId);
            return;
        }

        string zoneId = BackendZoneForCamera(cameraId);
        UnityIncidentState incident = new UnityIncidentState
        {
            incident_id = incidentId,
            event_type = "intrusion",
            camera_id = cameraId,
            zone_id = zoneId,
            status = status,
            severity = 0.95f,
            detected_at = updatedAt,
            source_type = "backend_websocket",
        };

        ShowIncident(incident);
        ApplyCameraStatuses();

        if (backendWebSocketLogs)
        {
            Debug.Log("Backend WebSocket event applied: " + packet.type + " " + incidentId + " " + cameraId + " " + status);
        }

        if (backendWebSocketPlaysScenarios && status == "detected" && playedBackendIncidentIds.Add(incidentId))
        {
            string scenarioId = ScenarioForCamera(cameraId);
            if (!string.IsNullOrWhiteSpace(scenarioId))
            {
                suppressScenarioIncidentUntil = Time.unscaledTime + Mathf.Max(1f, backendScenarioIncidentSuppressSeconds);
                PlayScenario(scenarioId);
            }
        }
    }

#if !UNITY_WEBGL || UNITY_EDITOR
    private async Task RunBackendWebSocketLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                using (ClientWebSocket socket = new ClientWebSocket())
                {
                    await socket.ConnectAsync(new Uri(backendWebSocketUrl), token);
                    EnqueueBackendWebSocketLog("INFO", "Backend WebSocket connected: " + backendWebSocketUrl);
                    await ReceiveBackendWebSocket(socket, token);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                EnqueueBackendWebSocketLog("WARN", "Backend WebSocket disconnected: " + ex.Message);
            }

            if (token.IsCancellationRequested) break;

            try
            {
                int delayMilliseconds = (int)(Math.Max(0.5f, backendWebSocketReconnectSeconds) * 1000f);
                await Task.Delay(delayMilliseconds, token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ReceiveBackendWebSocket(ClientWebSocket socket, CancellationToken token)
    {
        byte[] buffer = new byte[8192];
        while (!token.IsCancellationRequested && socket.State == WebSocketState.Open)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                WebSocketReceiveResult result;
                do
                {
                    result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", token);
                        return;
                    }

                    stream.Write(buffer, 0, result.Count);
                }
                while (!result.EndOfMessage);

                if (result.MessageType != WebSocketMessageType.Text) continue;

                string json = Encoding.UTF8.GetString(stream.ToArray());
                lock (backendWebSocketLock)
                {
                    backendWebSocketMessages.Enqueue(json);
                }
            }
        }
    }
#endif

    public void ApplyDashboardState(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return;

        UnityDashboardState state = JsonUtility.FromJson<UnityDashboardState>(json);
        if (state == null) return;

        Dictionary<string, string> cameraBaseStatuses = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (state.cameras != null)
        {
            foreach (UnityCameraState camera in state.cameras)
            {
                if (camera == null || string.IsNullOrWhiteSpace(camera.camera_id)) continue;
                cameraBaseStatuses[camera.camera_id] = camera.status;
            }
        }

        HashSet<string> active = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string selectedIncidentCandidate = "";

        if (state.incidents != null)
        {
            foreach (UnityIncidentState incident in state.incidents)
            {
                if (incident == null || string.IsNullOrWhiteSpace(incident.incident_id)) continue;
                if (incident.status != "detected" && incident.status != "reviewed") continue;

                active.Add(incident.incident_id);
                ShowIncident(incident);
                if (incident.status == "detected" && string.IsNullOrEmpty(selectedIncidentCandidate) && !knownIncidentIds.Contains(incident.incident_id))
                {
                    selectedIncidentCandidate = incident.incident_id;
                }
            }
        }

        foreach (string incidentId in incidentMarkers.Keys.ToArray())
        {
            if (!active.Contains(incidentId))
            {
                Destroy(incidentMarkers[incidentId].gameObject);
                incidentMarkers.Remove(incidentId);
            }
        }

        foreach (string incidentId in incidentCameraIds.Keys.ToArray())
        {
            if (!active.Contains(incidentId))
            {
                incidentCameraIds.Remove(incidentId);
                incidentStatuses.Remove(incidentId);
            }
        }

        ApplyCameraStatuses(cameraBaseStatuses);

        if (!string.IsNullOrWhiteSpace(state.selected_incident_id)) FocusIncident(state.selected_incident_id);
        else if (!string.IsNullOrWhiteSpace(selectedIncidentCandidate)) FocusIncident(selectedIncidentCandidate);

        knownIncidentIds.Clear();
        foreach (string id in active) knownIncidentIds.Add(id);
    }

    public void FocusIncident(string incidentId)
    {
        if (string.IsNullOrWhiteSpace(incidentId)) return;
        if (!incidentCameraIds.TryGetValue(incidentId, out string cameraId)) return;
        SelectCameraForView(cameraId);
    }

    public void SelectCameraForView(string cameraId)
    {
        if (!string.IsNullOrWhiteSpace(cameraId)) selectedCameraId = cameraId;
    }

    public void OpenCameraView(string cameraId)
    {
        SelectCameraForView(cameraId);
        EnterSelectedCctvView();
    }

    public void PlayScenario(string scenarioId)
    {
        if (string.IsNullOrWhiteSpace(scenarioId)) return;
        if (scenarioController != null && scenarioController.PlayScenario(scenarioId)) return;

        if (scenarioRoutine != null) StopCoroutine(scenarioRoutine);
        scenarioRoutine = StartCoroutine(RunScenario(scenarioId));
    }

    public void ResetView()
    {
        if (mapCameraController != null)
        {
            mapCameraController.enabled = true;
            mapCameraController.ResetView();
        }
    }

    public void SetZoomLevel(string normalizedZoom)
    {
        if (mapCameraController == null || string.IsNullOrWhiteSpace(normalizedZoom)) return;
        if (float.TryParse(normalizedZoom, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
        {
            mapCameraController.enabled = true;
            mapCameraController.SetZoomLevel(value);
        }
    }

    public void HandleMapClick(string eventType, string incidentId, string cameraId, string zoneId)
    {
        string payload = "{\"type\":\"" + Escape(eventType) + "\",\"incident_id\":\"" + Escape(incidentId) +
            "\",\"camera_id\":\"" + Escape(cameraId) + "\",\"zone_id\":\"" + Escape(zoneId) + "\"}";

#if UNITY_WEBGL && !UNITY_EDITOR
        BogobogoUnityMapEvent(payload);
#else
        Debug.Log("bogobogo:unity-map-event " + payload);
#endif
    }

    public void FocusWorldPosition(Vector3 worldPosition, float preferredSize = -1f)
    {
        if (mapCameraController != null)
        {
            mapCameraController.enabled = true;
            mapCameraController.FocusOn(worldPosition, preferredSize > 0f ? preferredSize : Mathf.Max(16f, sceneBounds.size.magnitude * 0.08f));
        }
    }

    public void RaiseScenarioIncident(string incidentId, string cameraId, string zoneId, string status = "detected")
    {
        if (string.IsNullOrWhiteSpace(incidentId)) return;
        if (IsScenarioIncidentSuppressed())
        {
            if (backendWebSocketLogs) Debug.Log("Suppressed backend-triggered scenario incident echo: " + incidentId);
            return;
        }

        UnityIncidentState incident = new UnityIncidentState
        {
            incident_id = incidentId,
            camera_id = cameraId,
            zone_id = zoneId,
            status = string.IsNullOrWhiteSpace(status) ? "detected" : status,
            severity = 0.95f,
        };

        ShowIncident(incident);
        ApplyCameraStatuses();
        WriteDemoBridge(incident);
    }

    public void RaiseScenarioIncident(string incidentId, string cameraId, string zoneId, Vector3 ignoredWorldPosition, string status = "detected")
    {
        RaiseScenarioIncident(incidentId, cameraId, zoneId, status);
    }

    private void ShowIncident(UnityIncidentState incident)
    {
        Vector3 position = IncidentPosition(incident.camera_id, incident.zone_id);
        Vector3 markerPosition = position + Vector3.up * Mathf.Max(2.5f, sceneBounds.size.y * 0.08f);
        if (!string.IsNullOrWhiteSpace(incident.camera_id))
        {
            incidentCameraIds[incident.incident_id] = incident.camera_id;
            incidentStatuses[incident.incident_id] = incident.status;
        }

        if (showIncidentPins && (!incidentMarkers.TryGetValue(incident.incident_id, out SecurityIncidentMarker existingMarker) || existingMarker == null))
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "incident_" + incident.incident_id;
            go.transform.SetParent(markerRoot, true);
            go.transform.localScale = Vector3.one * Mathf.Max(1.8f, sceneBounds.size.magnitude * 0.012f);
            go.GetComponent<Renderer>().sharedMaterial = matMarker;
            existingMarker = go.AddComponent<SecurityIncidentMarker>();
            incidentMarkers[incident.incident_id] = existingMarker;
        }

        if (showIncidentPins && incidentMarkers.TryGetValue(incident.incident_id, out SecurityIncidentMarker activeMarker) && activeMarker != null)
        {
            activeMarker.transform.position = markerPosition;
            activeMarker.Configure(this, incident.incident_id, incident.camera_id, incident.zone_id);
        }

        if (cameras.ContainsKey(incident.camera_id))
        {
            selectedCameraId = incident.camera_id;
        }
    }

    private void ApplyCameraStatuses(Dictionary<string, string> cameraBaseStatuses = null)
    {
        foreach (KeyValuePair<string, SecurityCameraRig> item in cameras)
        {
            string status = cameraBaseStatuses != null && cameraBaseStatuses.TryGetValue(item.Key, out string cameraStatus)
                ? cameraStatus
                : "normal";

            bool hasReviewedIncident = false;
            foreach (KeyValuePair<string, string> incident in incidentCameraIds)
            {
                if (!string.Equals(incident.Value, item.Key, StringComparison.OrdinalIgnoreCase)) continue;
                string incidentStatus = incidentStatuses.TryGetValue(incident.Key, out string activeStatus) ? activeStatus : "detected";
                if (incidentStatus == "detected")
                {
                    status = "detected";
                    break;
                }

                if (incidentStatus == "reviewed") hasReviewedIncident = true;
            }

            if (status != "detected" && hasReviewedIncident) status = "reviewed";
            item.Value.SetStatus(status);
        }
    }

    private void ClearIncident(string incidentId, string cameraId)
    {
        if (!string.IsNullOrWhiteSpace(incidentId))
        {
            if (incidentMarkers.TryGetValue(incidentId, out SecurityIncidentMarker marker) && marker != null)
            {
                Destroy(marker.gameObject);
            }

            incidentMarkers.Remove(incidentId);
            incidentCameraIds.Remove(incidentId);
            incidentStatuses.Remove(incidentId);
            knownIncidentIds.Remove(incidentId);
            playedBackendIncidentIds.Remove(incidentId);
        }

        if (!string.IsNullOrWhiteSpace(cameraId))
        {
            foreach (string id in incidentCameraIds
                .Where(item => string.Equals(item.Value, cameraId, StringComparison.OrdinalIgnoreCase))
                .Select(item => item.Key)
                .ToArray())
            {
                if (incidentMarkers.TryGetValue(id, out SecurityIncidentMarker marker) && marker != null)
                {
                    Destroy(marker.gameObject);
                }

                incidentMarkers.Remove(id);
                incidentCameraIds.Remove(id);
                incidentStatuses.Remove(id);
                knownIncidentIds.Remove(id);
                playedBackendIncidentIds.Remove(id);
            }
        }

        ApplyCameraStatuses();
    }

    private static string NormalizeBackendIncidentStatus(string status)
    {
        string normalized = string.IsNullOrWhiteSpace(status) ? "detected" : status.Trim().ToLowerInvariant();
        if (normalized == "detected") return "detected";
        if (normalized == "reviewed") return "reviewed";
        if (normalized == "confirmed") return "reviewed";
        if (normalized == "false_alarm" || normalized == "normal" || normalized == "resolved" || normalized == "closed") return "normal";
        return "detected";
    }

    private string BackendZoneForCamera(string cameraId)
    {
        if (!string.IsNullOrWhiteSpace(cameraId) && cameras.TryGetValue(cameraId, out SecurityCameraRig rig) && rig != null && !string.IsNullOrWhiteSpace(rig.zoneId))
        {
            return rig.zoneId;
        }

        int index = CameraIndex(cameraId);
        return "zone_" + Mathf.Max(1, index).ToString("00", CultureInfo.InvariantCulture);
    }

    private static string ScenarioForCamera(string cameraId)
    {
        int index = CameraIndex(cameraId);
        switch (index)
        {
            case 1:
                return "fence_climb";
            case 2:
                return "fence_damage";
            case 3:
                return "facility_filming";
            case 4:
                return "facility_damage";
            default:
                return "";
        }
    }

    private bool IsScenarioIncidentSuppressed()
    {
        return suppressScenarioIncidentUntil > 0f && Time.unscaledTime < suppressScenarioIncidentUntil;
    }

    private Vector3 IncidentPosition(string cameraId, string zoneId)
    {
        if (!string.IsNullOrWhiteSpace(cameraId) && cameras.TryGetValue(cameraId, out SecurityCameraRig rig))
        {
            return rig.FocusPoint();
        }

        Transform anchor = FindAnchor(zoneId);
        if (anchor != null) return anchor.position;
        return sceneBounds.center;
    }

    private IEnumerator RunScenario(string scenarioId)
    {
        string id = scenarioId.ToLowerInvariant();
        Transform start = EnsureAnchor(id + "_start", AnchorPosition(0.18f, 0.18f, 0f));
        Transform target = EnsureAnchor(id + "_target", AnchorPosition(0.62f, 0.58f, 0f));
        Transform prop = EnsureAnchor("forbidden_equipment", AnchorPosition(0.66f, 0.55f, 1.2f));

        GameObject intruder = EnsureIntruder();
        intruder.transform.position = start.position;
        intruder.SetActive(true);

        yield return MoveIntruder(intruder.transform, target.position, 3.2f);

        if (id.Contains("break"))
        {
            yield return BreakEquipmentMotion(intruder.transform, prop);
            RaiseLocalIncident("INC-UNITY-BREAK", "cam_01", "forbidden_equipment");
        }
        else if (id.Contains("loiter"))
        {
            yield return LoiterMotion(intruder.transform, prop.position, 4.5f);
            RaiseLocalIncident("INC-UNITY-LOITER", "cam_02", "forbidden_equipment");
        }
        else
        {
            yield return RestrictedEntryMotion(intruder.transform);
            RaiseLocalIncident("INC-UNITY-RESTRICTED", "cam_03", "restricted_zone");
        }
    }

    private void RaiseLocalIncident(string incidentId, string cameraId, string zoneId)
    {
        UnityIncidentState incident = new UnityIncidentState
        {
            incident_id = incidentId,
            camera_id = cameraId,
            zone_id = zoneId,
            status = "detected",
            severity = 0.9f,
        };

        ShowIncident(incident);
        ApplyCameraStatuses();
        WriteDemoBridge(incident);
    }

    private void WriteDemoBridge(UnityIncidentState incident)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return;
#else
        if (!writeDemoBridgeFile || incident == null || string.IsNullOrWhiteSpace(incident.camera_id)) return;

        demoBridgeSeq++;
        string now = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        string normalizedStatus = NormalizeBridgeStatus(incident.status);
        string incidentId = string.IsNullOrWhiteSpace(incident.incident_id)
            ? "UNITY-" + incident.camera_id.ToUpperInvariant().Replace("_", "-")
            : incident.incident_id;
        string cameraId = incident.camera_id;
        string zoneId = string.IsNullOrWhiteSpace(incident.zone_id) ? "unity_demo_zone" : incident.zone_id;
        float severity = Mathf.Clamp01(incident.severity > 0f ? incident.severity : 0.9f);

        string json = BuildFullDemoBridgeJson(incidentId, cameraId, zoneId, normalizedStatus, severity, now);

        WriteDemoBridgeFile(demoBridgeOutputPath, json);
        WriteDemoBridgeFile(demoBridgeMirrorOutputPath, json);
        WriteDemoBridgeFile(ResolveRepoDemoBridgePath(), json);
#endif
    }

    private void WriteDemoBridge(string cameraId, string status)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return;
#else
        if (!writeDemoBridgeFile || string.IsNullOrWhiteSpace(cameraId)) return;

        demoBridgeSeq++;
        string normalizedStatus = status == "reviewed" ? "reviewed" : status == "normal" ? "normal" : "detected";
        string json = "{\n" +
            "  \"schema_version\": \"bogobogo_demo_bridge.v1\",\n" +
            "  \"seq\": " + demoBridgeSeq.ToString(CultureInfo.InvariantCulture) + ",\n" +
            "  \"camera_id\": \"" + Escape(cameraId) + "\",\n" +
            "  \"status\": \"" + Escape(normalizedStatus) + "\",\n" +
            "  \"updated_at\": \"" + DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) + "\"\n" +
            "}\n";

        WriteDemoBridgeFile(demoBridgeOutputPath, json);
        WriteDemoBridgeFile(demoBridgeMirrorOutputPath, json);
        WriteDemoBridgeFile(ResolveRepoDemoBridgePath(), json);
#endif
    }

    private string BuildFullDemoBridgeJson(string incidentId, string cameraId, string zoneId, string status, float severity, string now)
    {
        string sourceId = ScenarioSourceId(incidentId, cameraId);
        string scenarioTitle = ScenarioTitle(incidentId);
        string scenarioAction = ScenarioActionEvent(incidentId);
        int trackId = CameraTrackId(cameraId);
        string timeline = BuildTimelineJson(now, scenarioAction);
        string objects = ScenarioObjectsJson(incidentId);

        return "{\n" +
            "  \"schema_version\": \"bogobogo_demo_bridge.full.v1\",\n" +
            "  \"seq\": " + demoBridgeSeq.ToString(CultureInfo.InvariantCulture) + ",\n" +
            "  \"camera_id\": \"" + Escape(cameraId) + "\",\n" +
            "  \"status\": \"" + Escape(status) + "\",\n" +
            "  \"updated_at\": \"" + Escape(now) + "\",\n" +
            "  \"incident\": {\n" +
            "    \"incident_id\": \"" + Escape(incidentId) + "\",\n" +
            "    \"event_type\": \"intrusion\",\n" +
            "    \"status\": \"" + Escape(status) + "\",\n" +
            "    \"detected_at\": \"" + Escape(now) + "\",\n" +
            "    \"camera_id\": \"" + Escape(cameraId) + "\",\n" +
            "    \"zone_id\": \"" + Escape(zoneId) + "\",\n" +
            "    \"location\": {\n" +
            "      \"site\": \"factory_A\",\n" +
            "      \"zone\": \"" + Escape(zoneId) + "\",\n" +
            "      \"camera_id\": \"" + Escape(cameraId) + "\"\n" +
            "    },\n" +
            "    \"score\": {\n" +
            "      \"eventization_score\": " + severity.ToString("0.000", CultureInfo.InvariantCulture) + ",\n" +
            "      \"detector_confidence_avg\": " + Mathf.Clamp01(severity - 0.03f).ToString("0.000", CultureInfo.InvariantCulture) + "\n" +
            "    },\n" +
            "    \"source_summary\": {\n" +
            "      \"source_type\": \"simulator\",\n" +
            "      \"source_id\": \"" + Escape(sourceId) + "\",\n" +
            "      \"camera_id\": \"" + Escape(cameraId) + "\"\n" +
            "    },\n" +
            "    \"evidence\": {\n" +
            "      \"thumbnail_url\": null,\n" +
            "      \"clip_url\": null,\n" +
            "      \"objects\": " + objects + ",\n" +
            "      \"track_ids\": [" + trackId.ToString(CultureInfo.InvariantCulture) + "]\n" +
            "    },\n" +
            "    \"timeline\": " + timeline + ",\n" +
            "    \"eventization_basis\": {\n" +
            "      \"line_crossed\": " + BoolJson(IsFenceScenario(incidentId)) + ",\n" +
            "      \"roi_entered\": true,\n" +
            "      \"duration_sec\": " + ScenarioDuration(incidentId).ToString("0.0", CultureInfo.InvariantCulture) + ",\n" +
            "      \"zone_policy\": \"" + Escape(scenarioTitle) + "_security_rule\"\n" +
            "    },\n" +
            "    \"operator\": {\n" +
            "      \"note\": null,\n" +
            "      \"reviewed_by\": null,\n" +
            "      \"reviewed_at\": null\n" +
            "    },\n" +
            "    \"report_summary\": {\n" +
            "      \"report_available\": false,\n" +
            "      \"report_id\": null,\n" +
            "      \"report_generated_at\": null\n" +
            "    },\n" +
            "    \"status_history_preview\": [\n" +
            "      {\n" +
            "        \"from\": null,\n" +
            "        \"to\": \"" + Escape(status) + "\",\n" +
            "        \"actor\": { \"type\": \"system\", \"id\": \"unity_demo_bridge\" },\n" +
            "        \"at\": \"" + Escape(now) + "\",\n" +
            "        \"note\": \"" + Escape("Unity scenario created " + scenarioTitle + " incident") + "\"\n" +
            "      }\n" +
            "    ],\n" +
            "    \"ai_output\": null\n" +
            "  }\n" +
            "}\n";
    }

    private static string BuildTimelineJson(string now, string action)
    {
        DateTime detectedAt;
        if (!DateTime.TryParse(now, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out detectedAt))
        {
            detectedAt = DateTime.UtcNow;
        }

        string t0 = detectedAt.AddSeconds(-2.0).ToString("o", CultureInfo.InvariantCulture);
        string t1 = detectedAt.ToString("o", CultureInfo.InvariantCulture);
        string t2 = detectedAt.AddSeconds(1.2).ToString("o", CultureInfo.InvariantCulture);
        string t3 = detectedAt.AddSeconds(2.4).ToString("o", CultureInfo.InvariantCulture);

        return "[\n" +
            "      { \"ts\": \"" + Escape(t0) + "\", \"event\": \"actor_detected\" },\n" +
            "      { \"ts\": \"" + Escape(t1) + "\", \"event\": \"" + Escape(action) + "\" },\n" +
            "      { \"ts\": \"" + Escape(t2) + "\", \"event\": \"roi_entered\" },\n" +
            "      { \"ts\": \"" + Escape(t3) + "\", \"event\": \"incident_created\" }\n" +
            "    ]";
    }

    private static string NormalizeBridgeStatus(string status)
    {
        if (status == "reviewed") return "reviewed";
        if (status == "confirmed") return "confirmed";
        if (status == "false_alarm") return "false_alarm";
        if (status == "normal") return "normal";
        return "detected";
    }

    private static string ScenarioTitle(string incidentId)
    {
        string normalized = (incidentId ?? "").ToLowerInvariant();
        if (normalized.Contains("fence-climb") || normalized.Contains("climb")) return "fence_climb";
        if (normalized.Contains("fence-damage")) return "fence_damage";
        if (normalized.Contains("filming")) return "facility_filming";
        if (normalized.Contains("facility-damage") || normalized.Contains("damage")) return "facility_damage";
        if (normalized.Contains("loiter")) return "restricted_loitering";
        if (normalized.Contains("restricted")) return "restricted_entry";
        return "intrusion";
    }

    private static string ScenarioActionEvent(string incidentId)
    {
        string title = ScenarioTitle(incidentId);
        if (title == "fence_climb") return "fence_crossed";
        if (title == "fence_damage") return "fence_damage_observed";
        if (title == "facility_filming") return "unauthorized_filming";
        if (title == "facility_damage") return "equipment_damage_observed";
        if (title == "restricted_loitering") return "restricted_loitering";
        if (title == "restricted_entry") return "restricted_entry";
        return "intrusion_detected";
    }

    private static string ScenarioObjectsJson(string incidentId)
    {
        string title = ScenarioTitle(incidentId);
        if (title == "facility_damage" || title == "fence_damage") return "[\"intruder\", \"equipment\"]";
        if (title == "facility_filming") return "[\"intruder\", \"camera_device\"]";
        return "[\"intruder\"]";
    }

    private static bool IsFenceScenario(string incidentId)
    {
        string title = ScenarioTitle(incidentId);
        return title == "fence_climb" || title == "fence_damage";
    }

    private static float ScenarioDuration(string incidentId)
    {
        string title = ScenarioTitle(incidentId);
        if (title == "facility_filming") return 6.2f;
        if (title == "facility_damage" || title == "fence_damage") return 4.8f;
        if (title == "fence_climb") return 3.6f;
        return 3.2f;
    }

    private static string ScenarioSourceId(string incidentId, string cameraId)
    {
        string id = string.IsNullOrWhiteSpace(incidentId) ? "unity_incident" : incidentId.ToLowerInvariant();
        return id.Replace("inc-", "").Replace("inc_", "") + "_" + cameraId;
    }

    private static int CameraTrackId(string cameraId)
    {
        string digits = new string((cameraId ?? "").Where(char.IsDigit).ToArray());
        int number;
        return int.TryParse(digits, out number) && number > 0 ? number : 1;
    }

    private static string BoolJson(bool value)
    {
        return value ? "true" : "false";
    }

    private static string ResolveRepoDemoBridgePath()
    {
        try
        {
            string repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "..", "..", ".."));
            return Path.Combine(repoRoot, "apps", "dashboard", "public", "simulator", "demo-bridge.json");
        }
        catch
        {
            return "";
        }
    }

    private static void WriteDemoBridgeFile(string path, string json)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        try
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Failed to write demo bridge file: " + path + "\n" + ex.Message);
        }
    }

    private IEnumerator MoveIntruder(Transform actor, Vector3 target, float seconds)
    {
        Vector3 start = actor.position;
        for (float elapsed = 0f; elapsed < seconds; elapsed += Time.deltaTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / seconds);
            actor.position = Vector3.Lerp(start, target, t);
            Vector3 direction = target - actor.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.01f) actor.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            yield return null;
        }
        actor.position = target;
    }

    private IEnumerator BreakEquipmentMotion(Transform actor, Transform equipment)
    {
        Vector3 baseScale = equipment.localScale;
        for (float elapsed = 0f; elapsed < 2.4f; elapsed += Time.deltaTime)
        {
            actor.localRotation = Quaternion.Euler(0f, actor.eulerAngles.y, Mathf.Sin(elapsed * 18f) * 9f);
            equipment.localScale = baseScale * (1f + Mathf.Abs(Mathf.Sin(elapsed * 18f)) * 0.08f);
            yield return null;
        }
        actor.localRotation = Quaternion.Euler(0f, actor.eulerAngles.y, 0f);
        equipment.localScale = baseScale;
    }

    private IEnumerator LoiterMotion(Transform actor, Vector3 center, float seconds)
    {
        for (float elapsed = 0f; elapsed < seconds; elapsed += Time.deltaTime)
        {
            float angle = elapsed * 1.8f;
            actor.position = center + new Vector3(Mathf.Cos(angle) * 3f, 0f, Mathf.Sin(angle) * 3f);
            actor.LookAt(new Vector3(center.x, actor.position.y, center.z));
            yield return null;
        }
    }

    private IEnumerator RestrictedEntryMotion(Transform actor)
    {
        Vector3 target = AnchorPosition(0.5f, 0.5f, 0f);
        yield return MoveIntruder(actor, target, 2.2f);
    }

    private void EnsureRoots()
    {
        securityRoot = securityRoot != null ? securityRoot : EnsureChild(null, "SecurityShowcaseRoot");
        cctvRoot = cctvRoot != null ? cctvRoot : EnsureChild(securityRoot, "CCTV_Rigs");
        droneRoot = droneRoot != null ? droneRoot : EnsureChild(securityRoot, "Drone_Rigs");
        intruderRoot = intruderRoot != null ? intruderRoot : EnsureChild(securityRoot, "IntruderActors");
        playerRoot = playerRoot != null ? playerRoot : EnsureChild(securityRoot, "PlayerRobot");
        markerRoot = markerRoot != null ? markerRoot : EnsureChild(securityRoot, "IncidentMarkers");
        anchorRoot = anchorRoot != null ? anchorRoot : EnsureChild(securityRoot, "ScenarioAnchors");
        dashboardBridgeRoot = dashboardBridgeRoot != null ? dashboardBridgeRoot : EnsureChild(securityRoot, "DashboardBridge");
        importedSetpiecesRoot = importedSetpiecesRoot != null ? importedSetpiecesRoot : EnsureChild(securityRoot, "ImportedSetpieces");
    }

    private Transform EnsureChild(Transform parent, string childName)
    {
        Transform existing = parent == null ? GameObject.Find(childName)?.transform : parent.Find(childName);
        if (existing != null) return existing;
        GameObject go = new GameObject(childName);
        if (parent != null) go.transform.SetParent(parent, false);
        return go.transform;
    }

    private void EnsureMapCamera()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            mainCamera = go.AddComponent<Camera>();
        }
        CameraViewState startupView = CameraViewState.Capture(mainCamera);

        if (FindObjectOfType<AudioListener>() == null && mainCamera.GetComponent<AudioListener>() == null)
        {
            mainCamera.gameObject.AddComponent<AudioListener>();
        }

        mapCameraController = mainCamera.GetComponent<FactoryMapCameraController>() ?? mainCamera.gameObject.AddComponent<FactoryMapCameraController>();
        float width = Mathf.Max(30f, sceneBounds.size.x);
        float depth = Mathf.Max(30f, sceneBounds.size.z);
        bool preserveStartupView = preserveMainCameraOnStart;
#if UNITY_WEBGL && !UNITY_EDITOR
        preserveStartupView = false;
#endif
        mapCameraController.ConfigureSiteBounds(width, depth, sceneBounds.center, !preserveStartupView);
        if (preserveStartupView)
        {
            startupView.Apply(mainCamera);
            mapCameraController.CaptureCurrentCameraView();
        }
        mapCameraController.enabled = enableMapCameraControls;
    }

    private void AdoptExistingSurveillanceCameras()
    {
        Transform[] surveillanceCameras = FindObjectsOfType<Transform>(true)
            .Where(item => item != null &&
                IsSurveillanceCameraName(item.name) &&
                !HasSurveillanceCameraParent(item))
            .OrderBy(item => item.name)
            .Take(SecurityCameraCount)
            .ToArray();

        if (surveillanceCameras.Length > 0)
        {
            foreach (SecurityCameraRig existing in cctvRoot.GetComponentsInChildren<SecurityCameraRig>(true))
            {
                if (existing != null && !IsSurveillanceCameraName(existing.name) && existing.name.StartsWith("cam_", StringComparison.OrdinalIgnoreCase))
                {
                    existing.gameObject.SetActive(false);
                }
            }

            for (int i = 0; i < surveillanceCameras.Length; i++)
            {
                Transform candidate = surveillanceCameras[i];
                string id = "cam_" + (i + 1).ToString("00", CultureInfo.InvariantCulture);
                candidate.SetParent(cctvRoot, true);
                SecurityCameraRig rig = candidate.GetComponent<SecurityCameraRig>() ?? candidate.gameObject.AddComponent<SecurityCameraRig>();
                rig.cameraId = id;
                rig.zoneId = "zone_" + (i + 1).ToString("00", CultureInfo.InvariantCulture);
                if (rig.lookTarget == null)
                {
                    rig.lookTarget = EnsureAnchor(id + "_look_target", candidate.position + candidate.forward * 12f);
                }
            }

            return;
        }

        int nextIndex = 1;
        foreach (SecurityCameraRig existing in FindObjectsOfType<SecurityCameraRig>(true).OrderBy(item => item.name))
        {
            if (existing == null) continue;
            if (string.IsNullOrWhiteSpace(existing.cameraId) || existing.cameraId == "cam_01")
            {
                existing.cameraId = "cam_" + nextIndex.ToString("00", CultureInfo.InvariantCulture);
            }
            nextIndex = Mathf.Max(nextIndex + 1, CameraIndex(existing.cameraId) + 1);
        }

        Transform[] candidates = FindObjectsOfType<Transform>(true)
            .Where(item => item != null &&
                item.GetComponentInParent<SecurityCameraRig>(true) == null &&
                IsSurveillanceCameraName(item.name))
            .OrderBy(item => item.name)
            .Take(SecurityCameraCount)
            .ToArray();

        foreach (Transform candidate in candidates)
        {
            if (nextIndex > SecurityCameraCount) break;
            string id = "cam_" + nextIndex.ToString("00", CultureInfo.InvariantCulture);
            candidate.SetParent(cctvRoot, true);
            SecurityCameraRig rig = candidate.gameObject.AddComponent<SecurityCameraRig>();
            rig.cameraId = id;
            rig.zoneId = "zone_" + nextIndex.ToString("00", CultureInfo.InvariantCulture);
            if (rig.lookTarget == null)
            {
                rig.lookTarget = EnsureAnchor(id + "_look_target", candidate.position + candidate.forward * 12f);
            }
            nextIndex++;
        }
    }

    private void EnsureSecurityRigs()
    {
        for (int i = 0; i < SecurityCameraCount; i++)
        {
            string id = "cam_" + (i + 1).ToString("00", CultureInfo.InvariantCulture);
            if (FindRig<SecurityCameraRig>(cctvRoot, id) != null) continue;
            CreateCameraRig(id, i);
        }

        for (int i = 0; i < SecurityDroneCount; i++)
        {
            string id = "drone_" + (i + 1).ToString("00", CultureInfo.InvariantCulture);
            if (FindRig<SecurityDroneRig>(droneRoot, id) != null) continue;
            CreateDroneRig(id, i);
        }
    }

    private void BindSecurityRigs()
    {
        cameras.Clear();
        foreach (SecurityCameraRig rig in cctvRoot.GetComponentsInChildren<SecurityCameraRig>(true))
        {
            if (rig == null || string.IsNullOrWhiteSpace(rig.cameraId)) continue;
            rig.BindController(this, matCameraNormal, matDetected, matReviewed);
            cameras[rig.cameraId] = rig;
        }
    }

    private T FindRig<T>(Transform root, string id) where T : Component
    {
        if (typeof(T) == typeof(SecurityCameraRig))
        {
            return root.GetComponentsInChildren<SecurityCameraRig>(true)
                .FirstOrDefault(item => item.cameraId == id || item.name == id) as T;
        }

        return root.GetComponentsInChildren<T>(true).FirstOrDefault(item => item.name == id);
    }

    private void CreateCameraRig(string id, int index)
    {
        Transform target = EnsureAnchor(id + "_look_target", AnchorPosition(CameraU(index), CameraV(index), 0f));
        GameObject go = cctvPrefab != null ? Instantiate(cctvPrefab) : CreatePrimitiveCamera(id);
        go.name = id;
        go.transform.SetParent(cctvRoot, true);
        go.transform.position = target.position + Vector3.up * Mathf.Max(5f, sceneBounds.size.y * 0.18f);
        go.transform.LookAt(target);
        SecurityCameraRig rig = go.GetComponent<SecurityCameraRig>() ?? go.AddComponent<SecurityCameraRig>();
        rig.Configure(id, "zone_" + (index + 1).ToString("00", CultureInfo.InvariantCulture), target, matCameraNormal, matDetected, matReviewed);
        rig.BindController(this, matCameraNormal, matDetected, matReviewed);
    }

    private GameObject CreatePrimitiveCamera(string id)
    {
        GameObject root = new GameObject(id);
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "camera_body";
        body.transform.SetParent(root.transform, false);
        body.transform.localScale = new Vector3(1.1f, 0.55f, 0.75f);
        body.GetComponent<Renderer>().sharedMaterial = matCameraNormal;

        GameObject lens = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lens.name = "camera_lens";
        lens.transform.SetParent(root.transform, false);
        lens.transform.localPosition = new Vector3(0f, 0f, 0.52f);
        lens.transform.localScale = Vector3.one * 0.32f;
        lens.GetComponent<Renderer>().sharedMaterial = matReviewed;
        return root;
    }

    private void CreateDroneRig(string id, int index)
    {
        Transform center = EnsureAnchor(id + "_orbit_center", AnchorPosition(CameraU(index), CameraV(index), Mathf.Max(8f, sceneBounds.size.y * 0.35f)));
        GameObject go = dronePrefab != null ? Instantiate(dronePrefab) : CreatePrimitiveDrone(id);
        go.name = id;
        go.transform.SetParent(droneRoot, true);
        go.transform.position = center.position + Vector3.right * 5f;
        SecurityDroneRig rig = go.GetComponent<SecurityDroneRig>() ?? go.AddComponent<SecurityDroneRig>();
        rig.Configure(id, "zone_" + (index + 1).ToString("00", CultureInfo.InvariantCulture), center, Mathf.Max(4f, sceneBounds.size.magnitude * 0.035f));
    }

    private void EnsureLocalRuntimeControllers()
    {
        scenarioController = scenarioController != null ? scenarioController :
            GetComponent<SecurityScenarioController>() ?? FindObjectOfType<SecurityScenarioController>(true);
        if (scenarioController == null) scenarioController = gameObject.AddComponent<SecurityScenarioController>();
        scenarioController.Configure(this, intruderRoot, anchorRoot, sceneBounds, intrusionRobotPrefab, attackRobotPrefab);

        playerController = playerController != null ? playerController :
            GetComponent<SecurityPlayerRobotController>() ?? FindObjectOfType<SecurityPlayerRobotController>(true);
        if (playerController == null) playerController = gameObject.AddComponent<SecurityPlayerRobotController>();
        playerController.Configure(playerRoot, playerRobotPrefab, mapCameraController);
    }

    private GameObject CreatePrimitiveDrone(string id)
    {
        GameObject root = new GameObject(id);
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.name = "drone_body";
        body.transform.SetParent(root.transform, false);
        body.transform.localScale = new Vector3(0.8f, 0.25f, 0.8f);
        body.GetComponent<Renderer>().sharedMaterial = matDrone;

        for (int i = 0; i < 4; i++)
        {
            GameObject rotor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rotor.name = "rotor_" + i;
            rotor.transform.SetParent(root.transform, false);
            float x = i < 2 ? -0.75f : 0.75f;
            float z = i % 2 == 0 ? -0.75f : 0.75f;
            rotor.transform.localPosition = new Vector3(x, 0f, z);
            rotor.transform.localScale = new Vector3(0.7f, 0.04f, 0.12f);
            rotor.GetComponent<Renderer>().sharedMaterial = matDrone;
        }
        return root;
    }

    private GameObject EnsureIntruder()
    {
        Transform existing = intruderRoot.Find("intruder_actor");
        if (existing != null) return existing.gameObject;

        GameObject go = intruderPrefab != null ? Instantiate(intruderPrefab) : GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "intruder_actor";
        go.transform.SetParent(intruderRoot, true);
        go.transform.localScale = Vector3.one * Mathf.Max(1f, sceneBounds.size.magnitude * 0.008f);
        Renderer renderer = go.GetComponentInChildren<Renderer>();
        if (renderer != null) renderer.sharedMaterial = matIntruder;
        return go;
    }

    private void EnterSelectedCctvView()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        if (string.IsNullOrWhiteSpace(selectedCameraId) || !cameras.TryGetValue(selectedCameraId, out SecurityCameraRig rig))
        {
            rig = cameras.Values.FirstOrDefault();
        }
        if (rig == null) return;

        cameraViewBeforeCctv = CameraViewState.Capture(mainCamera);
        hasCameraViewBeforeCctv = true;
        cctvViewActive = true;
        if (mapCameraController != null) mapCameraController.enabled = false;
        UpdateCctvViewCamera();
    }

    private void UpdateCctvViewCamera()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null || string.IsNullOrWhiteSpace(selectedCameraId)) return;
        if (!cameras.TryGetValue(selectedCameraId, out SecurityCameraRig rig) || rig == null) return;

        mainCamera.orthographic = false;
        mainCamera.fieldOfView = cctvViewFieldOfView;
        mainCamera.transform.position = rig.ViewPosition();
        mainCamera.transform.rotation = rig.ViewRotation();
    }

    private void ExitCctvView()
    {
        cctvViewActive = false;
        if (hasCameraViewBeforeCctv)
        {
            cameraViewBeforeCctv.Apply(mainCamera);
            hasCameraViewBeforeCctv = false;
        }

        if (mapCameraController != null)
        {
            mapCameraController.CaptureCurrentCameraView();
            mapCameraController.enabled = enableMapCameraControls;
        }
    }

    public void ToggleCctvQuadView()
    {
        if (cctvQuadViewActive) ExitCctvQuadView();
        else EnterCctvQuadView();
    }

    private void EnterCctvQuadView()
    {
        if (cctvViewActive) ExitCctvView();

        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cameraViewBeforeQuad = CameraViewState.Capture(mainCamera);
            hasCameraViewBeforeQuad = true;
            mainCameraEnabledBeforeQuad = mainCamera.enabled;
            mainCamera.enabled = false;
        }

        if (mapCameraController != null) mapCameraController.enabled = false;

        EnsureCctvQuadCameras();
        cctvQuadViewActive = true;
        foreach (Camera quadCamera in cctvQuadCameras)
        {
            if (quadCamera != null) quadCamera.enabled = true;
        }
        UpdateCctvQuadCameras();
    }

    private void ExitCctvQuadView()
    {
        cctvQuadViewActive = false;
        foreach (Camera quadCamera in cctvQuadCameras)
        {
            if (quadCamera != null) quadCamera.enabled = false;
        }

        if (mainCamera != null)
        {
            mainCamera.enabled = mainCameraEnabledBeforeQuad;
            if (hasCameraViewBeforeQuad)
            {
                cameraViewBeforeQuad.Apply(mainCamera);
                hasCameraViewBeforeQuad = false;
            }
        }

        if (mapCameraController != null)
        {
            mapCameraController.CaptureCurrentCameraView();
            mapCameraController.enabled = enableMapCameraControls;
        }
    }

    private void EnsureCctvQuadCameras()
    {
        while (cctvQuadCameras.Count < 4)
        {
            GameObject go = new GameObject("CCTV_Quad_View_" + (cctvQuadCameras.Count + 1).ToString("00", CultureInfo.InvariantCulture));
            go.transform.SetParent(dashboardBridgeRoot != null ? dashboardBridgeRoot : transform, false);
            Camera camera = go.AddComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = false;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = cctvQuadBackgroundColor;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = Mathf.Max(500f, sceneBounds.size.magnitude * 4f);
            camera.fieldOfView = cctvViewFieldOfView;
            camera.depth = 30f + cctvQuadCameras.Count;
            cctvQuadCameras.Add(camera);
        }
    }

    private void UpdateCctvQuadCameras()
    {
        EnsureCctvQuadCameras();
        Rect[] viewports =
        {
            new Rect(0f, 0.5f, 0.5f, 0.5f),
            new Rect(0.5f, 0.5f, 0.5f, 0.5f),
            new Rect(0f, 0f, 0.5f, 0.5f),
            new Rect(0.5f, 0f, 0.5f, 0.5f),
        };

        for (int i = 0; i < cctvQuadCameras.Count; i++)
        {
            Camera quadCamera = cctvQuadCameras[i];
            if (quadCamera == null) continue;

            string cameraId = CctvQuadCameraId(i);
            SecurityCameraRig rig = null;
            bool hasRig = !string.IsNullOrWhiteSpace(cameraId) && cameras.TryGetValue(cameraId, out rig) && rig != null;
            quadCamera.enabled = cctvQuadViewActive && hasRig;
            quadCamera.rect = viewports[Mathf.Min(i, viewports.Length - 1)];
            quadCamera.fieldOfView = cctvViewFieldOfView;

            if (!hasRig) continue;
            quadCamera.transform.position = rig.ViewPosition();
            quadCamera.transform.rotation = rig.ViewRotation();
        }
    }

    private string CctvQuadCameraId(int index)
    {
        if (cctvQuadCameraIds != null && index >= 0 && index < cctvQuadCameraIds.Length)
        {
            return cctvQuadCameraIds[index];
        }

        return "cam_" + (index + 1).ToString("00", CultureInfo.InvariantCulture);
    }

    private void DrawCctvQuadOverlay()
    {
        Rect[] labels =
        {
            new Rect(quitButtonRect.x + quitButtonRect.width + 14f, 12f, 220f, 28f),
            new Rect(Screen.width * 0.5f + 12f, 12f, 220f, 28f),
            new Rect(12f, Screen.height * 0.5f + 12f, 220f, 28f),
            new Rect(Screen.width * 0.5f + 12f, Screen.height * 0.5f + 12f, 220f, 28f),
        };

        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.Box(new Rect(Screen.width * 0.5f - 1f, 0f, 2f, Screen.height), GUIContent.none);
        GUI.Box(new Rect(0f, Screen.height * 0.5f - 1f, Screen.width, 2f), GUIContent.none);

        for (int i = 0; i < labels.Length; i++)
        {
            string cameraId = CctvQuadCameraId(i);
            string status = CameraStatusFor(cameraId);
            GUI.color = status == "detected"
                ? new Color(1f, 0.12f, 0.08f, 0.86f)
                : status == "reviewed"
                    ? new Color(1f, 0.55f, 0.08f, 0.82f)
                    : new Color(0.02f, 0.08f, 0.12f, 0.72f);
            GUI.Box(labels[i], cameraId.ToUpperInvariant() + "  " + status.ToUpperInvariant());
        }

        GUI.color = Color.white;
    }

    private string CameraStatusFor(string cameraId)
    {
        if (string.IsNullOrWhiteSpace(cameraId)) return "normal";
        foreach (KeyValuePair<string, string> incident in incidentCameraIds)
        {
            if (!string.Equals(incident.Value, cameraId, StringComparison.OrdinalIgnoreCase)) continue;
            string status = incidentStatuses.TryGetValue(incident.Key, out string activeStatus) ? activeStatus : "detected";
            if (status == "detected") return "detected";
            if (status == "reviewed") return "reviewed";
        }
        return "normal";
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

    private static bool WasPressed(KeyCode key)
    {
#if ENABLE_INPUT_SYSTEM
        return Input.GetKeyDown(key);
#else
        return Input.GetKeyDown(key);
#endif
    }

    private void ApplyCursorPolicy()
    {
        if (!forceVisibleCursor) return;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private static int CameraIndex(string cameraId)
    {
        if (string.IsNullOrWhiteSpace(cameraId)) return 0;
        string digits = new string(cameraId.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : 0;
    }

    private static bool IsSurveillanceCameraName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        string normalized = name.ToLowerInvariant();
        return normalized.Contains("surveillance camera") ||
            normalized.Contains("surveiliance camera");
    }

    private static bool HasSurveillanceCameraParent(Transform item)
    {
        Transform current = item.parent;
        while (current != null)
        {
            if (IsSurveillanceCameraName(current.name)) return true;
            current = current.parent;
        }

        return false;
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

    private Transform FindAnchor(string anchorName)
    {
        if (anchorRoot == null || string.IsNullOrWhiteSpace(anchorName)) return null;
        return anchorRoot.Find(anchorName);
    }

    private float CameraU(int index)
    {
        float[] values = { 0.15f, 0.35f, 0.60f, 0.85f, 0.18f, 0.42f, 0.68f, 0.82f };
        return values[index % values.Length];
    }

    private float CameraV(int index)
    {
        float[] values = { 0.18f, 0.16f, 0.20f, 0.18f, 0.82f, 0.78f, 0.82f, 0.76f };
        return values[index % values.Length];
    }

    private Vector3 AnchorPosition(float u, float v, float yOffset)
    {
        Vector3 min = sceneBounds.min;
        Vector3 max = sceneBounds.max;
        return new Vector3(Mathf.Lerp(min.x, max.x, u), sceneBounds.min.y + yOffset, Mathf.Lerp(min.z, max.z, v));
    }

    private void CalculateSceneBounds()
    {
        Renderer[] renderers = FindObjectsOfType<Renderer>(true)
            .Where(item => securityRoot == null || !item.transform.IsChildOf(securityRoot))
            .ToArray();

        if (renderers.Length == 0)
        {
            sceneBounds = new Bounds(Vector3.zero, new Vector3(120f, 20f, 80f));
            return;
        }

        sceneBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) sceneBounds.Encapsulate(renderers[i].bounds);
    }

    private void CreateMaterials()
    {
        matCameraNormal = RuntimeMaterial("Security Camera Normal", new Color(0.08f, 0.09f, 0.11f, 1f));
        matDetected = RuntimeMaterial("Security Detected", new Color(1f, 0.08f, 0.05f, 1f));
        matReviewed = RuntimeMaterial("Security Reviewed", new Color(1f, 0.54f, 0.08f, 1f));
        matDrone = RuntimeMaterial("Security Drone", new Color(0.10f, 0.16f, 0.22f, 1f));
        matIntruder = RuntimeMaterial("Security Intruder", new Color(0.85f, 0.04f, 0.06f, 1f));
        matMarker = RuntimeMaterial("Security Marker", new Color(1f, 0.05f, 0.04f, 1f));
    }

    private Material RuntimeMaterial(string materialName, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ??
            Shader.Find("Universal Render Pipeline/Unlit") ??
            Shader.Find("Standard") ??
            Shader.Find("Diffuse");
        Material material = new Material(shader);
        material.name = materialName;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        else material.color = color;
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 0.35f);
        }
        return material;
    }

    private static string Escape(string value)
    {
        if (value == null) return "";
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");
    }
}
