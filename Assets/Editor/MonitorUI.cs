using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class MonitorUIGenerator : Editor
{
    [MenuItem("OPEN FEED/Scripts/Monitor UI - Clear")]
    static void ClearMonitorUI()
    {
        GameObject existing = GameObject.Find("MonitorCanvas");
        if (existing != null)
        {
            DestroyImmediate(existing);
            Debug.Log("OPENFEED Monitor UI cleared.");
        }
    }

    [MenuItem("OPEN FEED/Scripts/Monitor UI")]
    static void GenerateMonitorUI()
    {
        GameObject existing = GameObject.Find("MonitorCanvas");
        if (existing != null) DestroyImmediate(existing);

        GameObject monitor = GameObject.Find("Monitor");
        if (monitor == null)
            monitor = GameObject.Find("CRTMonitor");
        if (monitor == null)
        {
            Debug.LogError("Monitor object not found! Generate the Desktop scene first.");
            return;
        }

        // Remove old screen lights
        Transform oldScreenLight = monitor.transform.Find("ScreenLight");
        if (oldScreenLight != null) DestroyImmediate(oldScreenLight.gameObject);
        Transform oldScreenFill = monitor.transform.Find("ScreenFillLight");
        if (oldScreenFill != null) DestroyImmediate(oldScreenFill.gameObject);

        // ============================
        // CANVAS
        // ============================
        GameObject canvasObj = new GameObject("MonitorCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        canvasObj.transform.SetParent(monitor.transform, false);

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(760, 560);
        canvasObj.transform.localPosition = new Vector3(0f, 0.2f, 0.199f);
        canvasObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        canvasObj.transform.localScale = new Vector3(0.0005f, 0.0005f, 0.0005f);

        // Set event camera at runtime via MonitorBrowser
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Camera[] cams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            if (cams.Length > 0) mainCam = cams[0];
        }
        if (mainCam != null)
            canvas.worldCamera = mainCam;

        // ============================
        // SCREEN GLOW LIGHTS
        // ============================
        GameObject glowLight = new GameObject("ScreenGlow");
        glowLight.transform.SetParent(canvasObj.transform, false);
        glowLight.transform.localPosition = new Vector3(0, 0, -200);
        Light gl = glowLight.AddComponent<Light>();
        gl.type = LightType.Point;
        gl.color = new Color(0.45f, 0.75f, 0.5f);
        gl.intensity = 0.4f;
        gl.range = 3f;
        gl.shadows = LightShadows.Soft;

        GameObject fillLight = new GameObject("ScreenFill");
        fillLight.transform.SetParent(canvasObj.transform, false);
        fillLight.transform.localPosition = new Vector3(0, 0, -400);
        Light fl = fillLight.AddComponent<Light>();
        fl.type = LightType.Point;
        fl.color = new Color(0.35f, 0.65f, 0.4f);
        fl.intensity = 0.15f;
        fl.range = 4f;
        fl.shadows = LightShadows.None;

        // ============================
        // BACKGROUND
        // ============================
        GameObject bg = CreateUIElement("Background", canvasObj.transform);
        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0.88f, 0.92f, 0.89f, 1f);
        bgImage.raycastTarget = true;
        StretchFill(bg);

        // ============================
        // TOP BAR (browser address bar)
        // ============================
        GameObject topBar = CreateUIElement("TopBar", canvasObj.transform);
        Image topBarBg = topBar.AddComponent<Image>();
        topBarBg.color = new Color(0.78f, 0.82f, 0.79f, 1f);
        topBarBg.raycastTarget = false;
        RectTransform topBarRect = topBar.GetComponent<RectTransform>();
        topBarRect.anchorMin = new Vector2(0, 1);
        topBarRect.anchorMax = new Vector2(1, 1);
        topBarRect.pivot = new Vector2(0.5f, 1);
        topBarRect.sizeDelta = new Vector2(0, 40);
        topBarRect.anchoredPosition = Vector2.zero;

        // Address bar
        GameObject addressBar = CreateUIElement("AddressBar", topBar.transform);
        Image addrBg = addressBar.AddComponent<Image>();
        addrBg.color = new Color(0.92f, 0.94f, 0.92f, 1f);
        addrBg.raycastTarget = false;
        RectTransform addrRect = addressBar.GetComponent<RectTransform>();
        addrRect.anchorMin = new Vector2(0, 0.5f);
        addrRect.anchorMax = new Vector2(1, 0.5f);
        addrRect.pivot = new Vector2(0.5f, 0.5f);
        addrRect.sizeDelta = new Vector2(-100, 24);
        addrRect.anchoredPosition = Vector2.zero;

        CreateText("URLText", addressBar.transform,
            "http://openfeed.icu", 14, TextAlignmentOptions.Left, new Color(0.2f, 0.35f, 0.22f),
            new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, 0.5f),
            new Vector2(0, 20), new Vector2(10, 0));

        // ============================
        // SCROLL VIEW (main content area)
        // ============================
        GameObject scrollView = CreateUIElement("ScrollView", canvasObj.transform);
        RectTransform scrollRect = scrollView.GetComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0, 0);
        scrollRect.anchorMax = new Vector2(1, 1);
        scrollRect.offsetMin = new Vector2(0, 30);  // above status bar
        scrollRect.offsetMax = new Vector2(0, -40);  // below top bar

        ScrollRect scroll = scrollView.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.elasticity = 0.1f;
        scroll.scrollSensitivity = 30f;

        // Mask to clip content
        Image scrollBg = scrollView.AddComponent<Image>();
        scrollBg.color = new Color(0.88f, 0.92f, 0.89f, 1f);
        scrollBg.raycastTarget = true;
        Mask mask = scrollView.AddComponent<Mask>();
        mask.showMaskGraphic = true;

        // Scrollbar (right side, thin)
        GameObject scrollbar = CreateUIElement("Scrollbar", scrollView.transform);
        Image scrollbarBg = scrollbar.AddComponent<Image>();
        scrollbarBg.color = new Color(0.75f, 0.78f, 0.76f, 0.5f);
        RectTransform scrollbarRect = scrollbar.GetComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(1, 0);
        scrollbarRect.anchorMax = new Vector2(1, 1);
        scrollbarRect.pivot = new Vector2(1, 0.5f);
        scrollbarRect.sizeDelta = new Vector2(8, 0);
        scrollbarRect.anchoredPosition = Vector2.zero;

        Scrollbar sb = scrollbar.AddComponent<Scrollbar>();
        sb.direction = Scrollbar.Direction.BottomToTop;

        // Scrollbar handle
        GameObject handle = CreateUIElement("Handle", scrollbar.transform);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = new Color(0.5f, 0.55f, 0.52f, 0.7f);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = Vector2.one;
        handleRect.offsetMin = Vector2.zero;
        handleRect.offsetMax = Vector2.zero;

        sb.handleRect = handleRect;
        sb.targetGraphic = handleImg;

        scroll.verticalScrollbar = sb;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        // ============================
        // SCROLL CONTENT
        // ============================
        GameObject content = CreateUIElement("Content", scrollView.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 900); // taller than viewport to enable scroll

        scroll.content = contentRect;

        // ============================
        // HEADER
        // ============================
        CreateText("SiteTitle", content.transform,
            "OPEN FEED", 42, TextAlignmentOptions.Center, new Color(0.15f, 0.2f, 0.15f),
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(500, 50), new Vector2(0, -30));

        // Divider
        GameObject divider1 = CreateUIElement("Divider1", content.transform);
        Image div1Img = divider1.AddComponent<Image>();
        div1Img.color = new Color(0.55f, 0.65f, 0.56f, 0.6f);
        div1Img.raycastTarget = false;
        RectTransform div1Rect = divider1.GetComponent<RectTransform>();
        div1Rect.anchorMin = new Vector2(0.15f, 1);
        div1Rect.anchorMax = new Vector2(0.85f, 1);
        div1Rect.pivot = new Vector2(0.5f, 1);
        div1Rect.sizeDelta = new Vector2(0, 1);
        div1Rect.anchoredPosition = new Vector2(0, -75);

        // Tagline
        CreateText("Tagline", content.transform,
            "live surveillance feeds - worldwide", 14, TextAlignmentOptions.Center, new Color(0.35f, 0.45f, 0.36f),
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(500, 24), new Vector2(0, -85));

        // ============================
        // CAMERA FEED GRID (2x2) - CLICKABLE BUTTONS
        // ============================
        float gridStartY = -125f;
        float cellWidth = 165f;
        float cellHeight = 110f;
        float gapX = 12f;
        float gapY = 12f;

        string[] feedLabels = { "CAM 01 - OSLO", "CAM 02 - MANILA", "CAM 03 - LAGOS", "CAM 04 - QUITO" };
        string[] feedStatus = { "LIVE", "LIVE", "OFFLINE", "LIVE" };
        Color[] statusColors = {
            new Color(0.2f, 0.55f, 0.25f),
            new Color(0.2f, 0.55f, 0.25f),
            new Color(0.6f, 0.2f, 0.15f),
            new Color(0.2f, 0.55f, 0.25f)
        };

        Color feedNormal = new Color(0.1f, 0.12f, 0.1f, 1f);
        Color feedHover = new Color(0.15f, 0.2f, 0.16f, 1f);
        Color feedPressed = new Color(0.08f, 0.1f, 0.08f, 1f);
        Color feedDisabled = new Color(0.06f, 0.06f, 0.06f, 1f);

        for (int i = 0; i < 4; i++)
        {
            int col = i % 2;
            int row = i / 2;

            float x = (col == 0 ? -1 : 1) * (cellWidth / 2 + gapX / 2);
            float y = gridStartY - row * (cellHeight + gapY);

            // Feed container - this is the BUTTON
            GameObject feed = CreateUIElement($"Feed_{i}", content.transform);
            Image feedBg = feed.AddComponent<Image>();
            feedBg.color = feedNormal;
            feedBg.raycastTarget = true;

            RectTransform feedRect = feed.GetComponent<RectTransform>();
            feedRect.anchorMin = new Vector2(0.5f, 1);
            feedRect.anchorMax = new Vector2(0.5f, 1);
            feedRect.pivot = new Vector2(0.5f, 1);
            feedRect.sizeDelta = new Vector2(cellWidth, cellHeight);
            feedRect.anchoredPosition = new Vector2(x, y);

            // Add Button component
            Button btn = feed.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = feedNormal;
            colors.highlightedColor = feedHover;
            colors.pressedColor = feedPressed;
            colors.disabledColor = feedDisabled;
            colors.fadeDuration = 0.1f;
            btn.colors = colors;
            btn.targetGraphic = feedBg;

            // Disable button for offline feed
            if (i == 2)
                btn.interactable = false;

            // Add CameraFeedButton component for identification
            CameraFeedButton cfb = feed.AddComponent<CameraFeedButton>();
            cfb.feedIndex = i;
            cfb.feedName = feedLabels[i];
            cfb.isOnline = (i != 2);

            // Border
            GameObject feedBorder = CreateUIElement($"FeedBorder_{i}", feed.transform);
            Image borderImg = feedBorder.AddComponent<Image>();
            borderImg.color = new Color(0.4f, 0.5f, 0.42f, 0.5f);
            borderImg.raycastTarget = false;
            StretchFill(feedBorder);

            // Inner area
            GameObject feedInner = CreateUIElement($"FeedInner_{i}", feed.transform);
            Image innerImg = feedInner.AddComponent<Image>();
            innerImg.color = (i == 2)
                ? new Color(0.05f, 0.05f, 0.05f, 1f)
                : new Color(0.08f, 0.1f, 0.08f, 1f);
            innerImg.raycastTarget = false;
            RectTransform innerRect = feedInner.GetComponent<RectTransform>();
            innerRect.anchorMin = Vector2.zero;
            innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(1, 1);
            innerRect.offsetMax = new Vector2(-1, -1);

            // Content hint
            if (i == 2)
            {
                CreateText($"NoSignal_{i}", feedInner.transform,
                    "NO SIGNAL", 14, TextAlignmentOptions.Center, new Color(0.6f, 0.2f, 0.15f),
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(140, 24), Vector2.zero);
            }
            else
            {
                CreateText($"FeedHint_{i}", feedInner.transform,
                    "click to view", 11, TextAlignmentOptions.Center, new Color(0.3f, 0.45f, 0.32f, 0.5f),
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(140, 24), Vector2.zero);
            }

            // Feed label
            CreateText($"FeedLabel_{i}", feed.transform,
                feedLabels[i], 9, TextAlignmentOptions.Left, new Color(0.55f, 0.7f, 0.57f),
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0),
                new Vector2(0, 16), new Vector2(5, 2));

            // Status
            CreateText($"FeedStatus_{i}", feed.transform,
                feedStatus[i], 8, TextAlignmentOptions.Right, statusColors[i],
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(50, 14), new Vector2(-5, -4));

            // Recording dot
            if (i != 2)
            {
                GameObject dot = CreateUIElement($"RecDot_{i}", feed.transform);
                Image dotImg = dot.AddComponent<Image>();
                dotImg.color = new Color(0.7f, 0.2f, 0.15f, 0.9f);
                dotImg.raycastTarget = false;
                RectTransform dotRect = dot.GetComponent<RectTransform>();
                dotRect.anchorMin = new Vector2(1, 1);
                dotRect.anchorMax = new Vector2(1, 1);
                dotRect.pivot = new Vector2(1, 1);
                dotRect.sizeDelta = new Vector2(5, 5);
                dotRect.anchoredPosition = new Vector2(-55, -6);
            }
        }

        // ============================
        // ADDITIONAL CONTENT (below feeds, scrollable)
        // ============================

        // Divider
        GameObject divider2 = CreateUIElement("Divider2", content.transform);
        Image div2Img = divider2.AddComponent<Image>();
        div2Img.color = new Color(0.55f, 0.65f, 0.56f, 0.6f);
        div2Img.raycastTarget = false;
        RectTransform div2Rect = divider2.GetComponent<RectTransform>();
        div2Rect.anchorMin = new Vector2(0.15f, 1);
        div2Rect.anchorMax = new Vector2(0.85f, 1);
        div2Rect.pivot = new Vector2(0.5f, 1);
        div2Rect.sizeDelta = new Vector2(0, 1);
        div2Rect.anchoredPosition = new Vector2(0, -380);

        // Instructions
        CreateText("Instructions", content.transform,
            "select a feed to begin observation.\nyou are responsible for what you see.", 11, TextAlignmentOptions.Center,
            new Color(0.3f, 0.38f, 0.3f),
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(500, 40), new Vector2(0, -395));

        // Disclaimer
        CreateText("Disclaimer", content.transform,
            "all feeds are unmonitored. proceed at your own risk.", 9, TextAlignmentOptions.Center,
            new Color(0.4f, 0.48f, 0.42f, 0.7f),
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(500, 20), new Vector2(0, -440));

        // Extra section below fold (reward for scrolling)
        GameObject divider3 = CreateUIElement("Divider3", content.transform);
        Image div3Img = divider3.AddComponent<Image>();
        div3Img.color = new Color(0.55f, 0.65f, 0.56f, 0.3f);
        div3Img.raycastTarget = false;
        RectTransform div3Rect = divider3.GetComponent<RectTransform>();
        div3Rect.anchorMin = new Vector2(0.1f, 1);
        div3Rect.anchorMax = new Vector2(0.9f, 1);
        div3Rect.pivot = new Vector2(0.5f, 1);
        div3Rect.sizeDelta = new Vector2(0, 1);
        div3Rect.anchoredPosition = new Vector2(0, -500);

        CreateText("ForumLink", content.transform,
            ">> community board", 12, TextAlignmentOptions.Center,
            new Color(0.25f, 0.45f, 0.55f),
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(300, 24), new Vector2(0, -520));

        CreateText("FAQ", content.transform,
            ">> faq / about", 12, TextAlignmentOptions.Center,
            new Color(0.25f, 0.45f, 0.55f),
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(300, 24), new Vector2(0, -548));

        CreateText("Donate", content.transform,
            ">> support the project", 12, TextAlignmentOptions.Center,
            new Color(0.25f, 0.45f, 0.55f),
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(300, 24), new Vector2(0, -576));

        CreateText("FooterText", content.transform,
            "openfeed.icu - est. 2019\n\"we only watch. we never interfere.\"\n\n12 users online", 9,
            TextAlignmentOptions.Center, new Color(0.4f, 0.45f, 0.42f, 0.5f),
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(400, 80), new Vector2(0, -630));

        // Visitor counter
        CreateText("VisitorCount", content.transform,
            "total views: 847,203", 8, TextAlignmentOptions.Center,
            new Color(0.35f, 0.4f, 0.36f, 0.4f),
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(300, 20), new Vector2(0, -710));

        // ASCII art / decoration at very bottom
        CreateText("AsciiDecor", content.transform,
            "- - - - - - - - - - - - -", 8, TextAlignmentOptions.Center,
            new Color(0.4f, 0.45f, 0.42f, 0.3f),
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(400, 16), new Vector2(0, -740));

        // ============================
        // BOTTOM STATUS BAR (fixed, outside scroll)
        // ============================
        GameObject statusBar = CreateUIElement("StatusBar", canvasObj.transform);
        Image statusBg = statusBar.AddComponent<Image>();
        statusBg.color = new Color(0.75f, 0.8f, 0.76f, 1f);
        statusBg.raycastTarget = false;
        RectTransform statusBarRect = statusBar.GetComponent<RectTransform>();
        statusBarRect.anchorMin = new Vector2(0, 0);
        statusBarRect.anchorMax = new Vector2(1, 0);
        statusBarRect.pivot = new Vector2(0.5f, 0);
        statusBarRect.sizeDelta = new Vector2(0, 28);
        statusBarRect.anchoredPosition = Vector2.zero;

        CreateText("ConnectionStatus", statusBar.transform,
            "connected", 10, TextAlignmentOptions.Left, new Color(0.2f, 0.45f, 0.25f),
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(120, 20), new Vector2(12, 0));

        GameObject connDot = CreateUIElement("ConnDot", statusBar.transform);
        Image connDotImg = connDot.AddComponent<Image>();
        connDotImg.color = new Color(0.15f, 0.55f, 0.25f, 1f);
        connDotImg.raycastTarget = false;
        RectTransform connDotRect = connDot.GetComponent<RectTransform>();
        connDotRect.anchorMin = new Vector2(0, 0.5f);
        connDotRect.anchorMax = new Vector2(0, 0.5f);
        connDotRect.pivot = new Vector2(0, 0.5f);
        connDotRect.sizeDelta = new Vector2(6, 6);
        connDotRect.anchoredPosition = new Vector2(5, 0);

        CreateText("Latency", statusBar.transform,
            "ping: 247ms", 10, TextAlignmentOptions.Right, new Color(0.3f, 0.4f, 0.32f),
            new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
            new Vector2(120, 20), new Vector2(-12, 0));

        CreateText("Encrypted", statusBar.transform,
            "TOR // encrypted", 10, TextAlignmentOptions.Center, new Color(0.35f, 0.45f, 0.37f, 0.6f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(150, 20), Vector2.zero);

        if (canvasObj.GetComponent<MonitorWebViewHost>() == null)
            canvasObj.AddComponent<MonitorWebViewHost>();

        // ============================
        // FINALIZE
        // ============================
        Selection.activeGameObject = canvasObj;
        Debug.Log("OPENFEED Monitor UI generated with scroll and clickable feeds.");
    }

    // ============================
    // HELPERS
    // ============================

    static GameObject CreateUIElement(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        return obj;
    }

    static void StretchFill(GameObject obj)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static GameObject CreateText(string name, Transform parent,
        string text, float fontSize, TextAlignmentOptions alignment, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 sizeDelta, Vector2 anchoredPosition)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.enableAutoSizing = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = anchoredPosition;

        return obj;
    }
}