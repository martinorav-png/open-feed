/*
 * Copyright (C) 2012 GREE, Inc.
 *
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 * claim that you wrote the original software. If you use this software
 * in a product, an acknowledgment in the product documentation would be
 * appreciated but is not required.
 * 2. Altered source versions must be plainly marked as such, and must not be
 * misrepresented as being the original software.
 * 3. This notice may not be removed or altered from any source distribution.
 *
 * Source: https://github.com/CodeOverflowYT/Unity-Web-View-Tutorial (WebViewController.cs)
 * Requires: net.gree.unity-webview (WebViewObject).
 *
 * Desk monitor: enable Align To World Viewport + assign the ScrollView RectTransform.
 * gree draws in screen space; LateUpdate syncs SetMargins so the page lines up with the 3D monitor.
 */

using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_2018_4_OR_NEWER
using UnityEngine.Networking;
#endif

[DefaultExecutionOrder(50)]
public class WebViewController : MonoBehaviour
{
    public string Url;
    public int LeftMargin, RightMargin, TopMargin, BottomMargin;

    [Header("Desk monitor (in-world alignment)")]
    [Tooltip("When on, SetMargins follows the world RectTransform each frame while browsing.")]
    [SerializeField] bool alignToWorldViewport;

    [Tooltip("When on (and align is on), native webview fills the screen while browsing instead of matching ScrollView.")]
    [SerializeField] bool fullscreenWhileBrowsing = true;

    [Tooltip("Usually MonitorCanvas → ScrollView (the inner screen area).")]
    [SerializeField] RectTransform worldViewportRect;

    [Tooltip("Defaults to the Canvas world camera (set by MonitorInteraction when you zoom).")]
    [SerializeField] Camera worldViewportCamera;

    [SerializeField] int minViewportPixels = 32;

    [SerializeField]
    private WebViewObject webViewObject;

    private Coroutine _loadCoroutine;
    private Canvas _parentCanvas;
    private GraphicRaycaster _canvasRaycaster;
    private Transform _scrollContent;
    private bool _loadComplete;
    private bool _browsingMode;

    /// <summary>True when this controller should own monitor browse mode (hides duplicate MonitorWebViewHost).</summary>
    public bool AlignsToWorldViewport => alignToWorldViewport && worldViewportRect != null;

    private void Awake()
    {
        _parentCanvas = GetComponentInParent<Canvas>();
        if (_parentCanvas != null)
            _canvasRaycaster = _parentCanvas.GetComponent<GraphicRaycaster>();

        if (webViewObject == null)
        {
            var go = new GameObject("WebViewObject");
            go.transform.SetParent(transform, false);
            webViewObject = go.AddComponent<WebViewObject>();
        }

        ResolveViewportAndContent();
    }

    void ResolveViewportAndContent()
    {
        Transform canvasRoot = _parentCanvas != null ? _parentCanvas.transform : transform;

        if (worldViewportRect == null && alignToWorldViewport)
        {
            var scroll = canvasRoot.Find("ScrollView");
            if (scroll != null)
                worldViewportRect = scroll as RectTransform;
        }

        if (_scrollContent == null)
        {
            var scroll = canvasRoot.Find("ScrollView");
            if (scroll != null)
            {
                var content = scroll.Find("Content");
                if (content != null)
                    _scrollContent = content;
            }
        }
    }

    private void Start()
    {
        _loadCoroutine = StartCoroutine(LoadWebView(Url));
    }

    private void OnDisable()
    {
        if (_loadCoroutine != null)
        {
            StopCoroutine(_loadCoroutine);
            _loadCoroutine = null;
        }
    }

    public void SetVisibility(bool visibility)
    {
        if (webViewObject != null)
            webViewObject.SetVisibility(visibility);
    }

    public bool GetVisibility()
    {
        return webViewObject != null && webViewObject.GetVisibility();
    }

    /// <summary>
    /// Call from MonitorInteraction when the player enters/leaves monitor browse mode.
    /// </summary>
    public void SetMonitorBrowsingMode(bool browsing)
    {
        if (!AlignsToWorldViewport)
        {
            if (webViewObject != null && _loadComplete)
            {
                webViewObject.SetVisibility(browsing);
                webViewObject.SetInteractionEnabled(browsing);
            }
            return;
        }

        _browsingMode = browsing;

        if (_canvasRaycaster != null)
            _canvasRaycaster.enabled = !browsing;

        if (_scrollContent != null)
            _scrollContent.gameObject.SetActive(!browsing);

        if (webViewObject == null || !_loadComplete)
            return;

        webViewObject.SetVisibility(browsing);
        webViewObject.SetInteractionEnabled(browsing);
    }

    void LateUpdate()
    {
        if (!AlignsToWorldViewport || !_browsingMode || webViewObject == null || !_loadComplete)
            return;

        if (fullscreenWhileBrowsing)
        {
            webViewObject.SetMargins(0, 0, 0, 0);
            return;
        }

        if (worldViewportRect == null)
            return;

        var cam = worldViewportCamera != null ? worldViewportCamera : (_parentCanvas != null ? _parentCanvas.worldCamera : null);
        if (cam == null)
            return;

        var corners = new Vector3[4];
        worldViewportRect.GetWorldCorners(corners);

        float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
        var anyInFront = false;
        for (var i = 0; i < 4; i++)
        {
            var s = cam.WorldToScreenPoint(corners[i]);
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

        if (Screen.width - (left + right) < minViewportPixels || Screen.height - (top + bottom) < minViewportPixels)
            return;

        webViewObject.SetMargins(left, top, right, bottom);
    }

    private IEnumerator LoadWebView(string url)
    {
        if (webViewObject == null)
            yield break;

        webViewObject.Init(
            cb: (msg) => { },
            err: (msg) => { },
            httpErr: (msg) => { },
            started: (msg) => { },
            hooked: (msg) => { },
            cookies: (msg) => { },
            ld: (msg) =>
            {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IOS
                var js = @"
if (!(window.webkit && window.webkit.messageHandlers)) {
  window.Unity = {
    call: function(msg) {
      window.location = 'unity:' + msg;
    }
  };
}
";
#elif UNITY_WEBPLAYER || UNITY_WEBGL
                var js = @"
window.Unity = {
  call:function(msg) {
    parent.unityWebView.sendMessage('WebViewObject', msg);
  }
};
";
#else
                var js = "";
#endif
                webViewObject.EvaluateJS(js + @"Unity.call('ua=' + navigator.userAgent)");
            }
        );

        while (!webViewObject.IsInitialized())
            yield return null;

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        webViewObject.bitmapRefreshCycle = 1;
        webViewObject.devicePixelRatio = 1;
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        webViewObject.bitmapRefreshCycle = 2;
#endif

        if (!AlignsToWorldViewport)
            webViewObject.SetMargins(LeftMargin, TopMargin, RightMargin, BottomMargin);

        webViewObject.SetTextZoom(100);

#if !UNITY_WEBPLAYER && !UNITY_WEBGL
        if (url.StartsWith("http"))
        {
            webViewObject.LoadURL(url.Replace(" ", "%20"));
        }
        else
        {
            var srcHtml = Path.Combine(Application.streamingAssetsPath, url);
            if (!srcHtml.Contains("://"))
            {
                var cachedHtml = CopyStreamingFolderToCache(url);
                if (!string.IsNullOrEmpty(cachedHtml))
                {
                    webViewObject.LoadURL("file://" + cachedHtml.Replace(" ", "%20"));
                    goto LoadComplete;
                }
            }

            byte[] result = null;
            if (srcHtml.Contains("://"))
            {
#if UNITY_2018_4_OR_NEWER
                using (var unityWebRequest = UnityWebRequest.Get(srcHtml))
                {
                    yield return unityWebRequest.SendWebRequest();
                    if (unityWebRequest.result == UnityWebRequest.Result.Success)
                        result = unityWebRequest.downloadHandler.data;
                }
#endif
            }

            if (result != null)
            {
                var dst = Path.Combine(Application.temporaryCachePath, url);
                Directory.CreateDirectory(Path.GetDirectoryName(dst) ?? "");
                File.WriteAllBytes(dst, result);
                webViewObject.LoadURL("file://" + dst.Replace(" ", "%20"));
            }
        }
#else
        if (url.StartsWith("http"))
            webViewObject.LoadURL(url.Replace(" ", "%20"));
        else
            webViewObject.LoadURL("StreamingAssets/" + url.Replace(" ", "%20"));
#endif

LoadComplete:
        _loadComplete = true;

        if (AlignsToWorldViewport)
        {
            webViewObject.SetMargins(0, 0, 0, 0);
            webViewObject.SetVisibility(false);
            webViewObject.SetInteractionEnabled(false);
        }
        else
        {
            SetVisibility(true);
        }

        yield break;
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
}
