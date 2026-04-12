#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class SupermarketGroundMaterialSetup
{
    const string ScenePath = "Assets/supermarket.unity";

    static readonly string[] GroundObjectNames =
    {
        "WildernessTerrain",
        "WildernessSkirt",
        "Ground",
        "Terrain_STGFlat",
        "Terrain_Grass",
        "Terrain_Grass Instance",
        "Shoulder_L",
        "Shoulder_R",
    };

    [MenuItem("OPEN FEED/Supermarket/Apply TerrainMat to ground objects", false, 20)]
    static void Run()
    {
        // Find the TerrainMat material — check scene renderers first, then project assets
        Material grassMat = null;
        foreach (var r in Object.FindObjectsByType<MeshRenderer>())
        {
            if (r.sharedMaterial != null && r.sharedMaterial.name == "TerrainMat")
            {
                grassMat = r.sharedMaterial;
                break;
            }
        }

        if (grassMat == null)
        {
            foreach (var guid in AssetDatabase.FindAssets("TerrainMat t:Material"))
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (mat != null && mat.name == "TerrainMat")
                {
                    grassMat = mat;
                    break;
                }
            }
        }

        if (grassMat == null)
        {
            EditorUtility.DisplayDialog("Ground Material Setup",
                "Could not find a material named 'TerrainMat' in the scene or project.", "OK");
            return;
        }

        int count = 0;
        foreach (var r in Object.FindObjectsByType<MeshRenderer>())
        {
            if (!IsUnderGroundObject(r.transform)) continue;
            Undo.RecordObject(r, "Apply Terrain_Grass");
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
                mats[i] = grassMat;
            r.sharedMaterials = mats;
            count++;
        }

        EditorUtility.DisplayDialog("Ground Material Setup",
            $"Applied TerrainMat to {count} renderer(s).\nSave the scene when done.", "OK");

        Debug.Log($"[Supermarket] Applied TerrainMat to {count} renderer(s).");
    }

    // Walk up the hierarchy — if any ancestor (or self) matches, apply the material.
    static bool IsUnderGroundObject(Transform t)
    {
        while (t != null)
        {
            foreach (var n in GroundObjectNames)
                if (t.name == n || t.name.StartsWith(n + " ")) return true;
            t = t.parent;
        }
        return false;
    }
}
#endif
