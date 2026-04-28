import { C } from '../tokens';
import Icon from './shared/Icon';

const NAV = [
  { id: 'home',      icon: 'layout-dashboard', label: 'Dashboard' },
  { id: 'incidents', icon: 'shield-alert',      label: 'Incidents' },
  { id: 'analytics', icon: 'file-text',         label: 'AI Report' },
  { id: 'settings',  icon: 'settings',          label: 'Settings' },
];

export default function Sidebar({ page, setPage, newCount }) {
  return (
    <div style={{ width: 188, background: C.bg2, borderRight: `1px solid ${C.border}`, display: 'flex', flexDirection: 'column', flexShrink: 0, height: '100%' }}>
      {/* Logo */}
      <div style={{ padding: '20px 18px 14px', borderBottom: `1px solid ${C.border}` }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          <div style={{ width: 22, height: 22, background: C.orange, borderRadius: 4, display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0 }}>
            <Icon name="shield" size={12} color="#fff" />
          </div>
          <div>
            <div style={{ fontFamily: C.mono, fontSize: 13, color: C.fg1, letterSpacing: '-0.3px' }}>보고보고</div>
            <div style={{ fontFamily: C.mono, fontSize: 7.5, color: C.fg4, letterSpacing: '1.8px', textTransform: 'uppercase' }}>Security Ops</div>
          </div>
        </div>
      </div>
      {/* CTA */}
      <div style={{ padding: '10px 12px 6px' }}>
        <button
          onClick={() => setPage('incidents')}
          style={{ width: '100%', background: C.orange, color: '#fff', padding: '8px 14px', borderRadius: '999px', fontFamily: C.mono, fontSize: 9.5, letterSpacing: '0.8px', textTransform: 'uppercase', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 6, transition: 'opacity 120ms ease' }}
          onMouseEnter={e => { e.currentTarget.style.opacity = '0.88'; }}
          onMouseLeave={e => { e.currentTarget.style.opacity = '1'; }}
        >
          <Icon name="plus" size={12} color="#fff" /> New Incident
        </button>
      </div>
      {/* Nav */}
      <nav style={{ flex: 1, padding: '6px 8px' }}>
        {NAV.map((item) => {
          const active = item.id === page;
          return (
            <div
              key={item.id}
              onClick={() => setPage(item.id)}
              style={{ display: 'flex', alignItems: 'center', gap: 9, padding: '8px 10px', marginBottom: 1, cursor: 'pointer', borderRadius: '6px', background: active ? C.orangeD : 'transparent', transition: 'background 120ms ease' }}
              onMouseEnter={e => { if (!active) e.currentTarget.style.background = 'rgba(255,255,255,0.03)'; }}
              onMouseLeave={e => { if (!active) e.currentTarget.style.background = 'transparent'; }}
            >
              <Icon name={item.icon} size={14} color={active ? C.orange : C.fg4} />
              <span style={{ fontFamily: C.sans, fontSize: 12.5, color: active ? C.fg1 : C.fg3, flex: 1 }}>{item.label}</span>
              {item.id === 'incidents' && newCount > 0 && (
                <span style={{ fontFamily: C.mono, fontSize: 8, color: C.orange, background: 'rgba(232,98,42,0.15)', padding: '1px 5px', borderRadius: '999px' }}>{newCount}</span>
              )}
            </div>
          );
        })}
      </nav>
      {/* User */}
      <div style={{ borderTop: `1px solid ${C.border}`, padding: '12px 14px 14px', flexShrink: 0 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 9 }}>
          <div style={{ width: 28, height: 28, borderRadius: '50%', background: `linear-gradient(135deg,${C.orange},#c94f1e)`, display: 'flex', alignItems: 'center', justifyContent: 'center', fontFamily: C.mono, fontSize: 10, color: '#fff', flexShrink: 0 }}>OP</div>
          <div style={{ minWidth: 0 }}>
            <div style={{ fontFamily: C.sans, fontSize: 11.5, color: C.fg1 }}>operator_01</div>
            <div style={{ fontFamily: C.mono, fontSize: 8.5, color: C.fg4 }}>@factory_A</div>
          </div>
        </div>
      </div>
    </div>
  );
}
