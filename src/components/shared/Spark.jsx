import { C } from '../../tokens';

export default function Spark({ data, color = C.orange, w = 64, h = 28 }) {
  const max = Math.max(...data, 1);
  const pts = data.map((v, i) => `${i / (data.length - 1) * w},${h - v / max * h * 0.85}`).join(' ');
  const area = [
    ...data.map((v, i) => `${i / (data.length - 1) * w},${h - v / max * h * 0.85}`),
    `${w},${h}`, `0,${h}`,
  ].join(' ');
  const id = 'sg' + color.replace(/[^a-z0-9]/gi, '');
  return (
    <svg width={w} height={h} style={{ display: 'block', flexShrink: 0 }}>
      <defs>
        <linearGradient id={id} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor={color} stopOpacity="0.22" />
          <stop offset="100%" stopColor={color} stopOpacity="0" />
        </linearGradient>
      </defs>
      <polygon points={area} fill={`url(#${id})`} />
      <polyline points={pts} fill="none" stroke={color} strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}
