# Architecture Overview

## Responsibility Split

```mermaid
flowchart LR
  B["Backend API and WebSocket<br/>incident source of truth"] --> D["Dashboard<br/>incident list, detail, status, AI report"]
  B --> U["Unity Virtual Map<br/>spatial event visualization"]
  D --> UGL["Optional Unity WebGL map slot<br/>fallback if unavailable"]
  D --> FM["Fallback factory map<br/>stable dashboard view"]
```

The backend and dashboard state define the incident lifecycle. Unity visualizes where an incident appears in a 3D factory-like space, but it does not decide whether an incident is valid.

## Event Flow

```mermaid
sequenceDiagram
  participant Backend
  participant Dashboard
  participant Unity as Unity Virtual Map
  participant Operator

  Backend->>Dashboard: incident_created / incident_status_updated
  Backend->>Unity: WebSocket event with camera_id and status
  Unity->>Unity: highlight CCTV marker and play mapped scenario
  Operator->>Dashboard: open incident detail
  Operator->>Dashboard: review evidence and update status
  Dashboard->>Backend: status update / report generation request
```

## Why Unity Was Added

The dashboard is effective for lists, evidence, and workflow state, but it does not naturally show spatial relationships such as:

- which CCTV observed the incident,
- where the camera is positioned,
- how a robot or intruder moved through a scene,
- how multiple camera viewpoints relate to a physical area.

Unity was used to prototype this spatial layer. It provides camera movement, CCTV view switching, multi-camera panels, robot/drone scenario playback, and BIM/CAD extension experiments.

## WebGL and Fallback Decision

An embedded Unity WebGL map was investigated, but the high-asset Unity scene made WebGL less reliable for demo recording. Browser memory, build output structure, WebGL runtime initialization, and asset loading risk could break the dashboard map area.

The final demo therefore used:

- Dashboard as the primary review and report UI.
- Unity standalone/editor as the Virtual Map visualization.
- WebSocket events to keep both views aligned.
- A dashboard fallback map for stable frontend operation.

## Scenario Mapping

The demo used camera-centered scenario mapping:

| Camera | Scenario |
| --- | --- |
| `cam_01` | Fence climbing |
| `cam_02` | Fence damage |
| `cam_03` | Facility filming |
| `cam_04` | Facility damage |

This mapping was a demo simplification. In a production system, scenario type, zone, object id, and policy context should come from backend incident data.
