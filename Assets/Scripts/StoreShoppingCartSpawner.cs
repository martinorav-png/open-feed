using UnityEngine;

/// <summary>
/// Spawns the PSX shopping cart from <c>Resources/OpenFeed/PsxShoppingCart/model</c> when the scene has none;
/// falls back to a procedural primitive cart if that asset is missing.
/// </summary>
public static class StoreShoppingCartSpawner
{
    const string PsxCartResourcePath = "OpenFeed/PsxShoppingCart/model";
    const float PsxCartYawOffsetDegrees = 180f;

    static readonly Color CartColor = new Color(0.15f, 0.15f, 0.16f);
    static bool _warnedMissingResource;

    public static void EnsureAtLeastOneCartInScene(Transform playerOrReference)
    {
        StoreShoppingCart[] existing = Object.FindObjectsByType<StoreShoppingCart>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (existing != null && existing.Length > 0)
            return;

        Vector3 pos;
        Quaternion rot = Quaternion.identity;
        SupermarketDriveInIntro intro = Object.FindAnyObjectByType<SupermarketDriveInIntro>(FindObjectsInactive.Include);
        if (intro != null)
        {
            Vector3 f = intro.insideSpawnForward;
            f.y = 0f;
            if (f.sqrMagnitude < 0.01f)
                f = Vector3.forward;
            f.Normalize();
            float floorY = intro.insideSpawnPosition.y - 1.6f;
            pos = intro.insideSpawnPosition - f * 1.85f + Vector3.right * 0.65f;
            pos.y = floorY;
            rot = Quaternion.LookRotation(f, Vector3.up);
        }
        else if (playerOrReference != null)
        {
            pos = playerOrReference.position + playerOrReference.forward * 2f + playerOrReference.right * 0.8f;
            pos.y = playerOrReference.position.y;
            Vector3 flat = playerOrReference.forward;
            flat.y = 0f;
            if (flat.sqrMagnitude > 1e-4f)
                rot = Quaternion.LookRotation(flat.normalized, Vector3.up);
        }
        else
        {
            pos = Vector3.zero;
        }

        GameObject cart = SpawnCartOrFallback(pos, rot);
        cart.name = "ShoppingCart_Runtime";
    }

    /// <summary>PSX cart from Resources, or <see cref="BuildProceduralCart"/> if the asset is unavailable.</summary>
    public static GameObject SpawnCartOrFallback(Vector3 worldPos, Quaternion worldRot)
    {
        GameObject template = Resources.Load<GameObject>(PsxCartResourcePath);
        if (template != null)
        {
            GameObject cart = Object.Instantiate(template, worldPos, worldRot);
            StoreShoppingCart marker;
            if (!cart.TryGetComponent<StoreShoppingCart>(out marker))
                marker = cart.AddComponent<StoreShoppingCart>();
            marker.yawOffsetDegrees = PsxCartYawOffsetDegrees;
            cart.transform.rotation = marker.ApplyYawOffset(worldRot);
            ApplyPsxCartMaterialSettings(cart.transform);
            return cart;
        }

        if (!_warnedMissingResource)
        {
            _warnedMissingResource = true;
            Debug.LogWarning(
                $"[StoreShoppingCartSpawner] Couldn't load '{PsxCartResourcePath}' as a GameObject from Resources. " +
                "Falling back to procedural cart. If you expect the PSX cart model, ensure a glTF importer is installed " +
                "(e.g. glTFast) so model.gltf imports as a prefab/GameObject.");
        }
        return BuildProceduralCart(worldPos, worldRot);
    }

    static void ApplyPsxCartMaterialSettings(Transform root)
    {
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
        {
            if (r == null)
                continue;
            Material[] mats = r.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                Material m = mats[i];
                if (m == null)
                    continue;
                if (m.HasProperty("_BaseMap"))
                {
                    Texture t = m.GetTexture("_BaseMap");
                    if (t != null)
                        t.filterMode = FilterMode.Point;
                }
                else if (m.mainTexture != null)
                    m.mainTexture.filterMode = FilterMode.Point;

                if (m.HasProperty("_Smoothness"))
                    m.SetFloat("_Smoothness", 0f);
                if (m.HasProperty("_Metallic"))
                    m.SetFloat("_Metallic", 0f);
            }
        }
    }

    public static GameObject BuildProceduralCart(Vector3 worldPos, Quaternion worldRot)
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null)
            lit = Shader.Find("Standard");
        Material mat = new Material(lit != null ? lit : Shader.Find("Diffuse"))
        {
            name = "StoreFlow_ShoppingCart_Runtime"
        };
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", CartColor);
        else
            mat.color = CartColor;
        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", 0f);
        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", 0f);

        GameObject root = new GameObject("ShoppingCart");
        root.transform.SetPositionAndRotation(worldPos, worldRot);

        void AddCube(string name, Vector3 lp, Vector3 euler, Vector3 scale)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = lp;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            Object.Destroy(go.GetComponent<Collider>());
            Renderer r = go.GetComponent<Renderer>();
            if (r != null)
                r.sharedMaterial = mat;
        }

        AddCube("CartBasket", new Vector3(0f, 0.55f, 0f), Vector3.zero, new Vector3(0.5f, 0.35f, 0.7f));
        AddCube("CartHandle", new Vector3(0f, 0.8f, -0.35f), new Vector3(-20f, 0f, 0f), new Vector3(0.4f, 0.03f, 0.03f));
        AddCube("CartLegFL", new Vector3(-0.2f, 0.2f, 0.3f), Vector3.zero, new Vector3(0.02f, 0.4f, 0.02f));
        AddCube("CartLegFR", new Vector3(0.2f, 0.2f, 0.3f), Vector3.zero, new Vector3(0.02f, 0.4f, 0.02f));
        AddCube("CartLegBL", new Vector3(-0.2f, 0.2f, -0.3f), Vector3.zero, new Vector3(0.02f, 0.4f, 0.02f));
        AddCube("CartLegBR", new Vector3(0.2f, 0.2f, -0.3f), Vector3.zero, new Vector3(0.02f, 0.4f, 0.02f));

        for (int w = 0; w < 4; w++)
        {
            float sx = (w & 1) == 0 ? -0.22f : 0.22f;
            float sz = (w & 2) == 0 ? 0.32f : -0.32f;
            GameObject wh = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            wh.name = "CartWheel" + w;
            wh.transform.SetParent(root.transform, false);
            wh.transform.localPosition = new Vector3(sx, 0.08f, sz);
            wh.transform.localScale = Vector3.one * 0.1f;
            Object.Destroy(wh.GetComponent<Collider>());
            Renderer wr = wh.GetComponent<Renderer>();
            if (wr != null)
                wr.sharedMaterial = mat;
        }

        root.AddComponent<StoreShoppingCart>();
        return root;
    }
}
