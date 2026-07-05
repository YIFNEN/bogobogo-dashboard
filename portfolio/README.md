# Portfolio Source Samples

This folder contains selected implementation samples from the BogoBogo capstone project.

The original working repository was a larger monorepo containing a dashboard app, backend, Unity simulator project, generated builds, evidence files, and third-party asset folders. Only the safe, reviewable source portions are copied here.

## Included

- `dashboard-integration/`: React and TypeScript code used to connect dashboard incident state to Unity or a fallback map.
- `unity-virtual-map/`: Unity runtime scripts and editor tools for the Virtual Map prototype.

## Not Included

- Full Unity scenes and prefabs.
- Asset Store packages.
- WebGL, Windows, or macOS build outputs.
- Private `.env` files and backend secrets.
- Uploaded evidence videos or real incident media.
- Raw CAD/BIM model files.

The goal is to show design decisions and implementation style without leaking private infrastructure or large licensed assets.
