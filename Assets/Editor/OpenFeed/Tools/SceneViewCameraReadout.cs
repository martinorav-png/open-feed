#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Shows the active Scene view camera position and rotation while you navigate.
/// OPEN FEED → Tools → Scene View Camera Readout.
/// </summary>
public class SceneViewCameraReadout : EditorWindow
{
    [MenuItem("OPEN FEED/Tools/Scene View Camera Readout", false, 20)]
    static void Open()
    {
        var window = GetWindow<SceneViewCameraReadout>();
        window.titleContent = new GUIContent("Scene Cam Readout");
        window.minSize = new Vector2(320f, 140f);
        window.Show();
    }

    void OnEnable()
    {
        EditorApplication.update += OnEditorUpdate;
    }

    void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }

    void OnEditorUpdate()
    {
        Repaint();
    }

    void OnGUI()
    {
        SceneView sv = SceneView.lastActiveSceneView;
        if (sv == null || sv.camera == null)
        {
            EditorGUILayout.HelpBox("No active Scene view. Click the Scene tab and navigate.", MessageType.Info);
            return;
        }

        Transform t = sv.camera.transform;
        Vector3 pos = t.position;
        Vector3 euler = t.rotation.eulerAngles;

        EditorGUILayout.LabelField("Position (world)", pos.ToString("F4"));
        EditorGUILayout.LabelField("Rotation (euler)", euler.ToString("F2"));
        EditorGUILayout.LabelField("Forward", t.forward.ToString("F4"));

        EditorGUILayout.Space(6f);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Copy Vector3"))
            {
                GUIUtility.systemCopyBuffer =
                    $"new Vector3({pos.x:F4}f, {pos.y:F4}f, {pos.z:F4}f)";
            }

            if (GUILayout.Button("Copy Euler"))
            {
                GUIUtility.systemCopyBuffer =
                    $"new Vector3({euler.x:F2}f, {euler.y:F2}f, {euler.z:F2}f)";
            }
        }

        if (GUILayout.Button("Copy YAML-style (pos + rot)"))
        {
            Quaternion q = t.rotation;
            GUIUtility.systemCopyBuffer =
                $"m_LocalPosition: {{x: {pos.x}, y: {pos.y}, z: {pos.z}}}\n" +
                $"m_LocalRotation: {{x: {q.x}, y: {q.y}, z: {q.z}, w: {q.w}}}";
        }
    }
}
#endif
