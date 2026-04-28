import { C } from '../../tokens';

export default function SecLabel({ children, action }) {
  return (
    <div style={{
      display: 'flex', justifyContent: 'space-between', alignItems: 'center',
      fontFamily: C.mono, fontSize: 9, color: C.fg4,
      letterSpacing: '1.4px', textTransform: 'uppercase',
      marginBottom: 10, paddingBottom: 6, borderBottom: `1px solid ${C.border}`,
    }}>
      <span>{children}</span>
      {action}
    </div>
  );
}
