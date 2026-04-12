using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Tools → Radio Subtitle Editor
/// Fill in your subtitle entries, then hit "Copy to Clipboard" and paste the result to Claude.
/// </summary>
public class RadioSubtitleEditor : EditorWindow
{
    [MenuItem("Tools/Radio Subtitle Editor")]
    static void Open() => GetWindow<RadioSubtitleEditor>("Radio Subtitles");

    class Entry
    {
        public float time;
        public string text = "";
        public float displayDuration = 2.5f;
    }

    readonly List<Entry> _entries = new List<Entry>();
    Vector2 _scroll;

    void OnGUI()
    {
        EditorGUILayout.LabelField("AlpineRadio subtitle entries", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("'Time' = seconds after the player clicks the radio.", EditorStyles.miniLabel);
        EditorGUILayout.Space(6);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        for (int i = 0; i < _entries.Count; i++)
        {
            Entry e = _entries[i];

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"#{i + 1}", GUILayout.Width(28));
            EditorGUILayout.LabelField("Time (s)", GUILayout.Width(58));
            e.time = EditorGUILayout.FloatField(e.time, GUILayout.Width(60));
            EditorGUILayout.LabelField("Duration (s)", GUILayout.Width(80));
            e.displayDuration = EditorGUILayout.FloatField(e.displayDuration, GUILayout.Width(60));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                _entries.RemoveAt(i);
                break;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Text");
            e.text = EditorGUILayout.TextField(e.text);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(6);

        if (GUILayout.Button("+ Add Entry", GUILayout.Height(28)))
        {
            float nextTime = _entries.Count > 0
                ? _entries[_entries.Count - 1].time + _entries[_entries.Count - 1].displayDuration + 1f
                : 0f;
            _entries.Add(new Entry { time = nextTime });
        }

        EditorGUILayout.Space(4);

        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.5f);
        if (GUILayout.Button("Copy to Clipboard", GUILayout.Height(36)))
            CopyToClipboard();
        GUI.backgroundColor = Color.white;
    }

    void CopyToClipboard()
    {
        if (_entries.Count == 0)
        {
            Debug.LogWarning("No subtitle entries to copy.");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("Radio subtitle entries:");
        sb.AppendLine();

        foreach (Entry e in _entries)
            sb.AppendLine($"  time={e.time}s | duration={e.displayDuration}s | \"{e.text}\"");

        GUIUtility.systemCopyBuffer = sb.ToString();
        Debug.Log("Radio subtitle entries copied to clipboard.");
    }
}
