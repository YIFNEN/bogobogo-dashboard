# BIM/CAD Virtual Map Extension

The Virtual Map prototype was extended with a small BIM/CAD experiment to show how a manually authored Unity scene could evolve into a more systematic spatial setup.

## Goal

The goal was not full digital twin automation. The implemented target was a semi-automated workflow:

1. Import a BIM/CAD-derived model into Unity.
2. Extract object metadata where available.
3. Generate candidate zone anchors for review.
4. Let a user choose CCTV mount points and look targets.
5. Automatically create CCTV rig settings, viewpoint, look target, marker, and preview.
6. Produce an audit report for zones or objects that still need manual review.

## Why Semi-Automation

Security camera placement is operationally sensitive. A fully automatic placement tool can suggest positions, but final CCTV location and target direction should be checked by a person who understands blind spots, privacy constraints, physical installation limits, and site-specific policies.

The prototype therefore automates repetitive Unity wiring while keeping important judgment points manual.

## Implemented Automation Points

| Area | Implemented Direction |
| --- | --- |
| Metadata | Attach `BimObjectMetadata` from extracted IFC/BIM metadata where object names match. |
| Zone review | Generate `BimZoneAnchor` objects as candidate review points. |
| CCTV placement | Use selected mount point and look target to create or update a CCTV rig. |
| View tuning | Generate `LookTarget` and `ViewPoint` so the user can preview the camera view. |
| Audit | Report unassigned zones, missing CCTV coverage, and objects needing manual review. |

## Limits

- Imported geometry quality depends on the source model and conversion path.
- Materials may not transfer cleanly from Revit/IFC/FBX into Unity.
- Metadata matching can fail when converted object names change.
- Actual object-level incident logging requires backend payloads such as `object_id`, `zone_id`, `event_type`, and policy context.
- Field-of-view coverage and CCTV optimization were left as future work.

## Thesis-Framing Statement

This work should be described as a Virtual Map prototype and semi-automated spatial setup tool, not as a complete industrial digital twin. It demonstrates how dashboard incident data can be connected to spatial visualization and how BIM/CAD imports could reduce repetitive scene setup work in a future system.
