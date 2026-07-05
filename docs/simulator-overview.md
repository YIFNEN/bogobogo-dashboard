# Unity Virtual Map Simulator Overview

This document is the portfolio-facing summary of the Unity simulator work.

## Purpose

The simulator was built to make industrial security incidents spatially understandable. A dashboard can show incident cards, evidence, status, and reports, but it is harder to understand where a camera is placed, what the camera sees, and how an event unfolds in physical space. The Unity Virtual Map fills that role.

It does not replace the backend or the dashboard. It listens to incident events and visualizes them.

## Core Responsibilities

| Responsibility | Description |
| --- | --- |
| Incident visualization | Highlight the CCTV associated with an incoming incident event. |
| Spatial scenario playback | Play a short 3D scenario corresponding to the selected camera. |
| CCTV viewpoint preview | Let the operator enter the exact view configured for a CCTV. |
| Multi-camera monitoring | Show four CCTV viewpoints together for demo recording. |
| Operator navigation | Allow orbit, zoom, and inspection of the virtual factory scene. |
| BIM/CAD-assisted setup | Reduce repetitive CCTV rig setup after importing building models. |

## Event Handling

The simulator receives backend WebSocket events such as incident creation or status updates. For the demo, Unity uses `camera_id` as the main routing key:

```text
cam_01 -> fence climbing scenario
cam_02 -> fence damage scenario
cam_03 -> facility filming scenario
cam_04 -> facility damage scenario
```

When an event arrives, Unity updates the marker state of the matching CCTV and can play the mapped scenario. This makes the dashboard incident and the 3D scene feel synchronized during recording.

## Implemented Controls

| Control | Purpose |
| --- | --- |
| `C` | Enter selected CCTV viewpoint. |
| `Esc` | Return to main observation view. |
| `V` or `CCTV x4` | Toggle four-camera panel view. |
| Mouse drag / scroll | Inspect the scene with pan, orbit, and zoom. |
| `P` | Toggle player robot patrol mode. |
| `WASD` / arrow keys | Move the player robot in patrol mode. |
| `EXIT` | Reset demo state and quit or stop play mode. |

## BIM/CAD Extension

The later experiment focused on reducing manual Unity scene setup. Instead of automatically placing all CCTV cameras, the assistant lets a user define:

1. a mount point,
2. a look target,
3. camera id and zone id.

The tool then creates or updates a CCTV rig, aligns it toward the target, generates viewpoint/look-target references, and supports preview. This keeps the human decision about camera placement while automating repetitive Unity wiring.

## What This Prototype Does Not Claim

- It is not a complete industrial digital twin.
- It does not simulate real factory physics or production flow.
- It does not decide incident validity.
- It does not replace backend incident state.
- It does not contain production CCTV optimization or full blind-spot analysis.

The implementation is best understood as a Unity-based Virtual Map simulator for spatial incident presentation and demo validation.
