using System.IO;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Click trigger for the living-room TV. Plays the recorded EAS transmission on
/// a real world-space video surface attached to the TV, so walls and furniture
/// occlude it normally.
/// </summary>
[DefaultExecutionOrder(60)]
public class WorldspaceEasTvWebViewHost : MonoBehaviour
{
    const string AutoObjectName = "WorldspaceEasTvScreenHost";
    const string DefaultTvName = "tv";
    const string PreferredTvName = "vizio flatscreen";
    const string ScreenName = "EAS_FinalTransmission_VideoScreen";
    const string OldWorldspaceScreenName = "EAS_WorldspaceScreen";

    [Header("Video Source")]
    [SerializeField] string assetRelativePath = "OPEN FEED eas message/finalvideo1.mp4";
    [SerializeField] double stopAtSeconds = 249.0;

    [Header("TV Target")]
    [SerializeField] Transform tvRoot;
    [SerializeField] string tvObjectName = PreferredTvName;
    [SerializeField] float interactDistance = 4f;

    [Header("Screen Placement")]
    [SerializeField] Vector3 screenLocalPosition = new Vector3(0f, 0.04f, -0.265f);
    [SerializeField] Vector3 screenLocalEuler = new Vector3(0f, 180f, 0f);
    [SerializeField] Vector2 screenLocalSize = new Vector2(0.38f, 0.285f);

    [Header("Playback")]
    [SerializeField] bool playOnAwake;
    [SerializeField] bool restartWhenClicked = true;
    [SerializeField, Range(0f, 1f)] float volume = 1f;

    [Header("Fullscreen Hint")]
    [SerializeField] string fullscreenHintText = "click for fullscreen";
    [SerializeField] Vector3 hintLocalPosition = new Vector3(0f, 1.15f, -0.05f);
    [SerializeField] float hintCharacterSize = 0.008f;

    Camera playerCamera;
    VideoPlayer videoPlayer;
    AudioSource audioSource;
    MeshRenderer screenRenderer;
    MeshFilter screenMeshFilter;
    Mesh screenMesh;
    RenderTexture renderTexture;
    Material screenMaterial;
    GameObject fullscreenCanvas;
    RawImage fullscreenImage;
    TextMesh hintTextMesh;
    MeshRenderer hintRenderer;
    bool hasPrepared;
    bool fullscreenOpen;
    bool userStartedPlayback;
    bool _gaveUpResolvingTv;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (FindAnyObjectByType<WorldspaceEasTvWebViewHost>() != null)
            return;

        // Skip scenes that have no chance of containing a TV — avoids spawning a host whose
        // Update() then does a full scene-wide transform scan every frame trying to find one.
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName == "supermarket" || sceneName == "supermarket2" || sceneName == "GroceryStore"
            || sceneName == "ForestDrive" || sceneName == "ParkingLot" || sceneName == "MainMenu")
            return;

        if (FindPreferredTvTransform() == null)
            return;

        var host = new GameObject(AutoObjectName);
        host.AddComponent<WorldspaceEasTvWebViewHost>();
    }

    void Start()
    {
        ResolveTv();
        if (tvRoot == null)
            return;

        RemoveOldWorldspaceScreen();
        EnsureCollider();
        EnsureVideoSurface();
        PrepareVideo();

        if (playOnAwake)
            PlayFromStart();
    }

    void OnDisable()
    {
        CloseFullscreen();

        if (videoPlayer != null)
            videoPlayer.Stop();
    }

    void OnDestroy()
    {
        if (renderTexture != null)
            renderTexture.Release();

        if (Application.isPlaying)
        {
            if (screenMaterial != null)
                Destroy(screenMaterial);
            if (renderTexture != null)
                Destroy(renderTexture);
            if (screenMesh != null)
                Destroy(screenMesh);
        }
    }

    void Update()
    {
        if (_gaveUpResolvingTv)
            return;

        if (tvRoot == null)
        {
            ResolveTv();
            if (tvRoot == null)
            {
                // Single attempt; bail forever instead of scanning every transform every frame.
                _gaveUpResolvingTv = true;
                return;
            }
        }

        if (videoPlayer == null)
        {
            EnsureVideoSurface();
            PrepareVideo();
        }

        EnsureHint();
        UpdateHintBillboard();
        StopAtCutoff();
        HandleInput();
    }

    void OnDrawGizmos()
    {
        ResolveTv();
        if (tvRoot == null)
            return;

        Matrix4x4 oldMatrix = Gizmos.matrix;
        Color oldColor = Gizmos.color;

        Vector3 size = new Vector3(screenLocalSize.x, screenLocalSize.y, 0.01f);
        Matrix4x4 localToWorld = tvRoot.localToWorldMatrix
            * Matrix4x4.TRS(screenLocalPosition, Quaternion.Euler(screenLocalEuler), Vector3.one);

        Gizmos.matrix = localToWorld;
        Gizmos.color = new Color(0.2f, 1f, 0.35f, 0.9f);
        Gizmos.DrawWireCube(Vector3.zero, size);

        Gizmos.color = new Color(0.2f, 1f, 0.35f, 0.2f);
        Gizmos.DrawCube(Vector3.zero, size);

        Gizmos.matrix = oldMatrix;
        Gizmos.color = oldColor;
    }

    void ResolveTv()
    {
        Transform preferred = FindPreferredTvTransform();
        if (preferred != null && tvRoot != preferred)
        {
            tvRoot = preferred;
            return;
        }

        if (tvRoot != null)
            return;

        Transform found = FindTransform(tvObjectName);
        if (found == null)
            found = FindTransform(DefaultTvName);

        tvRoot = found;
    }

    public bool IsTvHitTransform(Transform hitTransform)
    {
        if (hitTransform == null)
            return false;

        ResolveTv();
        return tvRoot != null && (hitTransform == tvRoot || hitTransform.IsChildOf(tvRoot));
    }

    public static bool IsAnyTvHitTransform(Transform hitTransform)
    {
        if (hitTransform == null)
            return false;

        WorldspaceEasTvWebViewHost[] hosts = FindObjectsByType<WorldspaceEasTvWebViewHost>(FindObjectsInactive.Exclude);
        for (int i = 0; i < hosts.Length; i++)
        {
            if (hosts[i] != null && hosts[i].IsTvHitTransform(hitTransform))
                return true;
        }

        return false;
    }

    void RemoveOldWorldspaceScreen()
    {
        Transform oldScreen = tvRoot.Find(OldWorldspaceScreenName);
        if (oldScreen != null)
            Destroy(oldScreen.gameObject);
    }

    void EnsureVideoSurface()
    {
        Transform screenTransform = tvRoot.Find(ScreenName);
        if (screenTransform == null)
        {
            var screen = new GameObject(ScreenName);
            screen.name = ScreenName;
            screen.transform.SetParent(tvRoot, false);
            screen.AddComponent<MeshFilter>();
            screen.AddComponent<MeshRenderer>();
            screenTransform = screen.transform;
        }

        screenTransform.localPosition = screenLocalPosition;
        screenTransform.localRotation = Quaternion.Euler(screenLocalEuler);
        screenTransform.localScale = Vector3.one;

        screenMeshFilter = screenTransform.GetComponent<MeshFilter>();
        if (screenMeshFilter == null)
            screenMeshFilter = screenTransform.gameObject.AddComponent<MeshFilter>();
        screenRenderer = screenTransform.GetComponent<MeshRenderer>();
        if (screenRenderer == null)
            screenRenderer = screenTransform.gameObject.AddComponent<MeshRenderer>();

        RebuildFlatScreenMesh();
        screenRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        screenRenderer.receiveShadows = false;

        if (renderTexture == null)
        {
            renderTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
            renderTexture.name = "EAS_FinalTransmission_RenderTexture";
            renderTexture.Create();
        }

        if (screenMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Texture");

            screenMaterial = new Material(shader);
            screenMaterial.name = "EAS_FinalTransmission_ScreenMaterial_Runtime";
            screenMaterial.mainTexture = renderTexture;
            screenMaterial.SetColor("_BaseColor", Color.white);
            screenMaterial.SetColor("_Color", Color.white);
            screenMaterial.SetFloat("_Cull", 0f);
        }

        screenRenderer.sharedMaterial = screenMaterial;

        videoPlayer = screenTransform.GetComponent<VideoPlayer>();
        if (videoPlayer == null)
            videoPlayer = screenTransform.gameObject.AddComponent<VideoPlayer>();

        audioSource = screenTransform.GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = screenTransform.gameObject.AddComponent<AudioSource>();
    }

    void EnsureHint()
    {
        if (tvRoot == null)
            return;

        if (hintTextMesh != null && hintTextMesh.transform.parent == tvRoot)
        {
            hintTextMesh.text = GetHintText();
            hintTextMesh.transform.localPosition = hintLocalPosition;
            hintTextMesh.characterSize = hintCharacterSize;
            return;
        }

        Transform existing = tvRoot.Find("EAS_FullscreenHint");
        GameObject hintObject = existing != null ? existing.gameObject : new GameObject("EAS_FullscreenHint");
        hintObject.transform.SetParent(tvRoot, false);
        hintObject.transform.localPosition = hintLocalPosition;

        hintTextMesh = hintObject.GetComponent<TextMesh>();
        if (hintTextMesh == null)
            hintTextMesh = hintObject.AddComponent<TextMesh>();

        hintTextMesh.text = GetHintText();
        hintTextMesh.anchor = TextAnchor.MiddleCenter;
        hintTextMesh.alignment = TextAlignment.Center;
        hintTextMesh.characterSize = hintCharacterSize;
        hintTextMesh.fontSize = 64;
        hintTextMesh.color = new Color(0.75f, 1f, 0.78f, 0.92f);

        hintRenderer = hintObject.GetComponent<MeshRenderer>();
        if (hintRenderer != null)
        {
            hintRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            hintRenderer.receiveShadows = false;
        }
    }

    string GetHintText()
    {
        return userStartedPlayback ? fullscreenHintText : "click to play";
    }

    void UpdateHintBillboard()
    {
        if (hintTextMesh == null)
            return;

        Camera cam = GetPlayerCamera();
        if (cam == null)
            return;

        hintTextMesh.transform.rotation = Quaternion.LookRotation(hintTextMesh.transform.position - cam.transform.position, Vector3.up);

        if (hintRenderer == null)
            hintRenderer = hintTextMesh.GetComponent<MeshRenderer>();

        if (hintRenderer != null)
            hintRenderer.enabled = HasLineOfSightToHint(cam);
    }

    bool HasLineOfSightToHint(Camera cam)
    {
        Vector3 target = hintTextMesh.transform.position;
        Vector3 origin = cam.transform.position;
        Vector3 direction = target - origin;
        float distance = direction.magnitude;
        if (distance <= 0.01f)
            return true;

        RaycastHit[] hits = Physics.RaycastAll(origin, direction / distance, distance);
        if (hits == null || hits.Length == 0)
            return true;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            Transform hitTransform = hits[i].transform;
            if (hitTransform == null)
                continue;

            if (hintTextMesh != null && (hitTransform == hintTextMesh.transform || hitTransform.IsChildOf(hintTextMesh.transform)))
                return true;

            if (IsTvHitTransform(hitTransform))
                continue;

            return false;
        }

        return true;
    }

    void RebuildFlatScreenMesh()
    {
        float width = screenLocalSize.x;
        float height = screenLocalSize.y;
        float halfWidth = width * 0.5f;
        float halfHeight = height * 0.5f;

        if (screenMesh == null)
        {
            screenMesh = new Mesh();
            screenMesh.name = "EAS_FinalTransmission_FlatScreenMesh";
        }
        else
        {
            screenMesh.Clear();
        }

        screenMesh.vertices = new[]
        {
            new Vector3(-halfWidth, -halfHeight, 0f),
            new Vector3(-halfWidth, halfHeight, 0f),
            new Vector3(halfWidth, halfHeight, 0f),
            new Vector3(halfWidth, -halfHeight, 0f)
        };
        screenMesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0f)
        };
        screenMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        screenMesh.RecalculateNormals();
        screenMesh.RecalculateBounds();
        screenMeshFilter.sharedMesh = screenMesh;
    }

    void PrepareVideo()
    {
        string videoPath = ResolveVideoPath();
        if (string.IsNullOrEmpty(videoPath))
        {
            Debug.LogWarning("[EAS TV Video] Missing video file: " + assetRelativePath);
            return;
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.volume = volume;
        audioSource.minDistance = 1f;
        audioSource.maxDistance = 12f;

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = new Uri(videoPath).AbsoluteUri;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = renderTexture;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.SetTargetAudioSource(0, audioSource);
        videoPlayer.controlledAudioTrackCount = 1;
        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.waitForFirstFrame = true;
        hasPrepared = false;
        videoPlayer.Prepare();
    }

    void HandleInput()
    {
        Mouse mouse = Mouse.current;
        Keyboard keyboard = Keyboard.current;

        if (fullscreenOpen)
        {
            bool closeRequested = (mouse != null && mouse.leftButton.wasPressedThisFrame)
                || (keyboard != null && keyboard.escapeKey.wasPressedThisFrame);
            if (closeRequested)
                CloseFullscreen();
            return;
        }

        if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            return;

        Camera cam = GetPlayerCamera();
        if (cam == null)
            return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit[] hits = Physics.RaycastAll(ray, interactDistance);
        if (hits == null || hits.Length == 0)
            return;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            Transform hitTransform = hits[i].transform;
            if (hitTransform == null)
                continue;

            if (IsTvHitTransform(hitTransform))
            {
                HandleTvClick();
                return;
            }
        }
    }

    void HandleTvClick()
    {
        if (!userStartedPlayback)
        {
            PlayFromStart();
            return;
        }

        OpenFullscreen();
    }

    void OpenFullscreen()
    {
        EnsureVideoSurface();

        if (videoPlayer != null && !videoPlayer.isPlaying)
            PlayFromStart();

        if (fullscreenCanvas == null)
            BuildFullscreenCanvas();

        fullscreenOpen = true;
        fullscreenCanvas.SetActive(true);
        if (fullscreenImage != null)
            fullscreenImage.texture = renderTexture;
    }

    void CloseFullscreen()
    {
        fullscreenOpen = false;
        if (fullscreenCanvas != null)
            fullscreenCanvas.SetActive(false);
    }

    void BuildFullscreenCanvas()
    {
        fullscreenCanvas = new GameObject("EAS_FinalTransmission_FullscreenCanvas");
        fullscreenCanvas.transform.SetParent(transform, false);

        Canvas canvas = fullscreenCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 950;

        CanvasScaler scaler = fullscreenCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        fullscreenCanvas.AddComponent<GraphicRaycaster>();

        GameObject back = new GameObject("BlackBackground");
        back.transform.SetParent(fullscreenCanvas.transform, false);
        Image background = back.AddComponent<Image>();
        background.color = Color.black;
        background.raycastTarget = false;
        RectTransform backRect = background.rectTransform;
        backRect.anchorMin = Vector2.zero;
        backRect.anchorMax = Vector2.one;
        backRect.offsetMin = Vector2.zero;
        backRect.offsetMax = Vector2.zero;

        GameObject imageObject = new GameObject("Video");
        imageObject.transform.SetParent(fullscreenCanvas.transform, false);
        fullscreenImage = imageObject.AddComponent<RawImage>();
        fullscreenImage.texture = renderTexture;
        fullscreenImage.color = Color.white;
        fullscreenImage.raycastTarget = false;

        RectTransform imageRect = fullscreenImage.rectTransform;
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;

        AspectRatioFitter fitter = imageObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 16f / 9f;

        GameObject closeHint = new GameObject("CloseHint");
        closeHint.transform.SetParent(fullscreenCanvas.transform, false);
        Text hint = closeHint.AddComponent<Text>();
        hint.text = "click or esc to return";
        hint.alignment = TextAnchor.MiddleCenter;
        hint.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hint.fontSize = 18;
        hint.color = new Color(0.75f, 0.85f, 0.78f, 0.8f);
        hint.raycastTarget = false;

        RectTransform hintRect = hint.rectTransform;
        hintRect.anchorMin = new Vector2(0.5f, 0f);
        hintRect.anchorMax = new Vector2(0.5f, 0f);
        hintRect.pivot = new Vector2(0.5f, 0f);
        hintRect.anchoredPosition = new Vector2(0f, 28f);
        hintRect.sizeDelta = new Vector2(420f, 34f);
    }

    void PlayFromStart()
    {
        if (videoPlayer == null)
            return;

        if (!restartWhenClicked && videoPlayer.isPlaying)
            return;

        if (!videoPlayer.isPrepared && !hasPrepared)
        {
            videoPlayer.prepareCompleted += PlayWhenPrepared;
            hasPrepared = true;
            videoPlayer.Prepare();
            return;
        }

        StartPreparedVideo();
    }

    void PlayWhenPrepared(VideoPlayer preparedPlayer)
    {
        preparedPlayer.prepareCompleted -= PlayWhenPrepared;
        hasPrepared = false;
        StartPreparedVideo();
    }

    void StartPreparedVideo()
    {
        videoPlayer.Stop();
        videoPlayer.time = 0.0;
        audioSource.volume = volume;
        userStartedPlayback = true;
        videoPlayer.Play();
    }

    void StopAtCutoff()
    {
        if (videoPlayer == null || !videoPlayer.isPlaying)
            return;

        if (videoPlayer.time >= stopAtSeconds)
            videoPlayer.Stop();
    }

    string ResolveVideoPath()
    {
        string assetsPath = Path.Combine(Application.dataPath, assetRelativePath);
        if (File.Exists(assetsPath))
            return assetsPath;

        string streamingPath = Path.Combine(Application.streamingAssetsPath, assetRelativePath);
        if (File.Exists(streamingPath))
            return streamingPath;

        return null;
    }

    void EnsureCollider()
    {
        if (tvRoot.GetComponentInChildren<Collider>(true) != null)
            return;

        Renderer[] renderers = tvRoot.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            var fallback = tvRoot.gameObject.AddComponent<BoxCollider>();
            fallback.size = new Vector3(0.8f, 0.6f, 0.5f);
            fallback.center = new Vector3(0f, 0.25f, 0f);
            return;
        }

        Bounds world = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            world.Encapsulate(renderers[i].bounds);

        var box = tvRoot.gameObject.AddComponent<BoxCollider>();
        box.center = tvRoot.InverseTransformPoint(world.center);
        Vector3 lossy = tvRoot.lossyScale;
        box.size = new Vector3(
            world.size.x / Mathf.Max(lossy.x, 0.001f),
            world.size.y / Mathf.Max(lossy.y, 0.001f),
            world.size.z / Mathf.Max(lossy.z, 0.001f));
    }

    Camera GetPlayerCamera()
    {
        if (playerCamera != null)
            return playerCamera;

        playerCamera = Camera.main;
        if (playerCamera != null)
            return playerCamera;

        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Exclude);
        if (cameras.Length > 0)
            playerCamera = cameras[0];

        return playerCamera;
    }

    static Transform FindTransform(string exactName)
    {
        foreach (Transform transform in FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (transform.name == exactName)
                return transform;
        }

        string needle = exactName.ToLowerInvariant();
        foreach (Transform transform in FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            string name = transform.name.ToLowerInvariant();
            if (name.Contains(needle) || name.Contains("television"))
                return transform;
        }

        return null;
    }

    static Transform FindPreferredTvTransform()
    {
        Transform preferred = FindTransform(PreferredTvName);
        if (preferred != null)
            return preferred;

        foreach (Transform transform in FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            string name = transform.name.ToLowerInvariant();
            if (name.Contains("vizio") || name.Contains("flatscreen") || name.Contains("flat screen"))
                return transform;
        }

        return null;
    }
}
