# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
npm install       # install dependencies
npm run dev       # start dev server (http://localhost:5173)
npm run build     # production build
npm run preview   # preview production build
```

No test runner or linter is configured.

## Architecture

**보고보고 Security Dashboard** — a React 18 + Vite industrial security incident management dashboard. All styling is inline (no CSS framework). Fonts are DM Sans + DM Mono loaded via Google Fonts in `index.html`.

### Pages (controlled by `page` state in `App.jsx`)
| page value | component |
|---|---|
| `home` | `DashHome` — KPI strip, FactoryFloor map, zone distribution, activity feed |
| `incidents` | `Kanban` — drag-and-drop 4-column board (detected → reviewed → confirmed / false_alarm) |
| `detail` | `Detail` — tabbed incident detail (Overview / Notes / AI Report / Respond) |
| `analytics` | `Analytics` — AI Report viewer with report history sidebar |
| `settings` | `SettingsPage` — left-nav settings with 7 sections |

### Data Schema — API Contract v2.0
All incident data uses nested fields matching `Dashboard_API_Contract_v2.0`:

```js
// IncidentListItemProjection (mockIncidents.js + App state)
{
  incident_id, event_type, status,
  detected_at,                       // ISO 8601
  location: { site, zone, camera_id },
  score: { eventization_score, detector_confidence_avg },
  thumbnail_url,
  isNew,   // UI-only flag
  op,      // UI-only: creating operator id
}

// IncidentDetailProjection (mockDetails.js / DETAIL_DB)
{
  timeline,          // [{ ts, event, hi? }]
  eventization_basis: { line_crossed, roi_entered, duration_sec, zone_policy },
  source_summary:    { source_type, source_id, camera_id },
  evidence:          { thumbnail_url, clip_url, objects, track_ids },
  operator:          { note, reviewed_by, reviewed_at },
  report_summary:    { report_available, report_id, report_generated_at },
  status_history_preview: [{ from, to, actor: { type, id }, at, note }],
  history,           // internal audit log
  ai_output,         // null until confirmed; then { summary, checklist, report_draft, meta }
}
```

**Critical:** Never use flat field names (`inc.id`, `inc.zone`, `inc.score`, `det.basis`, `det.note`). Always use the nested paths above.

### Status State Machine
```
detected ──→ reviewed ──→ confirmed (terminal)
         ↘              ↘
          └──────────────→ false_alarm (terminal, requires mandatory note)
```
`confirmed` transition triggers AI output generation after 1800ms (simulated in `handleStatusChange` in App.jsx).

### Design Tokens (`src/tokens.js`)
- `C` — all color/font/radius constants. Never hardcode colors inline.
- `STATUS[status]` — per-status label, color, bg, border, dot.
- `SC(score)` — score → color function (red ≥ 0.85, yellow ≥ 0.70, slate otherwise).

### Shared Components (`src/components/shared/`)
| Component | Usage |
|---|---|
| `Icon` | `<Icon name="shield" size={16} color={C.fg2} />` — wraps lucide-react via ICON_MAP |
| `Badge` | Status pill: `<Badge status="confirmed" sm />` |
| `Spark` | Inline sparkline SVG |
| `Donut` | Donut chart SVG |
| `SecLabel` | Section header label |
| `KVRow` | Key/value row |
| `ScoreBar` | Horizontal score bar |

To add a new lucide icon, import it in `Icon.jsx` and add to `ICON_MAP`.

### CSS Animations
All keyframes are defined in `src/index.css`. Available animation names: `alertFlash`, `pulse`, `fadeIn`, `scan`, `modalIn`, `glowPulse`, `ripple`, `dash`, `ff-fast`, `ff-slow`. Use via `animation:` in inline styles.
