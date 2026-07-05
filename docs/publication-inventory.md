# Publication Inventory

This inventory records what was intentionally added to the public portfolio branch and what was intentionally excluded.

## Added Documentation

- `README.md`: portfolio overview, role, demo concept, safe run instructions, exclusion summary.
- `docs/simulator-overview.md`: simulator-first explanation of the Unity Virtual Map responsibility and controls.
- `docs/architecture.md`: backend, dashboard, and Unity Virtual Map responsibility split.
- `docs/bim-cad-extension.md`: BIM/CAD import and semi-automated CCTV placement extension.
- `docs/demo-flow.md`: recording/demo flow and Unity controls.
- `docs/security-and-exclusions.md`: pre-push safety rules.
- `docs/media/`: sanitized screenshot assets only.

## Added Unity Virtual Map Source Samples

- Runtime coordination, CCTV marker/view, camera control, drone/player robot, scenario control, and mech attack adapter scripts.
- BIM/CAD metadata, zone anchor, CCTV placement assistant, and audit helper scripts.

The source samples are enough to explain the implemented simulator architecture and thesis/portfolio contribution, but they intentionally do not reconstruct the full Unity project.

## Added Dashboard Source Samples

- `portfolio/dashboard-integration/UnityFactoryMap.tsx`
- `portfolio/dashboard-integration/FactoryMapFallback.tsx`
- `portfolio/dashboard-integration/unityDashboardState.ts`
- `portfolio/dashboard-integration/verify-unity-dashboard-state.mjs`

These files show the Unity/fallback map integration approach. They are supporting source samples from the larger monorepo, not the main portfolio focus and not a full app migration.

## Explicitly Excluded

- `.env` and backend credential files.
- OpenAI, Supabase, service-role, database, JWT, and private API keys.
- Unity `Library`, `Temp`, `Logs`, `UserSettings`, build folders, and generated solution files.
- Windows/macOS/WebGL executable build outputs.
- Raw incident videos, thumbnails, uploaded evidence, and private recordings.
- Revit, IFC, FBX, NWD, point-cloud, and other CAD/BIM source files.
- Unity Asset Store packages and other redistributable-restricted assets.

## Verification Performed

The public branch was checked for:

- known key patterns such as OpenAI key prefixes, JWT-like Supabase keys, PostgreSQL URLs, and service-role names,
- local user/project absolute paths,
- files larger than 5 MB,
- generated build/output folders covered by `.gitignore`.

No publish-blocking secret or oversized file was found in the curated addition.
