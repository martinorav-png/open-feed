using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class GameFlowManager : MonoBehaviour
{
    // ============================
    // CONFIGURATION
    // ============================

    [Header("Scene Names (must match Build Settings)")]
    public string mainMenuScene = "MainMenu";
    public string storeScene = "GroceryStore";
    public string drivingScene = "Driving";
    public string deskScene = "Desk";

    [Header("Timing")]
    public float fadeDuration = 1.5f;
    public float subtitleDisplayTime = 3.5f;
    public float subtitleFadeTime = 0.8f;
    public float delayBeforeSubtitle = 1f;
    public float delayAfterSubtitle = 1f;

    [Header("Store Flow")]
    public bool useStoreIntroWhenAvailable = true;
    public bool fallbackToStoreCutscene = false;

    // ============================
    // STATE
    // ============================
    private enum GameState
    {
        MainMenu,
        TransitionToStore,
        Store,
        TransitionToDriving,
        Driving,
        TransitionToDesk,
        Desk
    }

    private GameState state = GameState.MainMenu;

    // UI references (created at runtime)
    private Canvas uiCanvas;
    private Image fadePanel;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI promptText;
    private TextMeshProUGUI subtitleText;
    private TextMeshProUGUI creatorCreditText;

    private bool inputReady = false;
    private float inputCooldown = 0.5f;
    private float inputTimer = 0f;
    private bool storeAutoAdvanceEnabled = false;

    // Singleton
    public static GameFlowManager Instance;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        CreateUI();
        SceneManager.sceneLoaded += OnSceneLoaded;

        SixTwelveIntroController sixTwelveIntro = FindAnyObjectByType<SixTwelveIntroController>();
        if (sixTwelveIntro != null)
        {
            state = GameState.MainMenu;
            if (titleText != null) titleText.gameObject.SetActive(false);
            if (promptText != null) promptText.gameObject.SetActive(false);
            return;
        }

        // Determine initial state from current scene
        string current = SceneManager.GetActiveScene().name;
        if (current == mainMenuScene)
        {
            state = GameState.MainMenu;
            StartCoroutine(PlayCreatorCreditThenMainMenu());
        }
        else if (current == storeScene)
        {
            state = GameState.Store;
        }
        else if (current == drivingScene)
        {
            state = GameState.Driving;
        }
        else if (current == deskScene)
        {
            state = GameState.Desk;
        }
    }

    void Update()
    {
        Mouse mouse = Mouse.current;
        Keyboard keyboard = Keyboard.current;
        if (mouse == null && keyboard == null) return;

        // Input cooldown to prevent accidental double-clicks
        if (!inputReady)
        {
            inputTimer += Time.deltaTime;
            if (inputTimer >= inputCooldown)
                inputReady = true;
        }

        // Auto-advance for store only when explicitly using cutscene path.
        if (state == GameState.Store)
        {
            if (storeAutoAdvanceEnabled)
            {
                CutscenePlayer cutscene = CutscenePlayer.Instance;
                if (cutscene != null && cutscene.IsComplete)
                {
                    StartCoroutine(TransitionToScene(
                        drivingScene,
                        GameState.TransitionToDriving,
                        GameState.Driving,
                        "You get back in the car.",
                        "The roads are empty tonight."
                    ));
                }
            }
            return;
        }

        // Driving always remains a cutscene transition scene.
        if (state == GameState.Driving)
        {
            CutscenePlayer cutscene = CutscenePlayer.Instance;
            if (cutscene != null && cutscene.IsComplete)
            {
                StartCoroutine(TransitionToScene(
                    deskScene,
                    GameState.TransitionToDesk,
                    GameState.Desk,
                    "2:13 AM",
                    "You can't sleep.\nThe browser is still open."
                ));
            }
            return; // Don't process click input during cutscenes
        }

        // MainMenu input is now handled by MainMenuUI buttons.
        // No raw click detection needed for MainMenu state.
        // Desk is the final scene — no transitions.
    }

    IEnumerator BeginStoreSceneFlow()
    {
        // Let scene objects finish Awake/Start before intro lookup.
        yield return null;

        if (useStoreIntroWhenAvailable)
        {
            StoreFlowIntroController storeFlowIntro = FindAnyObjectByType<StoreFlowIntroController>();
            if (storeFlowIntro != null && storeFlowIntro.BeginIntroSequence())
            {
                state = GameState.TransitionToStore;
                storeAutoAdvanceEnabled = false;

                if (titleText != null) titleText.gameObject.SetActive(false);
                if (promptText != null) promptText.gameObject.SetActive(false);
                yield break;
            }
        }

        if (fallbackToStoreCutscene)
        {
            AttachCutscenePlayer(CutscenePlayer.CutsceneType.Store);
            storeAutoAdvanceEnabled = true;
        }
        else
        {
            storeAutoAdvanceEnabled = false;
        }
    }

    public void StartGame()
    {
        if (state != GameState.MainMenu)
            return;

        StoreFlowIntroController storeFlowIntro = FindAnyObjectByType<StoreFlowIntroController>();
        if (storeFlowIntro != null && storeFlowIntro.BeginIntroSequence())
        {
            state = GameState.TransitionToStore;
            storeAutoAdvanceEnabled = false;
            if (titleText != null) titleText.gameObject.SetActive(false);
            if (promptText != null) promptText.gameObject.SetActive(false);
            return;
        }

        SixTwelveIntroController sixTwelveIntro = FindAnyObjectByType<SixTwelveIntroController>();
        if (sixTwelveIntro != null && sixTwelveIntro.BeginIntroSequence())
        {
            state = GameState.TransitionToStore;
            storeAutoAdvanceEnabled = false;
            if (titleText != null) titleText.gameObject.SetActive(false);
            if (promptText != null) promptText.gameObject.SetActive(false);
            return;
        }

        StartCoroutine(TransitionToScene(
            storeScene,
            GameState.TransitionToStore,
            GameState.Store,
            "You head into the store.",
            "The fluorescent lights hum overhead."
        ));
    }

    // ============================
    // SCENE TRANSITIONS
    // ============================

    IEnumerator TransitionToScene(string sceneName, GameState transitState, GameState nextState,
        string subtitle1, string subtitle2)
    {
        state = transitState;
        inputReady = false;
        inputTimer = 0f;

        // Hide menu text
        if (titleText != null) titleText.gameObject.SetActive(false);
        if (promptText != null) promptText.gameObject.SetActive(false);

        // Fade to black
        yield return StartCoroutine(Fade(0f, 1f, fadeDuration));

        // Show first subtitle
        yield return new WaitForSeconds(delayBeforeSubtitle);

        if (!string.IsNullOrEmpty(subtitle1))
        {
            yield return StartCoroutine(ShowSubtitle(subtitle1));
        }

        // Load scene
        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
        while (!load.isDone)
            yield return null;

        // Brief pause on black
        yield return new WaitForSeconds(0.5f);

        // Show second subtitle
        if (!string.IsNullOrEmpty(subtitle2))
        {
            yield return StartCoroutine(ShowSubtitle(subtitle2));
        }

        yield return new WaitForSeconds(delayAfterSubtitle);

        // Fade in
        yield return StartCoroutine(Fade(1f, 0f, fadeDuration));

        state = nextState;
        inputReady = false;
        inputTimer = 0f;

        // If the new scene is a cutscene scene, the CutscenePlayer will
        // auto-start and Update() will detect when it finishes.
    }

    public void FinishIntroToStore()
    {
        state = GameState.Store;
        storeAutoAdvanceEnabled = false;
        inputReady = false;
        inputTimer = 0f;
    }

    IEnumerator ShowSubtitle(string text)
    {
        subtitleText.text = text;
        subtitleText.gameObject.SetActive(true);

        // Fade subtitle in
        yield return StartCoroutine(FadeText(subtitleText, 0f, 1f, subtitleFadeTime));

        // Hold
        yield return new WaitForSeconds(subtitleDisplayTime);

        // Fade subtitle out
        yield return StartCoroutine(FadeText(subtitleText, 1f, 0f, subtitleFadeTime));

        subtitleText.gameObject.SetActive(false);
    }

    // ============================
    // FADE HELPERS
    // ============================

    IEnumerator Fade(float from, float to, float duration)
    {
        fadePanel.gameObject.SetActive(true);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Smooth ease
            float smooth = t * t * (3f - 2f * t);
            float alpha = Mathf.Lerp(from, to, smooth);
            fadePanel.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        fadePanel.color = new Color(0, 0, 0, to);

        if (to <= 0f)
            fadePanel.gameObject.SetActive(false);
    }

    IEnumerator FadeText(TextMeshProUGUI tmp, float fromAlpha, float toAlpha, float duration)
    {
        float elapsed = 0f;
        Color c = tmp.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smooth = t * t * (3f - 2f * t);
            c.a = Mathf.Lerp(fromAlpha, toAlpha, smooth);
            tmp.color = c;
            yield return null;
        }

        c.a = toAlpha;
        tmp.color = c;
    }

    // ============================
    // MAIN MENU DISPLAY
    // ============================

    void ShowMainMenu()
    {
        SetupMainMenuTexts();
        fadePanel.color = new Color(0, 0, 0, 0);
        fadePanel.gameObject.SetActive(false);
        StartCoroutine(PulsePrompt());
    }

    void SetupMainMenuTexts()
    {
        titleText.text = "OPEN FEED";
        titleText.gameObject.SetActive(true);
        promptText.text = "click to begin";
        promptText.gameObject.SetActive(true);
    }

    IEnumerator PlayCreatorCreditThenMainMenu()
    {
        if (titleText != null) titleText.gameObject.SetActive(false);
        if (promptText != null) promptText.gameObject.SetActive(false);

        fadePanel.gameObject.SetActive(true);
        fadePanel.color = new Color(0, 0, 0, 1);

        creatorCreditText.gameObject.SetActive(true);
        Color c = creatorCreditText.color;
        c.a = 0f;
        creatorCreditText.color = c;

        yield return StartCoroutine(FadeText(creatorCreditText, 0f, 1f, 1.25f));
        yield return new WaitForSeconds(2.35f);
        yield return StartCoroutine(FadeText(creatorCreditText, 1f, 0f, 0.95f));
        creatorCreditText.gameObject.SetActive(false);

        SetupMainMenuTexts();
        yield return StartCoroutine(Fade(1f, 0f, fadeDuration));
        inputReady = false;
        inputTimer = 0f;
        StartCoroutine(PulsePrompt());
    }

    IEnumerator PulsePrompt()
    {
        while (state == GameState.MainMenu)
        {
            // Fade out
            yield return StartCoroutine(FadeText(promptText, 0.7f, 0.2f, 1.5f));
            // Fade in
            yield return StartCoroutine(FadeText(promptText, 0.2f, 0.7f, 1.5f));
        }
    }

    // ============================
    // SCENE LOADED CALLBACK
    // ============================

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Ensure UI canvas stays on top after scene load
        if (uiCanvas != null)
            uiCanvas.sortingOrder = 999;

        // Auto-attach CutscenePlayer to cutscene scenes
        if (scene.name == storeScene)
        {
            StartCoroutine(BeginStoreSceneFlow());
        }
        else if (scene.name == drivingScene)
        {
            AttachCutscenePlayer(CutscenePlayer.CutsceneType.Driving);
        }
    }

    void AttachCutscenePlayer(CutscenePlayer.CutsceneType type)
    {
        // Check if one already exists
        CutscenePlayer existing = FindAnyObjectByType<CutscenePlayer>();
        if (existing != null) return;

        GameObject cutsceneObj = new GameObject("CutscenePlayer");
        CutscenePlayer player = cutsceneObj.AddComponent<CutscenePlayer>();
        player.cutsceneType = type;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ============================
    // UI CREATION
    // ============================

    void CreateUI()
    {
        // Screen-space overlay canvas that persists across scenes
        GameObject canvasObj = new GameObject("GameFlowCanvas");
        canvasObj.transform.SetParent(transform);

        uiCanvas = canvasObj.AddComponent<Canvas>();
        uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        uiCanvas.sortingOrder = 999;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        // ---- Fade panel (fullscreen black) ----
        GameObject fadePanelObj = new GameObject("FadePanel");
        fadePanelObj.transform.SetParent(canvasObj.transform, false);

        fadePanel = fadePanelObj.AddComponent<Image>();
        fadePanel.color = new Color(0, 0, 0, 0);
        fadePanel.raycastTarget = false;

        RectTransform fprt = fadePanelObj.GetComponent<RectTransform>();
        fprt.anchorMin = Vector2.zero;
        fprt.anchorMax = Vector2.one;
        fprt.offsetMin = Vector2.zero;
        fprt.offsetMax = Vector2.zero;

        // ---- Title text ----
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(canvasObj.transform, false);

        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "OPEN FEED";
        titleText.fontSize = 72;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(0.7f, 0.75f, 0.7f, 0.9f);
        titleText.enableAutoSizing = false;

        RectTransform trt = titleObj.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.5f, 0.5f);
        trt.anchorMax = new Vector2(0.5f, 0.5f);
        trt.pivot = new Vector2(0.5f, 0.5f);
        trt.sizeDelta = new Vector2(800, 120);
        trt.anchoredPosition = new Vector2(0, 50);
        titleObj.SetActive(false);

        // ---- Prompt text ----
        GameObject promptObj = new GameObject("PromptText");
        promptObj.transform.SetParent(canvasObj.transform, false);

        promptText = promptObj.AddComponent<TextMeshProUGUI>();
        promptText.text = "click to begin";
        promptText.fontSize = 22;
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.color = new Color(0.5f, 0.55f, 0.5f, 0.7f);
        promptText.enableAutoSizing = false;

        RectTransform prt = promptObj.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(500, 50);
        prt.anchoredPosition = new Vector2(0, -40);
        promptObj.SetActive(false);

        // ---- Subtitle text ----
        GameObject subObj = new GameObject("SubtitleText");
        subObj.transform.SetParent(canvasObj.transform, false);

        subtitleText = subObj.AddComponent<TextMeshProUGUI>();
        subtitleText.text = "";
        subtitleText.fontSize = 28;
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.color = new Color(0.7f, 0.7f, 0.7f, 0f);
        subtitleText.enableAutoSizing = false;
        subtitleText.lineSpacing = 20f;

        RectTransform srt = subObj.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.5f, 0.5f);
        srt.anchorMax = new Vector2(0.5f, 0.5f);
        srt.pivot = new Vector2(0.5f, 0.5f);
        srt.sizeDelta = new Vector2(800, 200);
        srt.anchoredPosition = Vector2.zero;
        subObj.SetActive(false);

        GameObject creditObj = new GameObject("CreatorCreditText");
        creditObj.transform.SetParent(canvasObj.transform, false);

        creatorCreditText = creditObj.AddComponent<TextMeshProUGUI>();
        creatorCreditText.text = "created by martin.";
        creatorCreditText.fontSize = 30;
        creatorCreditText.alignment = TextAlignmentOptions.Center;
        creatorCreditText.color = new Color(1f, 1f, 1f, 0f);
        creatorCreditText.enableAutoSizing = false;

        RectTransform crt = creditObj.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.5f, 0.5f);
        crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.pivot = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(900, 80);
        crt.anchoredPosition = Vector2.zero;
        creditObj.SetActive(false);
    }
}
