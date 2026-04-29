using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SupermarketSubtitleOverlay : MonoBehaviour
{
    static SupermarketSubtitleOverlay _instance;
    public static SupermarketSubtitleOverlay Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("SupermarketSubtitleOverlay");
                _instance = go.AddComponent<SupermarketSubtitleOverlay>();
            }
            return _instance;
        }
    }

    public static void DestroyIfExists()
    {
        if (_instance != null)
        {
            Destroy(_instance.gameObject);
            _instance = null;
        }
    }

    void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    Canvas _canvas;
    CanvasGroup _group;
    TextMeshProUGUI _tmp;
    Image _fadeImage;
    CanvasGroup _fadeGroup;

    void Awake()
    {
        EnsureUI();
    }

    void EnsureUI()
    {
        if (_canvas != null) return;
        var go = new GameObject("SupermarketSubtitleCanvas");
        go.transform.SetParent(transform, false);
        _canvas = go.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 800;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();

        // fade-to-black image
        var fade = new GameObject("FadeImage");
        fade.transform.SetParent(go.transform, false);
        _fadeImage = fade.AddComponent<Image>();
        _fadeImage.color = Color.black;
        var frt = _fadeImage.rectTransform;
        frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
        frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;
        _fadeGroup = fade.AddComponent<CanvasGroup>();
        _fadeGroup.alpha = 0f;
        _fadeGroup.blocksRaycasts = false;

        // subtitle text
        var t = new GameObject("Subtitle");
        t.transform.SetParent(go.transform, false);
        _tmp = t.AddComponent<TextMeshProUGUI>();
        _tmp.alignment = TextAlignmentOptions.Center;
        _tmp.fontSize = 36f;
        _tmp.color = new Color(0.33f, 0.53f, 0.93f, 1f); // player blue, like the dialogue color
        _tmp.text = "";
        if (TMP_Settings.defaultFontAsset != null) _tmp.font = TMP_Settings.defaultFontAsset;
        var rt = _tmp.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(1400f, 160f);
        rt.anchoredPosition = new Vector2(0f, 110f);
        _group = t.AddComponent<CanvasGroup>();
        _group.alpha = 0f;
    }

    public Coroutine ShowLine(string text, float hold = 2f, float fadeDur = 0.35f, Color? color = null)
    {
        EnsureUI();
        return StartCoroutine(RunLine(text, hold, fadeDur, color ?? Color.white));
    }

    // IEnumerator-returning variants so callers can `yield return` them inside their own coroutines.
    public IEnumerator RunLineCo(string text, float hold = 2f, float fadeDur = 0.35f, Color? color = null)
    {
        EnsureUI();
        yield return RunLine(text, hold, fadeDur, color ?? Color.white);
    }
    public IEnumerator FadeToBlackCo(float dur)
    {
        EnsureUI();
        yield return RunFade(0f, 1f, dur);
    }
    public IEnumerator FadeFromBlackCo(float dur)
    {
        EnsureUI();
        yield return RunFade(1f, 0f, dur);
    }
    public IEnumerator ShowLineOverBlackCo(string text, float hold)
    {
        EnsureUI();
        yield return ShowLineOverBlack(text, hold);
    }

    IEnumerator RunLine(string text, float hold, float fadeDur, Color color)
    {
        _tmp.text = text;
        _tmp.color = color;
        float t = 0f;
        while (t < fadeDur) { t += Time.deltaTime; _group.alpha = Mathf.Clamp01(t / fadeDur); yield return null; }
        _group.alpha = 1f;
        float h = 0f;
        while (h < hold) { h += Time.deltaTime; yield return null; }
        t = 0f;
        while (t < fadeDur) { t += Time.deltaTime; _group.alpha = 1f - Mathf.Clamp01(t / fadeDur); yield return null; }
        _group.alpha = 0f;
        _tmp.text = "";
    }

    public Coroutine FadeToBlack(float dur)
    {
        EnsureUI();
        return StartCoroutine(RunFade(0f, 1f, dur));
    }
    public Coroutine FadeFromBlack(float dur)
    {
        EnsureUI();
        return StartCoroutine(RunFade(1f, 0f, dur));
    }

    IEnumerator RunFade(float from, float to, float dur)
    {
        float t = 0f;
        _fadeGroup.alpha = from;
        while (t < dur) { t += Time.deltaTime; _fadeGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / dur)); yield return null; }
        _fadeGroup.alpha = to;
    }

    public IEnumerator ShowLineOverBlack(string text, float hold)
    {
        EnsureUI();
        _tmp.text = text;
        _tmp.color = Color.white;
        float fade = 0.5f;
        float t = 0f;
        while (t < fade) { t += Time.deltaTime; _group.alpha = Mathf.Clamp01(t / fade); yield return null; }
        _group.alpha = 1f;
        yield return new WaitForSeconds(hold);
        t = 0f;
        while (t < fade) { t += Time.deltaTime; _group.alpha = 1f - Mathf.Clamp01(t / fade); yield return null; }
        _group.alpha = 0f;
        _tmp.text = "";
    }
}
