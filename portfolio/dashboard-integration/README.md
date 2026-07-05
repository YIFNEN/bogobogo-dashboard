# Dashboard Integration Samples

These files show how the dashboard side handled the Virtual Map integration.

## Files

- `UnityFactoryMap.tsx`: Loads Unity WebGL when available, sends dashboard state to Unity, and falls back when WebGL fails or is disabled.
- `FactoryMapFallback.tsx`: SVG/HTML fallback factory map for demo reliability.
- `unityDashboardState.ts`: Converts incident and camera state into a compact Unity-compatible payload.
- `verify-unity-dashboard-state.mjs`: Contract-style verification script for the Unity map state mapper.

## Design Notes

The project initially considered embedding Unity WebGL directly in the dashboard map slot. In practice, the high-asset Unity scene was better treated as an optional path because WebGL loading, browser memory, and build artifacts made demo reliability fragile.

For the final demo flow, the dashboard kept a fallback map and the Unity Virtual Map could run separately as a standalone spatial visualization. The dashboard/backend incident state remained the source of truth.

## Public Repository Note

The copied samples reference types and styles from the original monorepo, so they are provided as source excerpts rather than drop-in files for the minimal public dashboard app.
