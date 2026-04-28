import { STATUS } from '../../tokens';

export default function Badge({ status, sm }) {
  const m = STATUS[status];
  if (!m) return null;
  return (
    <span style={{
      display: 'inline-flex', alignItems: 'center', gap: 5,
      padding: sm ? '2px 7px' : '3px 10px',
      background: m.bg, border: `1px solid ${m.border}`, borderRadius: 4,
      fontFamily: "'DM Mono',ui-monospace,monospace",
      fontSize: sm ? 9 : 10, color: m.color,
      letterSpacing: '0.8px', textTransform: 'uppercase',
      whiteSpace: 'nowrap', flexShrink: 0,
    }}>
      <span style={{ width: 5, height: 5, borderRadius: '50%', background: m.dot, display: 'inline-block', flexShrink: 0 }} />
      {m.label}
    </span>
  );
}
