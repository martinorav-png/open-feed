using UnityEngine;

[DisallowMultipleComponent]
public class MarketAmbience : MonoBehaviour
{
    [Header("Source")]
    public AudioClip clip;
    [Tooltip("Where in the clip (seconds) to start playback. 167 = 02:47.")]
    public float startTime = 167f;

    [Header("Spatial sources")]
    [Tooltip("World positions where ambience emanates from (typically along the fridge wall).")]
    public Vector3[] sourcePositions = new Vector3[]
    {
        new Vector3(1.5f, 1.5f, 32.0f),
        new Vector3(5.0f, 1.5f, 32.0f),
        new Vector3(8.5f, 1.5f, 32.0f),
    };
    [Range(0f, 1f)] public float volumePerSource = 0.18f;
    public float minDistance = 2f;
    public float maxDistance = 14f;
    public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;

    [Header("Reverb")]
    public AudioReverbPreset reverbPreset = AudioReverbPreset.Hallway;
    [Range(0f, 0.4f)] public float fadeInSeconds = 1.5f;

    AudioSource[] _sources;
    bool _built;

    void Awake() { Build(); }

    void Build()
    {
        if (_built) return;
        if (clip == null || sourcePositions == null || sourcePositions.Length == 0) return;
        _sources = new AudioSource[sourcePositions.Length];
        for (int i = 0; i < sourcePositions.Length; i++)
        {
            var go = new GameObject("FridgeAmbience_" + i);
            go.transform.SetParent(transform, false);
            go.transform.position = sourcePositions[i];

            var src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.loop = true;
            src.spatialBlend = 1f;
            src.minDistance = minDistance;
            src.maxDistance = maxDistance;
            src.rolloffMode = rolloffMode;
            src.volume = 0f;
            src.playOnAwake = false;
            src.dopplerLevel = 0f;
            src.reverbZoneMix = 1.1f;

            var rev = go.AddComponent<AudioReverbFilter>();
            rev.reverbPreset = reverbPreset;

            _sources[i] = src;
        }
        _built = true;
    }

    public void Play()
    {
        if (!_built) Build();
        if (_sources == null) return;
        float clipLen = clip != null ? clip.length : 0f;
        float t = Mathf.Clamp(startTime, 0f, Mathf.Max(0f, clipLen - 0.5f));
        foreach (var s in _sources)
        {
            if (s == null || s.clip == null) continue;
            s.time = t;
            s.Play();
        }
        StartCoroutine(FadeIn());
    }

    System.Collections.IEnumerator FadeIn()
    {
        if (_sources == null) yield break;
        float dur = Mathf.Max(0.01f, fadeInSeconds);
        float k = 0f;
        while (k < 1f)
        {
            k += Time.deltaTime / dur;
            float v = Mathf.SmoothStep(0f, volumePerSource, Mathf.Clamp01(k));
            for (int i = 0; i < _sources.Length; i++)
                if (_sources[i] != null) _sources[i].volume = v;
            yield return null;
        }
        for (int i = 0; i < _sources.Length; i++)
            if (_sources[i] != null) _sources[i].volume = volumePerSource;
    }

    public void Stop()
    {
        if (_sources == null) return;
        foreach (var s in _sources) if (s != null) s.Stop();
    }
}
