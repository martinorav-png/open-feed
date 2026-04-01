using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Desk / MainArea: on play, shows typewriter subtitles with typing SFX; hides when the player starts monitor zoom.
/// Spawns automatically when <see cref="MonitorInteraction"/> exists in the loaded scene.
/// Optional clip: <c>Resources/Typing</c> (e.g. Assets/Audio/Resources/Typing.wav). If missing, uses a short synthetic tick.
/// </summary>
[DefaultExecutionOrder(-150)]
public class DriveIntroSubtitles : MonoBehaviour
{
    static readonly string[] Lines =
    {
        "the road back is always faster than the road out",
        "you know this stretch",
        "you've driven it enough times",
        "that your hands know the turns before you do",
        "there was a time when this drive meant something",
        "coming home to someone",
        "that's not what it is now",
    };

    const string ResourcesTypingClipName = "Typing";

    [SerializeField] float delayBeforeTyping = 0.35f;
    [SerializeField] float overlayFadeInDuration = 0.45f;
    [SerializeField] float overlayFadeOutDuration = 0.28f;
    [SerializeField] float secondsPerCharacter = 0.032f;
    [SerializeField] float pauseAfterSentenceDisplayed = 0.7f;
    [SerializeField] float pauseAfterClearBeforeNext = 0.12f;
    [SerializeField] int playTypingSoundEveryNChars = 2;
    [SerializeField] float typingSoundVolume = 0.22f;
    [SerializeField] int canvasSortOrder = 32;

    CanvasGroup _canvasGroup;
    TextMeshProUGUI _bodyText;
    AudioSource _audio;
    AudioClip _typingClip;
    Coroutine _sequenceRoutine;
    bool _dismissed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreateIfDeskScene()
    {
        if (FindAnyObjectByType<DriveIntroSubtitles>() != null)
            return;
        if (FindAnyObjectByType<MonitorInteraction>() == null)
            return;

        var host = new GameObject("DriveIntroSubtitles");
        host.AddComponent<DriveIntroSubtitles>();
    }

    void Awake()
    {
        BuildUi();
        _typingClip = Resources.Load<AudioClip>(ResourcesTypingClipName);
        if (_typingClip == null)
            _typingClip = CreateSyntheticKeystrokeClip();

        _audio = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake = false;
        _audio.spatialBlend = 0f;
        _audio.loop = false;
    }

    void OnEnable()
    {
        MonitorInteraction.MonitorZoomStarted += OnMonitorZoomStarted;
    }

    void OnDisable()
    {
        MonitorInteraction.MonitorZoomStarted -= OnMonitorZoomStarted;
    }

    void Start()
    {
        _canvasGroup.alpha = 0f;
        _bodyText.text = string.Empty;
        _sequenceRoutine = StartCoroutine(RunSequence());
    }

    void OnMonitorZoomStarted()
    {
        if (_dismissed)
            return;
        _dismissed = true;
        if (_sequenceRoutine != null)
        {
            StopCoroutine(_sequenceRoutine);
            _sequenceRoutine = null;
        }

        _audio.Stop();
        StartCoroutine(FadeOutAndDestroy());
    }

    IEnumerator RunSequence()
    {
        yield return new WaitForSecondsRealtime(delayBeforeTyping);

        float t = 0f;
        while (t < overlayFadeInDuration)
        {
            if (_dismissed)
                yield break;
            t += Time.unscaledDeltaTime;
            _canvasGroup.alpha = Mathf.Clamp01(t / overlayFadeInDuration);
            yield return null;
        }

        _canvasGroup.alpha = 1f;

        var sb = new StringBuilder(128);

        for (int lineIdx = 0; lineIdx < Lines.Length; lineIdx++)
        {
            string line = Lines[lineIdx];
            sb.Clear();
            _bodyText.text = string.Empty;
            int tickCounter = 0;

            for (int i = 0; i < line.Length; i++)
            {
                if (_dismissed)
                    yield break;

                sb.Append(line[i]);
                _bodyText.text = sb.ToString();

                char c = line[i];
                if ((char.IsLetterOrDigit(c) || c == '\'') && (++tickCounter % playTypingSoundEveryNChars == 0))
                    PlayTypingTick();

                yield return new WaitForSecondsRealtime(secondsPerCharacter);
            }

            float readWait = 0f;
            while (readWait < pauseAfterSentenceDisplayed)
            {
                if (_dismissed)
                    yield break;
                readWait += Time.unscaledDeltaTime;
                yield return null;
            }

            if (lineIdx < Lines.Length - 1)
            {
                _bodyText.text = string.Empty;
                float gap = 0f;
                while (gap < pauseAfterClearBeforeNext)
                {
                    if (_dismissed)
                        yield break;
                    gap += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
        }

        if (!_dismissed)
        {
            _audio.Stop();
            yield return FadeCanvasAlpha(_canvasGroup.alpha, overlayFadeOutDuration);
        }

        _sequenceRoutine = null;
        if (!_dismissed)
            Destroy(gameObject);
    }

    void PlayTypingTick()
    {
        if (_audio == null || _typingClip == null)
            return;
        _audio.pitch = Random.Range(0.94f, 1.06f);
        _audio.PlayOneShot(_typingClip, typingSoundVolume);
    }

    IEnumerator FadeOutAndDestroy()
    {
        yield return FadeCanvasAlpha(_canvasGroup.alpha, overlayFadeOutDuration);
        Destroy(gameObject);
    }

    IEnumerator FadeCanvasAlpha(float startAlpha, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t / duration);
            yield return null;
        }

        _canvasGroup.alpha = 0f;
    }

    void BuildUi()
    {
        var canvasGo = new GameObject("DriveIntroSubtitlesCanvas");
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = canvasSortOrder;

        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.AddComponent<GraphicRaycaster>();

        _canvasGroup = canvasGo.AddComponent<CanvasGroup>();
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;

        var panelGo = new GameObject("Panel");
        panelGo.transform.SetParent(canvasGo.transform, false);
        var panelRt = panelGo.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0f);
        panelRt.anchorMax = new Vector2(0.5f, 0f);
        panelRt.pivot = new Vector2(0.5f, 0f);
        panelRt.anchoredPosition = new Vector2(0f, 32f);
        panelRt.sizeDelta = new Vector2(520f, 88f);

        var panelImg = panelGo.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.42f);
        panelImg.raycastTarget = false;

        var textGo = new GameObject("Body");
        textGo.transform.SetParent(panelGo.transform, false);
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(18f, 12f);
        textRt.offsetMax = new Vector2(-18f, -12f);

        _bodyText = textGo.AddComponent<TextMeshProUGUI>();
        _bodyText.text = string.Empty;
        _bodyText.fontSize = 20f;
        _bodyText.alignment = TextAlignmentOptions.MidlineLeft;
        _bodyText.color = new Color(0.92f, 0.9f, 0.84f, 1f);
        _bodyText.lineSpacing = 4f;
        _bodyText.textWrappingMode = TextWrappingModes.Normal;
        _bodyText.raycastTarget = false;
    }

    static AudioClip CreateSyntheticKeystrokeClip()
    {
        int sampleRate = 44100;
        float frequency = 720f;
        float duration = 0.018f;
        int samples = Mathf.Max(1, (int)(sampleRate * duration));
        var clip = AudioClip.Create("DriveIntroTypingTick", samples, 1, sampleRate, false);
        var data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float env = Mathf.Exp(-i / (samples * 0.35f));
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * env * 0.35f;
        }

        clip.SetData(data, 0);
        return clip;
    }
}
