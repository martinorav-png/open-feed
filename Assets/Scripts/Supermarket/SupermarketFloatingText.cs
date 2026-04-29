using System.Collections;
using TMPro;
using UnityEngine;

public class SupermarketFloatingText : MonoBehaviour
{
    static SupermarketFloatingText _instance;
    public static SupermarketFloatingText Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("SupermarketFloatingText");
                _instance = go.AddComponent<SupermarketFloatingText>();
            }
            return _instance;
        }
    }

    TextMeshPro _tmp;
    Camera _cam;

    void EnsureTMP()
    {
        if (_tmp != null) return;
        var go = new GameObject("SupermarketFloatingText_TMP");
        go.transform.SetParent(transform, false);
        _tmp = go.AddComponent<TextMeshPro>();
        _tmp.fontSize = 0.4f;
        _tmp.alignment = TextAlignmentOptions.Center;
        _tmp.textWrappingMode = TextWrappingModes.Normal;
        _tmp.color = new Color(1f, 0.92f, 0.78f, 0f);
        if (TMP_Settings.defaultFontAsset != null) _tmp.font = TMP_Settings.defaultFontAsset;
        _tmp.rectTransform.sizeDelta = new Vector2(2.4f, 0.8f);
        _tmp.rectTransform.pivot = new Vector2(0.5f, 0f);
        go.SetActive(false);
    }

    public Coroutine Show(Transform anchor, Vector3 worldOffset, string text, float hold = 2f, float fadeDur = 0.45f, float floatDist = 0.18f, float? fontSize = null, Color? color = null)
    {
        EnsureTMP();
        return StartCoroutine(Run(anchor, worldOffset, text, hold, fadeDur, floatDist, fontSize, color));
    }

    public Coroutine ShowAtPosition(Vector3 worldPos, string text, float hold = 2f, float fadeDur = 0.45f, float floatDist = 0.18f, float? fontSize = null, Color? color = null)
    {
        EnsureTMP();
        return StartCoroutine(RunStatic(worldPos, text, hold, fadeDur, floatDist, fontSize, color));
    }

    Camera ResolveCam()
    {
        if (_cam == null || !_cam.isActiveAndEnabled) _cam = Camera.main;
        return _cam;
    }

    IEnumerator Run(Transform anchor, Vector3 worldOffset, string text, float hold, float fadeDur, float floatDist, float? fontSize, Color? color)
    {
        if (_tmp == null) yield break;
        _tmp.text = text;
        if (fontSize.HasValue) _tmp.fontSize = fontSize.Value;
        Color baseColor = color ?? new Color(1f, 0.92f, 0.78f, 1f);
        _tmp.gameObject.SetActive(true);

        Vector3 GetAnchorPos() => (anchor != null ? anchor.position : Vector3.zero) + worldOffset;
        float elapsed = 0f;
        Color c = baseColor; c.a = 0f; _tmp.color = c;

        // fade in, drift up 30%
        while (elapsed < fadeDur)
        {
            elapsed += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fadeDur));
            Vector3 p = GetAnchorPos() + Vector3.up * (floatDist * 0.3f * k);
            _tmp.transform.position = p;
            FaceCamera(p);
            c.a = k; _tmp.color = c;
            yield return null;
        }

        float held = 0f;
        while (held < hold)
        {
            held += Time.deltaTime;
            Vector3 p = GetAnchorPos() + Vector3.up * (floatDist * 0.3f);
            _tmp.transform.position = p;
            FaceCamera(p);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < fadeDur)
        {
            elapsed += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fadeDur));
            Vector3 p = GetAnchorPos() + Vector3.up * (floatDist * (0.3f + 0.7f * k));
            _tmp.transform.position = p;
            FaceCamera(p);
            c.a = 1f - k; _tmp.color = c;
            yield return null;
        }

        c.a = 0f; _tmp.color = c;
        _tmp.gameObject.SetActive(false);
    }

    IEnumerator RunStatic(Vector3 anchorPos, string text, float hold, float fadeDur, float floatDist, float? fontSize, Color? color)
    {
        if (_tmp == null) yield break;
        _tmp.text = text;
        if (fontSize.HasValue) _tmp.fontSize = fontSize.Value;
        Color baseColor = color ?? new Color(1f, 0.92f, 0.78f, 1f);
        _tmp.gameObject.SetActive(true);

        float elapsed = 0f;
        Color c = baseColor; c.a = 0f; _tmp.color = c;
        while (elapsed < fadeDur)
        {
            elapsed += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fadeDur));
            Vector3 p = anchorPos + Vector3.up * (floatDist * 0.3f * k);
            _tmp.transform.position = p;
            FaceCamera(p);
            c.a = k; _tmp.color = c;
            yield return null;
        }
        float held = 0f;
        while (held < hold)
        {
            held += Time.deltaTime;
            Vector3 p = anchorPos + Vector3.up * (floatDist * 0.3f);
            _tmp.transform.position = p;
            FaceCamera(p);
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < fadeDur)
        {
            elapsed += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fadeDur));
            Vector3 p = anchorPos + Vector3.up * (floatDist * (0.3f + 0.7f * k));
            _tmp.transform.position = p;
            FaceCamera(p);
            c.a = 1f - k; _tmp.color = c;
            yield return null;
        }
        c.a = 0f; _tmp.color = c;
        _tmp.gameObject.SetActive(false);
    }

    void FaceCamera(Vector3 fromPos)
    {
        var cam = ResolveCam();
        if (cam == null || _tmp == null) return;
        Vector3 dir = cam.transform.position - fromPos; dir.y = 0f;
        if (dir.sqrMagnitude < 1e-6f) return;
        _tmp.transform.rotation = Quaternion.LookRotation(-dir.normalized, Vector3.up);
    }
}
