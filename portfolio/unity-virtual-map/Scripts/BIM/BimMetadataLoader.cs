using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class BimMetadataLoader : MonoBehaviour
{
    [Header("Import Target")]
    public Transform importedModelRoot;

    [Header("Metadata")]
    public TextAsset metadataJson;
    public bool applyOnStart = true;
    public bool includeInactiveChildren = true;

    [Header("Matching")]
    public bool matchByName = true;
    public bool matchByNormalizedName = true;
    public bool matchByContainedName = true;
    public bool createMissingMetadataComponents = true;

    private void Start()
    {
        if (applyOnStart)
        {
            ApplyMetadata();
        }
    }

    [ContextMenu("Apply Metadata")]
    public void ApplyMetadata()
    {
        if (importedModelRoot == null || metadataJson == null)
        {
            Debug.LogWarning("BIM metadata loader needs an imported model root and metadata JSON.", this);
            return;
        }

        BimMetadataCollection collection;
        try
        {
            collection = JsonUtility.FromJson<BimMetadataCollection>(metadataJson.text);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to parse BIM metadata JSON: {ex.Message}", this);
            return;
        }

        if (collection == null || collection.objects == null)
        {
            Debug.LogWarning("BIM metadata JSON has no objects array.", this);
            return;
        }

        var transformsByName = new Dictionary<string, Transform>(StringComparer.OrdinalIgnoreCase);
        var transformsByNormalizedName = new Dictionary<string, Transform>(StringComparer.OrdinalIgnoreCase);
        var allTransforms = new List<Transform>();
        foreach (var child in importedModelRoot.GetComponentsInChildren<Transform>(includeInactiveChildren))
        {
            allTransforms.Add(child);
            if (!transformsByName.ContainsKey(child.name))
            {
                transformsByName.Add(child.name, child);
            }

            string normalizedName = NormalizeName(child.name);
            if (!string.IsNullOrWhiteSpace(normalizedName) && !transformsByNormalizedName.ContainsKey(normalizedName))
            {
                transformsByNormalizedName.Add(normalizedName, child);
            }
        }

        var applied = 0;
        var unmatched = 0;
        foreach (var entry in collection.objects)
        {
            if (entry == null)
            {
                continue;
            }

            Transform target = FindMatchingTransform(entry, transformsByName, transformsByNormalizedName, allTransforms);

            if (target == null)
            {
                unmatched++;
                continue;
            }

            var metadata = target.GetComponent<BimObjectMetadata>();
            if (metadata == null && createMissingMetadataComponents)
            {
                metadata = target.gameObject.AddComponent<BimObjectMetadata>();
            }

            if (metadata == null)
            {
                continue;
            }

            metadata.sourceGlobalId = entry.sourceGlobalId;
            metadata.sourceName = entry.sourceName;
            metadata.sourceType = entry.sourceType;
            metadata.objectId = entry.objectId;
            metadata.zoneId = entry.zoneId;
            metadata.floorName = entry.floorName;
            metadata.riskClass = entry.riskClass;
            metadata.notes = entry.notes;
            applied++;
        }

        Debug.Log($"Applied BIM metadata to {applied} imported objects. Unmatched metadata entries: {unmatched}.", this);
        if (applied == 0 && collection.objects.Length > 0)
        {
            Debug.LogWarning(
                "No BIM metadata matched the imported hierarchy. Revit/FBX may have renamed objects. " +
                "Select the FBX root in Hierarchy and confirm Imported Model Root points to that scene instance, then retry. " +
                "If it still stays at 0, this FBX likely collapsed source object names during export.",
                this);
        }
    }

    private Transform FindMatchingTransform(
        BimMetadataEntry entry,
        Dictionary<string, Transform> transformsByName,
        Dictionary<string, Transform> transformsByNormalizedName,
        List<Transform> allTransforms)
    {
        if (matchByName)
        {
            Transform exact = null;
            if (!string.IsNullOrWhiteSpace(entry.unityName))
            {
                transformsByName.TryGetValue(entry.unityName, out exact);
            }

            if (exact == null && !string.IsNullOrWhiteSpace(entry.sourceName))
            {
                transformsByName.TryGetValue(entry.sourceName, out exact);
            }

            if (exact != null)
            {
                return exact;
            }
        }

        if (matchByNormalizedName)
        {
            Transform normalized = null;
            string normalizedUnityName = NormalizeName(entry.unityName);
            if (!string.IsNullOrWhiteSpace(normalizedUnityName))
            {
                transformsByNormalizedName.TryGetValue(normalizedUnityName, out normalized);
            }

            if (normalized == null)
            {
                string normalizedSourceName = NormalizeName(entry.sourceName);
                if (!string.IsNullOrWhiteSpace(normalizedSourceName))
                {
                    transformsByNormalizedName.TryGetValue(normalizedSourceName, out normalized);
                }
            }

            if (normalized != null)
            {
                return normalized;
            }
        }

        if (matchByContainedName)
        {
            string normalizedUnityName = NormalizeName(entry.unityName);
            string normalizedSourceName = NormalizeName(entry.sourceName);
            foreach (Transform candidate in allTransforms)
            {
                string candidateName = NormalizeName(candidate.name);
                if (IsContainedMatch(candidateName, normalizedUnityName) ||
                    IsContainedMatch(candidateName, normalizedSourceName))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private static bool IsContainedMatch(string candidateName, string metadataName)
    {
        if (string.IsNullOrWhiteSpace(candidateName) || string.IsNullOrWhiteSpace(metadataName))
        {
            return false;
        }

        if (metadataName.Length < 4)
        {
            return false;
        }

        return candidateName.Contains(metadataName) || metadataName.Contains(candidateName);
    }

    private static string NormalizeName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        char[] buffer = new char[value.Length];
        int count = 0;
        foreach (char ch in value)
        {
            if (char.IsLetterOrDigit(ch))
            {
                buffer[count++] = char.ToLowerInvariant(ch);
            }
        }

        return new string(buffer, 0, count);
    }
}

[Serializable]
public sealed class BimMetadataCollection
{
    public string schemaVersion = "bogobogo_bim_metadata.v1";
    public BimMetadataEntry[] objects;
}

[Serializable]
public sealed class BimMetadataEntry
{
    public string unityName;
    public string sourceGlobalId;
    public string sourceName;
    public string sourceType;
    public string objectId;
    public string zoneId;
    public string floorName;
    public string riskClass;
    public string notes;
}
