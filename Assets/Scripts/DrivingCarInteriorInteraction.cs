using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Driving scene: crosshair + radio; after a delay, bottom-right prompt — type "home" to end the drive.
/// </summary>
[DisallowMultipleComponent]
public class DrivingCarInteriorInteraction : MonoBehaviour
{
    const string RadioClipResourcePath = "radiothing";
    const string DriveHomeWord = "home";

    static readonly Color CrosshairIdle = new Color(0.65f, 0.65f, 0.68f, 0.92f);
    static readonly Color CrosshairOnRadio = new Color(0.2f, 0.85f, 0.35f, 0.95f);

    // ---- Radio subtitle entry (set from CutscenePlayer Inspector) ----
    [System.Serializable]
    public struct RadioSubtitleEntry
    {
        [Tooltip("Seconds from when the radio clip starts playing.")]
        public float time;
        [TextArea(1, 3)]
        public string text;
        [Tooltip("Seconds the subtitle is held fully visible (excluding fade in/out).")]
        public float displayDuration;
    }

    [SerializeField] float raycastDistance = 4.5f;
    [SerializeField] float crosshairSize = 5f;

    /// <summary>Timed subtitles that float from the AlpineRadio when it plays. Assigned by CutscenePlayer.</summary>
    public RadioSubtitleEntry[] radioSubtitles;

    // ---- Core references ----
    Transform _radioRoot;
    Canvas _canvas;
    Image _dot;
    Camera _cam;
    AudioClip _radioClip;
    AudioSource _radioAudioSource;

    // ---- Drive-home prompt ----
    Canvas _driveHomePromptCanvas;
    TextMeshProUGUI _driveHomePromptText;
    Coroutine _promptFadeCoroutine;
    bool _driveHomePromptVisible;
    bool _driveHomeChosen;
    readonly StringBuilder _driveHomeTypeBuffer = new StringBuilder(48);

    // ---- Radio subtitle (world-space 3-D text) ----
    TextMeshPro _radioSubTMP;
    float _radioElapsed;
    bool _radioTimerActive;

    // ---- Session state ----
    bool _sessionActive;
    bool _radioStartedThisSession;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        if (_cam == null)
            _cam = Camera.main;
    }

    public void BeginDrivingSession(Transform alpineRadioRoot)
    {
        _radioRoot = alpineRadioRoot;
        EnsureRadioCollider();
        EnsureRadioPositionalAudio();
        EnsureCrosshairUI();
        if (_canvas != null)
            _canvas.enabled = true;
        _sessionActive = true;
        _radioStartedThisSession = false;
        _driveHomePromptVisible = false;
        _driveHomeChosen = false;
        _driveHomeTypeBuffer.Length = 0;
        HideDriveHomeScreenPrompt();
    }

    // ============================
    // DRIVE-HOME PROMPT (fade in)
    // ============================

    public void ShowDriveHomeTypablePrompt()
    {
        if (_driveHomePromptVisible)
            return;

        EnsureDriveHomeScreenPrompt();
        _driveHomeTypeBuffer.Length = 0;
        _driveHomePromptVisible = true;

        if (_driveHomePromptCanvas != null)
        {
            _driveHomePromptCanvas.enabled = true;

            // Reset to invisible before fading in
            if (_driveHomePromptText != null)
            {
                Color c = _driveHomePromptText.color;
                c.a = 0f;
                _driveHomePromptText.color = c;
            }

            if (_promptFadeCoroutine != null)
                StopCoroutine(_promptFadeCoroutine);
            _promptFadeCoroutine = StartCoroutine(FadeInDriveHomePrompt());
        }
    }

    IEnumerator FadeInDriveHomePrompt()
    {
        const float duration = 1.5f;
        const float targetAlpha = 0.92f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smooth = t * t * (3f - 2f * t);
            if (_driveHomePromptText != null)
            {
                Color c = _driveHomePromptText.color;
                c.a = Mathf.Lerp(0f, targetAlpha, smooth);
                _driveHomePromptText.color = c;
            }
            yield return null;
        }

        if (_driveHomePromptText != null)
        {
            Color c = _driveHomePromptText.color;
            c.a = targetAlpha;
            _driveHomePromptText.color = c;
        }
        _promptFadeCoroutine = null;
    }

    public bool HasChosenDriveHome() => _driveHomeChosen;

    public void EndDrivingSession()
    {
        _sessionActive = false;
        _driveHomeChosen = false;
        _driveHomePromptVisible = false;
        _radioTimerActive = false;

        if (_promptFadeCoroutine != null)
        {
            StopCoroutine(_promptFadeCoroutine);
            _promptFadeCoroutine = null;
        }

        HideDriveHomeScreenPrompt();
        if (_canvas != null)
            _canvas.enabled = false;
        if (_radioSubTMP != null)
            _radioSubTMP.gameObject.SetActive(false);
        _radioStartedThisSession = false;
    }

    // ============================
    // RADIO COLLIDER / AUDIO
    // ============================

    void EnsureRadioCollider()
    {
        if (_radioRoot == null)
            return;

        if (_radioRoot.GetComponentInChildren<Collider>(true) != null)
            return;

        var box = _radioRoot.gameObject.AddComponent<BoxCollider>();
        box.isTrigger = false;
        Renderer[] rends = _radioRoot.GetComponentsInChildren<Renderer>(true);
        if (rends.Length == 0)
        {
            box.center = Vector3.zero;
            box.size = new Vector3(0.16f, 0.07f, 0.12f);
            return;
        }

        Bounds world = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++)
            world.Encapsulate(rends[i].bounds);

        box.center = _radioRoot.InverseTransformPoint(world.center);
        Vector3 lossy = _radioRoot.lossyScale;
        float avs = (lossy.x + lossy.y + lossy.z) / 3f;
        avs = Mathf.Max(0.001f, avs);
        Vector3 sz = world.size / avs * 1.08f;
        box.size = Vector3.Max(sz, new Vector3(0.08f, 0.04f, 0.06f));
    }

    void EnsureRadioPositionalAudio()
    {
        if (_radioRoot == null)
        {
            _radioAudioSource = null;
            return;
        }

        Transform audioTr = _radioRoot.Find("RadioAudio");
        AudioSource src = audioTr != null ? audioTr.GetComponent<AudioSource>() : null;
        if (src == null)
        {
            var go = new GameObject("RadioAudio");
            go.transform.SetParent(_radioRoot, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            src = go.AddComponent<AudioSource>();
        }

        ConfigurePositionalRadioSource(src);
        _radioAudioSource = src;
    }

    static void ConfigurePositionalRadioSource(AudioSource a)
    {
        if (a == null) return;
        a.playOnAwake = false;
        a.loop = false;
        a.spatialBlend = 1f;
        a.minDistance = 0.4f;
        a.maxDistance = 5.5f;
        a.rolloffMode = AudioRolloffMode.Logarithmic;
        a.dopplerLevel = 0f;
        a.spread = 30f;
    }

    // ============================
    // CROSSHAIR UI
    // ============================

    void EnsureCrosshairUI()
    {
        if (_dot != null)
            return;

        GameObject canvasGo = new GameObject("DrivingCrosshairCanvas");
        canvasGo.transform.SetParent(transform, false);
        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 2100;

        GameObject dotGo = new GameObject("CrosshairDot");
        dotGo.transform.SetParent(canvasGo.transform, false);
        _dot = dotGo.AddComponent<Image>();
        _dot.sprite = CreateWhiteSprite();
        _dot.color = CrosshairIdle;
        _dot.raycastTarget = false;

        RectTransform rt = _dot.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(crosshairSize, crosshairSize);
        rt.anchoredPosition = Vector2.zero;

        _canvas.enabled = false;
    }

    // ============================
    // DRIVE-HOME SCREEN PROMPT UI
    // ============================

    void EnsureDriveHomeScreenPrompt()
    {
        if (_driveHomePromptCanvas != null)
            return;

        GameObject go = new GameObject("DriveHomePromptCanvas");
        go.transform.SetParent(transform, false);
        _driveHomePromptCanvas = go.AddComponent<Canvas>();
        _driveHomePromptCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _driveHomePromptCanvas.sortingOrder = 2080;

        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        GameObject textGo = new GameObject("PromptText");
        textGo.transform.SetParent(go.transform, false);
        _driveHomePromptText = textGo.AddComponent<TextMeshProUGUI>();
        _driveHomePromptText.text = "Type \"home\" to drive home";
        _driveHomePromptText.fontSize = 28f;
        _driveHomePromptText.alignment = TextAlignmentOptions.BottomRight;
        _driveHomePromptText.color = new Color(0.82f, 0.84f, 0.78f, 0f); // start transparent
        if (TMP_Settings.defaultFontAsset != null)
            _driveHomePromptText.font = TMP_Settings.defaultFontAsset;
        _driveHomePromptText.raycastTarget = false;

        RectTransform rt = textGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.sizeDelta = new Vector2(520, 110);
        rt.anchoredPosition = new Vector2(-36f, 52f);

        _driveHomePromptCanvas.enabled = false;
    }

    void HideDriveHomeScreenPrompt()
    {
        _driveHomePromptVisible = false;
        if (_driveHomePromptCanvas != null)
            _driveHomePromptCanvas.enabled = false;
        _driveHomeTypeBuffer.Length = 0;
    }

    // ============================
    // RADIO SUBTITLE OVERLAY
    // ============================

    void EnsureRadioSubtitleUI()
    {
        if (_radioSubTMP != null)
            return;

        // World-space 3-D text that lives in the scene (no canvas needed).
        var go = new GameObject("RadioSubtitleText");
        // No parent — positioned dynamically in world space each frame.

        _radioSubTMP = go.AddComponent<TextMeshPro>();
        // Font size is in world units. At ~0.5 m viewing distance 0.045 ≈ readable.
        // Tweak this value if text is too small or large in-game.
        _radioSubTMP.fontSize = 0.26f;
        _radioSubTMP.alignment = TextAlignmentOptions.Center;
        _radioSubTMP.textWrappingMode = TextWrappingModes.Normal;
        _radioSubTMP.overflowMode = TextOverflowModes.Overflow;
        // Warm amber matching the radio's orange glow; starts fully transparent.
        _radioSubTMP.color = new Color(1f, 0.82f, 0.55f, 0f);
        if (TMP_Settings.defaultFontAsset != null)
            _radioSubTMP.font = TMP_Settings.defaultFontAsset;

        // Width/height of the text box in world units (~40 cm × 15 cm).
        _radioSubTMP.rectTransform.sizeDelta = new Vector2(1.2f, 0.6f);
        _radioSubTMP.rectTransform.pivot = new Vector2(0.5f, 0f); // bottom-centre pivot

        go.SetActive(false);
    }

    public void BeginRadioSubtitles()
    {
        if (radioSubtitles == null || radioSubtitles.Length == 0)
            return;

        EnsureRadioSubtitleUI();
        _radioElapsed = 0f;
        _radioTimerActive = true;
        StartCoroutine(TrackRadioTime());
        StartCoroutine(RunRadioSubtitles());
    }

    IEnumerator TrackRadioTime()
    {
        while (_radioTimerActive)
        {
            _radioElapsed += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator RunRadioSubtitles()
    {
        for (int i = 0; i < radioSubtitles.Length; i++)
        {
            RadioSubtitleEntry entry = radioSubtitles[i];

            // Wait until this subtitle's trigger time (timer runs in parallel)
            yield return new WaitUntil(() => _radioElapsed >= entry.time || !_radioTimerActive);

            if (!_radioTimerActive)
                yield break;

            yield return StartCoroutine(ShowFloatingRadioSubtitle(entry.text, entry.displayDuration));
        }

        _radioTimerActive = false;
    }

    IEnumerator ShowFloatingRadioSubtitle(string text, float holdDuration)
    {
        if (_radioSubTMP == null || _radioRoot == null || _cam == null)
            yield break;

        _radioSubTMP.text = text;
        _radioSubTMP.gameObject.SetActive(true);

        // Start just above the radio in world space.
        Vector3 startPos = _radioRoot.position + Vector3.up * 0.06f;

        // Total world-unit drift upward over the full lifetime of this subtitle.
        const float floatDist = 0.09f;
        const float fadeDur   = 0.5f;

        Color col = _radioSubTMP.color;

        // — Fade in, floating up 30 % of the drift —
        float elapsed = 0f;
        while (elapsed < fadeDur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fadeDur));

            Vector3 pos = startPos + Vector3.up * (floatDist * 0.3f * t);
            _radioSubTMP.transform.position = pos;
            FaceCamera(pos);

            col.a = t;
            _radioSubTMP.color = col;
            yield return null;
        }

        Vector3 holdPos = _radioSubTMP.transform.position;
        col.a = 1f;
        _radioSubTMP.color = col;

        // — Hold, keeping the billboard facing the camera —
        float holdElapsed = 0f;
        while (holdElapsed < holdDuration)
        {
            holdElapsed += Time.deltaTime;
            _radioSubTMP.transform.position = holdPos;
            FaceCamera(holdPos);
            yield return null;
        }

        // — Fade out, floating up the remaining 70 % —
        elapsed = 0f;
        while (elapsed < fadeDur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fadeDur));

            Vector3 pos = holdPos + Vector3.up * (floatDist * 0.7f * t);
            _radioSubTMP.transform.position = pos;
            FaceCamera(pos);

            col.a = 1f - t;
            _radioSubTMP.color = col;
            yield return null;
        }

        col.a = 0f;
        _radioSubTMP.color = col;
        _radioSubTMP.gameObject.SetActive(false);
    }

    void FaceCamera(Vector3 fromPos)
    {
        if (_cam == null) return;
        // Negate the direction so the text's readable face points toward the camera.
        Vector3 dir = fromPos - _cam.transform.position;
        if (dir.sqrMagnitude > 0.0001f)
            _radioSubTMP.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }

    // ============================
    // INPUT POLLING
    // ============================

    void PollDriveHomeTyping()
    {
        if (!_driveHomePromptVisible || _driveHomeChosen)
            return;

        Keyboard k = Keyboard.current;
        if (k == null)
            return;

        if (k.backspaceKey.wasPressedThisFrame && _driveHomeTypeBuffer.Length > 0)
            _driveHomeTypeBuffer.Length -= 1;

        void TryChar(UnityEngine.InputSystem.Controls.KeyControl ctrl, char lo, char hi)
        {
            if (ctrl == null || !ctrl.wasPressedThisFrame)
                return;
            _driveHomeTypeBuffer.Append(k.shiftKey.isPressed ? hi : lo);
        }

        TryChar(k.hKey, 'h', 'H');
        TryChar(k.oKey, 'o', 'O');
        TryChar(k.mKey, 'm', 'M');
        TryChar(k.eKey, 'e', 'E');

        while (_driveHomeTypeBuffer.Length > 64)
            _driveHomeTypeBuffer.Remove(0, _driveHomeTypeBuffer.Length - 32);

        string typed = _driveHomeTypeBuffer.ToString();
        if (typed.Length >= DriveHomeWord.Length)
        {
            string end = typed.Substring(typed.Length - DriveHomeWord.Length);
            if (string.Equals(end, DriveHomeWord, System.StringComparison.OrdinalIgnoreCase))
                _driveHomeChosen = true;
        }
    }

    // ============================
    // LATE UPDATE
    // ============================

    void LateUpdate()
    {
        if (!_sessionActive || _cam == null || _dot == null)
            return;

        bool cutsceneActive = CutscenePlayer.Instance != null &&
                               CutscenePlayer.Instance.IsDrivingCutsceneActive;

        if (cutsceneActive && _driveHomePromptVisible && !_driveHomeChosen)
            PollDriveHomeTyping();

        if (!cutsceneActive)
        {
            _dot.color = CrosshairIdle;
            return;
        }

        Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        bool onRadio = false;
        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
        {
            Transform h = hit.transform;
            if (_radioRoot != null && h != null && (h == _radioRoot || h.IsChildOf(_radioRoot)))
                onRadio = true;
        }

        _dot.color = onRadio ? CrosshairOnRadio : CrosshairIdle;

        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame && onRadio)
            PlayRadioClip();
    }

    // ============================
    // RADIO PLAYBACK
    // ============================

    void PlayRadioClip()
    {
        if (_radioStartedThisSession || (_radioAudioSource != null && _radioAudioSource.isPlaying))
            return;

        if (_radioClip == null)
            _radioClip = Resources.Load<AudioClip>(RadioClipResourcePath);
        if (_radioClip == null)
        {
            Debug.LogWarning("OPENFEED: Missing AudioClip at Resources/" + RadioClipResourcePath + " (expected Assets/Audio/Resources/radiothing.mp3).");
            return;
        }

        if (_radioAudioSource == null)
            EnsureRadioPositionalAudio();
        if (_radioAudioSource == null)
            return;

        _radioAudioSource.clip = _radioClip;
        _radioAudioSource.loop = false;
        _radioAudioSource.Play();
        _radioStartedThisSession = true;

        // Kick off floating subtitles if any are configured
        if (radioSubtitles != null && radioSubtitles.Length > 0)
            BeginRadioSubtitles();
    }

    // ============================
    // UTILITY
    // ============================

    static Sprite CreateWhiteSprite()
    {
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
    }
}
