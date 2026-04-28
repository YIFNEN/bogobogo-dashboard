import { useState, useEffect } from 'react';
import { C, STATUS, SC } from '../tokens';
import { relTime } from '../utils/relTime';
import { getDetail } from '../data/mockDetails';
import Icon from './shared/Icon';
import Badge from './shared/Badge';
import SecLabel from './shared/SecLabel';
import FAModal from './FAModal';

/* ── FindingsRadar ── */
function FindingsRadar({ size = 150 }) {
  const cx = size / 2, cy = size / 2, maxR = size * 0.41, AXES = 6;
  const pt = (i, v) => {
    const a = (i / AXES) * 2 * Math.PI - Math.PI / 2;
    return [cx + Math.cos(a) * v * maxR, cy + Math.sin(a) * v * maxR];
  };
  const poly = (data) => data.map((v, i) => pt(i, v).join(',')).join(' ');
  const d1 = [0.75, 0.55, 0.82, 0.45, 0.68, 0.38];
  const d2 = [0.42, 0.72, 0.35, 0.65, 0.52, 0.80];
  const d3 = [0.62, 0.38, 0.70, 0.78, 0.45, 0.55];
  return (
    <svg width={size} height={size} style={{ display: 'block' }}>
      <defs>
        <radialGradient id="rfill1" cx="50%" cy="50%" r="50%"><stop offset="0%" stopColor={C.orange} stopOpacity="0.35" /><stop offset="100%" stopColor={C.orange} stopOpacity="0.05" /></radialGradient>
        <radialGradient id="rfill2" cx="50%" cy="50%" r="50%"><stop offset="0%" stopColor="#EF4444" stopOpacity="0.3" /><stop offset="100%" stopColor="#EF4444" stopOpacity="0.05" /></radialGradient>
        <radialGradient id="rfill3" cx="50%" cy="50%" r="50%"><stop offset="0%" stopColor="#b42a0a" stopOpacity="0.3" /><stop offset="100%" stopColor="#b42a0a" stopOpacity="0.05" /></radialGradient>
      </defs>
      {[1, 0.75, 0.5, 0.25].map((r, i) => <circle key={i} cx={cx} cy={cy} r={maxR * r} fill={i === 0 ? 'rgba(255,255,255,0.015)' : 'none'} stroke={C.border} strokeWidth="0.7" strokeDasharray="2,3" />)}
      {[...Array(AXES)].map((_, i) => { const [x, y] = pt(i, 1); return <line key={i} x1={cx} y1={cy} x2={x} y2={y} stroke={C.border} strokeWidth="0.7" />; })}
      <polygon points={poly(d3)} fill="url(#rfill3)" stroke="rgba(180,42,10,0.7)" strokeWidth="1" />
      <polygon points={poly(d2)} fill="url(#rfill2)" stroke="rgba(239,68,68,0.7)" strokeWidth="1" />
      <polygon points={poly(d1)} fill="url(#rfill1)" stroke={C.orange} strokeWidth="1.2" />
    </svg>
  );
}

/* ── ThreatSpider ── */
function ThreatSpider({ size = 180, inc }) {
  const cx = size / 2, cy = size / 2, maxR = size * 0.38;
  const AXES = ['침입 강도', '경계 위반', '체류 시간', '카메라 범위', '대응 속도', '위험도'];
  const N = AXES.length;
  const pt = (i, v) => { const a = (i / N) * 2 * Math.PI - Math.PI / 2; return [cx + Math.cos(a) * v * maxR, cy + Math.sin(a) * v * maxR]; };
  const poly = (data) => data.map((v, i) => pt(i, v).join(',')).join(' ');
  const score = inc ? inc.score.eventization_score : 0.75;
  const obs   = [score, 0.7, score * 0.8, 0.6, 0.5, score * 0.9];
  const exp   = [0.5, 0.5, 0.5, 0.5, 0.5, 0.5];
  const resid = obs.map((v, i) => Math.max(0, v - exp[i]));
  return (
    <svg width={size} height={size} style={{ display: 'block' }}>
      <defs>
        <radialGradient id="sg1" cx="50%" cy="50%" r="50%"><stop offset="0%" stopColor={C.orange} stopOpacity="0.4" /><stop offset="100%" stopColor={C.orange} stopOpacity="0.05" /></radialGradient>
        <radialGradient id="sg2" cx="50%" cy="50%" r="50%"><stop offset="0%" stopColor="#EF4444" stopOpacity="0.3" /><stop offset="100%" stopColor="#EF4444" stopOpacity="0.03" /></radialGradient>
      </defs>
      {[1, 0.75, 0.5, 0.25].map((r, i) => <circle key={i} cx={cx} cy={cy} r={maxR * r} fill="none" stroke={C.border} strokeWidth="0.7" strokeDasharray="2,3" />)}
      {[...Array(N)].map((_, i) => { const [x, y] = pt(i, 1); return <line key={i} x1={cx} y1={cy} x2={x} y2={y} stroke={C.border} strokeWidth="0.7" />; })}
      <polygon points={poly(exp)} fill="rgba(255,255,255,0.04)" stroke="rgba(255,255,255,0.18)" strokeWidth="0.8" strokeDasharray="3,2" />
      <polygon points={poly(resid)} fill="url(#sg2)" stroke="rgba(239,68,68,0.6)" strokeWidth="1" />
      <polygon points={poly(obs)} fill="url(#sg1)" stroke={C.orange} strokeWidth="1.2" />
      {AXES.map((label, i) => { const [x, y] = pt(i, 1.18); return <text key={i} x={x} y={y} textAnchor="middle" dominantBaseline="middle" fill={C.fg4} fontSize="6.5" fontFamily={C.mono}>{label}</text>; })}
    </svg>
  );
}

/* ── Detail ── */
export default function Detail({ inc, incidents, onBack, onStatusChange, tweaks }) {
  const [tab, setTab] = useState('overview');
  const [note, setNote] = useState('');
  const [aiTab, setAiTab] = useState('summary');
  const [showFA, setShowFA] = useState(false);
  const [aiLoading, setAiLoading] = useState(false);

  const current = incidents.find(i => i.incident_id === inc.incident_id);
  const det = getDetail(inc.incident_id);
  const status = current.status;
  const terminal = status === 'confirmed' || status === 'false_alarm';
  const NEXT = { detected: ['reviewed', 'confirmed', 'false_alarm'], reviewed: ['confirmed', 'false_alarm'] };
  const TABS = ['overview', 'notes', 'ai report', 'respond'];

  useEffect(() => { setNote(det.operator.note || ''); }, [inc.incident_id]);

  const handleAction = (next) => {
    if (next === 'false_alarm') { setShowFA(true); return; }
    onStatusChange(inc.incident_id, next, note);
    if (next === 'confirmed') { setAiLoading(true); setTimeout(() => setAiLoading(false), 1800); }
  };

  const score = current.score.eventization_score;
  const conf  = current.score.detector_confidence_avg;

  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', overflow: 'hidden', animation: 'fadeIn 180ms ease' }}>

      {/* Top header */}
      <div style={{ padding: '9px 22px', borderBottom: `1px solid ${C.border}`, flexShrink: 0, background: C.bg2, display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          <button onClick={onBack} style={{ display: 'flex', alignItems: 'center', gap: 4, cursor: 'pointer' }}>
            <Icon name="layout-dashboard" size={11} color={C.fg4} />
          </button>
          <span style={{ color: C.border2 }}>›</span>
          <span style={{ fontFamily: C.mono, fontSize: 9.5, color: C.fg4, cursor: 'pointer' }} onClick={onBack}>Incidents</span>
          <span style={{ color: C.border2 }}>›</span>
          <span style={{ fontFamily: C.mono, fontSize: 9.5, color: C.fg2 }}>{inc.incident_id}</span>
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          {det.report_summary?.report_available && (
            <button style={{ display: 'flex', alignItems: 'center', gap: 6, background: C.bg3, border: `1px solid ${C.border2}`, borderRadius: '999px', padding: '6px 14px', fontFamily: C.mono, fontSize: 8.5, color: C.fg2, letterSpacing: '0.8px', cursor: 'pointer' }}>
              Download report <Icon name="download" size={11} color={C.fg3} />
            </button>
          )}
        </div>
      </div>

      {/* Incident title + meta */}
      <div style={{ padding: '12px 22px 0', borderBottom: `1px solid ${C.border}`, flexShrink: 0, background: C.bg2 }}>
        <div style={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', marginBottom: 10 }}>
          <div>
            <div style={{ fontFamily: C.sans, fontSize: 10, color: C.fg4, marginBottom: 2 }}>Incident details</div>
            <div style={{ fontFamily: C.sans, fontSize: 18, color: C.fg1, letterSpacing: '-0.5px' }}>Incident — {inc.incident_id.replace('INC-', '')}</div>
          </div>
          <div style={{ display: 'flex', gap: 20, alignItems: 'flex-start' }}>
            {[
              { label: 'ASSIGNEE',    val: current.op || 'Unassigned' },
              { label: 'CREATED',     val: new Date(current.detected_at).toLocaleDateString('ko-KR') },
              { label: 'ZONE',        val: current.location.zone.replace(/_/g, ' ') },
              { label: 'LAST UPDATE', val: relTime(current.detected_at) },
            ].map((m, i) => (
              <div key={i} style={{ textAlign: 'right' }}>
                <div style={{ fontFamily: C.mono, fontSize: 7.5, color: C.fg4, letterSpacing: '1.4px', textTransform: 'uppercase', marginBottom: 4 }}>{m.label}</div>
                <div style={{ fontFamily: C.mono, fontSize: 10, color: C.fg2 }}>{m.val}</div>
              </div>
            ))}
          </div>
        </div>

        {/* Status chips + tab nav */}
        <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 0 }}>
          <Badge status={status} />
          <span style={{ fontFamily: C.mono, fontSize: 8, color: C.fg4, background: C.bg3, border: `1px solid ${C.border}`, padding: '2px 8px', borderRadius: '999px', textTransform: 'uppercase' }}>{current.event_type}</span>
          <span style={{ fontFamily: C.mono, fontSize: 8, color: score >= 0.85 ? '#EF4444' : score >= 0.70 ? '#FACC15' : C.fg4, background: C.bg3, border: `1px solid ${C.border}`, padding: '2px 8px', borderRadius: '999px' }}>
            ● {score >= 0.85 ? 'High' : score >= 0.70 ? 'Medium' : 'Low'}
          </span>
          <div style={{ flex: 1 }} />
          <div style={{ display: 'flex', gap: 0 }}>
            {TABS.map(t => (
              <button key={t} onClick={() => setTab(t)} style={{ padding: '8px 14px', background: tab === t ? C.bg3 : 'transparent', border: 'none', borderTop: `2px solid ${tab === t ? C.orange : 'transparent'}`, fontFamily: C.mono, fontSize: 8.5, color: tab === t ? C.fg1 : C.fg4, letterSpacing: '0.8px', textTransform: 'uppercase', cursor: 'pointer', transition: 'all 140ms' }}>
                {t}
              </button>
            ))}
          </div>
        </div>
      </div>

      {/* Tab body */}
      <div style={{ flex: 1, overflow: 'hidden', display: 'flex' }}>

        {/* OVERVIEW TAB */}
        {tab === 'overview' && (
          <div style={{ flex: 1, display: 'flex', overflow: 'hidden' }}>

            {/* Left panel */}
            <div style={{ width: '42%', borderRight: `1px solid ${C.border}`, overflowY: 'auto', padding: '22px 24px', display: 'flex', flexDirection: 'column', gap: 24 }}>

              {/* Information */}
              <div>
                <SecLabel>Information</SecLabel>
                {[
                  { label: 'Incident type', val: current.event_type },
                  { label: 'Status',        val: status },
                  { label: 'Zone',          val: current.location.zone.replace(/_/g, ' ') },
                  { label: 'Camera',        val: current.location.camera_id },
                  { label: 'Site',          val: current.location.site },
                  { label: 'Detections',    val: det.evidence.track_ids.length },
                  { label: 'Confidence',    val: conf.toFixed(2) },
                  { label: 'Event score',   val: score.toFixed(2) },
                  { label: 'Created by',    val: current.op || '—' },
                  { label: 'Reviewed by',   val: det.operator.reviewed_by || '—' },
                ].map((r, i) => (
                  <div key={i} style={{ display: 'flex', justifyContent: 'space-between', padding: '7px 0', borderBottom: `1px solid ${C.border}` }}>
                    <span style={{ fontFamily: C.mono, fontSize: 9, color: C.fg4 }}>{r.label}:</span>
                    <span style={{ fontFamily: C.mono, fontSize: 9.5, color: C.fg2 }}>{r.val}</span>
                  </div>
                ))}
              </div>

              {/* Findings radar */}
              <div>
                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 10 }}>
                  <SecLabel>Findings</SecLabel>
                  <div style={{ display: 'flex', gap: 10 }}>
                    {[{ label: '침입', color: C.orange }, { label: '지체', color: 'rgba(239,68,68,0.8)' }, { label: '잠재위험', color: 'rgba(180,42,10,0.8)' }].map((l, i) => (
                      <div key={i} style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
                        <div style={{ width: 6, height: 6, borderRadius: '50%', background: l.color }} />
                        <span style={{ fontFamily: C.mono, fontSize: 7.5, color: C.fg4 }}>{l.label}</span>
                      </div>
                    ))}
                  </div>
                </div>
                <div style={{ display: 'flex', justifyContent: 'center', padding: '8px 0' }}>
                  <FindingsRadar size={160} />
                </div>
              </div>

              {/* Detection Timeline */}
              <div>
                <SecLabel>DETECTION TIMELINE</SecLabel>
                <div style={{ paddingLeft: 14, borderLeft: `1px solid ${C.border}`, display: 'flex', flexDirection: 'column' }}>
                  {det.timeline.map((t, i) => (
                    <div key={i} style={{ display: 'flex', gap: 10, alignItems: 'flex-start', paddingBottom: i < det.timeline.length - 1 ? 10 : 0 }}>
                      <div style={{ width: 7, height: 7, borderRadius: '50%', flexShrink: 0, marginTop: 2, marginLeft: -18, background: t.hi ? C.orange : C.bg4, border: t.hi ? `2px solid ${C.orange}` : `1px solid ${C.border2}`, boxShadow: t.hi ? `0 0 6px ${C.orangeD}` : 'none' }} />
                      <div style={{ flex: 1, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <span style={{ fontFamily: C.mono, fontSize: 10, color: t.hi ? C.fg1 : C.fg3 }}>{t.event}</span>
                        <span style={{ fontFamily: C.mono, fontSize: 9, color: C.fg4 }}>{t.ts}</span>
                      </div>
                    </div>
                  ))}
                </div>
              </div>

              {/* Eventization Basis */}
              <div>
                <SecLabel>EVENTIZATION BASIS</SecLabel>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 0 }}>
                  {[
                    { l: 'line_crossed',  v: det.eventization_basis.line_crossed  ? '✓ TRUE' : '✗ FALSE', ok: det.eventization_basis.line_crossed },
                    { l: 'roi_entered',   v: det.eventization_basis.roi_entered   ? '✓ TRUE' : '✗ FALSE', ok: det.eventization_basis.roi_entered },
                    { l: 'duration_sec',  v: `${det.eventization_basis.duration_sec}s`, ok: true },
                    { l: 'confidence',    v: conf.toFixed(2), ok: true },
                    { l: 'zone_policy',   v: det.eventization_basis.zone_policy,  ok: true },
                    { l: 'event_score',   v: score.toFixed(2), ok: true },
                  ].map((r, i) => (
                    <div key={r.l} style={{ display: 'flex', justifyContent: 'space-between', padding: '6px 10px', borderBottom: `1px solid ${C.border}`, borderRight: i % 2 === 0 ? `1px solid ${C.border}` : 'none' }}>
                      <span style={{ fontFamily: C.mono, fontSize: 8.5, color: C.fg4 }}>{r.l}</span>
                      <span style={{ fontFamily: C.mono, fontSize: 8.5, color: r.ok ? C.fg2 : 'rgba(239,68,68,0.8)' }}>{r.v}</span>
                    </div>
                  ))}
                </div>
              </div>

              {/* Evidence Files */}
              <div>
                <SecLabel>EVIDENCE FILES</SecLabel>
                {[
                  { name: 'cam_clip.mp4',         size: '14.2 MB', icon: 'play' },
                  { name: 'thumb_001.jpg',         size: '0.4 MB',  icon: 'file-text' },
                  { name: 'detection_log.json',    size: '0.1 MB',  icon: 'file-text' },
                ].map((f, i, arr) => (
                  <div key={i} style={{ display: 'flex', alignItems: 'center', gap: 10, padding: '8px 0', borderBottom: i < arr.length - 1 ? `1px solid ${C.border}` : 'none', cursor: 'pointer' }}>
                    <Icon name={f.icon} size={12} color={C.fg4} />
                    <div style={{ flex: 1 }}>
                      <div style={{ fontFamily: C.sans, fontSize: 11.5, color: C.fg2 }}>{f.name}</div>
                      <div style={{ fontFamily: C.mono, fontSize: 8.5, color: C.fg4 }}>{f.size}</div>
                    </div>
                    <Icon name="download" size={11} color={C.fg4} />
                  </div>
                ))}
              </div>

            </div>

            {/* Right panel */}
            <div style={{ flex: 1, overflowY: 'auto', padding: '22px 28px', display: 'flex', flexDirection: 'column', gap: 24 }}>

              {/* Threat Surface Map */}
              <div>
                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 12 }}>
                  <SecLabel>Threat Surface Map</SecLabel>
                  <div style={{ display: 'flex', gap: 12 }}>
                    {[{ label: 'Observed', color: C.orange }, { label: 'Expected', color: 'rgba(255,255,255,0.25)' }, { label: 'Residual Risk', color: 'rgba(239,68,68,0.7)' }].map((l, i) => (
                      <div key={i} style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
                        <div style={{ width: 6, height: 6, borderRadius: '50%', background: l.color }} />
                        <span style={{ fontFamily: C.mono, fontSize: 7.5, color: C.fg4 }}>{l.label}</span>
                      </div>
                    ))}
                  </div>
                </div>
                <div style={{ display: 'flex', justifyContent: 'center', padding: '8px 0 16px' }}>
                  <ThreatSpider size={200} inc={current} />
                </div>
              </div>

              {/* Evidence Clip */}
              <div>
                <SecLabel>EVIDENCE CLIP</SecLabel>
                <div style={{ height: 130, background: C.bg3, border: `1px solid ${C.border}`, borderRadius: C.R, display: 'flex', alignItems: 'center', justifyContent: 'center', position: 'relative', overflow: 'hidden', marginBottom: 12 }}>
                  <div style={{ position: 'absolute', inset: 0, backgroundImage: 'repeating-linear-gradient(0deg,transparent,transparent 3px,rgba(255,255,255,0.010) 3px,rgba(255,255,255,0.010) 4px)' }} />
                  <div style={{ position: 'absolute', top: 0, left: '-100%', width: '28%', height: '100%', background: `linear-gradient(90deg,transparent,${C.orange}1a,transparent)`, animation: 'scan 3s linear infinite' }} />
                  <div style={{ textAlign: 'center', position: 'relative', zIndex: 1 }}>
                    <div style={{ display: 'flex', justifyContent: 'center', marginBottom: 8 }}>
                      <div style={{ width: 36, height: 36, border: `1px solid ${C.border2}`, borderRadius: C.R, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                        <Icon name="play" size={16} color={C.fg2} />
                      </div>
                    </div>
                    <div style={{ fontFamily: C.mono, fontSize: 8.5, color: C.fg4 }}>{current.location.camera_id} · {det.evidence.objects.join(', ').toUpperCase()}</div>
                    {det.source_summary && <div style={{ fontFamily: C.mono, fontSize: 7.5, color: C.fg4, marginTop: 2 }}>{det.source_summary.source_type} · {det.source_summary.source_id}</div>}
                  </div>
                  <div style={{ position: 'absolute', top: 8, right: 8, display: 'flex', gap: 5 }}>
                    <span style={{ fontFamily: C.mono, fontSize: 7.5, color: C.fg4, background: 'rgba(0,0,0,0.55)', padding: '2px 6px', borderRadius: 3 }}>REC</span>
                    <span style={{ fontFamily: C.mono, fontSize: 7.5, color: C.orange, background: C.orangeD, padding: '2px 6px', borderRadius: 3, border: `1px solid ${C.orangeB}` }}>{(current.event_type || 'intrusion').toUpperCase()}</span>
                  </div>
                </div>
              </div>

              {/* Entities */}
              <div>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 10 }}>
                  <SecLabel>Entities</SecLabel>
                  <span style={{ fontFamily: C.mono, fontSize: 8, color: C.fg4 }}>Sort by score ▾</span>
                </div>
                <div style={{ border: `1px solid ${C.border}`, borderRadius: C.R, overflow: 'hidden' }}>
                  <div style={{ display: 'grid', gridTemplateColumns: '32px 1fr 80px 120px', padding: '7px 14px', borderBottom: `1px solid ${C.border}`, background: C.bg3 }}>
                    {['ID', 'TYPE', 'SCORE', 'REF'].map(h => <span key={h} style={{ fontFamily: C.mono, fontSize: 7.5, color: C.fg4, letterSpacing: '1px', textTransform: 'uppercase' }}>{h}</span>)}
                  </div>
                  {[
                    { id: 1, type: 'person', score, ref: det.evidence.track_ids[0] ? `TRACK #${det.evidence.track_ids[0]}` : '—' },
                    { id: 2, type: 'zone',   score: conf, ref: current.location.zone },
                  ].map((e, i) => (
                    <div key={i} style={{ display: 'grid', gridTemplateColumns: '32px 1fr 80px 120px', padding: '10px 14px', borderBottom: i < 1 ? `1px solid ${C.border}` : 'none', background: i % 2 === 0 ? 'transparent' : 'rgba(255,255,255,0.01)' }}>
                      <span style={{ fontFamily: C.mono, fontSize: 10, color: C.fg4 }}>{e.id}</span>
                      <span style={{ fontFamily: C.sans, fontSize: 11.5, color: C.fg1, textTransform: 'capitalize' }}>{e.type}</span>
                      <span style={{ fontFamily: C.mono, fontSize: 10, color: SC(e.score) }}>{e.score.toFixed(2)}</span>
                      <span style={{ fontFamily: C.mono, fontSize: 9, color: C.fg4, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{e.ref}</span>
                    </div>
                  ))}
                </div>
              </div>

            </div>
          </div>
        )}

        {/* NOTES TAB */}
        {tab === 'notes' && (
          <div style={{ flex: 1, overflowY: 'auto', padding: '24px 32px', display: 'flex', flexDirection: 'column', gap: 24, maxWidth: 680 }}>
            <div>
              <SecLabel>OPERATOR NOTE</SecLabel>
              <textarea value={note} onChange={e => setNote(e.target.value)} rows={5} placeholder="판단 근거를 입력하세요..."
                style={{ width: '100%', fontFamily: C.mono, fontSize: 11.5, background: 'transparent', color: C.fg1, border: `1px solid ${C.border2}`, borderRadius: C.R, padding: '12px 14px', resize: 'none', lineHeight: 1.7, outline: 'none' }} />
              {det.operator.reviewed_by && det.operator.reviewed_at && (
                <div style={{ fontFamily: C.mono, fontSize: 9, color: C.fg4, marginTop: 6 }}>{det.operator.reviewed_by} · {new Date(det.operator.reviewed_at).toLocaleString('ko-KR')}</div>
              )}
            </div>
            <div>
              <SecLabel>STATUS HISTORY</SecLabel>
              <div style={{ paddingLeft: 14, borderLeft: `1px solid ${C.border}`, display: 'flex', flexDirection: 'column', gap: 0 }}>
                {det.status_history_preview.map((h, i) => {
                  const isSystem = h.actor.type === 'system';
                  const dotColor = h.to === 'confirmed' ? '#22C55E' : h.to === 'false_alarm' ? '#EF4444' : h.to === 'reviewed' ? '#FACC15' : C.fg4;
                  return (
                    <div key={i} style={{ display: 'flex', gap: 10, alignItems: 'flex-start', paddingBottom: i < det.status_history_preview.length - 1 ? 14 : 0 }}>
                      <div style={{ width: 7, height: 7, borderRadius: '50%', flexShrink: 0, marginTop: 3, marginLeft: -18, background: dotColor }} />
                      <div style={{ flex: 1 }}>
                        <div style={{ display: 'flex', alignItems: 'center', gap: 6, marginBottom: 2 }}>
                          <span style={{ fontFamily: C.mono, fontSize: 9.5, color: dotColor }}>{h.from ? `${h.from} → ` : ''}{h.to}</span>
                          <span style={{ fontFamily: C.mono, fontSize: 7.5, color: isSystem ? C.fg4 : C.orange, background: isSystem ? 'transparent' : C.orangeD, padding: isSystem ? '0' : '1px 5px', borderRadius: 3 }}>{isSystem ? 'system' : h.actor.id}</span>
                        </div>
                        {h.note && <div style={{ fontFamily: C.sans, fontSize: 11, color: C.fg4, fontStyle: 'italic', lineHeight: 1.5 }}>{h.note}</div>}
                        <div style={{ fontFamily: C.mono, fontSize: 8, color: C.fg4, marginTop: 2, opacity: 0.6 }}>{new Date(h.at).toLocaleTimeString('ko-KR', { hour: '2-digit', minute: '2-digit', second: '2-digit' })}</div>
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>
          </div>
        )}

        {/* AI REPORT TAB */}
        {tab === 'ai report' && (
          <div style={{ flex: 1, display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>
            <div style={{ padding: '14px 28px', borderBottom: `1px solid ${C.border}`, flexShrink: 0, display: 'flex', alignItems: 'center', gap: 8 }}>
              <Icon name="zap" size={13} color={C.orange} />
              <span style={{ fontFamily: C.mono, fontSize: 9, color: C.fg2, letterSpacing: '1.4px', textTransform: 'uppercase', flex: 1 }}>AI ASSIST — intrusion_assist</span>
              {det.ai_output && <span style={{ fontFamily: C.mono, fontSize: 8, color: C.fg4 }}>{det.ai_output.meta.prompt_version}</span>}
              {det.report_summary?.report_available && <span style={{ fontFamily: C.mono, fontSize: 8, color: '#22C55E', background: 'rgba(34,197,94,0.08)', border: '1px solid rgba(34,197,94,0.2)', padding: '2px 7px', borderRadius: '999px' }}>{det.report_summary.report_id}</span>}
            </div>
            {status !== 'confirmed'
              ? <div style={{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', gap: 12, padding: '24px', textAlign: 'center' }}>
                  <div style={{ width: 44, height: 44, border: `1px solid ${C.border}`, borderRadius: C.R, display: 'flex', alignItems: 'center', justifyContent: 'center' }}><Icon name="lock" size={18} color={C.fg4} /></div>
                  <div style={{ fontFamily: C.mono, fontSize: 9, color: C.fg4, lineHeight: 2 }}>CONFIRMED 이후 AI 보고가 생성됩니다</div>
                </div>
              : aiLoading
              ? <div style={{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', gap: 12 }}>
                  <div style={{ fontFamily: C.mono, fontSize: 9, color: C.fg4, letterSpacing: '1px' }}>GENERATING REPORT...</div>
                  <div style={{ width: 140, height: 1, background: C.border, position: 'relative', overflow: 'hidden' }}>
                    <div style={{ position: 'absolute', top: 0, left: 0, height: '100%', width: '40%', background: C.orange, animation: 'scan 1.2s linear infinite' }} />
                  </div>
                </div>
              : !det.ai_output
              ? <div style={{ flex: 1, padding: '20px 28px' }}>
                  <div style={{ fontFamily: C.mono, fontSize: 10, color: 'rgba(239,68,68,0.7)', marginBottom: 12 }}>AI 보고 생성 실패</div>
                  <button style={{ background: 'transparent', border: `1px solid ${C.border2}`, borderRadius: '999px', padding: '7px 14px', fontFamily: C.mono, fontSize: 9, color: C.fg3, letterSpacing: '1px', textTransform: 'uppercase', display: 'flex', alignItems: 'center', gap: 5, cursor: 'pointer' }}>
                    <Icon name="refresh-cw" size={11} color={C.fg4} /> REGENERATE
                  </button>
                </div>
              : <div style={{ flex: 1, overflowY: 'auto' }}>
                  <div style={{ display: 'flex', borderBottom: `1px solid ${C.border}` }}>
                    {['summary', 'checklist', 'report'].map(t => (
                      <button key={t} onClick={() => setAiTab(t)} style={{ flex: 1, padding: '9px 4px', background: 'transparent', border: 'none', borderBottom: `2px solid ${aiTab === t ? C.orange : 'transparent'}`, fontFamily: C.mono, fontSize: 8.5, color: aiTab === t ? C.fg1 : C.fg4, letterSpacing: '1px', textTransform: 'uppercase', cursor: 'pointer', transition: 'all 140ms' }}>{t}</button>
                    ))}
                  </div>
                  <div style={{ padding: '18px 28px' }}>
                    {aiTab === 'summary' && <div style={{ borderLeft: `2px solid ${C.orange}`, paddingLeft: 14 }}>{det.ai_output.summary.lines.map((l, i) => <p key={i} style={{ fontFamily: C.mono, fontSize: 11.5, color: C.fg1, lineHeight: 1.9, marginBottom: 8 }}>{l}</p>)}</div>}
                    {aiTab === 'checklist' && det.ai_output.checklist.items.map((item, i) => (
                      <div key={i} style={{ display: 'flex', gap: 12, padding: '10px 0', borderBottom: `1px solid ${C.border}`, alignItems: 'flex-start' }}>
                        <span style={{ fontFamily: C.mono, fontSize: 9, color: C.orange, flexShrink: 0 }}>0{i + 1}</span>
                        <span style={{ fontFamily: C.sans, fontSize: 12.5, color: C.fg2, lineHeight: 1.5 }}>{item}</span>
                      </div>
                    ))}
                    {aiTab === 'report' && <div style={{ fontFamily: C.sans, fontSize: 12, color: C.fg2, lineHeight: 1.8, whiteSpace: 'pre-wrap', background: C.bg3, padding: '14px', borderRadius: C.R, border: `1px solid ${C.border}` }}>{det.ai_output.report_draft.markdown}</div>}
                  </div>
                  <div style={{ padding: '0 28px 16px', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <span style={{ fontFamily: C.mono, fontSize: 8, color: C.fg4 }}>{det.ai_output.meta.prompt_family} · {det.ai_output.meta.generated_at ? new Date(det.ai_output.meta.generated_at).toLocaleString('ko-KR') : ''}</span>
                    <button style={{ background: 'transparent', border: `1px solid ${C.border}`, borderRadius: '999px', padding: '5px 12px', fontFamily: C.mono, fontSize: 8, color: C.fg4, letterSpacing: '1px', textTransform: 'uppercase', display: 'flex', alignItems: 'center', gap: 4, cursor: 'pointer' }}>
                      <Icon name="refresh-cw" size={10} color={C.fg4} /> REGENERATE
                    </button>
                  </div>
                </div>
            }
          </div>
        )}

        {/* RESPOND TAB */}
        {tab === 'respond' && (
          <div style={{ flex: 1, overflowY: 'auto', padding: '24px 32px', maxWidth: 500 }}>
            <SecLabel>STATUS UPDATE</SecLabel>
            {!terminal
              ? <div style={{ display: 'flex', flexDirection: 'column', gap: 8, marginBottom: 28 }}>
                  {(NEXT[status] || []).map(next => {
                    const cfg = {
                      reviewed:    { label: 'Mark Reviewed',      bg: 'transparent', color: C.fg2,                    border: C.border2 },
                      confirmed:   { label: 'Confirm Incident',   bg: C.fg1,         color: C.bg,                    border: C.fg1 },
                      false_alarm: { label: 'Mark False Alarm',   bg: 'transparent', color: 'rgba(239,68,68,0.9)',    border: 'rgba(239,68,68,0.3)' },
                    }[next];
                    return <button key={next} onClick={() => handleAction(next)} style={{ padding: '11px 18px', background: cfg.bg, color: cfg.color, border: `1px solid ${cfg.border}`, borderRadius: '999px', fontFamily: C.mono, fontSize: 10, letterSpacing: '1.2px', textTransform: 'uppercase', cursor: 'pointer', transition: 'all 140ms', textAlign: 'center' }}>{cfg.label}</button>;
                  })}
                </div>
              : <div style={{ display: 'flex', alignItems: 'center', gap: 8, padding: '12px 16px', background: STATUS[status].bg, border: `1px solid ${STATUS[status].border}`, borderRadius: C.R, marginBottom: 28 }}>
                  <Icon name={status === 'confirmed' ? 'check-circle' : 'x-circle'} size={14} color={STATUS[status].dot} />
                  <span style={{ fontFamily: C.mono, fontSize: 10, color: STATUS[status].color, letterSpacing: '1px' }}>{STATUS[status].label} — 추가 액션 없음</span>
                </div>
            }
            <SecLabel>OPERATOR NOTE</SecLabel>
            <textarea value={note} onChange={e => setNote(e.target.value)} rows={5} placeholder="판단 근거를 입력하세요..."
              style={{ width: '100%', fontFamily: C.mono, fontSize: 11.5, background: 'transparent', color: C.fg1, border: `1px solid ${C.border2}`, borderRadius: C.R, padding: '12px 14px', resize: 'none', lineHeight: 1.7, outline: 'none', marginBottom: 16 }} />
            {det.report_summary && (
              <div>
                <SecLabel>REPORT</SecLabel>
                {det.report_summary.report_available
                  ? <div style={{ display: 'flex', alignItems: 'center', gap: 10, padding: '10px 12px', background: 'rgba(34,197,94,0.05)', border: '1px solid rgba(34,197,94,0.15)', borderRadius: C.R }}>
                      <Icon name="file-text" size={13} color="#22C55E" />
                      <div style={{ flex: 1 }}>
                        <div style={{ fontFamily: C.mono, fontSize: 10, color: C.fg1 }}>{det.report_summary.report_id}</div>
                        <div style={{ fontFamily: C.mono, fontSize: 8, color: C.fg4, marginTop: 2 }}>{new Date(det.report_summary.report_generated_at).toLocaleString('ko-KR')}</div>
                      </div>
                      <button style={{ background: 'transparent', border: `1px solid ${C.border2}`, borderRadius: '999px', padding: '5px 10px', fontFamily: C.mono, fontSize: 8, color: C.fg3, letterSpacing: '0.8px', textTransform: 'uppercase', cursor: 'pointer', display: 'flex', alignItems: 'center', gap: 4 }}>
                        <Icon name="download" size={9} color={C.fg4} /> PDF
                      </button>
                    </div>
                  : <div style={{ fontFamily: C.mono, fontSize: 9, color: C.fg4 }}>confirmed 이후 자동 생성됩니다</div>
                }
              </div>
            )}
          </div>
        )}

      </div>

      {showFA && <FAModal onConfirm={n => { onStatusChange(inc.incident_id, 'false_alarm', n); setShowFA(false); }} onCancel={() => setShowFA(false)} />}
    </div>
  );
}

