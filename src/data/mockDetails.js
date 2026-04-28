// IncidentDetailProjection — follows Dashboard_API_Contract_v2.0 §4-2
export const DETAIL_DB = {
  'INC-2026-001': {
    timeline: [
      { ts: '21:13:58', event: 'person_detected' },
      { ts: '21:14:01', event: 'line_crossed' },
      { ts: '21:14:02', event: 'roi_entered' },
      { ts: '21:14:03', event: 'duration_satisfied' },
      { ts: '21:14:03', event: 'incident_created', hi: true },
    ],
    eventization_basis: {
      line_crossed: true, roi_entered: true,
      duration_sec: 2.3, zone_policy: 'outer_fence_rule_v1',
    },
    source_summary: { source_type: 'clip', source_id: 'clip_intrusion_03', camera_id: 'cam_03' },
    evidence: {
      thumbnail_url: null,
      clip_url: null,
      objects: ['person'],
      track_ids: [12],
    },
    operator: { note: '', reviewed_by: 'operator_01', reviewed_at: null },
    report_summary: { report_available: false, report_id: null, report_generated_at: null },
    status_history_preview: [
      { from: null, to: 'detected', actor: { type: 'system', id: 'eventization_engine' }, at: '2026-04-24T21:14:03+09:00', note: null },
    ],
    history: [
      { who: 'system', action: 'incident_created', detail: 'AI detection score 0.87', ts: '21:14:03' },
      { who: 'operator_01', action: 'viewed', detail: 'panel opened', ts: '21:15:10' },
    ],
    ai_output: null,
  },

  'INC-2026-003': {
    timeline: [
      { ts: '20:11:44', event: 'person_detected' },
      { ts: '20:11:47', event: 'line_crossed' },
      { ts: '20:11:49', event: 'roi_entered' },
      { ts: '20:11:51', event: 'duration_satisfied' },
      { ts: '20:11:51', event: 'incident_created', hi: true },
    ],
    eventization_basis: {
      line_crossed: true, roi_entered: true,
      duration_sec: 3.1, zone_policy: 'warehouse_rule_v2',
    },
    source_summary: { source_type: 'clip', source_id: 'clip_intrusion_12', camera_id: 'cam_12' },
    evidence: {
      thumbnail_url: null,
      clip_url: null,
      objects: ['person'],
      track_ids: [8],
    },
    operator: { note: '현장 확인 완료. 실제 사건으로 판단.', reviewed_by: 'operator_01', reviewed_at: '2026-04-24T20:15:10+09:00' },
    report_summary: { report_available: true, report_id: 'AIR-2026-003', report_generated_at: '2026-04-24T20:16:00+09:00' },
    status_history_preview: [
      { from: null, to: 'detected', actor: { type: 'system', id: 'eventization_engine' }, at: '2026-04-24T20:11:51+09:00', note: null },
      { from: 'detected', to: 'reviewed', actor: { type: 'operator', id: 'operator_01' }, at: '2026-04-24T20:13:22+09:00', note: null },
      { from: 'reviewed', to: 'confirmed', actor: { type: 'operator', id: 'operator_01' }, at: '2026-04-24T20:15:10+09:00', note: '현장 확인 완료. 실제 사건으로 판단.' },
    ],
    history: [
      { who: 'system', action: 'incident_created', detail: 'AI detection score 0.94', ts: '20:11:51' },
      { who: 'operator_01', action: 'status → reviewed', detail: 'panel opened', ts: '20:13:22' },
      { who: 'operator_01', action: 'status → confirmed', detail: '현장 확인 완료.', ts: '20:15:10' },
      { who: 'ai_assist', action: 'report_generated', detail: 'intrusion_assist v2', ts: '20:16:00' },
    ],
    ai_output: {
      summary: {
        lines: [
          '창고 남측 구역에서 confirmed intrusion incident가 확인되었습니다.',
          '사람 객체가 경계선을 통과한 뒤 보호 구역에 진입했고 사건화 조건이 충족되었습니다.',
          '운영자는 현장 확인과 후속 대응 여부를 최종 판단해야 합니다.',
        ],
      },
      checklist: {
        items: ['창고 남측 CCTV 클립 재확인', '현장 인력에게 상황 전파', '인근 카메라와 출입기록 교차 확인'],
      },
      report_draft: {
        markdown: '## 1. 사건 개요\n- 사건 ID: INC-2026-003\n- 발생: 2026-04-24 20:12\n- 위치: factory_B / warehouse_south\n- 유형: intrusion\n\n## 2. 상세 내용\n사람 객체가 창고 남측 라인을 통과한 뒤 보호 구역에 진입하여 confirmed intrusion으로 판정됨.\n\n## 3. 조치 사항\n- 창고 CCTV 추가 확인\n- 현장 인력 즉시 파견',
      },
      meta: { prompt_family: 'intrusion_assist', prompt_version: 'v2_structured', generated_at: '2026-04-24T20:16:00+09:00' },
    },
  },
};

const FALLBACK_DETAIL = {
  timeline: [
    { ts: '--:--:--', event: 'person_detected' },
    { ts: '--:--:--', event: 'line_crossed' },
    { ts: '--:--:--', event: 'incident_created', hi: true },
  ],
  eventization_basis: { line_crossed: true, roi_entered: true, duration_sec: 2.1, zone_policy: 'default_rule_v1' },
  source_summary: { source_type: 'clip', source_id: 'clip_unknown', camera_id: 'cam_00' },
  evidence: { thumbnail_url: null, clip_url: null, objects: ['person'], track_ids: [99] },
  operator: { note: '', reviewed_by: 'operator_01', reviewed_at: null },
  report_summary: { report_available: false, report_id: null, report_generated_at: null },
  status_history_preview: [
    { from: null, to: 'detected', actor: { type: 'system', id: 'eventization_engine' }, at: new Date().toISOString(), note: null },
  ],
  history: [{ who: 'system', action: 'incident_created', detail: 'AI detection', ts: '--:--:--' }],
  ai_output: null,
};

export function getDetail(incident_id) {
  return DETAIL_DB[incident_id] || { ...FALLBACK_DETAIL };
}
