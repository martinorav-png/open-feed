using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class SupermarketNeonSign : MonoBehaviour
{
    [Header("Scale")]
    [SerializeField] Vector2 backplateSize = new Vector2(10.8f, 1.25f);
    [SerializeField] float titleCharacterSize = 0.18f;
    [SerializeField] float subtitleCharacterSize = 0.105f;

    [Header("Light")]
    [SerializeField] bool enableRealtimeNeonLights = false;
    [SerializeField] float neonLightIntensity = 9.5f;
    [SerializeField] float neonLightRange = 10.5f;

    [Header("Interior Culling")]
    [SerializeField] bool hideWhenPlayerInsideStore = true;
    [SerializeField] float insideStoreWorldZ = 18f;
    [SerializeField] float cullCheckInterval = 0.15f;

    Material backplateMaterial;
    Material blueGlowMaterial;
    Material cyanHaloMaterial;
    Material magentaHaloMaterial;
    Renderer[] generatedRenderers;
    Light[] generatedLights;
    Camera cachedCamera;
    float nextCullCheckTime;
    bool generatedVisible = true;
#if UNITY_EDITOR
    bool editorRebuildQueued;
#endif

    void OnEnable()
    {
        Rebuild();
    }

    void OnValidate()
    {
#if UNITY_EDITOR
        QueueEditorRebuild();
#else
        Rebuild();
#endif
    }

    void LateUpdate()
    {
        if (!hideWhenPlayerInsideStore || Time.unscaledTime < nextCullCheckTime)
            return;

        nextCullCheckTime = Time.unscaledTime + Mathf.Max(0.03f, cullCheckInterval);
        Camera cam = ResolveCamera();
        if (cam == null)
            return;

        SetGeneratedVisibility(cam.transform.position.z < insideStoreWorldZ);
    }

#if UNITY_EDITOR
    void QueueEditorRebuild()
    {
        if (editorRebuildQueued)
            return;

        editorRebuildQueued = true;
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null)
                return;

            editorRebuildQueued = false;
            Rebuild();
        };
    }
#endif

    [ContextMenu("Rebuild Neon Sign")]
    public void Rebuild()
    {
        EnsureMaterials();
        ClearGeneratedChildren();

        GameObject backing = CreateCube("WESTMART_Backlit_SignPanel", Vector3.zero, new Vector3(backplateSize.x, backplateSize.y, 0.08f), backplateMaterial);
        backing.transform.localPosition = new Vector3(0f, 0f, 0.05f);

        CreateCube("WESTMART_TopNeonTube", new Vector3(0f, 0.66f, -0.09f), new Vector3(backplateSize.x * 0.92f, 0.035f, 0.035f), blueGlowMaterial);
        CreateCube("WESTMART_BottomNeonTube", new Vector3(0f, -0.66f, -0.09f), new Vector3(backplateSize.x * 0.92f, 0.035f, 0.035f), blueGlowMaterial);
        CreateCube("WESTMART_Title_CyanHalo", new Vector3(0f, 0.16f, -0.125f), new Vector3(backplateSize.x * 0.86f, 0.58f, 0.015f), cyanHaloMaterial);
        CreateCube("WESTMART_Open_MagentaHalo", new Vector3(0f, -0.75f, -0.13f), new Vector3(backplateSize.x * 0.55f, 0.34f, 0.015f), magentaHaloMaterial);

        Color cyanCore = new Color(0.76f, 1f, 1f, 1f);
        Color cyanGlow = new Color(0.04f, 0.95f, 1f, 0.72f);
        Color pinkCore = new Color(1f, 0.72f, 0.98f, 1f);
        Color pinkGlow = new Color(1f, 0.08f, 0.68f, 0.72f);

        CreateText("WESTMART_Title_GlowWide", "WESTMART", new Vector3(0.035f, 0.15f, -0.16f), titleCharacterSize * 1.12f, cyanGlow);
        CreateText("WESTMART_Title_GlowSoft", "WESTMART", new Vector3(-0.03f, 0.15f, -0.17f), titleCharacterSize * 1.06f, cyanGlow);
        CreateText("WESTMART_Title", "WESTMART", new Vector3(0f, 0.15f, -0.19f), titleCharacterSize, cyanCore);

        CreateText("OPEN24H_GlowWide", "OPEN 24H", new Vector3(0.035f, -0.76f, -0.16f), subtitleCharacterSize * 1.22f, pinkGlow);
        CreateText("OPEN24H_GlowSoft", "OPEN 24H", new Vector3(-0.03f, -0.76f, -0.17f), subtitleCharacterSize * 1.13f, pinkGlow);
        CreateText("OPEN24H", "OPEN 24H", new Vector3(0f, -0.76f, -0.195f), subtitleCharacterSize, pinkCore);

        if (enableRealtimeNeonLights)
        {
            CreatePointLight("WESTMART_Cyan_NeonLight_L", new Vector3(-3.35f, 0.2f, -0.95f), new Color(0.08f, 0.95f, 1f), neonLightIntensity * 0.9f, neonLightRange);
            CreatePointLight("WESTMART_Cyan_NeonLight_C", new Vector3(0f, 0.2f, -1f), new Color(0.08f, 0.95f, 1f), neonLightIntensity * 1.2f, neonLightRange * 1.15f);
            CreatePointLight("WESTMART_Cyan_NeonLight_R", new Vector3(3.35f, 0.2f, -0.95f), new Color(0.08f, 0.95f, 1f), neonLightIntensity * 0.9f, neonLightRange);
            CreatePointLight("WESTMART_Magenta_OpenLight_L", new Vector3(-1.7f, -0.8f, -0.75f), new Color(1f, 0.06f, 0.72f), neonLightIntensity * 0.7f, neonLightRange * 0.82f);
            CreatePointLight("WESTMART_Magenta_OpenLight_R", new Vector3(1.7f, -0.8f, -0.75f), new Color(1f, 0.06f, 0.72f), neonLightIntensity * 0.7f, neonLightRange * 0.82f);
        }

        CacheGeneratedComponents();
        generatedVisible = true;
    }

    void EnsureMaterials()
    {
        backplateMaterial = CreateMaterial("WESTMART_Backplate_Runtime", new Color(0.015f, 0.017f, 0.03f), new Color(0.015f, 0.02f, 0.05f) * 0.8f);
        blueGlowMaterial = CreateMaterial("WESTMART_Blue_Glow_Runtime", new Color(0.04f, 0.22f, 0.28f), new Color(0.02f, 0.75f, 1f) * 2.6f);
        cyanHaloMaterial = CreateTransparentGlowMaterial("WESTMART_Cyan_Halo_Runtime", new Color(0.02f, 0.92f, 1f, 0.22f), new Color(0.02f, 0.95f, 1f) * 1.8f);
        magentaHaloMaterial = CreateTransparentGlowMaterial("WESTMART_Magenta_Halo_Runtime", new Color(1f, 0.05f, 0.72f, 0.26f), new Color(1f, 0.06f, 0.72f) * 1.9f);
    }

    Material CreateMaterial(string materialName, Color baseColor, Color emission)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        var material = new Material(shader);
        material.name = materialName;
        material.SetColor("_BaseColor", baseColor);
        material.SetColor("_Color", baseColor);
        material.SetFloat("_Cull", 0f);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", emission);
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        return material;
    }

    Material CreateTransparentGlowMaterial(string materialName, Color baseColor, Color emission)
    {
        Material material = CreateMaterial(materialName, baseColor, emission);
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        return material;
    }

    GameObject CreateCube(string objectName, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = objectName;
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = localScale;

        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        Collider collider = go.GetComponent<Collider>();
        if (collider != null)
            DestroyImmediateSafe(collider);

        return go;
    }

    void CreateText(string objectName, string text, Vector3 localPosition, float characterSize, Color color)
    {
        var go = new GameObject(objectName);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        TextMesh mesh = go.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.characterSize = characterSize;
        mesh.fontSize = 96;
        mesh.color = color;

        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    void CreatePointLight(string objectName, Vector3 localPosition, Color color, float intensity, float range)
    {
        var go = new GameObject(objectName);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.identity;

        Light light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
    }

    void ClearGeneratedChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediateSafe(transform.GetChild(i).gameObject);

        generatedRenderers = null;
        generatedLights = null;
    }

    void CacheGeneratedComponents()
    {
        generatedRenderers = GetComponentsInChildren<Renderer>(true);
        generatedLights = GetComponentsInChildren<Light>(true);
    }

    Camera ResolveCamera()
    {
        if (cachedCamera != null && cachedCamera.isActiveAndEnabled)
            return cachedCamera;

        cachedCamera = Camera.main;
        if (cachedCamera != null)
            return cachedCamera;

        Camera[] cameras = Camera.allCameras;
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] != null && cameras[i].isActiveAndEnabled)
            {
                cachedCamera = cameras[i];
                return cachedCamera;
            }
        }

        return null;
    }

    void SetGeneratedVisibility(bool visible)
    {
        if (generatedVisible == visible)
            return;

        generatedVisible = visible;

        if (generatedRenderers == null || generatedLights == null)
            CacheGeneratedComponents();

        if (generatedRenderers != null)
        {
            for (int i = 0; i < generatedRenderers.Length; i++)
            {
                if (generatedRenderers[i] != null)
                    generatedRenderers[i].enabled = visible;
            }
        }

        if (generatedLights != null)
        {
            for (int i = 0; i < generatedLights.Length; i++)
            {
                if (generatedLights[i] != null)
                    generatedLights[i].enabled = visible;
            }
        }
    }

    void DestroyImmediateSafe(Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }
}
