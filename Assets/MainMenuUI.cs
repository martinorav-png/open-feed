using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Builds and manages the stylized main menu UI at runtime.
/// Dark, minimal, late-night aesthetic matching the game's tone.
/// Attach to any GameObject in the MainMenu scene.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("Audio")]
    [Tooltip("Plays once when the menu UI fades in. Assign in Inspector, or place clips under Assets/Audio/Resources/ (see ResolveClipsFromResources).")]
    [SerializeField] AudioClip introTitleClip;
    [Tooltip("Loops after the intro finishes. Assign in Inspector, or use Audio/Resources auto-load.")]
    [SerializeField] AudioClip menuThemeClip;
    [Range(0f, 1f)] [SerializeField] float introVolume = 0.5f;
    [Range(0f, 1f)] [SerializeField] float themeVolume = 0.425f;

    // ============================
    // COLORS (desaturated, muted palette)
    // ============================
    private static readonly Color colTitle = new Color(0.55f, 0.6f, 0.55f, 0.85f);
    private static readonly Color colSubtitle = new Color(0.35f, 0.38f, 0.35f, 0.5f);
    private static readonly Color colButton = new Color(0.4f, 0.42f, 0.4f, 0.6f);
    private static readonly Color colButtonHover = new Color(0.65f, 0.62f, 0.5f, 0.9f);
    private static readonly Color colButtonPress = new Color(0.7f, 0.65f, 0.45f, 1f);
    private static readonly Color colDivider = new Color(0.3f, 0.3f, 0.3f, 0.15f);
    private static readonly Color colVersionText = new Color(0.25f, 0.25f, 0.28f, 0.3f);
    private static readonly Color colOverlay = new Color(0, 0, 0, 0.35f);

    const string CrtFeedShaderName = "OPENFEED/MainMenu CRT Feed";
    const string BigCamsSubPath = "MonitorSite/bigcams";

    static readonly string[] KnownBigCamFiles =
    {
        "001.png", "002.png", "003.png", "004.png", "005.png", "006.png", "007.png",
        "008.png", "009.png", "010.png", "011.png", "012.png", "013.png", "014.png"
    };

    static readonly int IdRollShift = Shader.PropertyToID("_RollShift");
    static readonly int IdStaticIntensity = Shader.PropertyToID("_StaticIntensity");
    static readonly int IdNoiseSeed = Shader.PropertyToID("_NoiseSeed");

    private Canvas menuCanvas;
    private CanvasGroup canvasGroup;
    private bool isTransitioning = false;
    private Material _crtFeedMaterial;
    private Texture2D _loadedCrtFeed;
    private bool _crtMenuActive;
    private Color _titleAnimBase;
    private Color _subtitleAnimBase;
    private float _crtGlitchTimer;
    private float _crtRollTarget;
    private float _crtRollCurrent;
    private float _crtStaticTarget = 0.1f;
    private Color _btnNormal;
    private Color _btnHover;
    private Color _btnPress;
    private RawImage _crtRawFeed;
    private AspectRatioFitter _crtAspectFitter;
    private CanvasGroup _crtFeedCanvasGroup;
    private CanvasGroup _transitionBlackout;

    // Button references for animations
    private TextMeshProUGUI playText;
    private TextMeshProUGUI settingsText;
    private TextMeshProUGUI quitText;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI taglineText;

    AudioSource musicSource;
    bool menuMusicCancelled;

    void Awake()
    {
        GameFlowManager.EnsureExists();
    }

    void Start()
    {
        EnsureAudioListener();
        PruneExtraAudioListeners();

        // Ensure EventSystem exists
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        if (ShouldUseCrtCamFeedMenu())
        {
            Texture2D feed = TryLoadRandomBigcamSync();
            BuildMenu(feed, true);
#if (UNITY_ANDROID || UNITY_WEBGL) && !UNITY_EDITOR
            if (feed == null)
                StartCoroutine(LoadStreamingBigcamAndApply());
#endif
            StartCoroutine(FadeIn());
            StartCoroutine(AmbientAnimations());
        }
        else
        {
            BuildMenu(null, false);
            StartCoroutine(FadeIn());
            StartCoroutine(AmbientAnimations());
        }
    }

    void OnDestroy()
    {
        StopMenuMusic();
        if (_loadedCrtFeed != null)
            Destroy(_loadedCrtFeed);
        if (_crtFeedMaterial != null)
            Destroy(_crtFeedMaterial);
    }

    void ReplaceCrtFeedTexture(Texture2D newTex)
    {
        if (newTex == null || _crtRawFeed == null)
            return;

        if (_loadedCrtFeed != null)
            Destroy(_loadedCrtFeed);

        newTex.filterMode = FilterMode.Point;
        newTex.wrapMode = TextureWrapMode.Clamp;
        newTex.Apply(false, true);
        _loadedCrtFeed = newTex;
        _crtRawFeed.texture = newTex;
        if (_crtAspectFitter != null)
            _crtAspectFitter.aspectRatio = newTex.width / Mathf.Max(1f, (float)newTex.height);
    }

    Texture2D TryLoadRandomBigcamSync()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return null;
#elif UNITY_WEBGL && !UNITY_EDITOR
        return null;
#else
        Texture2D tex = null;
        string folder = Path.Combine(Application.streamingAssetsPath,
            BigCamsSubPath.Replace('/', Path.DirectorySeparatorChar));

        if (Directory.Exists(folder))
        {
            string[] files = Directory.GetFiles(folder)
                .Where(p =>
                {
                    string e = Path.GetExtension(p).ToLowerInvariant();
                    return e == ".png" || e == ".jpg" || e == ".jpeg";
                })
                .ToArray();

            if (files.Length > 0)
            {
                string pick = files[Random.Range(0, files.Length)];
                try
                {
                    byte[] bytes = File.ReadAllBytes(pick);
                    tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    tex.LoadImage(bytes);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("[MainMenuUI] Could not load bigcam: " + e.Message);
                    tex = null;
                }
            }
        }
        else
        {
            foreach (string name in KnownBigCamFiles.OrderBy(_ => Random.value))
            {
                string path = Path.Combine(Application.streamingAssetsPath,
                    BigCamsSubPath.Replace('/', Path.DirectorySeparatorChar), name);
                if (!File.Exists(path))
                    continue;
                try
                {
                    byte[] bytes = File.ReadAllBytes(path);
                    tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    tex.LoadImage(bytes);
                    break;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("[MainMenuUI] Could not load bigcam: " + e.Message);
                }
            }
        }

        if (tex != null)
        {
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply(false, true);
            _loadedCrtFeed = tex;
        }

        return tex;
#endif
    }

    /// <summary>
    /// Surveillance-style noise frame when bigcam PNGs are missing from StreamingAssets.
    /// </summary>
    static Texture2D CreateProceduralCctvFallbackTexture()
    {
        const int w = 640;
        const int h = 360;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        float seed = Random.Range(0f, 500f);
        var pixels = new Color[w * h];
        int i = 0;
        for (int y = 0; y < h; y++)
        {
            float scan = 0.88f + 0.12f * Mathf.Sin(y * 0.22f);
            for (int x = 0; x < w; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.035f + seed, y * 0.035f + seed * 0.17f);
                float v = (0.1f + n * 0.38f) * scan;
                pixels[i++] = new Color(v * 0.52f, v * 0.68f, v * 0.45f);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(false, true);
        return tex;
    }

#if (UNITY_ANDROID || UNITY_WEBGL) && !UNITY_EDITOR
    IEnumerator LoadStreamingBigcamAndApply()
    {
        Texture2D tex = null;

#if UNITY_ANDROID
        var shuffled = new List<string>(KnownBigCamFiles);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        foreach (string name in shuffled)
        {
            string url = Path.Combine(Application.streamingAssetsPath, BigCamsSubPath, name)
                .Replace("\\", "/");
            using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(url))
            {
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                    continue;
                tex = DownloadHandlerTexture.GetContent(req);
                if (tex != null)
                    break;
            }
        }
#elif UNITY_WEBGL
        string pickName = KnownBigCamFiles[Random.Range(0, KnownBigCamFiles.Length)];
        string url = Path.Combine(Application.streamingAssetsPath, BigCamsSubPath, pickName)
            .Replace("\\", "/");
        using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(url))
        {
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
                tex = DownloadHandlerTexture.GetContent(req);
        }
#endif

        if (tex != null)
            ReplaceCrtFeedTexture(tex);
        else
            ReplaceCrtFeedTexture(CreateProceduralCctvFallbackTexture());
    }
#endif

    void Update()
    {
        if (!_crtMenuActive || _crtFeedMaterial == null)
            return;

        _crtGlitchTimer -= Time.deltaTime;
        if (_crtGlitchTimer <= 0f)
        {
            _crtGlitchTimer = Random.Range(0.35f, 1.8f);
            if (Random.value < 0.45f)
                _crtRollTarget = Random.Range(-0.035f, 0.035f);
            else
                _crtRollTarget = 0f;
            if (Random.value < 0.3f)
                _crtStaticTarget = Random.Range(0.14f, 0.28f);
            else
                _crtStaticTarget = Random.Range(0.06f, 0.12f);
        }

        _crtRollCurrent = Mathf.Lerp(_crtRollCurrent, _crtRollTarget, Time.deltaTime * 8f);
        _crtFeedMaterial.SetFloat(IdRollShift, _crtRollCurrent);

        float staticNow = Mathf.Lerp(0.06f, _crtStaticTarget,
            0.5f + 0.5f * Mathf.Sin(Time.time * 6.7f));
        _crtFeedMaterial.SetFloat(IdStaticIntensity, staticNow);
        _crtFeedMaterial.SetFloat(IdNoiseSeed, Time.time * 3.17f);
    }

    /// <summary>
    /// Full-screen surveillance feed + CRT/static shader — used for every main-menu entry
    /// (dedicated MainMenu scene or booting straight into supermarket with MainMenuUI).
    /// </summary>
    static bool ShouldUseCrtCamFeedMenu()
    {
        string n = SceneManager.GetActiveScene().name;
        if (string.Equals(n, "MainMenu", System.StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.Equals(n, "supermarket", System.StringComparison.OrdinalIgnoreCase))
            return true;
        if (GameFlowManager.Instance != null)
        {
            if (string.Equals(n, GameFlowManager.Instance.mainMenuScene, System.StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(n, GameFlowManager.Instance.storeScene, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    // ============================
    // BUILD MENU UI
    // ============================

    void BuildMenu(Texture2D crtFeed, bool crtStyle)
    {
        _crtMenuActive = crtStyle;
        _crtGlitchTimer = Random.Range(0.2f, 1f);
        _crtRollCurrent = 0f;
        _crtRollTarget = 0f;

        _titleAnimBase = colTitle;
        _subtitleAnimBase = colSubtitle;
        _btnNormal = colButton;
        _btnHover = colButtonHover;
        _btnPress = colButtonPress;

        if (crtStyle)
        {
            _subtitleAnimBase = new Color(0.74f, 0.76f, 0.74f, 0.9f);
            _btnNormal = new Color(0.72f, 0.74f, 0.72f, 0.96f);
            _btnHover = new Color(0.92f, 0.9f, 0.8f, 1f);
            _btnPress = new Color(0.98f, 0.95f, 0.88f, 1f);
        }

        // Canvas
        GameObject canvasObj = new GameObject("MainMenuCanvas");
        canvasObj.transform.SetParent(transform);

        menuCanvas = canvasObj.AddComponent<Canvas>();
        menuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // GameFlowManager canvas uses 999 — stay above it so the menu is never covered.
        menuCanvas.sortingOrder = FindAnyObjectByType<GameFlowManager>() != null ? 2000 : 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        Transform uiContentParent;

        // ---- Supermarket: feed is not under the fading CanvasGroup so it shows immediately ----
        if (crtStyle)
        {
            Texture2D displayTex = crtFeed;
            if (displayTex == null)
            {
                // No files under StreamingAssets/MonitorSite/bigcams (common in fresh clones) — use a
                // visible CCTV-style frame so the CRT shader + static read clearly instead of pure black.
                displayTex = CreateProceduralCctvFallbackTexture();
                _loadedCrtFeed = displayTex;
            }

            GameObject feedRoot = CreateUIObj("CrtCamFeed", canvasObj.transform);
            StretchFull(feedRoot);
            feedRoot.transform.SetAsFirstSibling();

            _crtFeedCanvasGroup = feedRoot.AddComponent<CanvasGroup>();
            _crtFeedCanvasGroup.alpha = 1f;
            _crtFeedCanvasGroup.blocksRaycasts = false;
            _crtFeedCanvasGroup.interactable = false;

            AspectRatioFitter arf = feedRoot.AddComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            arf.aspectRatio = displayTex.width / Mathf.Max(1f, (float)displayTex.height);
            _crtAspectFitter = arf;

            RawImage rawFeed = feedRoot.AddComponent<RawImage>();
            rawFeed.texture = displayTex;
            rawFeed.uvRect = new Rect(0f, 0f, 1f, 1f);
            rawFeed.raycastTarget = false;
            _crtRawFeed = rawFeed;

            Shader crtShader = Shader.Find(CrtFeedShaderName);
            if (crtShader != null)
            {
                if (_crtFeedMaterial != null)
                    Destroy(_crtFeedMaterial);
                _crtFeedMaterial = new Material(crtShader);
                _crtFeedMaterial.SetFloat(IdStaticIntensity, 0.1f);
                rawFeed.material = _crtFeedMaterial;
            }
            else
                Debug.LogWarning("[MainMenuUI] Shader not found: " + CrtFeedShaderName);

            GameObject uiFade = CreateUIObj("UiFade", canvasObj.transform);
            StretchFull(uiFade);
            canvasGroup = uiFade.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            uiContentParent = uiFade.transform;
        }
        else
        {
            _crtRawFeed = null;
            _crtAspectFitter = null;
            _crtFeedCanvasGroup = null;
            canvasGroup = canvasObj.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            uiContentParent = canvasObj.transform;
        }

        // ---- Dark overlay ----
        GameObject overlay = CreateUIObj("Overlay", uiContentParent);
        Image overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = colOverlay;
        overlayImg.raycastTarget = false;
        StretchFull(overlay);

        // ---- Local dark backing behind left-column UI (feeds / busy BG) ----
        if (crtStyle)
        {
            GameObject cloud = CreateUIObj("TextReadabilityBacking", uiContentParent);
            RectTransform cloudRT = cloud.GetComponent<RectTransform>();
            cloudRT.anchorMin = new Vector2(0f, 0f);
            cloudRT.anchorMax = new Vector2(0f, 1f);
            cloudRT.pivot = new Vector2(0f, 0.5f);
            cloudRT.sizeDelta = new Vector2(920f, 0f);
            cloudRT.anchoredPosition = Vector2.zero;
            Image cloudImg = cloud.AddComponent<Image>();
            cloudImg.color = new Color(0f, 0f, 0f, 0.52f);
            cloudImg.raycastTarget = false;
        }

        // ---- Main container (centered, left-aligned content) ----
        GameObject container = CreateUIObj("Container", uiContentParent);
        RectTransform contRT = container.GetComponent<RectTransform>();
        contRT.anchorMin = new Vector2(0, 0);
        contRT.anchorMax = new Vector2(1, 1);
        contRT.offsetMin = Vector2.zero;
        contRT.offsetMax = Vector2.zero;

        // ---- Title: OPEN FEED ----
        GameObject titleObj = CreateUIObj("Title", container.transform);
        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "OPEN FEED";
        titleText.fontSize = 78;
        titleText.fontStyle = FontStyles.Normal;
        titleText.characterSpacing = 18f;
        titleText.alignment = TextAlignmentOptions.Left;
        titleText.color = colTitle;
        titleText.enableAutoSizing = false;
        titleText.raycastTarget = false;

        RectTransform titleRT = titleObj.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 0.5f);
        titleRT.anchorMax = new Vector2(0, 0.5f);
        titleRT.pivot = new Vector2(0, 0.5f);
        titleRT.sizeDelta = new Vector2(700, 100);
        titleRT.anchoredPosition = new Vector2(140, 120);

        // ---- Thin divider line under title ----
        GameObject divider = CreateUIObj("Divider", container.transform);
        Image divImg = divider.AddComponent<Image>();
        divImg.color = colDivider;
        divImg.raycastTarget = false;

        RectTransform divRT = divider.GetComponent<RectTransform>();
        divRT.anchorMin = new Vector2(0, 0.5f);
        divRT.anchorMax = new Vector2(0, 0.5f);
        divRT.pivot = new Vector2(0, 0.5f);
        divRT.sizeDelta = new Vector2(320, 1);
        divRT.anchoredPosition = new Vector2(144, 62);

        // ---- Tagline ----
        GameObject tagObj = CreateUIObj("Tagline", container.transform);
        taglineText = tagObj.AddComponent<TextMeshProUGUI>();
        taglineText.text = "late november.";
        taglineText.fontSize = 18;
        taglineText.fontStyle = FontStyles.Italic;
        taglineText.alignment = TextAlignmentOptions.Left;
        taglineText.color = _subtitleAnimBase;
        taglineText.enableAutoSizing = false;
        taglineText.raycastTarget = false;

        RectTransform tagRT = tagObj.GetComponent<RectTransform>();
        tagRT.anchorMin = new Vector2(0, 0.5f);
        tagRT.anchorMax = new Vector2(0, 0.5f);
        tagRT.pivot = new Vector2(0, 0.5f);
        tagRT.sizeDelta = new Vector2(400, 35);
        tagRT.anchoredPosition = new Vector2(144, 38);

        // ---- Menu buttons ----
        float buttonStartY = -20f;
        float buttonSpacing = 52f;

        playText = CreateMenuButton(container.transform, "PLAY", buttonStartY, OnPlayClicked);
        settingsText = CreateMenuButton(container.transform, "SETTINGS", buttonStartY - buttonSpacing, null);
        quitText = CreateMenuButton(container.transform, "QUIT", buttonStartY - buttonSpacing * 2, OnQuitClicked);

        settingsText.color = new Color(_btnNormal.r, _btnNormal.g, _btnNormal.b, crtStyle ? 0.4f : 0.2f);

        // ---- Version text (bottom-left) ----
        GameObject verObj = CreateUIObj("Version", container.transform);
        TextMeshProUGUI verText = verObj.AddComponent<TextMeshProUGUI>();
        verText.text = "v0.1";
        verText.fontSize = 14;
        verText.alignment = TextAlignmentOptions.Left;
        verText.color = colVersionText;
        verText.enableAutoSizing = false;
        verText.raycastTarget = false;

        RectTransform verRT = verObj.GetComponent<RectTransform>();
        verRT.anchorMin = new Vector2(0, 0);
        verRT.anchorMax = new Vector2(0, 0);
        verRT.pivot = new Vector2(0, 0);
        verRT.sizeDelta = new Vector2(200, 30);
        verRT.anchoredPosition = new Vector2(30, 20);

        // ---- Bottom-right hint ----
        GameObject hintObj = CreateUIObj("Hint", container.transform);
        TextMeshProUGUI hintText = hintObj.AddComponent<TextMeshProUGUI>();
        hintText.text = "navigate with mouse";
        hintText.fontSize = 13;
        hintText.alignment = TextAlignmentOptions.Right;
        hintText.color = colVersionText;
        hintText.enableAutoSizing = false;
        hintText.raycastTarget = false;

        RectTransform hintRT = hintObj.GetComponent<RectTransform>();
        hintRT.anchorMin = new Vector2(1, 0);
        hintRT.anchorMax = new Vector2(1, 0);
        hintRT.pivot = new Vector2(1, 0);
        hintRT.sizeDelta = new Vector2(300, 30);
        hintRT.anchoredPosition = new Vector2(-30, 20);

        // ---- Full-screen black (Play: fade in first to hide world; stays until menu canvas off) ----
        GameObject bo = CreateUIObj("TransitionBlackout", canvasObj.transform);
        StretchFull(bo);
        Image boImg = bo.AddComponent<Image>();
        boImg.color = Color.black;
        boImg.raycastTarget = false;
        _transitionBlackout = bo.AddComponent<CanvasGroup>();
        _transitionBlackout.alpha = 0f;
        _transitionBlackout.blocksRaycasts = false;
        _transitionBlackout.interactable = false;
        bo.transform.SetAsLastSibling();
    }

    // ============================
    // BUTTON FACTORY
    // ============================

    TextMeshProUGUI CreateMenuButton(Transform parent, string label, float yOffset, System.Action onClick)
    {
        GameObject btnObj = CreateUIObj($"Btn_{label}", parent);

        RectTransform btnRT = btnObj.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0, 0.5f);
        btnRT.anchorMax = new Vector2(0, 0.5f);
        btnRT.pivot = new Vector2(0, 0.5f);
        btnRT.sizeDelta = new Vector2(300, 44);
        btnRT.anchoredPosition = new Vector2(140, yOffset);

        // Invisible button image for raycasting
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = Color.clear;

        // Button component
        Button btn = btnObj.AddComponent<Button>();
        btn.transition = Selectable.Transition.None; // We handle visuals ourselves

        if (onClick != null)
        {
            btn.onClick.AddListener(() => onClick());
        }
        else
        {
            btn.interactable = false;
        }

        // ---- Small dash prefix (appears on hover) ----
        GameObject dashObj = CreateUIObj("Dash", btnObj.transform);
        TextMeshProUGUI dashText = dashObj.AddComponent<TextMeshProUGUI>();
        dashText.text = "\u2014";
        dashText.fontSize = 22;
        dashText.alignment = TextAlignmentOptions.Left;
        dashText.color = new Color(_btnHover.r, _btnHover.g, _btnHover.b, 0f);
        dashText.enableAutoSizing = false;
        dashText.raycastTarget = false;

        RectTransform dashRT = dashObj.GetComponent<RectTransform>();
        dashRT.anchorMin = new Vector2(0, 0);
        dashRT.anchorMax = new Vector2(0, 1);
        dashRT.pivot = new Vector2(0, 0.5f);
        dashRT.sizeDelta = new Vector2(30, 0);
        dashRT.anchoredPosition = new Vector2(-8, 0);

        // ---- Label text ----
        GameObject labelObj = CreateUIObj("Label", btnObj.transform);
        TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 26;
        labelText.characterSpacing = 8f;
        labelText.alignment = TextAlignmentOptions.Left;
        labelText.color = _btnNormal;
        labelText.enableAutoSizing = false;
        labelText.raycastTarget = false;

        RectTransform labelRT = labelObj.GetComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0, 0);
        labelRT.anchorMax = new Vector2(1, 1);
        labelRT.offsetMin = new Vector2(28, 0);
        labelRT.offsetMax = Vector2.zero;

        // ---- Hover handler ----
        MenuButtonHover hover = btnObj.AddComponent<MenuButtonHover>();
        hover.labelText = labelText;
        hover.dashText = dashText;
        hover.normalColor = _btnNormal;
        hover.hoverColor = _btnHover;
        hover.pressColor = _btnPress;

        return labelText;
    }

    // ============================
    // BUTTON CALLBACKS
    // ============================

    void OnPlayClicked()
    {
        if (isTransitioning) return;
        isTransitioning = true;
        StartCoroutine(PlayTransition());
    }

    IEnumerator PlayTransition()
    {
        StopMenuMusic();

        const float blackDur = 0.22f;
        if (_transitionBlackout != null)
        {
            _transitionBlackout.blocksRaycasts = true;
            yield return StartCoroutine(FadeCanvasGroup(_transitionBlackout, _transitionBlackout.alpha, 1f, blackDur));
        }

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0f;
        if (_crtFeedCanvasGroup != null)
            _crtFeedCanvasGroup.alpha = 0f;

        SixTwelveIntroController intro = FindAnyObjectByType<SixTwelveIntroController>();
        if (intro != null && intro.BeginIntroSequence())
        {
            yield return null;
            if (menuCanvas != null)
                menuCanvas.enabled = false;
            yield break;
        }

        GameFlowManager gfm = GameFlowManager.Instance ?? GameFlowManager.EnsureExists();
        gfm.StartStoreFlowFromMainMenuExposition();

        yield return null;
        if (menuCanvas != null)
            menuCanvas.enabled = false;
    }

    void OnQuitClicked()
    {
        if (isTransitioning) return;
        isTransitioning = true;
        StartCoroutine(QuitTransition());
    }

    IEnumerator QuitTransition()
    {
        StopMenuMusic();

        const float dur = 0.8f;
        float uiA = canvasGroup.alpha;
        StartCoroutine(FadeCanvasGroup(canvasGroup, uiA, 0f, dur));
        if (_crtFeedCanvasGroup != null)
            StartCoroutine(FadeCanvasGroup(_crtFeedCanvasGroup, _crtFeedCanvasGroup.alpha, 0f, dur));
        yield return new WaitForSeconds(dur);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ============================
    // ANIMATIONS
    // ============================

    IEnumerator FadeIn()
    {
        // Staggered fade-in for cinematic feel
        canvasGroup.alpha = 0f;
        yield return new WaitForSeconds(0.3f);

        StartMenuMusic();

        // Fade in entire canvas
        yield return StartCoroutine(FadeCanvasGroup(canvasGroup, 0f, 1f, 1.2f));
    }

    static void EnsureAudioListener()
    {
        if (FindAnyObjectByType<AudioListener>() != null)
            return;

        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.gameObject.AddComponent<AudioListener>();
            return;
        }

        cam = FindAnyObjectByType<Camera>();
        if (cam != null)
            cam.gameObject.AddComponent<AudioListener>();
    }

    static void PruneExtraAudioListeners()
    {
        var listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Include);
        if (listeners == null || listeners.Length <= 1)
            return;

        AudioListener keep = null;
        if (Camera.main != null)
            keep = Camera.main.GetComponent<AudioListener>();

        if (keep == null)
        {
            foreach (AudioListener l in listeners)
            {
                if (l != null && l.enabled)
                {
                    keep = l;
                    break;
                }
            }
        }

        if (keep == null)
            keep = listeners[0];

        foreach (AudioListener l in listeners)
        {
            if (l != null && l != keep)
                l.enabled = false;
        }
    }

    void EnsureMusicSource()
    {
        if (musicSource != null)
            return;

        GameObject audioObj = new GameObject("MainMenuMusic");
        audioObj.transform.SetParent(transform, false);
        musicSource = audioObj.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = false;
        musicSource.spatialBlend = 0f;
    }

    void ResolveClipsFromResources()
    {
        if (introTitleClip == null)
        {
            introTitleClip = Resources.Load<AudioClip>("openfeed intro title")
                ?? Resources.Load<AudioClip>("openfeed_intro_title")
                ?? Resources.Load<AudioClip>("Audio/openfeed intro title")
                ?? Resources.Load<AudioClip>("Audio/openfeed_intro_title")
                ?? Resources.Load<AudioClip>("MainMenu/openfeed intro title")
                ?? Resources.Load<AudioClip>("MainMenu/openfeed_intro_title");
        }

        if (menuThemeClip == null)
        {
            menuThemeClip = Resources.Load<AudioClip>("openfeed2")
                ?? Resources.Load<AudioClip>("Audio/openfeed2")
                ?? Resources.Load<AudioClip>("MainMenu/openfeed2");
        }
    }

    void StartMenuMusic()
    {
        ResolveClipsFromResources();
        if (introTitleClip == null && menuThemeClip == null)
            return;

        EnsureMusicSource();
        menuMusicCancelled = false;

        if (introTitleClip != null)
        {
            musicSource.PlayOneShot(introTitleClip, introVolume);
            StartCoroutine(PlayMenuThemeAfterIntro());
        }
        else if (menuThemeClip != null)
        {
            musicSource.clip = menuThemeClip;
            musicSource.loop = true;
            musicSource.volume = themeVolume;
            musicSource.Play();
        }
    }

    IEnumerator PlayMenuThemeAfterIntro()
    {
        float len = introTitleClip != null ? introTitleClip.length : 0f;
        if (len > 0f)
            yield return new WaitForSeconds(len);

        if (menuMusicCancelled || menuThemeClip == null)
            yield break;

        musicSource.clip = menuThemeClip;
        musicSource.loop = true;
        musicSource.volume = themeVolume;
        musicSource.Play();
    }

    void StopMenuMusic()
    {
        menuMusicCancelled = true;
        if (musicSource != null)
        {
            musicSource.Stop();
            musicSource.clip = null;
            musicSource.loop = false;
        }
    }

    IEnumerator AmbientAnimations()
    {
        // Subtle title breathing and tagline pulse
        while (true)
        {
            if (titleText != null)
            {
                float breath = 0.8f + Mathf.Sin(Time.time * 0.4f) * 0.05f;
                Color c = _titleAnimBase;
                c.a = breath;
                titleText.color = c;
            }

            if (taglineText != null)
            {
                float wobble = 0.94f + Mathf.Sin(Time.time * 0.6f + 1f) * 0.05f;
                Color c = _subtitleAnimBase;
                c.a = Mathf.Clamp01(wobble * _subtitleAnimBase.a);
                taglineText.color = c;
            }

            yield return null;
        }
    }

    IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smooth = t * t * (3f - 2f * t);
            cg.alpha = Mathf.Lerp(from, to, smooth);
            yield return null;
        }
        cg.alpha = to;
    }

    // ============================
    // HELPERS
    // ============================

    GameObject CreateUIObj(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        return obj;
    }

    void StretchFull(GameObject obj)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}

/// <summary>
/// Handles hover/press visuals for main menu buttons.
/// Animates text color and shows/hides the em-dash prefix.
/// </summary>
public class MenuButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    public TextMeshProUGUI labelText;
    public TextMeshProUGUI dashText;
    public Color normalColor;
    public Color hoverColor;
    public Color pressColor;

    private bool isHovered = false;
    private bool isPressed = false;
    private float animProgress = 0f;
    private float dashAlpha = 0f;

    // Text offset when hovered (slides right slightly)
    private RectTransform labelRT;
    private float baseX;

    void Start()
    {
        if (labelText != null)
        {
            labelRT = labelText.GetComponent<RectTransform>();
            baseX = labelRT.offsetMin.x;
        }
    }

    void Update()
    {
        // Animate toward target state
        float target = isHovered ? 1f : 0f;
        animProgress = Mathf.MoveTowards(animProgress, target, Time.deltaTime * 5f);

        float dashTarget = isHovered ? 1f : 0f;
        dashAlpha = Mathf.MoveTowards(dashAlpha, dashTarget, Time.deltaTime * 6f);

        // Color
        Color targetColor = isPressed ? pressColor : (isHovered ? hoverColor : normalColor);
        if (labelText != null)
            labelText.color = Color.Lerp(normalColor, targetColor, animProgress);

        // Dash visibility
        if (dashText != null)
        {
            Color dc = dashText.color;
            dc.a = dashAlpha * hoverColor.a;
            dashText.color = dc;
        }

        // Subtle slide
        if (labelRT != null)
        {
            float slide = Mathf.Lerp(0, 6f, animProgress);
            labelRT.offsetMin = new Vector2(baseX + slide, labelRT.offsetMin.y);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        isPressed = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
    }
}
