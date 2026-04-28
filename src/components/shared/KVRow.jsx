import { C } from '../../tokens';

export default function KVRow({ label, children, mono }) {
  return (
    <div style={{
      display: 'flex', justifyContent: 'space-between', alignItems: 'center',
      padding: '8px 0', borderBottom: `1px solid ${C.border}`,
    }}>
      <span style={{
        fontFamily: C.mono, fontSize: 9, color: C.fg4,
        letterSpacing: '1.2px', textTransform: 'uppercase',
        flexShrink: 0, marginRight: 16,
      }}>{label}</span>
      <span style={{ fontFamily: mono ? C.mono : C.sans, fontSize: 12, color: C.fg2, textAlign: 'right' }}>
        {children}
      </span>
    </div>
  );
}
