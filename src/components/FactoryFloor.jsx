import { useState } from 'react';
import { C } from '../tokens';

function incidentTier(inc) {
  if (inc.status === 'confirmed')   return 'confirmed';
  if (inc.status === 'false_alarm') return 'dismissed';
  if (inc.score.eventization_score >= 0.85) return 'critical';
  return 'warning';
}

const TIER = {
  critical:  { dot: '#EF4444', glow: 'rgba(239,68,68,0.22)',   label: 'CRITICAL',  desc: '즉시 대응 필요 (score ≥ 0.85)' },
  warning:   { dot: '#FACC15', glow: 'rgba(250,204,21,0.16)',  label: 'WARNING',   desc: '검토 중 / 중간 위험' },
  confirmed: { dot: '#22C55E', glow: 'rgba(34,197,94,0.12)',   label: 'CONFIRMED', desc: '실제 사건 확정' },
  dismissed: { dot: '#64748B', glow: 'rgba(100,116,139,0.1)', label: 'DISMISSED', desc: '오탐 처리됨' },
};

const ZONES = [
  { id: 'outer_fence_north', x: 28,  y: 12,  w: 604, h: 32,  label: 'OUTER FENCE NORTH', fill: 'none', stroke: C.border2 },
  { id: 'main_area',         x: 68,  y: 58,  w: 490, h: 210, label: '',                  fill: C.bg4,  stroke: C.border  },
  { id: 'parking_west',      x: 8,   y: 90,  w: 54,  h: 110, label: 'PARKING W',         fill: C.bg3,  stroke: C.border  },
  { id: 'server_room_b2',    x: 268, y: 76,  w: 104, h: 72,  label: 'SERVER ROOM B2',    fill: C.bg3,  stroke: C.border2 },
  { id: 'roof_access_c',     x: 374, y: 60,  w: 84,  h: 40,  label: 'ROOF ACCESS C',     fill: C.bg3,  stroke: C.border  },
  { id: 'inner_gate_east',   x: 438, y: 146, w: 94,  h: 52,  label: 'GATE EAST',         fill: C.bg3,  stroke: C.border2 },
  { id: 'warehouse_south',   x: 88,  y: 188, w: 144, h: 68,  label: 'WAREHOUSE SOUTH',   fill: C.bg3,  stroke: C.border2 },
  { id: 'loading_dock_d',    x: 248, y: 228, w: 100, h: 48,  label: 'LOADING DOCK D',    fill: C.bg3,  stroke: C.border  },
];

const PTS = {
  outer_fence_north: { cx: 330, cy: 28  },
  server_room_b2:    { cx: 320, cy: 112 },
  roof_access_c:     { cx: 416, cy: 80  },
  inner_gate_east:   { cx: 485, cy: 172 },
  warehouse_south:   { cx: 160, cy: 222 },
  parking_west:      { cx: 35,  cy: 145 },
  loading_dock_d:    { cx: 298, cy: 252 },
};

const LEGEND = ['critical', 'warning', 'confirmed', 'dismissed'];

export default function FactoryFloor({ incidents, onSelect, compact }) {
  const [hovered, setHovered] = useState(null);

  const byZone = {};
  incidents.forEach((inc) => {
    const zone = inc.location.zone;
    if (!byZone[zone]) byZone[zone] = [];
    byZone[zone].push(inc);
  });

  const tierOrder = { critical: 0, warning: 1, confirmed: 2, dismissed: 3 };
  Object.values(byZone).forEach(arr =>
    arr.sort((a, b) => tierOrder[incidentTier(a)] - tierOrder[incidentTier(b)])
  );

  return (
    <div style={{ width: '100%', height: '100%', display: 'flex', flexDirection: 'column' }}>
      {/* Map */}
      <div style={{ flex: 1, minHeight: 0 }}>
        <svg width="100%" height="100%" viewBox="0 0 660 290" preserveAspectRatio="xMidYMid meet" style={{ display: 'block' }}>
          {/* Grid */}
          {[...Array(10)].map((_, i) => <line key={`h${i}`} x1="0" y1={i * 33} x2="660" y2={i * 33} stroke={C.border} strokeWidth="0.4" strokeDasharray="2,8" />)}
          {[...Array(14)].map((_, i) => <line key={`v${i}`} x1={i * 51} y1="0" x2={i * 51} y2="290" stroke={C.border} strokeWidth="0.4" strokeDasharray="2,8" />)}
          {/* Zones */}
          {ZONES.map((z) => (
            <g key={z.id}>
              <rect x={z.x} y={z.y} width={z.w} height={z.h} fill={z.fill} stroke={z.stroke} strokeWidth="1" rx="2" />
              {z.label && <text x={z.x + 7} y={z.y + 13} fill={C.fg4} fontSize="6.5" fontFamily={C.mono} letterSpacing="0.6">{z.label}</text>}
            </g>
          ))}
          {/* Incident markers */}
          {Object.entries(byZone).map(([zone, incs]) => {
            const pt = PTS[zone];
            if (!pt) return null;
            const top = incs[0];
            const tier = incidentTier(top);
            const t = TIER[tier];
            const isHov = hovered === zone;
            const count = incs.length;
            return (
              <g key={zone} onClick={() => onSelect(top.incident_id)} onMouseEnter={() => setHovered(zone)} onMouseLeave={() => setHovered(null)} style={{ cursor: 'pointer' }}>
                <circle cx={pt.cx} cy={pt.cy} r={isHov ? 28 : 20} fill={t.glow} />
                {tier === 'critical' && (
                  <circle cx={pt.cx} cy={pt.cy} r="7" fill="none" stroke={t.dot} strokeWidth="1.2">
                    <animate attributeName="r" values="7;24" dur="1.5s" repeatCount="indefinite" />
                    <animate attributeName="opacity" values="0.6;0" dur="1.5s" repeatCount="indefinite" />
                  </circle>
                )}
                <circle cx={pt.cx} cy={pt.cy} r="6" fill={t.dot} className={tier === 'critical' ? 'ff-c' : tier === 'warning' ? 'ff-w' : ''} />
                {count > 1 && (
                  <g>
                    <circle cx={pt.cx + 10} cy={pt.cy - 10} r="7" fill={C.bg2} stroke={t.dot} strokeWidth="1" />
                    <text x={pt.cx + 10} y={pt.cy - 6.5} textAnchor="middle" fill={t.dot} fontSize="7" fontFamily={C.mono} fontWeight="500">{count}</text>
                  </g>
                )}
                <text x={pt.cx} y={pt.cy + 19} textAnchor="middle" fill={isHov ? C.fg2 : C.fg4} fontSize="6.5" fontFamily={C.mono}>{zone.replace(/_/g, ' ')}</text>
                {isHov && (
                  <g>
                    <rect x={pt.cx - 54} y={pt.cy - 40} width="108" height="24" rx="3" fill={C.bg2} stroke={t.dot} strokeWidth="0.8" opacity="0.97" />
                    <text x={pt.cx} y={pt.cy - 24} textAnchor="middle" fill={t.dot} fontSize="7.5" fontFamily={C.mono} fontWeight="500">{top.incident_id}</text>
                    <text x={pt.cx} y={pt.cy - 14} textAnchor="middle" fill={C.fg3} fontSize="6.5" fontFamily={C.mono}>{t.label} · score {top.score.eventization_score.toFixed(2)}</text>
                  </g>
                )}
              </g>
            );
          })}
        </svg>
      </div>
      {/* Legend strip */}
      <div style={{ flexShrink: 0, display: 'flex', alignItems: 'center', gap: compact ? 8 : 14, padding: compact ? '5px 10px' : '7px 12px', borderTop: `1px solid ${C.border}`, flexWrap: 'wrap' }}>
        {LEGEND.map((tier) => {
          const t = TIER[tier];
          return (
            <div key={tier} style={{ display: 'flex', alignItems: 'center', gap: 5 }}>
              <div style={{
                width: 8, height: 8, borderRadius: '50%', background: t.dot,
                animation: tier === 'critical' ? 'ff-fast 1.1s ease-in-out infinite' : tier === 'warning' ? 'ff-slow 2.2s ease-in-out infinite' : 'none',
              }} />
              <span style={{ fontFamily: C.mono, fontSize: compact ? 7 : 7.5, color: C.fg3, whiteSpace: 'nowrap' }}>
                <span style={{ color: t.dot }}>{t.label}</span>
                {!compact && <span style={{ color: C.fg4 }}> {t.desc}</span>}
              </span>
            </div>
          );
        })}
        {compact && <span style={{ fontFamily: C.mono, fontSize: 7, color: C.fg4, marginLeft: 'auto' }}>클릭하여 확대</span>}
      </div>
    </div>
  );
}
