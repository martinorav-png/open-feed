using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class PhoneUIGenerator : Editor
{
    [MenuItem("OPEN FEED/Scripts/Phone UI - Clear")]
    static void ClearPhoneUI()
    {
        GameObject existing = GameObject.Find("PhoneCanvas");
        if (existing != null)
        {
            DestroyImmediate(existing);
            Debug.Log("OPENFEED Phone UI cleared.");
        }
        else
        {
            Debug.Log("No Phone UI found to clear.");
        }
    }

    [MenuItem("OPEN FEED/Scripts/Phone UI")]
    static void GeneratePhoneUI()
    {
        // Clean up existing
        GameObject existing = GameObject.Find("PhoneCanvas");
        if (existing != null) DestroyImmediate(existing);

        // Find the Phone object in the scene
        GameObject phone = GameObject.Find("Phone");
        if (phone == null)
        {
            Debug.LogError("Phone object not found! Generate the Desktop scene first.");
            return;
        }

        // Remove old phone glow light since canvas replaces it
        Transform oldGlow = phone.transform.Find("PhoneGlow");
        if (oldGlow != null) DestroyImmediate(oldGlow.gameObject);

        // ============================
        // WORLD-SPACE CANVAS (parented to Phone)
        // ============================
        GameObject canvasObj = new GameObject("PhoneCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        canvasObj.transform.SetParent(phone.transform, false);

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(550, 950);
        canvasObj.transform.localPosition = new Vector3(0f, 0.012f, 0.01f);
        canvasObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        canvasObj.transform.localScale = new Vector3(0.0001f, 0.0001f, 0.0001f);

        // ============================
        // SCREEN GLOW LIGHT
        // ============================
        GameObject glowLight = new GameObject("PhoneScreenGlow");
        glowLight.transform.SetParent(canvasObj.transform, false);
        glowLight.transform.localPosition = new Vector3(0, 0, -500);
        Light gl = glowLight.AddComponent<Light>();
        gl.type = LightType.Point;
        gl.color = new Color(0.6f, 0.65f, 0.85f);
        gl.intensity = 0.15f;
        gl.range = 0.6f;
        gl.shadows = LightShadows.None;

        // ============================
        // BACKGROUND (light / white)
        // ============================
        GameObject bg = CreateUIElement("Background", canvasObj.transform);
        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0.12f, 0.12f, 0.18f, 1f);
        StretchFill(bg);

        // ============================
        // STATUS BAR (top)
        // ============================
        GameObject statusBar = CreateUIElement("StatusBar", canvasObj.transform);
        RectTransform statusRect = statusBar.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0, 1);
        statusRect.anchorMax = new Vector2(1, 1);
        statusRect.pivot = new Vector2(0.5f, 1);
        statusRect.sizeDelta = new Vector2(0, 50);
        statusRect.anchoredPosition = Vector2.zero;

        // Time text (top left)
        CreateText("TimeText", statusBar.transform,
            "21:37", 22, TextAlignmentOptions.Left, new Color(0.7f, 0.7f, 0.75f),
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(120, 40), new Vector2(20, 0));

        // Battery text (top right)
        CreateText("BatteryText", statusBar.transform,
            "73%", 20, TextAlignmentOptions.Right, new Color(0.7f, 0.7f, 0.75f),
            new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
            new Vector2(80, 40), new Vector2(-20, 0));

        // Signal indicator
        CreateText("SignalText", statusBar.transform,
            "...", 20, TextAlignmentOptions.Right, new Color(0.5f, 0.5f, 0.55f),
            new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
            new Vector2(60, 40), new Vector2(-90, 0));

        // ============================
        // CALLER DISPLAY AREA
        // ============================
        GameObject displayArea = CreateUIElement("DisplayArea", canvasObj.transform);
        RectTransform displayRect = displayArea.GetComponent<RectTransform>();
        displayRect.anchorMin = new Vector2(0, 1);
        displayRect.anchorMax = new Vector2(1, 1);
        displayRect.pivot = new Vector2(0.5f, 1);
        displayRect.sizeDelta = new Vector2(-40, 200);
        displayRect.anchoredPosition = new Vector2(0, -60);

        // Number display
        CreateText("NumberDisplay", displayArea.transform,
            "_ _ _ - _ _ _ _", 36, TextAlignmentOptions.Center, new Color(0.8f, 0.85f, 0.9f),
            new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f),
            new Vector2(450, 60), Vector2.zero);

        // Label under the number
        CreateText("DialLabel", displayArea.transform,
            "enter number", 18, TextAlignmentOptions.Center, new Color(0.4f, 0.4f, 0.45f),
            new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.35f),
            new Vector2(300, 30), Vector2.zero);

        // ============================
        // DIVIDER LINE
        // ============================
        GameObject divider = CreateUIElement("Divider", canvasObj.transform);
        Image divImg = divider.AddComponent<Image>();
        divImg.color = new Color(0.2f, 0.2f, 0.28f, 1f);
        RectTransform divRect = divider.GetComponent<RectTransform>();
        divRect.anchorMin = new Vector2(0.05f, 1);
        divRect.anchorMax = new Vector2(0.95f, 1);
        divRect.pivot = new Vector2(0.5f, 1);
        divRect.sizeDelta = new Vector2(0, 2);
        divRect.anchoredPosition = new Vector2(0, -270);

        // ============================
        // NUMBER PAD (3x4 grid)
        // ============================
        GameObject numpad = CreateUIElement("Numpad", canvasObj.transform);
        RectTransform numpadRect = numpad.GetComponent<RectTransform>();
        numpadRect.anchorMin = new Vector2(0.5f, 0);
        numpadRect.anchorMax = new Vector2(0.5f, 1);
        numpadRect.pivot = new Vector2(0.5f, 1);
        numpadRect.sizeDelta = new Vector2(420, 520);
        numpadRect.anchoredPosition = new Vector2(0, -285);

        string[,] keys = {
            {"1", "2", "3"},
            {"4", "5", "6"},
            {"7", "8", "9"},
            {"*", "0", "#"}
        };

        string[,] subLabels = {
            {"", "ABC", "DEF"},
            {"GHI", "JKL", "MNO"},
            {"PQRS", "TUV", "WXYZ"},
            {"", "+", ""}
        };

        float buttonSize = 110f;
        float spacing = 130f;
        float startX = -spacing;
        float startY = -20f;

        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                float x = startX + col * spacing;
                float y = startY - row * spacing;

                // Button background
                GameObject btn = CreateUIElement($"Key_{keys[row, col]}", numpad.transform);
                Image btnImg = btn.AddComponent<Image>();
                btnImg.color = new Color(0.18f, 0.18f, 0.24f, 1f);
                RectTransform btnRect = btn.GetComponent<RectTransform>();
                btnRect.anchorMin = new Vector2(0.5f, 1);
                btnRect.anchorMax = new Vector2(0.5f, 1);
                btnRect.pivot = new Vector2(0.5f, 0.5f);
                btnRect.sizeDelta = new Vector2(buttonSize, buttonSize);
                btnRect.anchoredPosition = new Vector2(x, y);

                btnImg.type = Image.Type.Simple;

                // Number text
                CreateText($"KeyLabel_{keys[row, col]}", btn.transform,
                    keys[row, col], 32, TextAlignmentOptions.Center, new Color(0.85f, 0.85f, 0.9f),
                    new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f),
                    new Vector2(80, 40), Vector2.zero);

                // Sub-label (letters)
                if (!string.IsNullOrEmpty(subLabels[row, col]))
                {
                    CreateText($"KeySub_{keys[row, col]}", btn.transform,
                        subLabels[row, col], 12, TextAlignmentOptions.Center, new Color(0.45f, 0.45f, 0.5f),
                        new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0.3f),
                        new Vector2(80, 20), Vector2.zero);
                }
            }
        }

        // ============================
        // BOTTOM ACTION ROW (call / delete)
        // ============================
        float bottomY = startY - 4 * spacing - 10;

        // Call button (green)
        GameObject callBtn = CreateUIElement("CallButton", numpad.transform);
        Image callImg = callBtn.AddComponent<Image>();
        callImg.color = new Color(0.1f, 0.45f, 0.15f, 1f);
        RectTransform callRect = callBtn.GetComponent<RectTransform>();
        callRect.anchorMin = new Vector2(0.5f, 1);
        callRect.anchorMax = new Vector2(0.5f, 1);
        callRect.pivot = new Vector2(0.5f, 0.5f);
        callRect.sizeDelta = new Vector2(200, 55);
        callRect.anchoredPosition = new Vector2(0, bottomY);

        CreateText("CallLabel", callBtn.transform,
            "CALL", 22, TextAlignmentOptions.Center, new Color(0.8f, 1f, 0.8f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(180, 45), Vector2.zero);

        // Delete button (right side)
        GameObject delBtn = CreateUIElement("DeleteButton", numpad.transform);
        Image delImg = delBtn.AddComponent<Image>();
        delImg.color = new Color(0.25f, 0.14f, 0.14f, 1f);
        RectTransform delRect = delBtn.GetComponent<RectTransform>();
        delRect.anchorMin = new Vector2(0.5f, 1);
        delRect.anchorMax = new Vector2(0.5f, 1);
        delRect.pivot = new Vector2(0.5f, 0.5f);
        delRect.sizeDelta = new Vector2(90, 55);
        delRect.anchoredPosition = new Vector2(spacing, bottomY);

        CreateText("DeleteLabel", delBtn.transform,
            "<", 28, TextAlignmentOptions.Center, new Color(0.8f, 0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(60, 45), Vector2.zero);

        // ============================
        // BOTTOM NAV BAR
        // ============================
        GameObject navBar = CreateUIElement("NavBar", canvasObj.transform);
        Image navBg = navBar.AddComponent<Image>();
        navBg.color = new Color(0.08f, 0.08f, 0.12f, 1f);
        RectTransform navRect = navBar.GetComponent<RectTransform>();
        navRect.anchorMin = new Vector2(0, 0);
        navRect.anchorMax = new Vector2(1, 0);
        navRect.pivot = new Vector2(0.5f, 0);
        navRect.sizeDelta = new Vector2(0, 70);
        navRect.anchoredPosition = Vector2.zero;

        // Nav items
        string[] navLabels = { "recent", "contacts", "keypad", "voicemail" };
        float navSpacing = 550f / 4f;
        float navStartX = -navSpacing * 1.5f;

        for (int i = 0; i < navLabels.Length; i++)
        {
            Color labelColor = (i == 2)
                ? new Color(0.4f, 0.6f, 0.9f)
                : new Color(0.4f, 0.4f, 0.45f);

            CreateText($"Nav_{navLabels[i]}", navBar.transform,
                navLabels[i], 14, TextAlignmentOptions.Center, labelColor,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(120, 50), new Vector2(navStartX + i * navSpacing, 0));
        }

        // ============================
        // FINALIZE
        // ============================
        Selection.activeGameObject = canvasObj;
        Debug.Log("OPENFEED Phone UI generated with screen glow light.");
    }

    // ============================
    // HELPER METHODS
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

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = anchoredPosition;

        return obj;
    }
}