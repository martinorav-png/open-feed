using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Instantiates the Subaru Impreza (same FBX + textures as Store Flow), tuned for driver POV (hood through windshield).
/// </summary>
public static class OpenFeedDrivingCarBuilder
{
    /// <summary>Primary mesh — same asset path as Store Flow grocery parking Impreza.</summary>
    const string ImprezaFbxPath = "Assets/ModelsPlace/subaru-impreza/source/Subaru Impreza/subaru_impreza.fbx";
    const string SubaruRootFolder = "Assets/ModelsPlace/subaru-impreza";
    const string SubaruBodyTexPath = "Assets/ModelsPlace/subaru-impreza/textures/bs_sub_impreza.png";
    const string SubaruWheelTexPath = "Assets/ModelsPlace/subaru-impreza/textures/bs_sub_impreza_wheel.png";
    const string SubaruOtherTexPath = "Assets/ModelsPlace/subaru-impreza/textures/op_sub_impreza.png";

    static readonly string[] CarModelExtensions = { ".fbx", ".obj", ".glb", ".gltf" };

    /// <summary>Hand-tuned in ForestDrive — <c>ExteriorCar</c> local space under <c>CarInterior</c>.</summary>
    static readonly Vector3 ExteriorCarLocalPosition = new Vector3(0f, 1.55f, 3.38f);
    static readonly Vector3 ExteriorCarLocalEuler = new Vector3(0f, 180f, 0f);
    static readonly Vector3 ExteriorCarLocalScale = Vector3.one;

    /// <summary>Impreza mesh under <c>ExteriorCar</c> (parent handles main placement/flip).</summary>
    const float ImprezaChildUniformScale = 0.4f;

    public static GameObject AddExteriorCarUnderCarInterior(Transform carInterior)
    {
        GameObject imprezaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ImprezaFbxPath)
            ?? LoadFirstCarModelInFolder(SubaruRootFolder);

        if (imprezaPrefab == null)
        {
            Debug.LogError(
                "OPENFEED Forest Drive: Subaru Impreza model not found. Expected FBX at:\n  " + ImprezaFbxPath +
                "\nAdd/extract the mesh under ModelsPlace/subaru-impreza (same as grocery parking car), then run Forest Drive > Generate & Save Scene.");
            return null;
        }

        Material carBody = CreateTexturedLitFromPath("Drive_SubaruBody", SubaruBodyTexPath, Color.white, Vector2.one);
        Material wheel = CreateTexturedLitFromPath("Drive_SubaruWheel", SubaruWheelTexPath, Color.white, Vector2.one);
        Material tire = wheel;
        Material carInteriorMat = CreateTexturedLitFromPath("Drive_SubaruOther", SubaruOtherTexPath, Color.white, Vector2.one);
        Material carTrim = carInteriorMat;

        Material carGlass = CreateTransparent("Drive_CarGlass", new Color(0.74f, 0.86f, 0.96f, 0.26f));
        Material carHeadlight = CreateEmissive("Drive_CarHeadlight", new Color(0.95f, 0.97f, 1f), 0.7f);
        Material carTaillight = CreateEmissive("Drive_CarTaillight", new Color(1f, 0.18f, 0.14f), 0.8f);

        GameObject holder = new GameObject("ExteriorCar");
        holder.transform.SetParent(carInterior, false);
        holder.transform.localPosition = ExteriorCarLocalPosition;
        holder.transform.localRotation = Quaternion.Euler(ExteriorCarLocalEuler);
        holder.transform.localScale = ExteriorCarLocalScale;

        GameObject car = (GameObject)PrefabUtility.InstantiatePrefab(imprezaPrefab);
        car.name = "Subaru_Impreza";
        car.transform.SetParent(holder.transform, false);
        car.transform.localPosition = Vector3.zero;
        car.transform.localRotation = Quaternion.identity;
        car.transform.localScale = Vector3.one * ImprezaChildUniformScale;

        ApplyCarMaterials(car, carBody, carTrim, carGlass, tire, wheel, carHeadlight, carTaillight, carInteriorMat);
        foreach (Collider c in car.GetComponentsInChildren<Collider>(true))
            UnityEngine.Object.DestroyImmediate(c);

        return holder;
    }

    static GameObject LoadFirstCarModelInFolder(string folderAssetPath)
    {
        if (string.IsNullOrEmpty(folderAssetPath))
            return null;

        string[] preferredPaths =
        {
            $"{folderAssetPath}/source/Subaru Impreza/subaru_impreza.fbx",
            $"{folderAssetPath}/source/Subaru Impreza/Subaru Impreza.fbx",
            $"{folderAssetPath}/source/Subaru Impreza.fbx",
            $"{folderAssetPath}/source/Subaru_Impreza.fbx",
            $"{folderAssetPath}/source/subaru impreza.fbx",
            $"{folderAssetPath}/Subaru Impreza.fbx"
        };

        foreach (string path in preferredPaths)
        {
            GameObject preferred = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (preferred != null)
                return preferred;
        }

        string[] guids = AssetDatabase.FindAssets("", new[] { folderAssetPath });
        GameObject found = null;
        string foundPath = null;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.StartsWith(folderAssetPath, StringComparison.OrdinalIgnoreCase))
                continue;

            bool isModel = false;
            foreach (string ext in CarModelExtensions)
            {
                if (path.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                {
                    isModel = true;
                    break;
                }
            }

            if (!isModel)
                continue;

            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null)
                continue;

            if (found == null || path.Length < foundPath.Length)
            {
                found = go;
                foundPath = path;
            }
        }

        return found;
    }

    static bool IsSubaruImprezaWheelObjectName(string lowerName)
    {
        if (!lowerName.Contains("sub_imp"))
            return false;
        return lowerName.EndsWith("fl", StringComparison.Ordinal)
               || lowerName.EndsWith("fr", StringComparison.Ordinal)
               || lowerName.EndsWith("rl", StringComparison.Ordinal)
               || lowerName.EndsWith("rr", StringComparison.Ordinal);
    }

    static void ApplyCarMaterials(GameObject car, Material bodyMat, Material trimMat, Material glassMat, Material tireMat,
        Material wheelMat, Material headlightMat, Material taillightMat, Material interiorMat)
    {
        foreach (Renderer renderer in car.GetComponentsInChildren<Renderer>(true))
        {
            string name = renderer.gameObject.name.ToLowerInvariant();
            Material mat = bodyMat;
            if (name.Contains("glass") || name.Contains("window") || name.Contains("windshield") || name.Contains("windscreen"))
                mat = glassMat;
            else if (name.Contains("headlight") || name.Contains("headlamp") || name.Contains("fog"))
                mat = headlightMat;
            else if (name.Contains("brakelight") || name.Contains("taillight") || name.Contains("rearlight") || name.Contains("litfull") || name == "litsmd" || name == "lit_1smd")
                mat = taillightMat;
            else if (name.Contains("tire") || name.Contains("tyre"))
                mat = tireMat;
            else if (name.Contains("wheel") || name.Contains("rim") || name.Contains("hub")
                     || IsSubaruImprezaWheelObjectName(name))
                mat = wheelMat;
            else if (name.Contains("interior") || name.Contains("steering") || name.Contains("seat") || name.Contains("dash") || name == "root" || name == "root_1")
                mat = interiorMat;
            else if (name.Contains("chrome") || name.Contains("misc") || name.Contains("engine") || name.Contains("exhaust") || name.Contains("grille") || name.Contains("mirror"))
                mat = trimMat;
            renderer.sharedMaterial = mat;
        }
    }

    static Shader FindShader() => Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

    static Material CreateLit(string name, Color color)
    {
        Material mat = new Material(FindShader());
        mat.name = name;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        else
            mat.color = color;
        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", 0f);
        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", 0f);
        return mat;
    }

    static Material CreateTransparent(string name, Color color)
    {
        Material mat = CreateLit(name, color);
        if (mat.HasProperty("_Surface"))
            mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend"))
            mat.SetFloat("_Blend", 0f);
        if (mat.HasProperty("_ZWrite"))
            mat.SetFloat("_ZWrite", 0f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        return mat;
    }

    static Material CreateTexturedLitFromPath(string name, string texturePath, Color tint, Vector2 tiling)
    {
        Material mat = CreateLit(name, tint);
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (tex != null)
        {
            if (mat.HasProperty("_BaseMap"))
            {
                mat.SetTexture("_BaseMap", tex);
                mat.SetTextureScale("_BaseMap", tiling);
            }
            else
            {
                mat.mainTexture = tex;
                mat.mainTextureScale = tiling;
            }
        }

        return mat;
    }

    static Material CreateEmissive(string name, Color emission, float intensity)
    {
        Material mat = CreateLit(name, emission);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", emission * intensity);
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        return mat;
    }
}
