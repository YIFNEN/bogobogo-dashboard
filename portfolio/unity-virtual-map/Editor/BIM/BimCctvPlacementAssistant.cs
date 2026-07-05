#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class BimCctvPlacementAssistant : EditorWindow
{
    private Transform importedModelRoot;
    private GameObject cctvPrefab;
    private Transform mountPoint;
    private Transform lookTarget;
    private SecurityCameraRig existingRig;
    private string cameraId = "cam_01";
    private string zoneId = "zone_unassigned";
    private float viewPointBackOffset = 0.35f;
    private float viewPointHeightOffset = 0.2f;
    private float previewViewSize = 1.2f;
    private float placeholderScale = 0.8f;
    private bool pickMountPoint;
    private bool pickLookTarget;
    private string status = "";

    [MenuItem("BOGOBOGO/Virtual Map/BIM/CCTV Placement Assistant")]
    public static void Open()
    {
        GetWindow<BimCctvPlacementAssistant>("CCTV Placement");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGui;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGui;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("CCTV Placement Assistant", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Set a mount point and a look target, then create a CCTV rig. This does not choose CCTV locations automatically; it automates rotation, ViewPoint, LookTarget, marker, and zone wiring from your chosen points.",
            MessageType.Info);

        importedModelRoot = (Transform)EditorGUILayout.ObjectField("Imported Model Root", importedModelRoot, typeof(Transform), true);
        cctvPrefab = (GameObject)EditorGUILayout.ObjectField("Optional CCTV Prefab", cctvPrefab, typeof(GameObject), false);
        existingRig = (SecurityCameraRig)EditorGUILayout.ObjectField("Update Existing Rig", existingRig, typeof(SecurityCameraRig), true);

        EditorGUILayout.Space(6);
        cameraId = EditorGUILayout.TextField("Camera Id", cameraId);
        zoneId = EditorGUILayout.TextField("Zone Id", zoneId);
        viewPointBackOffset = EditorGUILayout.FloatField("ViewPoint Back Offset", viewPointBackOffset);
        viewPointHeightOffset = EditorGUILayout.FloatField("ViewPoint Height Offset", viewPointHeightOffset);
        previewViewSize = EditorGUILayout.FloatField("Preview View Size", previewViewSize);
        placeholderScale = EditorGUILayout.FloatField("Placeholder Scale", placeholderScale);

        EditorGUILayout.Space(6);
        mountPoint = (Transform)EditorGUILayout.ObjectField("Mount Point", mountPoint, typeof(Transform), true);
        lookTarget = (Transform)EditorGUILayout.ObjectField("Look Target", lookTarget, typeof(Transform), true);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Mount = Selection"))
            {
                mountPoint = CreateOrMoveMarker("CCTV_MountPoint", SelectionPositionOrPivot());
            }

            if (GUILayout.Button("Target = Selection"))
            {
                lookTarget = CreateOrMoveMarker("CCTV_LookTarget", SelectionPositionOrPivot());
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.backgroundColor = pickMountPoint ? Color.cyan : Color.white;
            if (GUILayout.Button("Pick Mount In Scene"))
            {
                pickMountPoint = !pickMountPoint;
                if (pickMountPoint) pickLookTarget = false;
                RepaintSceneViews();
            }

            GUI.backgroundColor = pickLookTarget ? Color.cyan : Color.white;
            if (GUILayout.Button("Pick Target In Scene"))
            {
                pickLookTarget = !pickLookTarget;
                if (pickLookTarget) pickMountPoint = false;
                RepaintSceneViews();
            }

            GUI.backgroundColor = Color.white;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Infer Zone From Target"))
            {
                zoneId = InferZoneId();
            }

            if (GUILayout.Button("Next Camera Id"))
            {
                cameraId = NextCameraId();
            }
        }

        EditorGUILayout.Space(8);
        if (GUILayout.Button(existingRig == null ? "Create CCTV Rig" : "Update Existing CCTV Rig"))
        {
            CreateOrUpdateRig();
        }

        if (GUILayout.Button("Preview CCTV View In Scene View"))
        {
            PreviewCctvView();
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            EditorGUILayout.HelpBox(status, MessageType.None);
        }
    }

    private void OnSceneGui(SceneView sceneView)
    {
        DrawSceneHandles();

        if (!pickMountPoint && !pickLookTarget)
        {
            return;
        }

        Event current = Event.current;
        Handles.BeginGUI();
        Rect labelRect = new Rect(12, 12, 420, 28);
        GUI.Label(labelRect, pickMountPoint ? "Click scene to set CCTV mount point" : "Click scene to set CCTV look target");
        Handles.EndGUI();

        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        if (current.type != EventType.MouseDown || current.button != 0)
        {
            return;
        }

        Vector3 point = PickPointFromMouse(current.mousePosition);
        if (pickMountPoint)
        {
            mountPoint = CreateOrMoveMarker("CCTV_MountPoint", point);
            pickMountPoint = false;
        }
        else if (pickLookTarget)
        {
            lookTarget = CreateOrMoveMarker("CCTV_LookTarget", point);
            pickLookTarget = false;
        }

        current.Use();
        Repaint();
        RepaintSceneViews();
    }

    private void DrawSceneHandles()
    {
        if (mountPoint != null)
        {
            Handles.color = Color.cyan;
            Handles.SphereHandleCap(0, mountPoint.position, Quaternion.identity, HandleUtility.GetHandleSize(mountPoint.position) * 0.18f, EventType.Repaint);
            Handles.Label(mountPoint.position + Vector3.up * HandleUtility.GetHandleSize(mountPoint.position) * 0.18f, "CCTV Mount");
        }

        if (lookTarget != null)
        {
            Handles.color = Color.yellow;
            Handles.SphereHandleCap(0, lookTarget.position, Quaternion.identity, HandleUtility.GetHandleSize(lookTarget.position) * 0.18f, EventType.Repaint);
            Handles.Label(lookTarget.position + Vector3.up * HandleUtility.GetHandleSize(lookTarget.position) * 0.18f, "Look Target");
        }

        if (mountPoint != null && lookTarget != null)
        {
            Handles.color = Color.green;
            Handles.DrawAAPolyLine(4f, mountPoint.position, lookTarget.position);
        }
    }

    private Vector3 PickPointFromMouse(Vector2 mousePosition)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 10000f))
        {
            return hit.point;
        }

        float planeY = 0f;
        Bounds bounds;
        if (importedModelRoot != null && TryGetRootBounds(importedModelRoot, out bounds))
        {
            planeY = bounds.center.y;
        }

        Plane plane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));
        if (plane.Raycast(ray, out float enter))
        {
            return ray.GetPoint(enter);
        }

        SceneView sceneView = SceneView.lastActiveSceneView;
        return sceneView != null ? sceneView.pivot : Vector3.zero;
    }

    private Transform CreateOrMoveMarker(string baseName, Vector3 position)
    {
        Transform root = EnsureChild(EnsureRoot("BIM_AutoGenerated"), "CCTV_Placement_Points");
        string name = cameraId + "_" + baseName;
        Transform existing = root.Find(name);
        GameObject marker;
        if (existing == null)
        {
            marker = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(marker, "Create CCTV placement marker");
            marker.transform.SetParent(root, false);
        }
        else
        {
            marker = existing.gameObject;
            Undo.RecordObject(marker.transform, "Move CCTV placement marker");
        }

        marker.transform.position = position;
        EditorSceneManager.MarkSceneDirty(marker.scene);
        Selection.activeGameObject = marker;
        return marker.transform;
    }

    private void CreateOrUpdateRig()
    {
        if (mountPoint == null || lookTarget == null)
        {
            status = "Mount Point and Look Target are both required.";
            return;
        }

        GameObject rigObject;
        SecurityCameraRig rig = existingRig;
        if (rig == null)
        {
            Transform root = EnsureChild(EnsureRoot("BIM_AutoGenerated"), "CCTV_Rigs");
            if (cctvPrefab != null)
            {
                rigObject = (GameObject)PrefabUtility.InstantiatePrefab(cctvPrefab);
                Undo.RegisterCreatedObjectUndo(rigObject, "Create CCTV rig");
            }
            else
            {
                rigObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Undo.RegisterCreatedObjectUndo(rigObject, "Create CCTV placeholder");
                rigObject.transform.localScale = Vector3.one * Mathf.Max(0.05f, placeholderScale);
            }

            rigObject.name = cameraId;
            rigObject.transform.SetParent(root, false);
            rig = rigObject.GetComponent<SecurityCameraRig>();
            if (rig == null) rig = rigObject.AddComponent<SecurityCameraRig>();
        }
        else
        {
            rigObject = rig.gameObject;
            Undo.RecordObject(rigObject.transform, "Update CCTV rig transform");
            Undo.RecordObject(rig, "Update CCTV rig");
        }

        rigObject.transform.position = mountPoint.position;
        Vector3 direction = lookTarget.position - mountPoint.position;
        if (direction.sqrMagnitude > 0.001f)
        {
            rigObject.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        rig.cameraId = cameraId;
        rig.zoneId = string.IsNullOrWhiteSpace(zoneId) ? InferZoneId() : zoneId;
        rig.lookTarget = EnsureRigChild(rig.transform, cameraId + "_LookTarget", lookTarget.position);
        rig.viewPoint = EnsureRigChild(rig.transform, cameraId + "_ViewPoint", CalculateViewPointPosition(rig.transform.position, lookTarget.position));
        rig.viewPoint.rotation = CalculateViewRotation(rig.viewPoint.position, lookTarget.position, rigObject.transform.rotation);

        EditorUtility.SetDirty(rig);
        EditorSceneManager.MarkSceneDirty(rigObject.scene);
        Selection.activeGameObject = rigObject;
        existingRig = rig;
        status = $"CCTV rig ready: {rig.cameraId}, zone={rig.zoneId}";
    }

    private Transform EnsureRigChild(Transform parent, string name, Vector3 position)
    {
        Transform child = parent.Find(name);
        if (child == null)
        {
            GameObject go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create CCTV helper");
            child = go.transform;
            child.SetParent(parent, true);
        }
        else
        {
            Undo.RecordObject(child, "Update CCTV helper");
        }

        child.position = position;
        return child;
    }

    private Vector3 CalculateViewPointPosition(Vector3 mount, Vector3 target)
    {
        Vector3 direction = target - mount;
        if (direction.sqrMagnitude < 0.001f)
        {
            direction = Vector3.forward;
        }

        return mount - direction.normalized * Mathf.Max(0f, viewPointBackOffset) + Vector3.up * viewPointHeightOffset;
    }

    private Quaternion CalculateViewRotation(Vector3 viewPosition, Vector3 target, Quaternion fallback)
    {
        Vector3 direction = target - viewPosition;
        if (direction.sqrMagnitude < 0.001f) return fallback;
        return Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private void PreviewCctvView()
    {
        SecurityCameraRig rig = existingRig;
        if (rig == null)
        {
            rig = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<SecurityCameraRig>() : null;
        }

        if (rig == null)
        {
            status = "Select or create a CCTV rig first.";
            return;
        }

        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null)
        {
            status = "No active Scene View found.";
            return;
        }

        sceneView.LookAt(rig.ViewPosition(), rig.ViewRotation(), Mathf.Clamp(previewViewSize, 0.1f, 20f));
        sceneView.Repaint();
        status = "Scene View moved to CCTV preview.";
    }

    private string InferZoneId()
    {
        Vector3 reference = lookTarget != null ? lookTarget.position : SelectionPositionOrPivot();
        BimZoneAnchor[] zones = FindObjectsOfType<BimZoneAnchor>(true);
        if (zones.Length == 0)
        {
            return string.IsNullOrWhiteSpace(zoneId) ? "zone_unassigned" : zoneId;
        }

        BimZoneAnchor nearest = zones.OrderBy(zone => (zone.transform.position - reference).sqrMagnitude).FirstOrDefault();
        return nearest != null ? nearest.zoneId : "zone_unassigned";
    }

    private string NextCameraId()
    {
        int max = 0;
        foreach (SecurityCameraRig rig in FindObjectsOfType<SecurityCameraRig>(true))
        {
            if (TryParseCameraIndex(rig.cameraId, out int index))
            {
                max = Mathf.Max(max, index);
            }
        }

        return "cam_" + (max + 1).ToString("00");
    }

    private static bool TryParseCameraIndex(string value, out int index)
    {
        index = 0;
        if (string.IsNullOrWhiteSpace(value)) return false;
        string digits = new string(value.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out index);
    }

    private Vector3 SelectionPositionOrPivot()
    {
        if (Selection.activeTransform != null)
        {
            return Selection.activeTransform.position;
        }

        SceneView sceneView = SceneView.lastActiveSceneView;
        return sceneView != null ? sceneView.pivot : Vector3.zero;
    }

    private static Transform EnsureRoot(string name)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null) return existing.transform;

        GameObject root = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(root, "Create BIM generated root");
        return root.transform;
    }

    private static Transform EnsureChild(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null) return existing;

        GameObject child = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(child, "Create BIM generated child");
        child.transform.SetParent(parent, false);
        return child.transform;
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

    private static void RepaintSceneViews()
    {
        foreach (SceneView view in SceneView.sceneViews)
        {
            view.Repaint();
        }
    }
}
#endif
