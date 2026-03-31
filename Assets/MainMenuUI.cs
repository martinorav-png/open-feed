using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

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
    [Range(0f, 1f)] [SerializeField] float introVolume = 1f;
    [Range(0f, 1f)] [SerializeField] float themeVolume = 0.85f;

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

    private Canvas menuCanvas;
    private CanvasGroup canvasGroup;
    private bool isTransitioning = false;

    // Button references for animations
    private TextMeshProUGUI playText;
    private TextMeshProUGUI settingsText;
    private TextMeshProUGUI quitText;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI taglineText;

    AudioSource musicSource;
    bool menuMusicCancelled;

    void Start()
    {
        EnsureAudioListener();

        // Ensure EventSystem exists
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        BuildMenu();
        StartCoroutine(FadeIn());
        StartCoroutine(AmbientAnimations());
    }

    void OnDestroy()
    {
        StopMenuMusic();
    }

    // ============================
    // BUILD MENU UI
    // ============================

    void BuildMenu()
    {
        // Canvas
        GameObject canvasObj = new GameObject("MainMenuCanvas");
        canvasObj.transform.SetParent(transform);

        menuCanvas = canvasObj.AddComponent<Canvas>();
        menuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        menuCanvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        canvasGroup = canvasObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        // ---- Dark overlay (subtle vignette-like darkening) ----
        GameObject overlay = CreateUIObj("Overlay", canvasObj.transform);
        Image overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = colOverlay;
        overlayImg.raycastTarget = false;
        StretchFull(overlay);

        // ---- Main container (centered, left-aligned content) ----
        GameObject container = CreateUIObj("Container", canvasObj.transform);
        RectTransform contRT = container.GetComponent<RectTransform>();
        contRT.anchorMin = new Vector2(0, 0);
        contRT.anchorMax = new Vector2(1, 1);
        contRT.offsetMin = Vector2.zero;
        contRT.offsetMax = Vector2.zero;

        // ---- Title: OPEN FEED ----
        // Positioned left-of-center, vertically centered with slight upward offset
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
        taglineText.color = colSubtitle;
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

        // Dim out settings (not implemented)
        settingsText.color = new Color(colButton.r, colButton.g, colButton.b, 0.2f);

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
        dashText.color = new Color(colButtonHover.r, colButtonHover.g, colButtonHover.b, 0f);
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
        labelText.color = colButton;
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
        hover.normalColor = colButton;
        hover.hoverColor = colButtonHover;
        hover.pressColor = colButtonPress;

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

        // Fade out menu
        yield return StartCoroutine(FadeCanvasGroup(canvasGroup, 1f, 0f, 0.6f));

        StoreFlowIntroController storeFlowIntro = FindAnyObjectByType<StoreFlowIntroController>();
        if (storeFlowIntro != null && storeFlowIntro.BeginIntroSequence())
            yield break;

        SixTwelveIntroController intro = FindAnyObjectByType<SixTwelveIntroController>();
        if (intro != null && intro.BeginIntroSequence())
            yield break;

        // Tell GameFlowManager to start the game
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.StartGame();
        }
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

        yield return StartCoroutine(FadeCanvasGroup(canvasGroup, 1f, 0f, 0.8f));

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
                Color c = colTitle;
                c.a = breath;
                titleText.color = c;
            }

            if (taglineText != null)
            {
                float pulse = 0.35f + Mathf.Sin(Time.time * 0.6f + 1f) * 0.12f;
                Color c = colSubtitle;
                c.a = pulse;
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
