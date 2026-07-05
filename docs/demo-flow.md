# Demo Flow

This document summarizes the recording-oriented demo flow used for the project.

## Setup

1. Start the backend server locally.
2. Start the dashboard development server.
3. Start the Unity Virtual Map after the backend WebSocket server is available.
4. Open the dashboard in a browser and keep Unity visible for spatial visualization.

## Scenario Flow

1. Trigger an incident from the backend Swagger UI or a test broadcast endpoint.
2. Unity receives the WebSocket event and highlights the matching CCTV marker.
3. Unity plays the scenario mapped to the camera id.
4. The camera marker remains visible so the operator can inspect the incident location.
5. Switch to the dashboard.
6. Open the incident from the dashboard list or map marker.
7. Review evidence, timeline, and eventization basis.
8. Change status to a reviewed or confirmed state.
9. Generate and show the AI report draft when the scenario requires it.

## Demo Camera Mapping

| Camera | Demo Incident |
| --- | --- |
| `cam_01` | Fence climbing |
| `cam_02` | Fence damage |
| `cam_03` | Facility filming |
| `cam_04` | Facility damage |

## Unity Controls

| Control | Purpose |
| --- | --- |
| Mouse drag / scroll | Operator camera orbit, pan, and zoom depending on camera mode. |
| `C` | Enter selected CCTV view. |
| `Esc` | Return from CCTV view to operator view. |
| `V` or `CCTV x4` | Toggle the four-camera CCTV panel. |
| `P` | Toggle player robot patrol mode. |
| `WASD` / arrow keys | Move player robot in patrol mode. |
| `EXIT` | Reset demo state and close the Unity app or stop play mode. |

## Recording Tip

For the cleanest recording, use two OBS scenes:

- Unity scene: show the 3D event and CCTV markers.
- Dashboard scene: show incident card, detail page, status transition, and AI report generation.

Switch scenes after the Unity robot/scenario motion finishes and before opening the dashboard incident detail.
