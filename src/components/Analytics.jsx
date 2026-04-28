import { useState } from 'react';
import { C, SC } from '../tokens';
import { DETAIL_DB } from '../data/mockDetails';
import { relTime } from '../utils/relTime';
import Icon from './shared/Icon';
import Badge from './shared/Badge';
import SecLabel from './shared/SecLabel';

const REPORTS = [
  { incident_id: 'INC-2026-003', zone: 'warehouse_south',   at: '2026-04-24T20:16:00+09:00', status: 'confirmed', score: 0.94 },
  { incident_id: 'INC-2026-001', zone: 'outer_fence_north', at: '2026-04-24T21:14:00+09:00', status: 'detected',  score: 0.87 },
  { incident_id: 'INC-2026-010', zone: 'outer_fence_north', at: '2026-04-23T18:42:00+09:00', status: 'confirmed', score: 0.88 },
];

export default function Analytics({ incidents, onSelect }) {
  const [activeId, setActiveId] = useState(REPORTS[0].incident_id);
  const rep = REPORTS.find(r => r.incident_id === activeId);
  const det = DETAIL_DB[activeId];

  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>

      {/* Header */}
      <div style={{ padding: '18px 32px 14px', borderBottom: `1px solid ${C.border}`, flexShrink: 0, display: 'flex', alignItems: 'center', gap: 16 }}>
        <div>
          <div style={{ fontFamily: C.sans, fontSize: 18, color: C.fg1, marginBottom: 2 }}>AI Report</div>
          <div style={{ fontFamily: C.mono, fontSize: 9, color: C.fg4, letterSpacing: '1.2px', textTransform: 'uppercase' }}>CONFIRMED INCIDENT — AI GENERATED REPORTS</div>
        </div>
        <div style={{ flex: 1 }} />
        <button style={{ display: 'flex', alignItems: 'center', gap: 6, background: 'transparent', border: `1px solid ${C.border2}`, borderRadius: C.R, padding: '8px 16px', fontFamily: C.mono, fontSize: 9, color: C.fg3, letterSpacing: '1px', textTransform: 'uppercase', cursor: 'pointer' }}>
          <Icon name="download" size={11} color={C.fg4} /> EXPORT PDF
        </button>
      </div>

      {/* Body */}
      <div style={{ flex: 1, display: 'flex', overflow: 'hidden' }}>

        {/* Left: report viewer */}
        <div style={{ flex: 1, overflowY: 'auto', padding: '28px 36px' }}>
          {det?.ai_output ? (
            <>
              {/* Report meta */}
              <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 28, paddingBottom: 20, borderBottom: `1px solid ${C.border}` }}>
                <div style={{ flex: 1 }}>
                  <div style={{ fontFamily: C.mono, fontSize: 14, color: C.fg1, marginBottom: 6 }}>{rep.incident_id}</div>
                  <div style={{ display: 'flex', gap: 14, flexWrap: 'wrap' }}>
                    {[{ icon: 'map-pin', val: rep.zone }, { icon: 'clock', val: relTime(rep.at) }, { icon: 'activity', val: `score ${rep.score}` }].map((m, i) => (
                      <div key={i} style={{ display: 'flex', alignItems: 'center', gap: 5 }}>
                        <Icon name={m.icon} size={11} color={C.fg4} />
                        <span style={{ fontFamily: C.mono, fontSize: 10, color: C.fg4 }}>{m.val}</span>
                      </div>
                    ))}
                  </div>
                </div>
                <Badge status={rep.status} />
              </div>

              {/* Summary */}
              <div style={{ marginBottom: 32 }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 14 }}>
                  <div style={{ width: 3, height: 20, background: C.orange, flexShrink: 0 }} />
                  <span style={{ fontFamily: C.mono, fontSize: 11, color: C.fg2, letterSpacing: '1.4px', textTransform: 'uppercase' }}>사건 요약</span>
                </div>
                <div style={{ background: C.bg2, border: `1px solid ${C.border}`, borderRadius: C.R, padding: '24px 28px', borderLeft: `3px solid ${C.orange}` }}>
                  {det.ai_output.summary.lines.map((l, i) => (
                    <p key={i} style={{ fontFamily: C.mono, fontSize: 13, color: C.fg1, lineHeight: 2, marginBottom: i < det.ai_output.summary.lines.length - 1 ? 10 : 0 }}>{l}</p>
                  ))}
                </div>
              </div>

              {/* Checklist */}
              <div style={{ marginBottom: 32 }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 14 }}>
                  <div style={{ width: 3, height: 20, background: '#22C55E', flexShrink: 0 }} />
                  <span style={{ fontFamily: C.mono, fontSize: 11, color: C.fg2, letterSpacing: '1.4px', textTransform: 'uppercase' }}>초동 대응 체크리스트</span>
                </div>
                <div style={{ background: C.bg2, border: `1px solid ${C.border}`, borderRadius: C.R, padding: '8px 0', borderLeft: '3px solid #22C55E' }}>
                  {det.ai_output.checklist.items.map((item, i) => (
                    <div key={i} style={{ display: 'flex', alignItems: 'center', gap: 16, padding: '14px 24px', borderBottom: i < det.ai_output.checklist.items.length - 1 ? `1px solid ${C.border}` : 'none' }}>
                      <div style={{ width: 22, height: 22, border: '1px solid rgba(34,197,94,0.4)', borderRadius: 4, background: 'rgba(34,197,94,0.08)', display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0 }}>
                        <Icon name="check" size={12} color="#22C55E" />
                      </div>
                      <span style={{ fontFamily: C.mono, fontSize: 9, color: C.fg4, width: 22, flexShrink: 0 }}>0{i + 1}</span>
                      <span style={{ fontFamily: C.sans, fontSize: 13, color: C.fg1 }}>{item}</span>
                    </div>
                  ))}
                </div>
              </div>

              {/* Report draft */}
              <div>
                <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 14 }}>
                  <div style={{ width: 3, height: 20, background: C.fg2, flexShrink: 0 }} />
                  <span style={{ fontFamily: C.mono, fontSize: 11, color: C.fg2, letterSpacing: '1.4px', textTransform: 'uppercase' }}>보고서 초안</span>
                  <div style={{ flex: 1 }} />
                  <span style={{ fontFamily: C.mono, fontSize: 8, color: C.fg4 }}>{det.ai_output.meta.prompt_family} · {det.ai_output.meta.prompt_version}</span>
                  <button style={{ display: 'flex', alignItems: 'center', gap: 4, background: 'transparent', border: `1px solid ${C.border2}`, borderRadius: C.R, padding: '5px 10px', fontFamily: C.mono, fontSize: 8, color: C.fg3, letterSpacing: '1px', textTransform: 'uppercase', cursor: 'pointer' }}>
                    <Icon name="refresh-cw" size={10} color={C.fg4} /> REGENERATE
                  </button>
                </div>
                <div style={{ background: C.bg2, border: `1px solid ${C.border2}`, borderRadius: C.R, padding: '28px 32px' }}>
                  {det.ai_output.report_draft.markdown.split('\n').map((line, i) => {
                    if (line.startsWith('## ')) return <div key={i} style={{ fontFamily: C.sans, fontSize: 15, color: C.fg1, fontWeight: 500, marginTop: i > 0 ? 24 : 0, marginBottom: 10, paddingBottom: 6, borderBottom: `1px solid ${C.border}` }}>{line.replace('## ', '')}</div>;
                    if (line.startsWith('- '))  return <div key={i} style={{ display: 'flex', gap: 10, alignItems: 'flex-start', padding: '4px 0' }}><span style={{ color: C.orange, flexShrink: 0, marginTop: 2 }}>—</span><span style={{ fontFamily: C.sans, fontSize: 13, color: C.fg2, lineHeight: 1.7 }}>{line.replace('- ', '')}</span></div>;
                    if (!line.trim())            return <div key={i} style={{ height: 8 }} />;
                    return <p key={i} style={{ fontFamily: C.sans, fontSize: 13, color: C.fg2, lineHeight: 1.7 }}>{line}</p>;
                  })}
                </div>
              </div>
            </>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', height: '100%', gap: 16 }}>
              <Icon name="file-text" size={32} color={C.fg4} />
              <div style={{ fontFamily: C.mono, fontSize: 10, color: C.fg4, letterSpacing: '1px', textAlign: 'center', lineHeight: 2 }}>AI 보고서 없음<br />Confirmed incident 처리 후 자동 생성됩니다</div>
              <button onClick={() => onSelect && onSelect(rep.incident_id)} style={{ background: 'transparent', border: `1px solid ${C.border2}`, borderRadius: C.R, padding: '8px 16px', fontFamily: C.mono, fontSize: 9, color: C.fg3, letterSpacing: '1px', textTransform: 'uppercase', cursor: 'pointer' }}>INCIDENT 보기</button>
            </div>
          )}
        </div>

        {/* Right: report list */}
        <div style={{ width: 280, borderLeft: `1px solid ${C.border}`, display: 'flex', flexDirection: 'column', flexShrink: 0, overflow: 'hidden' }}>
          <div style={{ padding: '18px 20px 12px', borderBottom: `1px solid ${C.border}`, flexShrink: 0 }}>
            <div style={{ fontFamily: C.mono, fontSize: 9, color: C.fg4, letterSpacing: '1.4px', textTransform: 'uppercase' }}>REPORT HISTORY</div>
          </div>
          <div style={{ flex: 1, overflowY: 'auto', padding: '8px 0' }}>
            {REPORTS.map(r => {
              const active = r.incident_id === activeId;
              const hasAi = !!DETAIL_DB[r.incident_id]?.ai_output;
              return (
                <div key={r.incident_id} onClick={() => setActiveId(r.incident_id)}
                  style={{ padding: '14px 20px', cursor: 'pointer', borderLeft: `3px solid ${active ? C.orange : 'transparent'}`, background: active ? C.orangeD : 'transparent', borderBottom: `1px solid ${C.border}`, transition: 'all 150ms ease' }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 5 }}>
                    <span style={{ fontFamily: C.mono, fontSize: 11, color: active ? C.fg1 : C.fg2 }}>{r.incident_id}</span>
                    {!hasAi && <span style={{ fontFamily: C.mono, fontSize: 8, color: C.fg4, border: `1px solid ${C.border}`, padding: '1px 5px', borderRadius: 3 }}>MOCK</span>}
                  </div>
                  <div style={{ fontFamily: C.mono, fontSize: 9, color: C.fg4, marginBottom: 4 }}>{r.zone}</div>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <span style={{ fontFamily: C.mono, fontSize: 9, color: C.fg4 }}>{relTime(r.at)}</span>
                    <span style={{ fontFamily: C.mono, fontSize: 9, color: SC(r.score) }}>{r.score}</span>
                  </div>
                </div>
              );
            })}
          </div>
        </div>

      </div>
    </div>
  );
}
