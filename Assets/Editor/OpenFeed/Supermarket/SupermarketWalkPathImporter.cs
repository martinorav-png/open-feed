#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Saves clipboard JSON (openfeed-scene-camera-path-v1) to Resources for the supermarket intro walk.
/// Copy the full JSON in Unity or your editor, then run the menu once.
/// </summary>
public static class SupermarketWalkPathImporter
{
    const string ResourcePath = "Assets/Resources/SupermarketIntroWalkPath.json";

    [MenuItem("OPEN FEED/Supermarket/Import walk path JSON from clipboard → Resources", false, 20)]
    static void ImportFromClipboard()
    {
        string text = GUIUtility.systemCopyBuffer;
        if (string.IsNullOrWhiteSpace(text) || !text.Contains("openfeed-scene-camera-path-v1"))
        {
            EditorUtility.DisplayDialog(
                "Supermarket walk path",
                "Clipboard must contain JSON with \"openfeed-scene-camera-path-v1\".\n\nCopy the full recording, then run this command again.",
                "OK");
            return;
        }

        string dir = Path.GetDirectoryName(ResourcePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(ResourcePath, text.Trim());
        AssetDatabase.Refresh();
        Debug.Log($"[Supermarket] Wrote walk path to {ResourcePath} ({text.Length} chars).");
    }
}
#endif
