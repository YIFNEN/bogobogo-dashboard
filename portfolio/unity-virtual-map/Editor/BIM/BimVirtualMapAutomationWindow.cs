#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class BimVirtualMapAutomationWindow : EditorWindow
{
    private Transform importedModelRoot;
    private TextAsset metadataJson;
    private GameObject cctvPrefab;
    private int cctvCount = 8;
    private float cctvHeight = 7.5f;
    private float cctvDistancePadding = 5f;
    private float viewPointBackOffset = 0.35f;
    private float viewPointHeightOffset = 0.2f;
    private string auditText = "";
    private Vector2 scroll;

    [MenuItem("BOGOBOGO/Virtual Map/BIM/Automation Assistant")]
    public static void Open()
    {
        GetWindow<BimVirtualMapAutomationWindow>("BIM Automation");
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.LabelField("BIM/CAD Virtual Map Automation", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Use this after importing an FBX/IFC-derived model into BIMImportTest. It creates metadata bindings, zone anchors, and audit notes. For practical CCTV placement, prefer BIM > CCTV Placement Assistant.",
            MessageType.Info);

        importedModelRoot = (Transform)EditorGUILayout.ObjectField("Imported Model Root", importedModelRoot, typeof(Transform), true);
        metadataJson = (TextAsset)EditorGUILayout.ObjectField("Metadata JSON", metadataJson, typeof(TextAsset), false);
        cctvPrefab = (GameObject)EditorGUILayout.ObjectField("Optional CCTV Prefab", cctvPrefab, typeof(GameObject), false);
        cctvCount = EditorGUILayout.IntSlider("CCTV Candidate Count", cctvCount, 1, 16);
        cctvHeight = EditorGUILayout.FloatField("CCTV Height", cctvHeight);
        cctvDistancePadding = EditorGUILayout.FloatField("CCTV Distance Padding", cctvDistancePadding);
        viewPointBackOffset = EditorGUILayout.FloatField("ViewPoint Back Offset", viewPointBackOffset);
        viewPointHeightOffset = EditorGUILayout.FloatField("ViewPoint Height Offset", viewPointHeightOffset);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Use Selection As Root"))
            {
                importedModelRoot = Selection.activeTransform;
            }

            if (GUILayout.Button("Find Metadata JSON"))
            {
                metadataJson = FindDefaultMetadataJson();
            }
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("1. Metadata", EditorStyles.boldLabel);
        if (GUILayout.Button("Create/Update Metadata Loader"))
        {
            CreateOrUpdateMetadataLoader();
        }

        if (GUILayout.Button("Apply Metadata Now"))
        {
            ApplyMetadataNow();
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("2. Zones And Optional Rough CCTV Draft", EditorStyles.boldLabel);
        if (GUILayout.Button("Generate Zone Anchors From Metadata"))
        {
            GenerateZoneAnchors();
        }

        EditorGUILayout.HelpBox(
            "Rough CCTV generation only creates placeholders around model bounds. Use CCTV Placement Assistant for final mount/target-based rigs.",
            MessageType.None);

        if (GUILayout.Button("Optional: Generate Rough CCTV Placeholders Around Model"))
        {
            GenerateCctvCandidates();
        }

        if (GUILayout.Button("Create ViewPoint/LookTarget For Existing CCTV Rigs"))
        {
            CreateViewPointsForExistingCctv();
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("3. Review", EditorStyles.boldLabel);
        if (GUILayout.Button("Run Coverage / Risk Audit"))
        {
            auditText = RunAudit();
        }

        if (!string.IsNullOrWhiteSpace(auditText))
        {
            EditorGUILayout.TextArea(auditText, GUILayout.MinHeight(180));
        }

        EditorGUILayout.EndScrollView();
    }

    private TextAsset FindDefaultMetadataJson()
    {
        string[] guids = AssetDatabase.FindAssets("t:TextAsset metadata", new[] { "Assets/Imported/BIM/Source" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith("_metadata.json", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith("sample_bim_metadata.json", StringComparison.OrdinalIgnoreCase))
            {
                return AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            }
        }

        Debug.LogWarning("No metadata JSON found under Assets/Imported/BIM/Source.");
        return null;
    }

    private void CreateOrUpdateMetadataLoader()
    {
        if (importedModelRoot == null)
        {
            Debug.LogWarning("Assign Imported Model Root first.");
            return;
        }

        BimMetadataLoader loader = FindObjectOfType<BimMetadataLoader>();
        if (loader == null)
        {
            GameObject go = new GameObject("BIM_Metadata_Loader");
            Undo.RegisterCreatedObjectUndo(go, "Create BIM Metadata Loader");
            loader = go.AddComponent<BimMetadataLoader>();
        }

        Undo.RecordObject(loader, "Update BIM Metadata Loader");
        loader.importedModelRoot = importedModelRoot;
        loader.metadataJson = metadataJson;
        EditorUtility.SetDirty(loader);
        EditorSceneManager.MarkSceneDirty(loader.gameObject.scene);
        Selection.activeGameObject = loader.gameObject;
        Debug.Log("BIM metadata loader is ready.");
    }

    private void ApplyMetadataNow()
    {
        CreateOrUpdateMetadataLoader();
        BimMetadataLoader loader = FindObjectOfType<BimMetadataLoader>();
        if (loader != null)
        {
            loader.ApplyMetadata();
            EditorUtility.SetDirty(loader);
            EditorSceneManager.MarkSceneDirty(loader.gameObject.scene);
        }
    }

    private void GenerateZoneAnchors()
    {
        BimObjectMetadata[] metadataItems = FindObjectsOfType<BimObjectMetadata>(true);
        if (metadataItems.Length == 0)
        {
            Debug.LogWarning("No BimObjectMetadata components found. Apply metadata first.");
            return;
        }

        Transform root = EnsureRoot("BIM_AutoGenerated");
        Transform zoneRoot = EnsureChild(root, "Zone_Anchors");
        ClearChildren(zoneRoot);

        var groups = metadataItems
            .GroupBy(item => EffectiveZoneId(item))
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase);

        int count = 0;
        foreach (var group in groups)
        {
            Bounds bounds;
            bool hasBounds = TryGetGroupBounds(group, out bounds);
            GameObject go = new GameObject("zone_" + Sanitize(group.Key));
            Undo.RegisterCreatedObjectUndo(go, "Create BIM Zone Anchor");
            go.transform.SetParent(zoneRoot, false);
            go.transform.position = hasBounds ? bounds.center : AveragePosition(group);

            BimZoneAnchor anchor = go.AddComponent<BimZoneAnchor>();
            anchor.zoneId = group.Key;
            anchor.displayName = group.Key;
            anchor.riskClass = InferRiskClass(group);
            anchor.sourceTypesSummary = string.Join(", ", group.Select(item => item.sourceType).Where(item => !string.IsNullOrWhiteSpace(item)).Distinct().Take(6));
            anchor.sourceBounds = hasBounds ? bounds : new Bounds(go.transform.position, new Vector3(4f, 0.2f, 4f));
            anchor.requiresManualReview = group.Key == "zone_unassigned";
            anchor.reviewNote = anchor.requiresManualReview ? "No zoneId was assigned. Review this zone manually." : "";
            count++;
        }

        EditorSceneManager.MarkSceneDirty(zoneRoot.gameObject.scene);
        Debug.Log($"Generated {count} BIM zone anchors.");
    }

    private void GenerateCctvCandidates()
    {
        if (importedModelRoot == null)
        {
            Debug.LogWarning("Assign Imported Model Root first.");
            return;
        }

        Bounds bounds;
        if (!TryGetRootBounds(importedModelRoot, out bounds))
        {
            Debug.LogWarning("Could not calculate imported model bounds.");
            return;
        }

        Transform root = EnsureRoot("BIM_AutoGenerated");
        Transform cctvRoot = EnsureChild(root, "CCTV_Candidates");
        ClearChildren(cctvRoot);

        float radius = Mathf.Max(bounds.extents.x, bounds.extents.z) + Mathf.Max(1f, cctvDistancePadding);
        Vector3 center = bounds.center;
        int created = 0;
        for (int i = 0; i < cctvCount; i++)
        {
            float angle = Mathf.PI * 2f * i / cctvCount;
            Vector3 outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 position = center + outward * radius + Vector3.up * cctvHeight;

            GameObject cameraObject;
            if (cctvPrefab != null)
            {
                cameraObject = (GameObject)PrefabUtility.InstantiatePrefab(cctvPrefab);
                Undo.RegisterCreatedObjectUndo(cameraObject, "Create BIM CCTV Candidate");
            }
            else
            {
                cameraObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Undo.RegisterCreatedObjectUndo(cameraObject, "Create BIM CCTV Candidate");
                cameraObject.transform.localScale = new Vector3(0.6f, 0.35f, 0.9f);
            }

            cameraObject.name = "bim_cam_" + (i + 1).ToString("00");
            cameraObject.transform.SetParent(cctvRoot, false);
            cameraObject.transform.position = position;
            cameraObject.transform.rotation = Quaternion.LookRotation((center - position).normalized, Vector3.up);

            SecurityCameraRig rig = cameraObject.GetComponent<SecurityCameraRig>();
            if (rig == null) rig = cameraObject.AddComponent<SecurityCameraRig>();
            rig.cameraId = "bim_cam_" + (i + 1).ToString("00");
            rig.zoneId = NearestZoneId(position);

            CreateRigViewObjects(rig, center);
            created++;
        }

        EditorSceneManager.MarkSceneDirty(cctvRoot.gameObject.scene);
        Debug.Log($"Generated {created} BIM CCTV candidates.");
    }

    private void CreateViewPointsForExistingCctv()
    {
        SecurityCameraRig[] rigs = FindObjectsOfType<SecurityCameraRig>(true);
        if (rigs.Length == 0)
        {
            Debug.LogWarning("No SecurityCameraRig components found.");
            return;
        }

        Vector3 fallbackTarget = Vector3.zero;
        Bounds bounds;
        if (importedModelRoot != null && TryGetRootBounds(importedModelRoot, out bounds))
        {
            fallbackTarget = bounds.center;
        }

        int created = 0;
        foreach (SecurityCameraRig rig in rigs)
        {
            CreateRigViewObjects(rig, fallbackTarget);
            created++;
        }

        Debug.Log($"Ensured ViewPoint/LookTarget for {created} CCTV rigs.");
    }

    private string RunAudit()
    {
        var lines = new List<string>();
        BimZoneAnchor[] zones = FindObjectsOfType<BimZoneAnchor>(true);
        SecurityCameraRig[] rigs = FindObjectsOfType<SecurityCameraRig>(true);
        BimObjectMetadata[] metadataItems = FindObjectsOfType<BimObjectMetadata>(true);

        lines.Add("BIM Virtual Map Audit");
        lines.Add("=====================");
        lines.Add($"Metadata objects: {metadataItems.Length}");
        lines.Add($"Zone anchors: {zones.Length}");
        lines.Add($"CCTV rigs: {rigs.Length}");
        lines.Add("");

        foreach (BimZoneAnchor zone in zones.OrderBy(item => item.zoneId))
        {
            int covering = rigs.Count(rig => string.Equals(rig.zoneId, zone.zoneId, StringComparison.OrdinalIgnoreCase));
            string flag = covering == 0 ? "NEEDS CCTV" : "covered";
            if (zone.riskClass == "high" || zone.riskClass == "restricted" || zone.riskClass == "hazard")
            {
                flag = covering == 0 ? "CRITICAL: high-risk zone has no CCTV" : "high-risk covered";
            }

            lines.Add($"- {zone.zoneId} | risk={zone.riskClass} | cctv={covering} | {flag}");
        }

        int unassigned = metadataItems.Count(item => string.IsNullOrWhiteSpace(item.zoneId));
        if (unassigned > 0)
        {
            lines.Add("");
            lines.Add($"Manual review required: {unassigned} BIM objects have no zoneId.");
        }

        string report = string.Join(Environment.NewLine, lines);
        string path = "Assets/Imported/BIM/Source/bim_virtual_map_audit.txt";
        File.WriteAllText(path, report);
        AssetDatabase.Refresh();
        Debug.Log($"BIM audit written to {path}");
        return report;
    }

    private void CreateRigViewObjects(SecurityCameraRig rig, Vector3 target)
    {
        Undo.RecordObject(rig, "Create CCTV View Objects");

        Transform lookTarget = rig.lookTarget;
        if (lookTarget == null)
        {
            GameObject targetObject = new GameObject(rig.cameraId + "_LookTarget");
            Undo.RegisterCreatedObjectUndo(targetObject, "Create CCTV LookTarget");
            targetObject.transform.SetParent(rig.transform, false);
            targetObject.transform.position = target;
            lookTarget = targetObject.transform;
            rig.lookTarget = lookTarget;
        }

        Transform viewPoint = rig.viewPoint;
        if (viewPoint == null)
        {
            GameObject viewObject = new GameObject(rig.cameraId + "_ViewPoint");
            Undo.RegisterCreatedObjectUndo(viewObject, "Create CCTV ViewPoint");
            viewObject.transform.SetParent(rig.transform, false);
            Vector3 direction = (lookTarget.position - rig.transform.position).normalized;
            if (direction.sqrMagnitude < 0.001f) direction = rig.transform.forward;
            viewObject.transform.position = rig.transform.position - direction * viewPointBackOffset + Vector3.up * viewPointHeightOffset;
            Vector3 viewDirection = lookTarget.position - viewObject.transform.position;
            if (viewDirection.sqrMagnitude > 0.001f)
            {
                viewObject.transform.rotation = Quaternion.LookRotation(viewDirection.normalized, Vector3.up);
            }

            viewPoint = viewObject.transform;
            rig.viewPoint = viewPoint;
        }

        EditorUtility.SetDirty(rig);
    }

    private static string EffectiveZoneId(BimObjectMetadata metadata)
    {
        if (!string.IsNullOrWhiteSpace(metadata.zoneId)) return metadata.zoneId;
        if (!string.IsNullOrWhiteSpace(metadata.sourceType) && metadata.sourceType.IndexOf("Space", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return string.IsNullOrWhiteSpace(metadata.objectId) ? "zone_space" : metadata.objectId;
        }

        return "zone_unassigned";
    }

    private static string InferRiskClass(IEnumerable<BimObjectMetadata> items)
    {
        foreach (BimObjectMetadata item in items)
        {
            string text = ((item.riskClass ?? "") + " " + (item.sourceName ?? "") + " " + (item.sourceType ?? "")).ToLowerInvariant();
            if (text.Contains("chemical") || text.Contains("fuel") || text.Contains("hazard") || text.Contains("restricted"))
            {
                return "high";
            }

            if (text.Contains("storage") || text.Contains("mechanical") || text.Contains("electrical") || text.Contains("hvac"))
            {
                return "medium";
            }
        }

        return "normal";
    }

    private static bool TryGetGroupBounds(IEnumerable<BimObjectMetadata> items, out Bounds bounds)
    {
        bool hasBounds = false;
        bounds = new Bounds();
        foreach (BimObjectMetadata item in items)
        {
            Renderer[] renderers = item.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
        }

        return hasBounds;
    }

    private static bool TryGetRootBounds(Transform root, out Bounds bounds)
    {
        bool hasBounds = false;
        bounds = new Bounds(root.position, Vector3.zero);
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private static Vector3 AveragePosition(IEnumerable<BimObjectMetadata> items)
    {
        Vector3 sum = Vector3.zero;
        int count = 0;
        foreach (BimObjectMetadata item in items)
        {
            sum += item.transform.position;
            count++;
        }

        return count == 0 ? Vector3.zero : sum / count;
    }

    private static Transform EnsureRoot(string name)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null) return existing.transform;

        GameObject root = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(root, "Create BIM Generated Root");
        return root.transform;
    }

    private static Transform EnsureChild(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null) return existing;

        GameObject child = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(child, "Create BIM Generated Child");
        child.transform.SetParent(parent, false);
        return child.transform;
    }

    private static void ClearChildren(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Undo.DestroyObjectImmediate(root.GetChild(i).gameObject);
        }
    }

    private static string Sanitize(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "unnamed";
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalid, '_');
        }

        return value.Replace(' ', '_');
    }

    private static string NearestZoneId(Vector3 position)
    {
        BimZoneAnchor[] zones = FindObjectsOfType<BimZoneAnchor>(true);
        if (zones.Length == 0) return "zone_unassigned";

        BimZoneAnchor nearest = zones
            .OrderBy(zone => (zone.transform.position - position).sqrMagnitude)
            .FirstOrDefault();
        return nearest != null ? nearest.zoneId : "zone_unassigned";
    }
}
#endif
