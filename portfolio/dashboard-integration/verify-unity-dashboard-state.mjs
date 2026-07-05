import assert from 'node:assert/strict';
import { mkdtempSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { pathToFileURL } from 'node:url';
import { build } from 'esbuild';

const tempDir = mkdtempSync(join(tmpdir(), 'bogobogo-unity-state-'));
const outfile = join(tempDir, 'unityDashboardState.mjs');

try {
  await build({
    entryPoints: [join(process.cwd(), 'src/features/factory_map/unityDashboardState.ts')],
    outfile,
    bundle: true,
    platform: 'browser',
    format: 'esm',
    logLevel: 'silent',
  });

  const { buildUnityDashboardState, toUnityVisualCameraId } = await import(pathToFileURL(outfile).href);

  assert.equal(toUnityVisualCameraId('sim_cam_01'), 'cam_03');
  assert.equal(toUnityVisualCameraId('cam_19'), 'cam_05');

  const timestamp = '2026-05-14T09:00:00+09:00';
  const state = buildUnityDashboardState([
    {
      incident_id: 'INC-UNITY-SIM-001',
      event_type: 'intrusion',
      status: 'detected',
      detected_at: timestamp,
      location: { site: 'factory_A', zone: 'outer_fence_north', camera_id: 'sim_cam_01' },
      score: { eventization_score: 0.82 },
      thumbnail_url: null,
      isNew: true,
      op: 'operator_01',
    },
    {
      incident_id: 'INC-2026-003',
      event_type: 'intrusion',
      status: 'reviewed',
      detected_at: timestamp,
      location: { site: 'factory_A', zone: 'restricted_corridor_a', camera_id: 'cam_19' },
      score: { eventization_score: 0.77 },
      thumbnail_url: null,
      isNew: false,
      op: 'operator_01',
    },
    {
      incident_id: 'INC-2026-DONE',
      event_type: 'intrusion',
      status: 'confirmed',
      detected_at: timestamp,
      location: { site: 'factory_A', zone: 'loading_dock_east', camera_id: 'cam_04' },
      score: { eventization_score: 0.9 },
      thumbnail_url: null,
      isNew: false,
      op: 'operator_01',
    },
  ], {
    timestamp,
    selectedIncidentId: 'INC-UNITY-SIM-001',
    details: {
      'INC-UNITY-SIM-001': { source_summary: { source_type: 'simulator', source_id: 'sim_intrusion_01', camera_id: 'sim_cam_01' } },
      'INC-2026-003': { source_summary: { source_type: 'clip', source_id: 'clip_intrusion_03', camera_id: 'cam_19' } },
    },
    rejectedCandidates: [
      {
        candidate_id: 'cand_2026_002',
        source_id: 'clip_hardneg_02',
        camera_id: 'sim_cam_01',
        track_id: 7,
        frame_index: 91,
        candidate_type: 'intrusion_candidate',
        zone_context: { line_crossed: true, roi_entered: true },
        rejected_reason: 'duration_not_satisfied',
        timestamp,
      },
    ],
  });

  assert.equal(state.schema_version, 'unity_dashboard_state.v1');
  assert.equal(state.site_id, 'factory_A');
  assert.equal(state.timestamp, timestamp);
  assert.equal(state.selected_incident_id, 'INC-UNITY-SIM-001');
  assert.equal(state.incidents.length, 2, 'confirmed/false_alarm incidents should not create Unity pins');

  const simIncident = state.incidents.find(item => item.incident_id === 'INC-UNITY-SIM-001');
  assert.equal(simIncident.camera_id, 'cam_03');
  assert.equal(simIncident.source_camera_id, 'sim_cam_01');
  assert.equal(simIncident.zone_id, 'outer_fence_north');
  assert.equal(simIncident.source_type, 'simulator');

  const legacyIncident = state.incidents.find(item => item.incident_id === 'INC-2026-003');
  assert.equal(legacyIncident.camera_id, 'cam_05');
  assert.equal(legacyIncident.source_camera_id, 'cam_19');
  assert.equal(legacyIncident.zone_id, 'restricted_corridor_a');

  assert.equal(state.cameras.find(item => item.camera_id === 'cam_03')?.status, 'detected');
  assert.equal(state.cameras.find(item => item.camera_id === 'cam_05')?.status, 'reviewed');
  assert.equal(state.cameras.find(item => item.camera_id === 'cam_04')?.status, 'normal');
  assert.equal(state.rejected_candidates?.[0]?.camera_id, 'cam_03');
  assert.equal(state.rejected_candidates?.[0]?.source_camera_id, 'sim_cam_01');

  console.log('Unity dashboard state mapper OK');
} finally {
  rmSync(tempDir, { recursive: true, force: true });
}
