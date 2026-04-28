import { useState, useMemo } from 'react';
import { C, SC } from '../tokens';
import { ACTIVITY } from '../data/mockActivity';
import { relTime } from '../utils/relTime';
import Icon from './shared/Icon';
import Spark from './shared/Spark';
import Donut from './shared/Donut';
import SecLabel from './shared/SecLabel';
import FactoryFloor from './FactoryFloor';

export default function DashHome({ incidents, setPage, onSelect, tweaks }) {
  const [mapOpen, setMapOpen] = useState(false);
  const [actSort, setActSort] = useState('recent');

  const d = incidents.filter(i => i.status === 'detected').length;
  const r = incidents.filter(i => i.status === 'reviewed').length;
  const c = incidents.filter(i => i.status === 'confirmed').length;
  const f = incidents.filter(i => i.status === 'false_alarm').length;
  const total = incidents.length;
  const compliance = total > 0 ? Math.round((1 - f / total) * 100) : 100;

  const STATS = [
    { label: 'OPEN INCIDENTS',   value: d + r,      trend: '+12%', up: false, chart: [2, 4, 3, 5, 7, 6, d + r],    col: '#EF4444' },
    { label: 'ACTIVE INCIDENTS', value: total - f,  trend: '-8%',  up: true,  chart: [8, 7, 9, 6, 5, 4, total - f], col: C.orange },
    { label: 'COMPLIANCE',       value: `${compliance}%`, trend: '+6%', up: true, chart: [44, 50, 55, 52, 58, 60, compliance], col: '#22C55E', donut: compliance },
    { label: 'AVG REVIEW TIME',  value: '18m',      trend: '-11%', up: true,  chart: [32, 28, 25, 22, 20, 19, 18], col: '#FACC15' },
  ];

  const sortedActivity = useMemo(() => {
    const a = [...ACTIVITY];
    if (actSort === 'severity') a.sort((x, y) => y.severity - x.severity);
    return a;
  }, [actSort]);

  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>
      {/* Stat strip */}
      <div style={{ display: 'flex', borderBottom: `1px solid ${C.border}`, flexShrink: 0 }}>
        {STATS.map((s, i) => (
          <div key={i} style={{ flex: 1, padding: '12px 20px 14px', borderRight: i < 3 ? `1px solid ${C.border}` : 'none' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
              <div>
                <div style={{ fontFamily: C.mono, fontSize: 7, color: C.fg4, letterSpacing: '1.6px', textTransform: 'uppercase', marginBottom: 5 }}>{s.label}</div>
                <div style={{ display: 'flex', alignItems: 'baseline', gap: 6, marginBottom: 2 }}>
                  <span style={{ fontFamily: C.mono, fontSize: 22, color: C.fg1, letterSpacing: '-1.5px' }}>{s.value}</span>
                  <span style={{ fontFamily: C.mono, fontSize: 9, color: s.up ? '#22C55E' : '#EF4444' }}>{s.up ? '↓' : '↑'}{s.trend}</span>
                </div>
                <div style={{ fontFamily: C.mono, fontSize: 8.5, color: C.fg4 }}>vs last week</div>
              </div>
              {s.donut !== undefined
                ? <Donut pct={s.donut} color={s.col} size={36} />
                : tweaks.showSparklines ? <Spark data={s.chart} color={s.col} w={56} h={24} /> : null
              }
            </div>
          </div>
        ))}
      </div>

      {/* Body */}
      <div style={{ flex: 1, display: 'flex', overflow: 'hidden' }}>
        {/* Left */}
        <div style={{ flex: 1, display: 'flex', flexDirection: 'column', borderRight: `1px solid ${C.border}`, overflow: 'hidden', padding: '20px 28px' }}>
          <div style={{ flex: 1, display: 'flex', flexDirection: 'column', gap: 0, overflow: 'hidden' }}>
            {/* Facility map */}
            <div style={{ flex: 1, display: 'flex', flexDirection: 'column', marginBottom: 16, minHeight: 0 }}>
              <SecLabel>FACILITY OVERVIEW
                <button
                  onClick={() => setMapOpen(true)}
                  style={{ fontFamily: C.mono, fontSize: 8, color: C.orange, letterSpacing: '1px', textTransform: 'uppercase', display: 'flex', alignItems: 'center', gap: 4, cursor: 'pointer' }}
                >
                  <Icon name="maximize-2" size={10} color={C.orange} /> EXPAND
                </button>
              </SecLabel>
              <div
                onClick={() => setMapOpen(true)}
                style={{ flex: 1, minHeight: 0, background: C.bg3, border: `1px solid ${C.border}`, borderRadius: C.R, overflow: 'hidden', cursor: 'pointer', position: 'relative', transition: 'border-color 150ms ease' }}
                onMouseEnter={e => { e.currentTarget.style.borderColor = C.border2; }}
                onMouseLeave={e => { e.currentTarget.style.borderColor = C.border; }}
              >
                <FactoryFloor incidents={incidents} onSelect={() => {}} compact={true} />
                <div style={{ position: 'absolute', bottom: 8, right: 8, fontFamily: C.mono, fontSize: 8, color: C.fg4, background: 'rgba(22,20,18,0.8)', padding: '2px 6px', borderRadius: 3 }}>클릭하여 확대</div>
              </div>
            </div>

            {/* Zone distribution */}
            <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minHeight: 0, overflowY: 'auto' }}>
              <SecLabel>ZONE DISTRIBUTION</SecLabel>
              {(() => {
                const zones = [
                  { label: 'outer_fence_north', cnt: 3 },
                  { label: 'server_room_b2',    cnt: 2 },
                  { label: 'warehouse_south',    cnt: 2 },
                  { label: 'inner_gate_east',    cnt: 1 },
                  { label: 'parking_west',       cnt: 1 },
                ];
                const maxCnt = Math.max(...zones.map(z => z.cnt));
                return zones.map((loc, i) => {
                  const pct = loc.cnt / maxCnt * 100;
                  return (
                    <div key={i} style={{ padding: '9px 0', borderTop: i > 0 ? `1px solid ${C.border}` : 'none' }}>
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline', marginBottom: 5 }}>
                        <span style={{ fontFamily: C.mono, fontSize: 17, color: C.fg1, lineHeight: 1, letterSpacing: '-1px' }}>{loc.cnt}</span>
                        <span style={{ fontFamily: C.mono, fontSize: 8.5, color: C.fg4, textAlign: 'right' }}>{loc.label.replace(/_/g, ' ')}</span>
                      </div>
                      <div style={{ position: 'relative', height: 2 }}>
                        <div style={{ position: 'absolute', left: 0, top: 0, width: `${pct}%`, height: '100%', background: C.orange }} />
                        <div style={{ position: 'absolute', left: `${pct}%`, top: 0, right: 0, height: '100%', backgroundImage: `repeating-linear-gradient(90deg, rgba(255,255,255,0.15) 0px, rgba(255,255,255,0.15) 2px, transparent 2px, transparent 5px)` }} />
                      </div>
                    </div>
                  );
                });
              })()}
              <button
                onClick={() => setPage('incidents')}
                style={{ marginTop: 14, display: 'flex', alignItems: 'center', gap: 6, background: 'transparent', border: `1px solid ${C.border2}`, borderRadius: '999px', padding: '7px 16px', fontFamily: C.mono, fontSize: 8.5, color: C.fg3, letterSpacing: '1px', textTransform: 'uppercase', cursor: 'pointer', alignSelf: 'flex-start' }}
              >
                <Icon name="arrow-up-right" size={10} color={C.fg4} /> VIEW ALL INCIDENTS
              </button>
            </div>
          </div>
        </div>

        {/* Right: Activity */}
        <div style={{ flex: 1, display: 'flex', flexDirection: 'column', overflow: 'hidden', flexShrink: 0 }}>
          <div style={{ padding: '14px 20px 10px', flexShrink: 0, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <span style={{ fontFamily: C.sans, fontSize: 13, color: C.fg1 }}>Recent Activity</span>
            <div style={{ display: 'flex', background: C.bg3, border: `1px solid ${C.border}`, borderRadius: C.R, overflow: 'hidden' }}>
              {['recent', 'severity'].map(s => (
                <button
                  key={s}
                  onClick={() => setActSort(s)}
                  style={{ padding: '5px 10px', background: actSort === s ? C.orangeD : 'transparent', borderRight: s === 'recent' ? `1px solid ${C.border}` : 'none', fontFamily: C.mono, fontSize: 8, color: actSort === s ? C.orange : C.fg4, letterSpacing: '0.8px', textTransform: 'uppercase', cursor: 'pointer', transition: 'all 150ms ease' }}
                >
                  {s === 'recent' ? '최근순' : '심각도'}
                </button>
              ))}
            </div>
          </div>
          <div style={{ flex: 1, overflowY: 'auto', padding: '0 20px 12px' }}>
            {sortedActivity.map((a, i) => (
              <div
                key={i}
                onClick={() => { if (a.link) { onSelect(a.link); setPage('detail'); } }}
                style={{ display: 'flex', gap: 9, alignItems: 'center', padding: '7px 0', borderBottom: i < sortedActivity.length - 1 ? `1px solid ${C.border}` : 'none', cursor: a.link ? 'pointer' : 'default' }}
              >
                <div style={{ width: 24, height: 24, background: C.bg3, border: `1px solid ${C.border}`, borderRadius: '50%', display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0 }}>
                  <Icon name={a.icon} size={11} color={a.color} />
                </div>
                <div style={{ flex: 1, minWidth: 0 }}>
                  <div style={{ fontFamily: C.sans, fontSize: 11, color: C.fg1, marginBottom: 1, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{a.title}</div>
                  <div style={{ fontFamily: C.mono, fontSize: 8, color: 'rgba(242,242,244,0.45)' }}>{a.sub}</div>
                </div>
                <div style={{ display: 'flex', alignItems: 'center', gap: 6, flexShrink: 0 }}>
                  {a.severity > 0 && <span style={{ fontFamily: C.mono, fontSize: 8.5, color: SC(a.severity) }}>{a.severity.toFixed(2)}</span>}
                  {a.link && <Icon name="arrow-up-right" size={10} color={C.fg4} />}
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Map Modal */}
      {mapOpen && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.8)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 9000 }} onClick={() => setMapOpen(false)}>
          <div style={{ background: C.bg2, border: `1px solid ${C.border2}`, borderRadius: C.R, width: '80vw', height: '70vh', overflow: 'hidden', animation: 'modalIn 200ms ease' }} onClick={e => e.stopPropagation()}>
            <div style={{ padding: '14px 20px', borderBottom: `1px solid ${C.border}`, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <span style={{ fontFamily: C.mono, fontSize: 11, color: C.fg1, letterSpacing: '1px', textTransform: 'uppercase' }}>FACILITY OVERVIEW — factory_A / factory_B</span>
              <button onClick={() => setMapOpen(false)} style={{ color: C.fg3 }}><Icon name="x" size={16} color={C.fg3} /></button>
            </div>
            <div style={{ height: 'calc(100% - 49px)', overflow: 'hidden' }}>
              <FactoryFloor incidents={incidents} onSelect={(id) => { onSelect(id); setMapOpen(false); setPage('detail'); }} compact={false} />
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
