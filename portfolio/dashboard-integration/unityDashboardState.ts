import { MOCK_CAMERAS, type MockCamera, visualCameraId } from '../../mocks/mockVisuals';
import type {
  IncidentDetail,
  IncidentListItem,
  IncidentStatus,
  RejectedCandidate,
  SourceType,
  UnityDashboardCamera,
  UnityDashboardIncident,
  UnityDashboardState,
  UnityRejectedCandidate,
  UnityVisualStatus,
} from '../../types';

const ACTIVE_STATUSES = new Set<IncidentStatus>(['detected', 'reviewed']);

const STATUS_PRIORITY: Record<IncidentStatus, number> = {
  detected: 0,
  reviewed: 1,
  confirmed: 2,
  false_alarm: 3,
};

export function isUnityActiveStatus(status: IncidentStatus): boolean {
  return ACTIVE_STATUSES.has(status);
}

export function toUnityVisualCameraId(cameraId: string): string {
  return visualCameraId(cameraId);
}

export function toUnityVisualStatus(status?: IncidentStatus): UnityVisualStatus {
  if (status === 'detected' || status === 'reviewed') return status;
  if (status === 'confirmed' || status === 'false_alarm') return 'normal';
  return 'normal';
}

interface BuildUnityDashboardStateOptions {
  timestamp?: string;
  selectedIncidentId?: string;
  details?: Record<string, IncidentDetail>;
  rejectedCandidates?: RejectedCandidate[];
  cameras?: MockCamera[];
}

export function buildUnityDashboardState(
  incidents: IncidentListItem[],
  options: BuildUnityDashboardStateOptions = {},
): UnityDashboardState {
  const cameras = options.cameras ?? MOCK_CAMERAS;
  const visualCameraMap = new Map<string, MockCamera>();

  cameras.forEach(camera => {
    const id = toUnityVisualCameraId(camera.id);
    if (!visualCameraMap.has(id)) {
      visualCameraMap.set(id, camera.id === id ? camera : { ...camera, id });
    }
  });

  const activeIncidents = incidents
    .filter(incident => isUnityActiveStatus(incident.status))
    .sort((a, b) => STATUS_PRIORITY[a.status] - STATUS_PRIORITY[b.status]);

  const statusByCamera = new Map<string, IncidentStatus>();
  activeIncidents.forEach(incident => {
    const visualId = toUnityVisualCameraId(incident.location.camera_id);
    if (!statusByCamera.has(visualId)) statusByCamera.set(visualId, incident.status);
  });

  const unityIncidents: UnityDashboardIncident[] = activeIncidents.map(incident => {
    const sourceCameraId = incident.location.camera_id;
    const cameraId = toUnityVisualCameraId(sourceCameraId);
    const visualCamera = visualCameraMap.get(cameraId);
    const detail = options.details?.[incident.incident_id];
    const sourceType = detail?.source_summary.source_type ?? inferSourceType(incident.incident_id, sourceCameraId);

    return {
      incident_id: incident.incident_id,
      event_type: incident.event_type,
      camera_id: cameraId,
      ...(cameraId !== sourceCameraId ? { source_camera_id: sourceCameraId } : {}),
      zone_id: visualCamera?.zone ?? incident.location.zone,
      status: incident.status,
      severity: incident.score.eventization_score,
      detected_at: incident.detected_at,
      source_type: sourceType,
    };
  });

  const unityCameras: UnityDashboardCamera[] = Array.from(visualCameraMap.values()).map(camera => ({
    camera_id: camera.id,
    zone_id: camera.zone,
    status: toUnityVisualStatus(statusByCamera.get(camera.id)),
  }));

  const rejectedCandidates = (options.rejectedCandidates ?? []).map(candidate => mapRejectedCandidate(candidate, visualCameraMap));

  return {
    schema_version: 'unity_dashboard_state.v1',
    site_id: 'factory_A',
    timestamp: options.timestamp ?? new Date().toISOString(),
    incidents: unityIncidents,
    cameras: unityCameras,
    ...(options.selectedIncidentId ? { selected_incident_id: options.selectedIncidentId } : {}),
    ...(rejectedCandidates.length > 0 ? { rejected_candidates: rejectedCandidates } : {}),
  };
}

function inferSourceType(incidentId: string, cameraId: string): SourceType {
  if (incidentId.includes('UNITY') || cameraId.startsWith('sim_')) return 'simulator';
  return 'clip';
}

function mapRejectedCandidate(
  candidate: RejectedCandidate,
  visualCameraMap: Map<string, MockCamera>,
): UnityRejectedCandidate {
  const sourceCameraId = candidate.camera_id;
  const cameraId = toUnityVisualCameraId(sourceCameraId);
  const visualCamera = visualCameraMap.get(cameraId);

  return {
    candidate_id: candidate.candidate_id,
    camera_id: cameraId,
    ...(cameraId !== sourceCameraId ? { source_camera_id: sourceCameraId } : {}),
    zone_id: visualCamera?.zone ?? 'outer_fence_north',
    rejected_reason: candidate.rejected_reason,
  };
}
