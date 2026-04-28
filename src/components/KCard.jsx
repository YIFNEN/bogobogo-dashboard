import { C, STATUS, SC } from '../tokens';
import { relTime } from '../utils/relTime';
import Badge from './shared/Badge';

export default function KCard({ inc, onSelect, tweaks }) {
  const score = inc.score.eventization_score;
  const scoreColor = SC(score);

  return (
    <div
      draggable
      onDragStart={e => {
        e.dataTransfer.setData('incidentId', inc.incident_id);
        e.dataTransfer.effectAllowed = 'move';
      }}
      onClick={() => onSelect(inc.incident_id)}
      style={{
        background: C.bg3,
        border: `1px solid ${C.border}`,
        borderLeft: inc.isNew ? `2px solid ${C.fg1}` : `1px solid ${C.border}`,
        borderRadius: C.R,
        padding: '10px 12px',
        cursor: 'pointer',
        marginBottom: 6,
        transition: 'border-color 120ms ease, opacity 120ms ease',
        animation: 'fadeIn 200ms ease',
      }}
      onMouseEnter={e => { e.currentTarget.style.borderColor = C.border2; }}
      onMouseLeave={e => { e.currentTarget.style.borderColor = inc.isNew ? C.fg1 : C.border; }}
    >
      {/* Top row: ID + NEW badge + event type */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 6, marginBottom: 6 }}>
        <span style={{ fontFamily: C.mono, fontSize: 9, color: C.fg4, flex: 1, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{inc.incident_id}</span>
        {inc.isNew && tweaks.showNewBadge && (
          <span style={{ fontFamily: C.mono, fontSize: 7, color: C.fg1, background: 'rgba(255,255,255,0.12)', border: `1px solid rgba(255,255,255,0.25)`, padding: '1px 5px', borderRadius: 3, flexShrink: 0 }}>NEW</span>
        )}
        <span style={{ fontFamily: C.mono, fontSize: 7.5, color: C.orange, background: C.orangeD, border: `1px solid ${C.orangeB}`, padding: '1px 6px', borderRadius: 3, textTransform: 'uppercase', letterSpacing: '0.5px', flexShrink: 0 }}>{inc.event_type}</span>
      </div>

      {/* Zone */}
      <div style={{ fontFamily: C.sans, fontSize: 11.5, color: C.fg1, marginBottom: 5, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
        {inc.location.zone.replace(/_/g, ' ')}
      </div>

      {/* Meta row: site + cam + time */}
      <div style={{ display: 'flex', gap: 8, marginBottom: 7, flexWrap: 'wrap' }}>
        <span style={{ fontFamily: C.mono, fontSize: 8, color: 'rgba(242,242,244,0.55)' }}>{inc.location.site}</span>
        <span style={{ fontFamily: C.mono, fontSize: 8, color: 'rgba(242,242,244,0.55)' }}>{inc.location.camera_id}</span>
        <span style={{ fontFamily: C.mono, fontSize: 8, color: 'rgba(242,242,244,0.45)', marginLeft: 'auto' }}>{relTime(inc.detected_at)}</span>
      </div>

      {/* Score + status */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
          <span style={{ fontFamily: C.mono, fontSize: 8, color: C.fg4, textTransform: 'uppercase', letterSpacing: '0.8px' }}>위협도</span>
          <span style={{ fontFamily: C.mono, fontSize: 13, color: scoreColor, letterSpacing: '-0.5px', fontWeight: 500 }}>{Math.round(score * 100)}%</span>
        </div>
        <Badge status={inc.status} sm />
      </div>
    </div>
  );
}
