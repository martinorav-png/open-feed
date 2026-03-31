using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public class BuildSettingsSetup : Editor
{
    [MenuItem("OPEN FEED/Setup Build Settings (All Scenes)")]
    static void SetupBuildSettings()
    {
        string[] scenePaths = new string[]
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/GroceryStore.unity",
            "Assets/Scenes/Driving.unity",
            "Assets/Scenes/Desk.unity",
        };

        List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();
        foreach (string path in scenePaths)
        {
            buildScenes.Add(new EditorBuildSettingsScene(path, true));
        }

        EditorBuildSettings.scenes = buildScenes.ToArray();
        Debug.Log("OPENFEED Build Settings configured with all 4 scenes:\n" +
            "  0: MainMenu\n  1: GroceryStore\n  2: Driving\n  3: Desk");
    }

    [MenuItem("OPEN FEED/Create All Scenes (Empty)")]
    static void CreateAllScenes()
    {
        // Create GroceryStore and Driving scene files if they don't exist
        CreateSceneIfMissing("Assets/Scenes/GroceryStore.unity");
        CreateSceneIfMissing("Assets/Scenes/Driving.unity");

        Debug.Log("OPENFEED scene files created. Now run the generators:\n" +
            "  1. Open GroceryStore scene -> OPEN FEED > Scripts > Grocery Store\n" +
            "  2. Open Driving scene -> OPEN FEED > Scripts > Driving Scene\n" +
            "  3. Run: OPEN FEED > Setup Build Settings");
    }

    static void CreateSceneIfMissing(string path)
    {
        // Check if file already exists
        if (System.IO.File.Exists(path))
        {
            Debug.Log($"Scene already exists: {path}");
            return;
        }

        // Create a new empty scene and save it
        var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

        // Add a GameFlowManager object so the flow works when starting from this scene
        GameObject gfm = new GameObject("GameFlowManager");
        gfm.AddComponent<GameFlowManager>();

        EditorSceneManager.SaveScene(newScene, path);
        EditorSceneManager.CloseScene(newScene, true);
        Debug.Log($"Created scene: {path}");
    }

    [MenuItem("OPEN FEED/Quick Setup (Create Scenes + Build Settings)")]
    static void QuickSetup()
    {
        CreateAllScenes();
        SetupBuildSettings();
        Debug.Log("OPENFEED Quick Setup complete! Open each scene and run its generator.");
    }
}
