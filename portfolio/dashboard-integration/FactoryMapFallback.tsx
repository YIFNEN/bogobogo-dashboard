import CctvFrame from '../../components/shared/CctvFrame';
import StatusIcon from '../../components/shared/StatusIcon';
import { MOCK_CAMERAS, getCameraVisual, visualCameraId } from '../../mocks/mockVisuals';
import { C } from '../../styles/tokens';
import type { IncidentListItem, IncidentStatus, UnityDashboardState, UnityVisualStatus } from '../../types';
import { cameraCode } from '../../utils/dashboardLabels';

const physicalCameras = MOCK_CAMERAS.filter(camera => visualCameraId(camera.id) === camera.id);

function visualStatus(status: UnityVisualStatus | undefined): { label: string; color: string; border: string } {
  if (status === 'detected') return { label: 'ALERT', color: C.red, border: C.red };
  if (status === 'reviewed') return { label: 'REVIEW', color: C.orange, border: C.orange };
  return { label: 'NORMAL', color: C.green, border: '#bbf7d0' };
}

function incidentByVisualCamera(incidents: IncidentListItem[]) {
  const priority: Record<IncidentStatus, number> = { detected: 0, reviewed: 1, confirmed: 2, false_alarm: 3 };
  const map = new Map<string, IncidentListItem>();
  [...incidents]
    .sort((a, b) => priority[a.status] - priority[b.status])
    .forEach(incident => {
      const cameraId = visualCameraId(incident.location.camera_id);
      if (!map.has(cameraId)) map.set(cameraId, incident);
    });
  return map;
}

interface FactoryMapFallbackProps {
  state: UnityDashboardState;
  incidents: IncidentListItem[];
  mapZoom: number;
  onIncidentSelect: (incidentId: string) => void;
}

export default function FactoryMapFallback({ state, incidents, mapZoom, onIncidentSelect }: FactoryMapFallbackProps) {
  const cameraStatuses = new Map(state.cameras.map(camera => [camera.camera_id, camera.status]));
  const incidentLookup = incidentByVisualCamera(incidents);

  return (
    <div className="factory-map-fallback" aria-label="Factory map">
      <div className="factory-map-fallback-stage" style={{ transform: `translateY(-50%) scale(${mapZoom})` }}>
        <div className="factory-map-ground">
          <div className="factory-road road-main" />
          <div className="factory-road road-dock" />
          <div className="factory-road road-patrol" />
          <div className="factory-building building-production">
            <div className="factory-machine machine-molding" />
            <div className="factory-machine machine-conveyor machine-conveyor-a" />
            <div className="factory-machine machine-conveyor machine-conveyor-b" />
            <div className="factory-safety-line safety-molding" />
          </div>
          <div className="factory-building building-warehouse">
            <div className="factory-rack rack-a" />
            <div className="factory-rack rack-b" />
          </div>
          <div className="factory-building building-office" />
          <div className="factory-dock-bay" />
          <div className="factory-gate" />
          <div className="factory-fence fence-north" />
          <div className="factory-fence fence-east" />
          <div className="factory-fence fence-south" />
          <div className="factory-fence fence-west" />
          <div className="factory-vehicle forklift" />
          <div className="factory-vehicle truck" />
          <div className="factory-worker worker-a" />
          <div className="factory-worker worker-b" />
          <div className="factory-worker worker-c" />
        </div>

        {physicalCameras.map(camera => {
          const related = incidentLookup.get(camera.id);
          const status = visualStatus(cameraStatuses.get(camera.id));
          return (
            <button
              key={camera.id}
              className="map-camera-marker unity-map-camera-marker"
              onClick={() => (related ? onIncidentSelect(related.incident_id) : undefined)}
              style={{
                left: `${camera.x}%`,
                top: `${camera.y}%`,
                borderColor: status.border,
              }}
            >
              <div className="map-camera-thumb">
                <CctvFrame scene={camera.scene} imageUrl={camera.imageUrl} dimmed={status.color === C.red} />
              </div>
              <div className="map-camera-caption">
                <span className="map-camera-dot" style={{ background: status.color }} />
                <span className="map-camera-label">{camera.label}</span>
                <span className="map-camera-status" style={{ background: status.color }}>{status.label}</span>
              </div>
            </button>
          );
        })}

        {state.incidents.map(incident => {
          const camera = getCameraVisual(incident.camera_id);
          return (
            <button
              key={incident.incident_id}
              className={`unity-incident-pin ${incident.status}`}
              style={{ left: `${camera.x}%`, top: `${Math.max(6, camera.y - 8)}%` }}
              onClick={() => onIncidentSelect(incident.incident_id)}
              aria-label={incident.incident_id}
            >
              <StatusIcon status={incident.status} size={19} color="#fff" />
              <span>{cameraCode(camera.id)}</span>
            </button>
          );
        })}
      </div>
    </div>
  );
}
