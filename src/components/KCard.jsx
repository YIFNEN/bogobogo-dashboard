import { useState } from 'react';
import { C, SC } from '../tokens';

export default function KCard({ inc, onSelect, tweaks }) {
  const [hov, setHov] = useState(false);
  const [isDragging, setIsDragging] = useState(false);
  const score = inc.score.eventization_score;
  const scoreColor = SC(score);
  const pct = Math.round(score * 100);
  const time = new Date(inc.detected_at).toLocaleTimeString('ko-KR', { hour: '2-digit', minute: '2-digit' });

  return (
    <div
      draggable
      onDragStart={e => {
        e.dataTransfer.setData('incidentId', inc.incident_id);
        e.dataTransfer.effectAllowed = 'move';
        setIsDragging(true);
      }}
      onDragEnd={() => setIsDragging(false)}
      onClick={() => onSelect(inc.incident_id)}
      onMouseEnter={() => setHov(true)}
      onMouseLeave={() => setHov(false)}
      style={{
        background: hov ? C.bg4 : C.bg3,
        border: `1px solid ${hov ? C.border2 : C.border}`,
        borderLeft: inc.isNew ? `2px solid ${C.orange}` : `1px solid ${hov ? C.border2 : C.border}`,
        borderRadius: C.R,
        padding: '10px 11px',
        cursor: 'grab',
        marginBottom: 5,
        transition: 'all 120ms ease',
        animation: 'fadeIn 250ms ease',
        opacity: isDragging ? 0.4 : 1,
      }}
    >
      {/* Row 1: ID + event_type badge + NEW badge */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 5 }}>
        <span style={{ fontFamily: C.mono, fontSize: 9, color: C.fg3 }}>{inc.incident_id}</span>
        <div style={{ display: 'flex', gap: 3 }}>
          <span style={{ fontFamily: C.mono, fontSize: 7, color: C.fg4, background: C.bg4, border: `1px solid ${C.border}`, padding: '1px 5px', borderRadius: 3, textTransform: 'uppercase' }}>{inc.event_type}</span>
          {inc.isNew && tweaks?.showNewBadge && (
            <span style={{ fontFamily: C.mono, fontSize: 7, color: C.orange, border: `1px solid ${C.orangeB}`, padding: '1px 5px', borderRadius: 3 }}>NEW</span>
          )}
        </div>
      </div>

      {/* Row 2: Zone */}
      <div style={{ fontFamily: C.sans, fontSize: 12.5, color: C.fg1, marginBottom: 6, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
        {inc.location.zone.replace(/_/g, ' ')}
      </div>

      {/* Row 3: cam · site + time */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 7 }}>
        <span style={{ fontFamily: C.mono, fontSize: 9, color: 'rgba(242,242,244,0.55)' }}>{inc.location.camera_id} · {inc.location.site}</span>
        <span style={{ fontFamily: C.mono, fontSize: 9, color: 'rgba(242,242,244,0.5)' }}>{time}</span>
      </div>

      {/* Row 4: 위협도 + operator */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 5 }}>
          <span style={{ fontFamily: C.mono, fontSize: 8, color: C.fg4 }}>위협도</span>
          <span style={{ fontFamily: C.mono, fontSize: 13, color: scoreColor, letterSpacing: '-0.5px' }}>{pct}%</span>
        </div>
        <span style={{ fontFamily: C.mono, fontSize: 8.5, color: 'rgba(242,242,244,0.45)' }}>{inc.op}</span>
      </div>
    </div>
  );
}
