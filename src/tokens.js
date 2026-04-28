export const C = {
  bg: '#0d0d0f', bg2: '#111113', bg3: '#171719', bg4: '#1d1d1f',
  orange: '#E8622A', orangeD: 'rgba(232,98,42,0.10)', orangeB: 'rgba(232,98,42,0.28)',
  fg1: '#f2f2f4', fg2: 'rgba(242,242,244,0.70)', fg3: 'rgba(242,242,244,0.42)', fg4: 'rgba(242,242,244,0.25)',
  border: 'rgba(255,255,255,0.06)', border2: 'rgba(255,255,255,0.11)',
  mono: "'DM Mono',ui-monospace,'SF Mono',Menlo,Monaco,Consolas,monospace",
  sans: "'DM Sans',sans-serif",
  R: '5px',
};

export const STATUS = {
  detected:    { label: 'DETECTED',    color: 'rgba(255,255,255,0.9)',    bg: 'rgba(255,255,255,0.08)',  border: 'rgba(255,255,255,0.25)',  dot: '#94a3b8' },
  reviewed:    { label: 'REVIEWED',    color: 'rgba(234,179,8,0.9)',      bg: 'rgba(234,179,8,0.12)',    border: 'rgba(234,179,8,0.3)',     dot: '#FACC15' },
  confirmed:   { label: 'CONFIRMED',   color: 'rgba(34,197,94,0.9)',      bg: 'rgba(34,197,94,0.12)',    border: 'rgba(34,197,94,0.3)',     dot: '#22C55E' },
  false_alarm: { label: 'FALSE ALARM', color: 'rgba(239,68,68,0.9)',      bg: 'rgba(239,68,68,0.12)',    border: 'rgba(239,68,68,0.3)',     dot: '#EF4444' },
};

// Score color: red ≥ 0.85, yellow ≥ 0.70, slate otherwise
export const SC = (s) => s >= 0.85 ? '#EF4444' : s >= 0.70 ? '#FACC15' : '#94a3b8';
