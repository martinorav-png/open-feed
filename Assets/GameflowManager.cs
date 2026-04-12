using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System.Text;

public class GameFlowManager : MonoBehaviour
{
    // ============================
    // CONFIGURATION
    // ============================

    [Header("Scene Names (must match Build Settings)")]
    public string mainMenuScene = "MainMenu";
    public string storeScene = "supermarket";
    public string drivingScene = "ForestDrive";
    public string mainAreaScene = "MainArea";
    public string deskScene = "Desk";

    [Header("Supermarket → drive")]
    [Tooltip("After the supermarket intro hands off to gameplay, wait this long then fade to ForestDrive. Set 0 to go immediately.")]
    public float supermarketToForestDriveDelay = 1.25f;

    [Header("Timing")]
    public float fadeDuration = 1.5f;
    public float subtitleDisplayTime = 3.5f;
    public float subtitleFadeTime = 0.8f;
    public float delayBeforeSubtitle = 1f;
    public float delayAfterSubtitle = 1f;

    [Header("Store Flow")]
    public bool useStoreIntroWhenAvailable = true;
    public bool fallbackToStoreCutscene = false;

    [Header("Drive → MainArea (type \"home\")")]
    [Tooltip("Seconds to hold full black after MainArea finishes loading, before fading in.")]
    public float driveHomeBlackHoldSeconds = 0.25f;

    [Header("Main menu → store (black + typewriter exposition)")]
    [Tooltip("Full-screen black hold while subtitles type out before exterior gameplay.")]
    public float mainMenuStoreBlackSeconds = 20f;
    [Tooltip("During the black exposition, press Space to skip the rest and go straight to the parking lot.")]
    public bool mainMenuStoreExpositionSkippableWithSpace = true;
    [Tooltip("Seconds between each typed character (unscaled).")]
    public float mainMenuStoreTypeSecondsPerChar = 0.028f;
    [Tooltip("Play typing SFX every N characters (1 = every character).")]
    public int mainMenuStoreTypingSoundEveryNChars = 2;
    [Range(0f, 1f)] public float mainMenuStoreTypingSoundVolume = 0.22f;
    [Tooltip("Optional: assign a typing clip, or leave null to use Resources/Typing or a synthetic tick.")]
    public AudioClip mainMenuStoreTypingClip;
    [Tooltip("Optional loop (e.g. slowing engine) during the black beat.")]
    public AudioClip mainMenuStoreCarSlowdownLoop;
    [Tooltip("Optional one-shot when the car \"parks\" (e.g. brake bump).")]
    public AudioClip mainMenuStoreParkSfx;
    [Range(0f, 1f)] public float mainMenuStoreCarSfxVolume = 0.65f;
    [Tooltip("Unscaled seconds after black starts to play the park one-shot (0 = skip).")]
    public float mainMenuStoreParkSfxTime = 14f;
    [TextArea(2, 4)]
    public string[] mainMenuStoreExpositionLines = new[]
    {
        "The lot is almost empty.",
        "You took the long way tonight — same as always.",
        "Fluorescent hum through glass. Someone forgot a cart by the curb.",
        "Keys in your pocket. List half-memorized.",
        "Cold air when the doors slide. Then the smell of detergent and bread.",
        "You are not here for anything important.",
        "You are here because the quiet at home is worse than the noise in here.",
        "Walk in when you're ready.",
    };

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
        TransitionToMainArea,
        MainArea,
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
    private TextMeshProUGUI menuStoreExpositionText;

    private bool inputReady = false;
    private float inputCooldown = 0.5f;
    private float inputTimer = 0f;
    private bool storeAutoAdvanceEnabled = false;
    bool _driveHomeToMainAreaInProgress;
    bool _hasMainMenuUI;
    bool _suppressStoreIntroOnNextLoad;
    bool _menuStoreExpositionFlowActive;
    bool _menuStoreExpositionSkipRequested;
    AudioSource _menuStoreExpoAudio;
    AudioSource _menuStoreCarBed;
    AudioClip _menuStoreResolvedTypingClip;

    // Singleton
    public static GameFlowManager Instance;

    /// <summary>Creates a persistent flow manager if the menu scene has none (Build Settings index 0 is MainMenu).</summary>
    public static GameFlowManager EnsureExists()
    {
        if (Instance != null)
            return Instance;
        GameObject go = new GameObject("GameFlowManager");
        return go.AddComponent<GameFlowManager>();
    }

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

        _hasMainMenuUI = FindAnyObjectByType<MainMenuUI>() != null;

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
            // If a dedicated MainMenuUI handles its own display, skip GFM's overlay texts.
            // (TMP defaults to raycastTarget=true, which would block MainMenuUI buttons.)
            if (!_hasMainMenuUI)
                StartCoroutine(PlayCreatorCreditThenMainMenu());
        }
        else if (current == storeScene)
        {
            // Show main menu first so the player can click to begin.
            // StartGame() will detect we're already in the store and skip the scene load.
            state = GameState.MainMenu;
            // When MainMenuUI is present it owns the menu (CRT feed, buttons). Skip GFM's
            // centered "click to begin" overlay — same as the dedicated MainMenu scene.
            if (!_hasMainMenuUI)
                StartCoroutine(PlayCreatorCreditThenMainMenu());
        }
        else if (current == drivingScene)
        {
            state = GameState.Driving;
            AttachCutscenePlayer(CutscenePlayer.CutsceneType.Driving);
        }
        else if (current == mainAreaScene)
        {
            state = GameState.MainArea;
        }
        else if (current == deskScene)
        {
            state = GameState.Desk;
        }

        // MainMenuUI draws the real menu; keep GFM's fullscreen canvas from sitting on top (sort 999).
        if (_hasMainMenuUI && (current == mainMenuScene || current == storeScene))
        {
            if (fadePanel != null)
            {
                fadePanel.gameObject.SetActive(false);
                fadePanel.color = new Color(0f, 0f, 0f, 0f);
            }
            if (titleText != null) titleText.gameObject.SetActive(false);
            if (promptText != null) promptText.gameObject.SetActive(false);
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

        // Raw click/key handler: used when there is no MainMenuUI in the scene
        // (e.g. pressing Play directly from the supermarket scene in the editor).
        if (state == GameState.MainMenu && inputReady && !_hasMainMenuUI)
        {
            bool clicked = (mouse != null && mouse.leftButton.wasPressedThisFrame) ||
                           (keyboard != null && (keyboard.enterKey.wasPressedThisFrame ||
                                                 keyboard.spaceKey.wasPressedThisFrame));
            if (clicked)
                StartGame();
            return;
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

        // Driving → MainArea is started from CutscenePlayer when the player types "home"
        // (fade to black → load → fade in on this DontDestroyOnLoad manager).

        // MainMenu input is now handled by MainMenuUI buttons.
        // No raw click detection needed for MainMenu state.
        // MainArea / Desk: gameplay scenes — no auto cutscene transitions here.
    }

    IEnumerator BeginStoreSceneFlow()
    {
        // Let scene objects finish Awake/Start before intro lookup.
        yield return null;

        StoreFlowExteriorNightSetup.ApplyActiveStoreExteriorIfMatch();

        // When arriving from the main menu, TransitionToScene is still fading in.
        // Wait until the transition completes so the intro doesn't start while the
        // GameFlowManager overlay is covering the screen.
        while (state == GameState.TransitionToStore)
            yield return null;

        if (useStoreIntroWhenAvailable)
        {
            // New drive-in intro (highway approach → parking → free walk to entrance)
            SupermarketDriveInIntro driveInIntro = FindSupermarketDriveInIntroForHandoff();
            if (driveInIntro != null && !driveInIntro.gameObject.activeInHierarchy)
            {
                driveInIntro.gameObject.SetActive(true);
                yield return null;
            }

            if (driveInIntro != null && driveInIntro.BeginIntroSequence())
            {
                storeAutoAdvanceEnabled = false;
                if (titleText != null) titleText.gameObject.SetActive(false);
                if (promptText != null) promptText.gameObject.SetActive(false);
                yield break;
            }

            // Legacy cinematic walk-from-car intro
            StoreFlowIntroController storeFlowIntro = FindAnyObjectByType<StoreFlowIntroController>();
            if (storeFlowIntro != null && storeFlowIntro.BeginIntroSequence())
            {
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

        SixTwelveIntroController sixTwelveIntro = FindAnyObjectByType<SixTwelveIntroController>();
        if (sixTwelveIntro != null && sixTwelveIntro.BeginIntroSequence())
        {
            state = GameState.TransitionToStore;
            storeAutoAdvanceEnabled = false;
            if (titleText != null) titleText.gameObject.SetActive(false);
            if (promptText != null) promptText.gameObject.SetActive(false);
            return;
        }

        // Main menu → supermarket: black beat + typewriter, then exterior walk (no drive-in).
        StartStoreFlowFromMainMenuExposition();
    }

    /// <summary>
    /// Used by <see cref="MainMenuUI"/> after its local fade to black. Loads the store (if needed),
    /// holds ~<see cref="mainMenuStoreBlackSeconds"/> on black with exposition, then hands off to
    /// <see cref="SupermarketDriveInIntro.BeginExteriorGameplayOnly"/>.
    /// </summary>
    public void StartStoreFlowFromMainMenuExposition()
    {
        if (_menuStoreExpositionFlowActive)
            return;
        if (state != GameState.MainMenu)
            return;

        _menuStoreExpositionFlowActive = true;
        StartCoroutine(MenuStoreExpositionFlowRoutine());
    }

    IEnumerator MenuStoreExpositionFlowRoutine()
    {
        if (fadePanel == null)
            CreateUI();

        if (uiCanvas != null)
            uiCanvas.sortingOrder = 999;

        fadePanel.gameObject.SetActive(true);
        fadePanel.color = new Color(0f, 0f, 0f, 1f);

        if (titleText != null) titleText.gameObject.SetActive(false);
        if (promptText != null) promptText.gameObject.SetActive(false);
        if (subtitleText != null) subtitleText.gameObject.SetActive(false);

        if (menuStoreExpositionText != null)
        {
            menuStoreExpositionText.gameObject.SetActive(true);
            menuStoreExpositionText.text = string.Empty;
        }

        bool needsLoad = !string.Equals(
            SceneManager.GetActiveScene().name,
            storeScene,
            System.StringComparison.OrdinalIgnoreCase);

        if (needsLoad)
        {
            _suppressStoreIntroOnNextLoad = true;
            state = GameState.TransitionToStore;
            inputReady = false;
            inputTimer = 0f;

            AsyncOperation load = SceneManager.LoadSceneAsync(storeScene);
            while (!load.isDone)
                yield return null;

            yield return null;
        }
        else
            yield return null;

        // Fog/sky/ambient must not depend on SupermarketDriveInIntro.Awake (intro object may be inactive until exposition ends).
        StoreFlowExteriorNightSetup.ApplyActiveStoreExteriorIfMatch();

        _menuStoreExpositionSkipRequested = false;
        yield return MenuStoreExpositionHoldRoutine();

        SupermarketDriveInIntro driveIn = FindSupermarketDriveInIntroForHandoff();
        if (driveIn != null && !driveIn.gameObject.activeInHierarchy)
        {
            driveIn.gameObject.SetActive(true);
            yield return null;
        }

        if (driveIn == null)
        {
            GameObject host = new GameObject("SupermarketDriveInIntro_Runtime");
            driveIn = host.AddComponent<SupermarketDriveInIntro>();
            yield return null;
        }

        if (driveIn != null)
            driveIn.BeginExteriorGameplayOnly();
        else
            yield return BeginStoreSceneFlow();

        state = GameState.Store;
        inputReady = false;
        inputTimer = 0f;
        storeAutoAdvanceEnabled = false;

        yield return Fade(1f, 0f, fadeDuration);

        if (menuStoreExpositionText != null)
        {
            menuStoreExpositionText.gameObject.SetActive(false);
            menuStoreExpositionText.text = string.Empty;
        }

        StopMenuStoreCarBed();
        _menuStoreExpositionFlowActive = false;
    }

    /// <summary>
    /// <see cref="FindAnyObjectByType{T}"/> skips inactive objects; the drive-in intro is often on a disabled root.
    /// </summary>
    SupermarketDriveInIntro FindSupermarketDriveInIntroForHandoff()
    {
        SupermarketDriveInIntro[] found = FindObjectsByType<SupermarketDriveInIntro>(FindObjectsInactive.Include);
        if (found == null || found.Length == 0)
            return null;
        return found[0];
    }

    IEnumerator MenuStoreExpositionHoldRoutine()
    {
        float deadline = Time.unscaledTime + Mathf.Max(0.5f, mainMenuStoreBlackSeconds);
        float parkAt = Time.unscaledTime + Mathf.Max(0f, mainMenuStoreParkSfxTime);
        bool parkPlayed = mainMenuStoreParkSfx == null || mainMenuStoreParkSfxTime <= 0f;

        StartMenuStoreCarBed();

        string[] lines = mainMenuStoreExpositionLines;
        if (lines == null || lines.Length == 0)
            lines = new[] { "…" };

        for (int i = 0; i < lines.Length && Time.unscaledTime < deadline && !_menuStoreExpositionSkipRequested; i++)
        {
            PollMenuStoreExpositionSkip();
            while (!parkPlayed && Time.unscaledTime >= parkAt)
            {
                PlayMenuStoreOneShot(mainMenuStoreParkSfx, mainMenuStoreCarSfxVolume);
                parkPlayed = true;
            }

            yield return MenuStoreTypeLineRoutine(lines[i], deadline);
            if (Time.unscaledTime >= deadline || _menuStoreExpositionSkipRequested)
                break;

            float pause = 0.55f;
            float pauseEnd = Time.unscaledTime + pause;
            while (Time.unscaledTime < pauseEnd && Time.unscaledTime < deadline && !_menuStoreExpositionSkipRequested)
            {
                PollMenuStoreExpositionSkip();
                yield return null;
            }
        }

        while (!parkPlayed && Time.unscaledTime >= parkAt)
        {
            PlayMenuStoreOneShot(mainMenuStoreParkSfx, mainMenuStoreCarSfxVolume);
            parkPlayed = true;
        }

        while (Time.unscaledTime < deadline && !_menuStoreExpositionSkipRequested)
        {
            PollMenuStoreExpositionSkip();
            yield return null;
        }

        StopMenuStoreCarBed();
    }

    void PollMenuStoreExpositionSkip()
    {
        if (!mainMenuStoreExpositionSkippableWithSpace || _menuStoreExpositionSkipRequested)
            return;
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.spaceKey.wasPressedThisFrame)
            _menuStoreExpositionSkipRequested = true;
    }

    IEnumerator MenuStoreTypeLineRoutine(string line, float deadline)
    {
        if (menuStoreExpositionText == null || string.IsNullOrEmpty(line))
            yield break;

        var sb = new StringBuilder();
        float cps = Mathf.Max(0.005f, mainMenuStoreTypeSecondsPerChar);
        int everyN = Mathf.Max(1, mainMenuStoreTypingSoundEveryNChars);

        for (int i = 0; i < line.Length; i++)
        {
            PollMenuStoreExpositionSkip();
            if (_menuStoreExpositionSkipRequested || Time.unscaledTime >= deadline)
            {
                menuStoreExpositionText.text = line;
                yield break;
            }

            sb.Append(line[i]);
            menuStoreExpositionText.text = sb.ToString();

            if (i % everyN == 0)
                PlayMenuStoreTypingSound();

            float next = Time.unscaledTime + cps;
            while (Time.unscaledTime < next && Time.unscaledTime < deadline && !_menuStoreExpositionSkipRequested)
            {
                PollMenuStoreExpositionSkip();
                yield return null;
            }
        }
    }

    void StartMenuStoreCarBed()
    {
        StopMenuStoreCarBed();
        if (mainMenuStoreCarSlowdownLoop == null)
            return;

        _menuStoreCarBed = gameObject.AddComponent<AudioSource>();
        _menuStoreCarBed.playOnAwake = false;
        _menuStoreCarBed.loop = true;
        _menuStoreCarBed.spatialBlend = 0f;
        _menuStoreCarBed.clip = mainMenuStoreCarSlowdownLoop;
        _menuStoreCarBed.volume = mainMenuStoreCarSfxVolume;
        _menuStoreCarBed.Play();
    }

    void StopMenuStoreCarBed()
    {
        if (_menuStoreCarBed != null)
        {
            _menuStoreCarBed.Stop();
            Destroy(_menuStoreCarBed);
            _menuStoreCarBed = null;
        }
    }

    void PlayMenuStoreOneShot(AudioClip clip, float volume)
    {
        if (clip == null)
            return;
        EnsureMenuStoreExpoAudio();
        _menuStoreExpoAudio.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    void PlayMenuStoreTypingSound()
    {
        AudioClip clip = ResolveMenuStoreTypingClip();
        if (clip == null)
            return;
        EnsureMenuStoreExpoAudio();
        _menuStoreExpoAudio.PlayOneShot(clip, mainMenuStoreTypingSoundVolume);
    }

    void EnsureMenuStoreExpoAudio()
    {
        if (_menuStoreExpoAudio != null)
            return;
        _menuStoreExpoAudio = gameObject.AddComponent<AudioSource>();
        _menuStoreExpoAudio.playOnAwake = false;
        _menuStoreExpoAudio.spatialBlend = 0f;
    }

    AudioClip ResolveMenuStoreTypingClip()
    {
        if (mainMenuStoreTypingClip != null)
            return mainMenuStoreTypingClip;
        if (_menuStoreResolvedTypingClip != null)
            return _menuStoreResolvedTypingClip;
        _menuStoreResolvedTypingClip = Resources.Load<AudioClip>("Typing");
        if (_menuStoreResolvedTypingClip == null)
            _menuStoreResolvedTypingClip = CreateSyntheticTypingTickClip();
        return _menuStoreResolvedTypingClip;
    }

    static AudioClip CreateSyntheticTypingTickClip()
    {
        const int sampleRate = 44100;
        const float frequency = 720f;
        const float duration = 0.018f;
        int samples = Mathf.Max(1, (int)(sampleRate * duration));
        var clip = AudioClip.Create("MenuStoreTypingTick", samples, 1, sampleRate, false);
        var data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float env = Mathf.Exp(-i / (samples * 0.35f));
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * env * 0.35f;
        }

        clip.SetData(data, 0);
        return clip;
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

    /// <summary>
    /// Called when the player types "home" at the end of the forest drive. Runs on this object so the
    /// coroutine survives <see cref="SceneManager.LoadSceneAsync(string)"/> unloading the drive scene.
    /// </summary>
    public void BeginDriveHomeToMainArea()
    {
        if (_driveHomeToMainAreaInProgress)
            return;
        StartCoroutine(DriveHomeToMainAreaRoutine());
    }

    IEnumerator DriveHomeToMainAreaRoutine()
    {
        _driveHomeToMainAreaInProgress = true;
        state = GameState.TransitionToMainArea;
        inputReady = false;
        inputTimer = 0f;

        if (titleText != null) titleText.gameObject.SetActive(false);
        if (promptText != null) promptText.gameObject.SetActive(false);
        if (subtitleText != null) subtitleText.gameObject.SetActive(false);

        if (fadePanel == null)
            CreateUI();

        fadePanel.gameObject.SetActive(true);
        yield return Fade(0f, 1f, fadeDuration);

        AsyncOperation load = SceneManager.LoadSceneAsync(mainAreaScene);
        while (!load.isDone)
            yield return null;

        if (uiCanvas != null)
            uiCanvas.sortingOrder = 999;

        float hold = Mathf.Max(0f, driveHomeBlackHoldSeconds);
        if (hold > 0f)
            yield return new WaitForSeconds(hold);

        yield return Fade(1f, 0f, fadeDuration);

        state = GameState.MainArea;
        inputReady = false;
        inputTimer = 0f;
        _driveHomeToMainAreaInProgress = false;
    }

    public void FinishIntroToStore()
    {
        state = GameState.Store;
        storeAutoAdvanceEnabled = false;
        inputReady = false;
        inputTimer = 0f;

        // Linear release flow: supermarket intro → ForestDrive → MainArea (no store CutscenePlayer).
        if (IsActiveSceneSupermarket())
            StartCoroutine(SupermarketToForestDriveAfterIntro());
    }

    static bool IsActiveSceneSupermarket()
    {
        return string.Equals(
            SceneManager.GetActiveScene().name,
            "supermarket",
            System.StringComparison.OrdinalIgnoreCase);
    }

    IEnumerator SupermarketToForestDriveAfterIntro()
    {
        if (supermarketToForestDriveDelay > 0f)
            yield return new WaitForSeconds(supermarketToForestDriveDelay);

        yield return StartCoroutine(TransitionToScene(
            drivingScene,
            GameState.TransitionToDriving,
            GameState.Driving,
            "You get back in the car.",
            "The roads are empty tonight."));
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
            if (_suppressStoreIntroOnNextLoad)
            {
                _suppressStoreIntroOnNextLoad = false;
                return;
            }

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

        titleText.raycastTarget = false;

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

        promptText.raycastTarget = false;

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

        // ---- Main-menu → store exposition (drawn above fade panel) ----
        GameObject expoObj = new GameObject("MenuStoreExpositionText");
        expoObj.transform.SetParent(canvasObj.transform, false);

        menuStoreExpositionText = expoObj.AddComponent<TextMeshProUGUI>();
        menuStoreExpositionText.text = string.Empty;
        menuStoreExpositionText.fontSize = 26f;
        menuStoreExpositionText.alignment = TextAlignmentOptions.Center;
        menuStoreExpositionText.color = new Color(0.88f, 0.86f, 0.8f, 1f);
        menuStoreExpositionText.enableAutoSizing = false;
        menuStoreExpositionText.lineSpacing = 8f;
        menuStoreExpositionText.textWrappingMode = TextWrappingModes.Normal;
        menuStoreExpositionText.raycastTarget = false;

        RectTransform ert = expoObj.GetComponent<RectTransform>();
        ert.anchorMin = new Vector2(0.5f, 0.5f);
        ert.anchorMax = new Vector2(0.5f, 0.5f);
        ert.pivot = new Vector2(0.5f, 0.5f);
        ert.sizeDelta = new Vector2(920f, 420f);
        ert.anchoredPosition = Vector2.zero;
        expoObj.SetActive(false);
    }
}
