#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Writes the active scene hierarchy, transforms, and basic object metadata to a JSON file
/// under tools/scene-snapshots/ (or a path you choose) so you can archive or share edits with others.
/// OPEN FEED → Tools → Export Scene Snapshot to JSON file…
/// </summary>
public static class SceneHierarchySnapshotExporter
{
    const string FormatId = "openfeed-scene-snapshot-v2";
    const string DefaultSnapshotsDir = "tools/scene-snapshots";

    [MenuItem("OPEN FEED/Tools/Export Scene Snapshot to JSON file…", false, 11)]
    static void ExportToFileMenu()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            EditorUtility.DisplayDialog("Scene snapshot", "No valid active scene loaded.", "OK");
            return;
        }

        if (!scene.isDirty && EditorUtility.DisplayDialog(
                "Scene snapshot",
                "Save the scene to disk first so the snapshot matches what is on disk?\n\n" +
                "Choose Save to write the .unity file, Skip to export unsaved editor state only.",
                "Save",
                "Skip"))
        {
            EditorSceneManager.SaveScene(scene);
        }

        string projectRoot = Path.GetDirectoryName(Application.dataPath) ?? ".";
        string defaultDir = Path.Combine(projectRoot, DefaultSnapshotsDir);
        if (!Directory.Exists(defaultDir))
            Directory.CreateDirectory(defaultDir);

        string safeScene = SanitizeFileName(scene.name);
        string defaultName = $"{safeScene}_snapshot_{DateTime.Now:yyyyMMdd_HHmmss}.json";

        string path = EditorUtility.SaveFilePanel(
            "Export scene snapshot (JSON)",
            defaultDir,
            defaultName,
            "json");

        if (string.IsNullOrEmpty(path))
            return;

        try
        {
            string json = BuildSnapshotJson(scene);
            File.WriteAllText(path, json, Encoding.UTF8);
            AssetDatabase.Refresh();
            EditorUtility.RevealInFinder(path);
            Debug.Log($"[Scene snapshot] Wrote {json.Length} characters to:\n{path}");
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("Scene snapshot failed", e.Message, "OK");
        }
    }

    static string SanitizeFileName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "Scene";
        char[] invalid = Path.GetInvalidFileNameChars();
        foreach (char c in invalid)
            name = name.Replace(c, '_');
        return name;
    }

    static string BuildSnapshotJson(Scene scene)
    {
        var sb = new StringBuilder(64 * 1024);
        sb.Append("{\n");
        AppendProp(sb, "format", FormatId, 0, true);
        AppendProp(sb, "exportedUtc", DateTime.UtcNow.ToString("o"), 0, true);
        AppendProp(sb, "unityVersion", Application.unityVersion, 0, true);
        AppendProp(sb, "sceneName", scene.name, 0, true);
        AppendProp(sb, "sceneAssetPath", scene.path, 0, true);
        sb.Append("  \"roots\": [\n");

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (i > 0) sb.Append(",\n");
            AppendNode(sb, roots[i].transform, "/", 4);
        }

        sb.Append("\n  ]\n}");
        return sb.ToString();
    }

    static void AppendNode(StringBuilder sb, Transform t, string parentPath, int indent)
    {
        string path = parentPath.TrimEnd('/') + "/" + t.name;
        string ind = new string(' ', indent);

        sb.Append(ind).Append("{\n");
        AppendProp(sb, "name", t.name, indent + 2, true);
        AppendProp(sb, "path", path, indent + 2, true);
        AppendProp(sb, "entityId", EntityId.ToULong(t.gameObject.GetEntityId()).ToString(), indent + 2, false);
        AppendProp(sb, "activeSelf", t.gameObject.activeSelf ? "true" : "false", indent + 2, false);
        AppendProp(sb, "activeInHierarchy", t.gameObject.activeInHierarchy ? "true" : "false", indent + 2, false);
        AppendProp(sb, "tag", t.tag, indent + 2, true);
        AppendProp(sb, "layer", LayerMask.LayerToName(t.gameObject.layer), indent + 2, true);
        AppendProp(sb, "layerIndex", t.gameObject.layer, indent + 2, false);
        AppendProp(sb, "isStatic", t.gameObject.isStatic ? "true" : "false", indent + 2, false);

        // Transforms: local + world (world helps if you re-parent later)
        sb.Append(ind).Append("  \"transform\": {\n");
        AppendVec3(sb, "localPosition", t.localPosition, indent + 4, true);
        AppendQuat(sb, "localRotation", t.localRotation, indent + 4, true);
        AppendVec3(sb, "localScale", t.localScale, indent + 4, true);
        AppendVec3(sb, "position", t.position, indent + 4, true);
        AppendQuat(sb, "rotation", t.rotation, indent + 4, false);
        sb.Append(ind).Append("  }");

        // Component type names (no serialized field values — keeps file smaller; add if needed)
        Component[] comps = t.gameObject.GetComponents<Component>();
        var names = new List<string>(comps.Length);
        foreach (Component c in comps)
        {
            if (c == null) { names.Add("(missing)"); continue; }
            names.Add(c.GetType().FullName ?? c.GetType().Name);
        }

        sb.Append(",\n");
        sb.Append(ind).Append("  \"components\": [");
        for (int i = 0; i < names.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            AppendJsonString(sb, names[i]);
        }
        sb.Append("]");

        sb.Append(",\n");
        sb.Append(ind).Append("  \"children\": [");
        int childCount = t.childCount;
        if (childCount > 0)
        {
            sb.Append("\n");
            for (int i = 0; i < childCount; i++)
            {
                if (i > 0) sb.Append(",\n");
                AppendNode(sb, t.GetChild(i), path, indent + 4);
            }
            sb.Append("\n").Append(ind).Append("  ");
        }
        sb.Append("]\n");
        sb.Append(ind).Append("}");
    }

    static void AppendProp(StringBuilder sb, string key, string value, int indent, bool quotedValue)
    {
        string ind = new string(' ', indent);
        sb.Append(ind).Append('"').Append(key).Append("\": ");
        if (quotedValue)
        {
            AppendJsonString(sb, value);
        }
        else
        {
            sb.Append(value);
        }
        sb.Append(",\n");
    }

    static void AppendProp(StringBuilder sb, string key, int value, int indent, bool unused)
    {
        string ind = new string(' ', indent);
        sb.Append(ind).Append('"').Append(key).Append("\": ").Append(value).Append(",\n");
    }

    static void AppendVec3(StringBuilder sb, string key, Vector3 v, int indent, bool trailingComma)
    {
        string ind = new string(' ', indent);
        sb.Append(ind).Append('"').Append(key).Append("\": {\"x\":")
            .Append(v.x.ToString("R")).Append(",\"y\":")
            .Append(v.y.ToString("R")).Append(",\"z\":")
            .Append(v.z.ToString("R")).Append("}");
        if (trailingComma) sb.Append(",");
        sb.Append("\n");
    }

    static void AppendQuat(StringBuilder sb, string key, Quaternion q, int indent, bool trailingComma)
    {
        string ind = new string(' ', indent);
        sb.Append(ind).Append('"').Append(key).Append("\": {\"x\":")
            .Append(q.x.ToString("R")).Append(",\"y\":")
            .Append(q.y.ToString("R")).Append(",\"z\":")
            .Append(q.z.ToString("R")).Append(",\"w\":")
            .Append(q.w.ToString("R")).Append("}");
        if (trailingComma) sb.Append(",");
        sb.Append("\n");
    }

    static void AppendJsonString(StringBuilder sb, string s)
    {
        if (s == null) s = "";
        sb.Append('"');
        foreach (char c in s)
        {
            switch (c)
            {
                case '"':  sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < 32)
                        sb.Append("\\u").Append(((int)c).ToString("x4"));
                    else
                        sb.Append(c);
                    break;
            }
        }
        sb.Append('"');
    }
}
#endif
