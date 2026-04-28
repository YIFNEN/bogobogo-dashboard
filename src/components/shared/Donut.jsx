import { C } from '../../tokens';

export default function Donut({ pct, color = C.orange, size = 40 }) {
  const r = (size - 5) / 2;
  const circ = 2 * Math.PI * r;
  return (
    <svg width={size} height={size} style={{ display: 'block', flexShrink: 0 }}>
      <circle cx={size / 2} cy={size / 2} r={r} fill="none" stroke={C.border2} strokeWidth="3" />
      <circle
        cx={size / 2} cy={size / 2} r={r} fill="none" stroke={color} strokeWidth="3"
        strokeDasharray={`${pct / 100 * circ} ${circ}`}
        strokeDashoffset={circ / 4}
        strokeLinecap="round"
      />
      <text x={size / 2} y={size / 2 + 3.5} textAnchor="middle" fill={C.fg2} fontSize="9" fontFamily={C.mono}>
        {pct}%
      </text>
    </svg>
  );
}
