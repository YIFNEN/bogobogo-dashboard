import { useState } from 'react';
import { C, STATUS } from '../tokens';
import Icon from './shared/Icon';
import KCard from './KCard';

const COLS = ['detected', 'reviewed', 'confirmed', 'false_alarm'];

export default function Kanban({ incidents, onSelect, statusFilter, setStatusFilter, onStatusChange, tweaks }) {
  const [dragId, setDragId] = useState(null);
  const [dragOver, setDragOver] = useState(null);

  const handleDragStart = (e, id) => {
    setDragId(id);
    e.dataTransfer.effectAllowed = 'move';
  };

  const handleDragOver = (e, col) => {
    e.preventDefault();
    e.dataTransfer.dropEffect = 'move';
    setDragOver(col);
  };

  const handleDrop = (e, col) => {
    e.preventDefault();
    const id = e.dataTransfer.getData('incidentId') || dragId;
    if (id) {
      const inc = incidents.find(i => i.incident_id === id);
      if (inc && inc.status !== col) {
        if (col === 'false_alarm') {
          const note = window.prompt('FALSE ALARM 처리 근거를 입력하세요 (필수):');
          if (!note || !note.trim()) { setDragId(null); setDragOver(null); return; }
          onStatusChange(id, col, note);
        } else {
          onStatusChange(id, col, '');
        }
      }
    }
    setDragId(null);
    setDragOver(null);
  };

  const handleDragEnd = () => { setDragId(null); setDragOver(null); };

  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>
      {/* Header */}
      <div style={{ padding: '11px 20px 9px', borderBottom: `1px solid ${C.border}`, flexShrink: 0, display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          <span style={{ fontFamily: C.sans, fontSize: 15, color: C.fg1 }}>Incidents</span>
          <div style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
            <span style={{ width: 5, height: 5, borderRadius: '50%', background: '#22C55E', display: 'inline-block', boxShadow: '0 0 5px rgba(34,197,94,0.5)', animation: 'pulse 2s infinite' }} />
            <span style={{ fontFamily: C.mono, fontSize: 7.5, color: C.fg4, letterSpacing: '1.2px' }}>LIVE</span>
          </div>
        </div>
        <div style={{ display: 'flex', gap: 8 }}>
          <span style={{ fontFamily: C.mono, fontSize: 7.5, color: C.fg4, alignSelf: 'center' }}>드래그로 상태 변경</span>
          <button style={{ background: 'transparent', border: `1px solid ${C.border2}`, borderRadius: '999px', padding: '4px 10px', fontFamily: C.mono, fontSize: 8, color: C.fg3, letterSpacing: '0.8px', textTransform: 'uppercase', display: 'flex', alignItems: 'center', gap: 4, cursor: 'pointer' }}>
            <Icon name="download" size={9} color={C.fg4} /> EXPORT
          </button>
        </div>
      </div>

      {/* Filter tabs */}
      <div style={{ display: 'flex', borderBottom: `1px solid ${C.border}`, padding: '0 20px', flexShrink: 0 }}>
        {['all', ...COLS].map(f => {
          const cnt = f === 'all' ? incidents.length : incidents.filter(i => i.status === f).length;
          const active = statusFilter === f;
          const dot = f !== 'all' ? STATUS[f].dot : C.fg2;
          return (
            <button
              key={f}
              onClick={() => setStatusFilter(f)}
              style={{ padding: '7px 10px', background: 'transparent', border: 'none', cursor: 'pointer', borderBottom: `2px solid ${active ? dot : 'transparent'}`, color: active ? C.fg1 : C.fg4, fontFamily: C.mono, fontSize: 8, letterSpacing: '1px', textTransform: 'uppercase', transition: 'all 150ms ease', marginRight: 2 }}
            >
              {f === 'all' ? 'ALL' : STATUS[f].label}
              <span style={{ marginLeft: 4, opacity: 0.5 }}>({cnt})</span>
            </button>
          );
        })}
      </div>

      {/* Columns */}
      <div style={{ flex: 1, display: 'flex', gap: 0, overflow: 'hidden' }}>
        {COLS.map((col, ci) => {
          const m = STATUS[col];
          const cards = incidents.filter(i => i.status === col && (statusFilter === 'all' || statusFilter === col));
          const all = incidents.filter(i => i.status === col);
          const isDragTarget = dragOver === col;
          return (
            <div
              key={col}
              onDragOver={e => handleDragOver(e, col)}
              onDrop={e => handleDrop(e, col)}
              onDragLeave={() => setDragOver(null)}
              style={{
                flex: 1, display: 'flex', flexDirection: 'column', overflow: 'hidden',
                borderRight: ci < COLS.length - 1 ? `1px solid ${C.border}` : 'none',
                background: isDragTarget ? `rgba(232,98,42,0.04)` : 'transparent',
                transition: 'background 150ms ease',
              }}
            >
              {/* Column header */}
              <div style={{ padding: '10px 12px', borderBottom: `1px solid ${isDragTarget ? C.orangeB : C.border}`, flexShrink: 0, transition: 'border-color 150ms ease' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: 6, marginBottom: 4 }}>
                  <span style={{ width: 5, height: 5, borderRadius: '50%', background: m.dot, display: 'inline-block', flexShrink: 0 }} />
                  <span style={{ fontFamily: C.mono, fontSize: 8, color: m.color, letterSpacing: '1px', flex: 1, textTransform: 'uppercase' }}>{m.label}</span>
                  <span style={{ fontFamily: C.mono, fontSize: 8, color: C.fg4 }}>{all.length}</span>
                </div>
                <div style={{ display: 'flex', alignItems: 'baseline', gap: 6 }}>
                  <div style={{ fontFamily: C.mono, fontSize: 22, color: C.fg1, letterSpacing: '-1.5px' }}>{all.length}</div>
                  <div style={{ fontFamily: C.mono, fontSize: 8, color: C.fg4 }}>
                    {(col === 'detected' || col === 'reviewed') && all.length > 0 &&
                      `avg ${(all.reduce((a, b) => a + b.score.eventization_score, 0) / all.length).toFixed(2)}`}
                    {col === 'confirmed' && `${Math.round(all.length / Math.max(incidents.length, 1) * 100)}% total`}
                    {col === 'false_alarm' && `${Math.round(all.length / Math.max(incidents.length, 1) * 100)}% false`}
                  </div>
                </div>
              </div>

              {/* Cards */}
              <div style={{ flex: 1, overflowY: 'auto', padding: '8px 10px' }}>
                {cards.length === 0
                  ? <div style={{ border: `1px dashed ${isDragTarget ? C.orangeB : C.border}`, padding: '20px 10px', fontFamily: C.mono, fontSize: 8.5, color: isDragTarget ? C.orange : C.fg4, textAlign: 'center', letterSpacing: '0.5px', borderRadius: C.R, marginTop: 4, transition: 'all 150ms ease' }}>
                      {isDragTarget ? '여기에 놓기' : 'NO INCIDENTS'}
                    </div>
                  : cards.map(inc => (
                    <KCard
                      key={inc.incident_id}
                      inc={inc}
                      onSelect={id => { onSelect(id); }}
                      tweaks={tweaks}
                    />
                  ))
                }
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
