#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Records the active Scene view camera path while you navigate in the editor.
/// Export JSON to share with tooling / AI for mapping intro or cinematic paths.
/// OPEN FEED → Tools → Scene Camera Path Recorder
/// </summary>
public class SceneCameraPathRecorder : EditorWindow
{
    const string DefaultExportDir = "tools/camera-path-recordings";

    [Serializable]
    class SampleJson
    {
        public float t;
        public float px, py, pz;
        public float qx, qy, qz, qw;
        public float eulerX, eulerY, eulerZ;
        public float fov;
    }

    [Serializable]
    class RecordingJson
    {
        public string format = "openfeed-scene-camera-path-v1";
        public string sceneName;
        public string unityVersion;
        public float recordedDurationSeconds;
        public float sampleIntervalSeconds;
        public SampleJson[] samples;
    }

    bool _recording;
    float _recordStartRealtime;
    float _lastSampleTime;
    float _sampleInterval = 1f / 30f;
    readonly List<SampleJson> _buffer = new List<SampleJson>(512);
    string _status = "Idle.";
    Vector2 _scroll;

    [MenuItem("OPEN FEED/Tools/Scene Camera Path Recorder", false, 10)]
    static void Open()
    {
        var w = GetWindow<SceneCameraPathRecorder>();
        w.titleContent = new GUIContent("Scene Path Recorder");
        w.minSize = new Vector2(360f, 280f);
        w.Show();
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
        if (!_recording)
            return;

        SceneView sv = SceneView.lastActiveSceneView;
        if (sv == null || sv.camera == null)
            return;

        float now = (float)EditorApplication.timeSinceStartup;
        float elapsed = now - _recordStartRealtime;
        if (elapsed - _lastSampleTime < _sampleInterval)
            return;

        _lastSampleTime = elapsed;
        Camera cam = sv.camera;
        Transform tr = cam.transform;
        Quaternion q = tr.rotation;
        Vector3 e = q.eulerAngles;

        _buffer.Add(new SampleJson
        {
            t = elapsed,
            px = tr.position.x,
            py = tr.position.y,
            pz = tr.position.z,
            qx = q.x,
            qy = q.y,
            qz = q.z,
            qw = q.w,
            eulerX = e.x,
            eulerY = e.y,
            eulerZ = e.z,
            fov = cam.fieldOfView
        });

        Repaint();
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Records the active Scene view camera while you fly the scene.", EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space(4f);

        _sampleInterval = EditorGUILayout.Slider("Min interval (sec)", _sampleInterval, 1f / 120f, 0.5f);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = !_recording;
            if (GUILayout.Button("Start recording", GUILayout.Height(28f)))
                StartRecording();

            GUI.enabled = _recording;
            if (GUILayout.Button("Stop", GUILayout.Height(28f)))
                StopRecording();

            GUI.enabled = true;
        }

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(_status, EditorStyles.wordWrappedMiniLabel);

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField($"Samples in buffer: {_buffer.Count}");

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Clear buffer"))
            {
                _buffer.Clear();
                _status = "Buffer cleared.";
            }

            GUI.enabled = _buffer.Count > 0;
            if (GUILayout.Button("Save JSON…"))
                SaveJsonToFile();

            if (GUILayout.Button("Copy JSON"))
                CopyJsonToClipboard();
            GUI.enabled = true;
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("How to send the path", EditorStyles.boldLabel);
        _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(100f));
        EditorGUILayout.LabelField(
            "1. Open the scene you care about, click the Scene tab, and navigate while recording.\n" +
            "2. Stop, then Save JSON (repo: tools/camera-path-recordings/) or Copy JSON.\n" +
            "3. Paste the file contents or attach the .json in chat — position + quaternion + time per keyframe.",
            EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.EndScrollView();
    }

    void StartRecording()
    {
        if (SceneView.lastActiveSceneView == null)
        {
            _status = "No Scene view — open the Scene tab first.";
            return;
        }

        _buffer.Clear();
        _recording = true;
        float now = (float)EditorApplication.timeSinceStartup;
        _recordStartRealtime = now;
        _lastSampleTime = -_sampleInterval;
        _status = "Recording… move/fly in Scene view.";
    }

    void StopRecording()
    {
        _recording = false;
        _status = _buffer.Count == 0
            ? "Stopped — no samples (interval too large or no Scene view?)."
            : $"Stopped — {_buffer.Count} samples.";
    }

    RecordingJson BuildPayload()
    {
        var scene = SceneManager.GetActiveScene();
        return new RecordingJson
        {
            sceneName = scene.IsValid() ? scene.name : "(no scene)",
            unityVersion = Application.unityVersion,
            recordedDurationSeconds = _buffer.Count > 0 ? _buffer[_buffer.Count - 1].t : 0f,
            sampleIntervalSeconds = _sampleInterval,
            samples = _buffer.ToArray()
        };
    }

    string Serialize()
    {
        var payload = BuildPayload();
        return JsonUtility.ToJson(payload, true);
    }

    void SaveJsonToFile()
    {
        string scene = SceneManager.GetActiveScene().name;
        if (string.IsNullOrEmpty(scene))
            scene = "UnknownScene";

        string safeScene = string.Join("_", scene.Split(Path.GetInvalidFileNameChars()));
        string fileName = $"{safeScene}_path_{DateTime.Now:yyyyMMdd_HHmmss}.json";

        string projectRoot = Path.GetDirectoryName(Application.dataPath) ?? ".";
        string defaultDir = Path.Combine(projectRoot, DefaultExportDir);
        if (!Directory.Exists(defaultDir))
            Directory.CreateDirectory(defaultDir);

        string path = EditorUtility.SaveFilePanel(
            "Save camera path JSON",
            defaultDir,
            fileName,
            "json");

        if (string.IsNullOrEmpty(path))
            return;

        try
        {
            File.WriteAllText(path, Serialize());
            AssetDatabase.Refresh();
            _status = $"Saved: {path}";
        }
        catch (Exception e)
        {
            _status = "Save failed: " + e.Message;
        }
    }

    void CopyJsonToClipboard()
    {
        GUIUtility.systemCopyBuffer = Serialize();
        _status = "JSON copied to clipboard.";
    }
}
#endif
