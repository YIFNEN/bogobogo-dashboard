#!/usr/bin/env python
"""Extract lightweight IFC metadata for the Unity Virtual Map prototype.

Usage:
  python Tools/BIM/extract_ifc_metadata.py input.ifc Assets/Imported/BIM/Source/bim_metadata.json

The script intentionally exports only stable identity and classification fields.
Geometry conversion should be handled separately by Revit, Blender, IfcConvert,
or another BIM/CAD tool.
"""

from __future__ import annotations

import json
import sys
from pathlib import Path


def safe_text(value: object) -> str:
    if value is None:
        return ""
    return str(value)


def main() -> int:
    if len(sys.argv) != 3:
        print("Usage: extract_ifc_metadata.py <input.ifc> <output.json>")
        return 2

    input_path = Path(sys.argv[1])
    output_path = Path(sys.argv[2])

    try:
        import ifcopenshell  # type: ignore
    except Exception as exc:  # pragma: no cover - depends on local environment
        print("ifcopenshell is not installed.")
        print("Install it in a local venv with: pip install ifcopenshell")
        print(f"Import error: {exc}")
        return 1

    model = ifcopenshell.open(str(input_path))
    objects = []

    target_prefixes = (
        "IfcSpace",
        "IfcBuildingStorey",
        "IfcWall",
        "IfcDoor",
        "IfcWindow",
        "IfcSlab",
        "IfcColumn",
        "IfcBeam",
        "IfcStair",
        "IfcCovering",
        "IfcFlow",
        "IfcDistribution",
        "IfcFurnishing",
        "IfcBuildingElementProxy",
    )

    for entity in model.by_type("IfcProduct"):
        entity_type = entity.is_a()
        if not entity_type.startswith(target_prefixes):
            continue

        global_id = safe_text(getattr(entity, "GlobalId", ""))
        name = safe_text(getattr(entity, "Name", ""))
        unity_name = name or global_id or entity_type
        safe_object_key = global_id or unity_name.replace(" ", "_")

        objects.append(
            {
                "unityName": unity_name,
                "sourceGlobalId": global_id,
                "sourceName": name,
                "sourceType": entity_type,
                "objectId": f"bim_{safe_object_key}",
                "zoneId": "",
                "floorName": "",
                "riskClass": "normal",
                "notes": "Auto-extracted from IFC. Fill zoneId/floorName/riskClass after import review.",
            }
        )

    payload = {
        "schemaVersion": "bogobogo_bim_metadata.v1",
        "sourceFile": str(input_path),
        "objects": objects,
    }

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"Wrote {len(objects)} BIM metadata entries to {output_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
