using UnityEngine;
using UnityEngine.UI;

public class CameraFeedButton : MonoBehaviour
{
    public int feedIndex = 0;
    public string feedName = "";
    public bool isOnline = true;

    void Start()
    {
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(OnFeedClicked);
        }
    }

    void OnFeedClicked()
    {
        if (!isOnline)
        {
            Debug.Log($"Feed {feedIndex} ({feedName}) is OFFLINE.");
            return;
        }

        Debug.Log($"Feed {feedIndex} selected: {feedName}");

        // Notify MonitorBrowser
        MonitorBrowser browser = FindAnyObjectByType<MonitorBrowser>();
        if (browser != null)
        {
            browser.OnFeedSelected(feedIndex, feedName);
        }
    }
}