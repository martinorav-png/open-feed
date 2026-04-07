using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_2018_4_OR_NEWER
using UnityEngine.Networking;
#endif

/// <summary>
/// Hosts gree unity-webview for the desk monitor. While browsing, the native view can fill the whole screen
/// or track the ScrollView rect in screen space (see <see cref="fullscreenWhileBrowsing"/>).
/// Wired from <see cref="MonitorInteraction"/>.
/// </summary>
[DefaultExecutionOrder(50)]
public class MonitorWebViewHost : MonoBehaviour
{
    [Header("Web source")]
    [Tooltip("If set, loads this http(s) URL. Otherwise loads StreamingAssets path below.")]
    [SerializeField] string httpUrl = "";

    [Tooltip("HTML entry under StreamingAssets when httpUrl is empty.")]
    [SerializeField] string streamingRelativePath = "MonitorSite/index.html";

    [Header("Screen mapping (browsing)")]
    [Tooltip("When on, native webview fills the display while browsing. When off, matches ScrollView on screen.")]
    [SerializeField] bool fullscreenWhileBrowsing = true;

    [Tooltip("World-space area to match when not fullscreen (default: child ScrollView).")]
    [SerializeField] RectTransform webViewport;

    Canvas _canvas;
    WebViewObject _web;
    GraphicRaycaster _raycaster;
    bool _initialized;
    bool _browsing;
    Transform _scrollContent;
    Camera _cam;

    void Awake()
    {
        _canvas = GetComponent<Canvas>();
        foreach (var wvc in FindObjectsByType<WebViewController>())
        {
            if (!wvc.AlignsToWorldViewport)
                continue;
            if (wvc.GetComponentInParent<Canvas>() == _canvas)
            {
                enabled = false;
                return;
            }
        }

        _raycaster = GetComponent<GraphicRaycaster>();
        if (webViewport == null)
        {
            var scroll = transform.Find("ScrollView");
            if (scroll != null)
                webViewport = scroll as RectTransform;
        }

        var sv = transform.Find("ScrollView");
        if (sv != null)
        {
            var content = sv.Find("Content");
            if (content != null)
                _scrollContent = content;
        }
    }

    public void SetMonitorBrowsing(bool active)
    {
        _browsing = active;
        if (_raycaster != null)
            _raycaster.enabled = !active;

        if (_scrollContent != null)
            _scrollContent.gameObject.SetActive(!active);

        if (_web != null)
        {
            _web.SetVisibility(active && _initialized);
            _web.SetInteractionEnabled(active && _initialized);
        }
    }

    IEnumerator Start()
    {
        _cam = _canvas != null ? _canvas.worldCamera : Camera.main;

        var host = new GameObject("MonitorWebViewObject");
        host.transform.SetParent(transform, false);
        _web = host.AddComponent<WebViewObject>();

        _web.Init(
            cb: msg => Debug.Log($"[MonitorWeb] JS: {msg}"),
            err: msg => Debug.LogWarning($"[MonitorWeb] {msg}"),
            httpErr: msg => Debug.LogWarning($"[MonitorWeb] HTTP {msg}"),
            started: _ => { },
            hooked: _ => { },
            cookies: _ => { },
            ld: _ => { }
        );

        while (!_web.IsInitialized())
            yield return null;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        _web.bitmapRefreshCycle = 2;
#endif

        _web.SetMargins(0, 0, 0, 0);
        _web.SetVisibility(false);
        _web.SetInteractionEnabled(false);

        yield return LoadPageCoroutine();

        _initialized = true;
    }

    IEnumerator LoadPageCoroutine()
    {
        if (!string.IsNullOrWhiteSpace(httpUrl))
        {
            _web.LoadURL(httpUrl.Trim().Replace(" ", "%20"));
            yield break;
        }

        string path = streamingRelativePath;
        if (string.IsNullOrEmpty(path))
            yield break;

        var src = Path.Combine(Application.streamingAssetsPath, path);
        if (!src.Contains("://"))
        {
            var dst = CopyStreamingFolderToCache(path);
            if (!string.IsNullOrEmpty(dst))
            {
                _web.LoadURL("file://" + dst.Replace(" ", "%20"));
                yield break;
            }
        }

        byte[] result = null;
        if (src.Contains("://"))
        {
#if UNITY_2018_4_OR_NEWER
            using (var req = UnityWebRequest.Get(src))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                    result = req.downloadHandler.data;
            }
#endif
        }

        if (result != null)
        {
            var dst = Path.Combine(Application.temporaryCachePath, path);
            Directory.CreateDirectory(Path.GetDirectoryName(dst) ?? "");
            File.WriteAllBytes(dst, result);
            _web.LoadURL("file://" + dst.Replace(" ", "%20"));
            yield break;
        }

        Debug.LogWarning($"[MonitorWeb] No page at StreamingAssets/{path}. Add Assets/StreamingAssets/MonitorSite/index.html.");
    }

    string CopyStreamingFolderToCache(string relativeHtmlPath)
    {
        var srcHtml = Path.Combine(Application.streamingAssetsPath, relativeHtmlPath);
        if (!File.Exists(srcHtml))
            return null;

        var relativeDir = Path.GetDirectoryName(relativeHtmlPath);
        if (string.IsNullOrEmpty(relativeDir))
            relativeDir = ".";

        var srcDir = Path.Combine(Application.streamingAssetsPath, relativeDir);
        var dstDir = Path.Combine(Application.temporaryCachePath, relativeDir);

        if (!Directory.Exists(srcDir))
            return null;

        CopyDirectoryRecursive(srcDir, dstDir);
        return Path.Combine(dstDir, Path.GetFileName(relativeHtmlPath));
    }

    void CopyDirectoryRecursive(string sourceDir, string destinationDir)
    {
        Directory.CreateDirectory(destinationDir);

        foreach (var filePath in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(filePath);
            var destPath = Path.Combine(destinationDir, fileName);
            File.Copy(filePath, destPath, true);
        }

        foreach (var dirPath in Directory.GetDirectories(sourceDir))
        {
            var dirName = Path.GetFileName(dirPath);
            var destPath = Path.Combine(destinationDir, dirName);
            CopyDirectoryRecursive(dirPath, destPath);
        }
    }

    void LateUpdate()
    {
        if (!_initialized || _web == null || !_browsing)
            return;

        if (fullscreenWhileBrowsing)
        {
            _web.SetMargins(0, 0, 0, 0);
            return;
        }

        if (webViewport == null)
            return;

        if (_cam == null)
            _cam = _canvas != null ? _canvas.worldCamera : Camera.main;
        if (_cam == null)
            return;

        var corners = new Vector3[4];
        webViewport.GetWorldCorners(corners);

        float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
        var anyInFront = false;
        for (var i = 0; i < 4; i++)
        {
            var s = _cam.WorldToScreenPoint(corners[i]);
            if (s.z <= 0.02f)
                continue;
            anyInFront = true;
            minX = Mathf.Min(minX, s.x);
            maxX = Mathf.Max(maxX, s.x);
            minY = Mathf.Min(minY, s.y);
            maxY = Mathf.Max(maxY, s.y);
        }

        if (!anyInFront)
            return;

        int left = Mathf.Clamp(Mathf.RoundToInt(minX), 0, Screen.width);
        int right = Mathf.Clamp(Mathf.RoundToInt(Screen.width - maxX), 0, Screen.width);
        int bottom = Mathf.Clamp(Mathf.RoundToInt(minY), 0, Screen.height);
        int top = Mathf.Clamp(Mathf.RoundToInt(Screen.height - maxY), 0, Screen.height);

        if (Screen.width - (left + right) < 32 || Screen.height - (top + bottom) < 32)
            return;

        _web.SetMargins(left, top, right, bottom);
    }
}
