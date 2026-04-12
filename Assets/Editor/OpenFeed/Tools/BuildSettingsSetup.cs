using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public class BuildSettingsSetup : Editor
{
    [MenuItem("OPEN FEED/Project/Build Settings (All Scenes)", false, 20)]
    static void SetupBuildSettings()
    {
        string[] scenePaths = new string[]
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/supermarket.unity",
            "Assets/Scenes/GroceryStore.unity",
            "Assets/ForestDrive.unity",
            "Assets/Scenes/MainArea.unity",
            "Assets/Scenes/Desk.unity",
        };

        List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();
        foreach (string path in scenePaths)
        {
            buildScenes.Add(new EditorBuildSettingsScene(path, true));
        }

        EditorBuildSettings.scenes = buildScenes.ToArray();
        Debug.Log("OPENFEED Build Settings configured with all scenes:\n" +
            "  MainMenu, supermarket, GroceryStore, ForestDrive, MainArea, Desk");
    }

    [MenuItem("OPEN FEED/Project/Create Empty Scenes", false, 30)]
    static void CreateAllScenes()
    {
        // Create GroceryStore and ForestDrive scene files if they don't exist
        CreateSceneIfMissing("Assets/Scenes/GroceryStore.unity");
        CreateSceneIfMissing("Assets/ForestDrive.unity");

        Debug.Log("OPENFEED scene files created. Now run the generators:\n" +
            "  1. Open GroceryStore scene -> OPEN FEED > Store Flow > Generate Scene\n" +
            "  2. OPEN FEED > Forest Drive > Generate & Save Scene\n" +
            "  3. Run: OPEN FEED > Project > Build Settings (All Scenes)");
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
