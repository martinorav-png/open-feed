using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MonitorBrowser : MonoBehaviour
{
    // State
    private int selectedFeed = -1;
    private string selectedFeedName = "";

    void Start()
    {
        // Ensure the MonitorCanvas has the correct event camera
        GameObject canvasObj = GameObject.Find("MonitorCanvas");
        if (canvasObj != null)
        {
            Canvas canvas = canvasObj.GetComponent<Canvas>();
            if (canvas != null && canvas.worldCamera == null)
            {
                Camera cam = Camera.main;
                if (cam == null)
                {
                    Camera[] cams = FindObjectsByType<Camera>();
                    if (cams.Length > 0) cam = cams[0];
                }
                if (cam != null)
                    canvas.worldCamera = cam;
            }
        }
    }

    public void OnFeedSelected(int index, string name)
    {
        selectedFeed = index;
        selectedFeedName = name;

        Debug.Log($"MonitorBrowser: Opening feed {index} - {name}");

        // Placeholder - for now just log it
        // In the full game this would switch to a camera feed view
        // showing the actual surveillance footage

        // You could do things like:
        // - Change the monitor UI to show a fullscreen feed
        // - Load a scene overlay
        // - Switch camera to a render texture
        // - Play a static/connecting animation
    }

    public int GetSelectedFeed()
    {
        return selectedFeed;
    }
}