import { C, SC } from '../../tokens';

export default function ScoreBar({ score, width = 54 }) {
  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: 7 }}>
      <div style={{ width, height: 2, background: C.border, flexShrink: 0 }}>
        <div style={{ width: `${score * 100}%`, height: '100%', background: SC(score) }} />
      </div>
      <span style={{ fontFamily: "'DM Mono',ui-monospace,monospace", fontSize: 10, color: SC(score) }}>
        {score.toFixed(2)}
      </span>
    </div>
  );
}
