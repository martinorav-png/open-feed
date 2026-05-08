using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class HouseCrosshair : MonoBehaviour
{
    [SerializeField] float interactRange = 4f;
    [SerializeField] float dotSize = 5f;
    [SerializeField] Color idleColor = new Color(1f, 1f, 1f, 0.65f);
    [SerializeField] Color hoverColor = new Color(0.4f, 0.95f, 0.5f, 0.95f);

    StoreFirstPersonController _fpc;
    Camera _cam;
    Image _dot;

    void Awake()
    {
        _fpc = GetComponent<StoreFirstPersonController>();
        BuildUI();
    }

    void BuildUI()
    {
        var canvasGo = new GameObject("HouseCrosshairCanvas");
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 760;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var dotGo = new GameObject("Dot");
        dotGo.transform.SetParent(canvasGo.transform, false);
        _dot = dotGo.AddComponent<Image>();
        _dot.color = idleColor;
        _dot.raycastTarget = false;

        var rt = _dot.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(dotSize, dotSize);
        rt.anchoredPosition = Vector2.zero;
    }

    void Update()
    {
        if (_dot == null)
            return;

        Camera cam = ResolveCamera();
        bool hover = cam != null && InteractableHoverQuery.IsCrosshairOverInteractable(cam, interactRange);
        _dot.color = hover ? hoverColor : idleColor;
    }

    Camera ResolveCamera()
    {
        if (_cam != null && _cam.isActiveAndEnabled)
            return _cam;

        if (_fpc != null && _fpc.cameraPivot != null)
            _cam = _fpc.cameraPivot.GetComponentInChildren<Camera>(true);

        if (_cam == null)
            _cam = Camera.main;

        return _cam;
    }
}
