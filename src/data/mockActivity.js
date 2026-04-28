import { C } from '../tokens';

export const ACTIVITY = [
  { icon: 'alert-triangle', color: '#EF4444', title: 'INC-2026-001 detected',      sub: 'outer_fence_north · 3분 전',  severity: 0.87, link: 'INC-2026-001' },
  { icon: 'alert-triangle', color: '#EF4444', title: 'INC-2026-005 detected',      sub: 'server_room_b2 · 10분 전',    severity: 0.79, link: 'INC-2026-005' },
  { icon: 'check-circle',   color: '#22C55E', title: 'INC-2026-003 confirmed',     sub: 'warehouse_south · 38분 전',   severity: 0.94, link: 'INC-2026-003' },
  { icon: 'file-text',      color: C.fg3,     title: 'AI report — INC-2026-003',   sub: 'intrusion_assist v2 · 42분 전', severity: 0, link: null },
  { icon: 'x-circle',       color: '#EF4444', title: 'INC-2026-004 false_alarm',   sub: 'parking_west · 1h 19분 전',   severity: 0.61, link: 'INC-2026-004' },
  { icon: 'file-text',      color: C.fg4,     title: 'Daily report exported',      sub: 'operator_01 · 2h 전',         severity: 0, link: null },
  { icon: 'eye',            color: '#FACC15', title: 'INC-2026-006 reviewed',      sub: 'loading_dock_d · 1h 34분 전', severity: 0.68, link: 'INC-2026-006' },
  { icon: 'shield-alert',   color: C.fg4,     title: 'System health check passed', sub: 'all cameras · 3h 전',         severity: 0, link: null },
];

export const TREND = {
  detected:    [2, 4, 3, 5, 7, 6, 8],
  reviewed:    [1, 2, 3, 2, 4, 3, 5],
  confirmed:   [1, 1, 2, 1, 3, 2, 2],
  false_alarm: [0, 1, 0, 1, 0, 1, 1],
};

export const DAILY = [3, 5, 4, 7, 6, 8, 7];

export const AI_REPORTS_LIST = [
  { id: 'AIR-2026-003', incident_id: 'INC-2026-003', zone: 'warehouse_south', time: '38분 전', confidence: 0.94 },
  { id: 'AIR-2026-001', incident_id: 'INC-2026-001', zone: 'outer_fence_north', time: '2h 전', confidence: 0.87 },
  { id: 'AIR-2026-002', incident_id: 'INC-2026-002', zone: 'inner_gate_east', time: '1d 전', confidence: 0.72 },
];
