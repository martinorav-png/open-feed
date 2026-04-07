#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Batch PSX-style texture import: Point filter, no mipmaps, capped max size.
/// Unity menu: OPEN FEED → Textures → Apply PSX Import (All Assets).
/// (IvanMurzak MCP script-execute often cannot complete this; use the menu in Editor.)
/// </summary>
public static class PsxTextureImportTools
{
    const int PsxMaxTextureSize = 256;

    [MenuItem("OPEN FEED/Textures/Apply PSX Import (All Assets)", false, 0)]
    public static void ApplyPsxImportAllTextures()
    {
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });
        int count = 0;
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path) || path.StartsWith("Packages/", StringComparison.Ordinal))
                continue;

            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null)
                continue;

            ti.filterMode = FilterMode.Point;
            ti.mipmapEnabled = false;
            ti.maxTextureSize = PsxMaxTextureSize;

            var def = ti.GetDefaultPlatformTextureSettings();
            def.maxTextureSize = PsxMaxTextureSize;
            ti.SetPlatformTextureSettings(def);

            EditorUtility.SetDirty(ti);
            ti.SaveAndReimport();
            count++;
        }

        AssetDatabase.Refresh();
        Debug.Log($"OPENFEED: PSX texture import applied to {count} textures (Point, no mips, max {PsxMaxTextureSize}).");
    }
}
#endif
