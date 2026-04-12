#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Copies a recorder JSON into Resources so <see cref="SupermarketDriveInIntro"/> can load it by resource name.
/// </summary>
public static class SupermarketDriveCameraPathImporter
{
    const string ResourcesDir = "Assets/Resources";
    const string DefaultAssetName = "SupermarketDriveCameraPath.json";

    [MenuItem("OPEN FEED/Supermarket/Import drive camera path JSON to Resources…", false, 52)]
    static void ImportJson()
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath) ?? ".";
        string defaultDir = Path.Combine(projectRoot, "tools", "camera-path-recordings");
        if (!Directory.Exists(defaultDir))
            defaultDir = projectRoot;

        string src = EditorUtility.OpenFilePanel("Drive camera path JSON", defaultDir, "json");
        if (string.IsNullOrEmpty(src))
            return;

        if (!Directory.Exists(ResourcesDir))
            Directory.CreateDirectory(ResourcesDir);

        string dest = Path.Combine(ResourcesDir, DefaultAssetName);
        try
        {
            File.Copy(src, dest, overwrite: true);
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Import failed", e.Message, "OK");
            return;
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog(
            "Drive path imported",
            $"Saved to:\n{dest}\n\nOn SupermarketDriveInIntro enable Use Recorded Drive Path and ensure resource name is '{Path.GetFileNameWithoutExtension(DefaultAssetName)}' (default).",
            "OK");
    }
}
#endif
