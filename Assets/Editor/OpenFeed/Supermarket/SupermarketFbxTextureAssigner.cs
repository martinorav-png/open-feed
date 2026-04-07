#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Maps super_market_low_poly_for_free/textures/*_baseColor* onto materials used by the imported scene.fbx hierarchy.
/// OPENFEED: URP Lit, Metallic/Smoothness 0, no normal map, Point filtering on textures.
/// </summary>
public static class SupermarketFbxTextureAssigner
{
    const string TexturesFolder = "Assets/ModelsPlace/super_market_low_poly_for_free/textures";
    const string MaterialsFolder = "Assets/Materials/Supermarket";

    [MenuItem("OPEN FEED/Supermarket/Apply textures to scene FBX (super_market textures)", false, 10)]
    static void Run()
    {
        GameObject root = Selection.activeGameObject;
        if (root == null)
            root = GameObject.Find("scene");
        if (root == null)
        {
            EditorUtility.DisplayDialog(
                "Supermarket",
                "Select the FBX root in the Hierarchy (e.g. \"scene\"), or leave nothing selected and ensure an object named \"scene\" exists, then run again.",
                "OK");
            return;
        }
        ApplyTo(root);
    }

    public static void ApplyTo(GameObject root)
    {
        if (root == null)
            return;

        Dictionary<string, string> texByKey = BuildBaseColorMap();
        if (texByKey.Count == 0)
        {
            Debug.LogError("[Supermarket] No *baseColor* textures found under " + TexturesFolder);
            return;
        }

        EnsureFolder(MaterialsFolder);

        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        int slotsUpdated = 0;
        var uniqueMats = new HashSet<Material>();

        foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
        {
            Material[] shared = r.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < shared.Length; i++)
            {
                Material m = shared[i];
                if (m == null)
                    continue;

                string key = NormalizeMaterialName(m.name);
                if (!TryResolveTexturePath(texByKey, key, out string texPath))
                {
                    Debug.LogWarning($"[Supermarket] No baseColor file for material \"{m.name}\" (key \"{key}\").");
                    continue;
                }

                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                if (tex == null)
                {
                    Debug.LogWarning($"[Supermarket] Could not load texture: {texPath}");
                    continue;
                }

                ApplyPointFilterImport(texPath);

                Material newMat = GetOrCreateSceneMaterial(shader, key, tex);
                if (newMat != m || shared[i] != newMat)
                {
                    shared[i] = newMat;
                    changed = true;
                    slotsUpdated++;
                }
                uniqueMats.Add(newMat);
            }

            if (changed)
                r.sharedMaterials = shared;
        }

        EditorSceneManager.MarkSceneDirty(root.scene);
        Debug.Log($"[Supermarket] Updated {slotsUpdated} material slot(s) under \"{root.name}\" ({uniqueMats.Count} scene material asset(s)).");
    }

    static Dictionary<string, string> BuildBaseColorMap()
    {
        var map = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
        if (!AssetDatabase.IsValidFolder(TexturesFolder))
            return map;

        string[] guids = AssetDatabase.FindAssets("", new[] { TexturesFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string file = Path.GetFileNameWithoutExtension(path);
            if (file.Length <= 9 || !file.EndsWith("_baseColor", System.StringComparison.OrdinalIgnoreCase))
                continue;

            string ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext != ".png" && ext != ".jpg" && ext != ".jpeg")
                continue;

            string key = file.Substring(0, file.Length - "_baseColor".Length);
            if (!map.ContainsKey(key))
                map[key] = path;
        }

        return map;
    }

    static bool TryResolveTexturePath(Dictionary<string, string> texByKey, string materialKey, out string texPath)
    {
        texPath = null;
        if (texByKey.TryGetValue(materialKey, out texPath))
            return true;

        foreach (var kv in texByKey)
        {
            if (string.Equals(kv.Key, materialKey, System.StringComparison.OrdinalIgnoreCase))
            {
                texPath = kv.Value;
                return true;
            }
        }

        return false;
    }

    static string NormalizeMaterialName(string name)
    {
        int p = name.IndexOf(" (");
        if (p > 0)
            name = name.Substring(0, p);
        return name.Trim();
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string[] parts = path.Split('/');
        string cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = cur + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }

    static Material GetOrCreateSceneMaterial(Shader shader, string key, Texture2D tex)
    {
        string safe = key.Replace('.', '_').Replace(' ', '_');
        string matPath = $"{MaterialsFolder}/Supermarket_{safe}.mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (existing != null)
        {
            ApplyOpenFeedLit(existing, tex);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        Material mat = new Material(shader);
        mat.name = "Supermarket_" + safe;
        ApplyOpenFeedLit(mat, tex);
        AssetDatabase.CreateAsset(mat, matPath);
        return mat;
    }

    static void ApplyOpenFeedLit(Material mat, Texture2D tex)
    {
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_BaseMap"))
        {
            mat.SetTexture("_BaseMap", tex);
            mat.SetTextureScale("_BaseMap", Vector2.one);
        }
        else
        {
            mat.mainTexture = tex;
            mat.mainTextureScale = Vector2.one;
        }

        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", 0f);
        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", 0f);

        if (mat.HasProperty("_BumpMap"))
            mat.SetTexture("_BumpMap", null);
        if (mat.HasProperty("_BumpScale"))
            mat.SetFloat("_BumpScale", 0f);
    }

    static void ApplyPointFilterImport(string textureAssetPath)
    {
        TextureImporter ti = AssetImporter.GetAtPath(textureAssetPath) as TextureImporter;
        if (ti == null)
            return;

        bool dirty = false;
        if (ti.filterMode != FilterMode.Point)
        {
            ti.filterMode = FilterMode.Point;
            dirty = true;
        }

        if (ti.mipmapEnabled)
        {
            ti.mipmapEnabled = false;
            dirty = true;
        }

        if (dirty)
            ti.SaveAndReimport();
    }
}
#endif
