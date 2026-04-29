using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FootstepAudio : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("Long footstep loop. Sliced into one-shots based on transient detection.")]
    public AudioClip sourceClip;

    [Header("Slicing")]
    [Tooltip("Window length used to compute envelope energy, in seconds.")]
    public float energyWindow = 0.012f;
    [Tooltip("Energy ratio above the running average required to count as a transient.")]
    public float peakThreshold = 2.6f;
    [Tooltip("Minimum spacing between detected steps, in seconds.")]
    public float minStepSpacing = 0.22f;
    [Tooltip("Samples kept before each detected peak, in seconds.")]
    public float preRoll = 0.04f;
    [Tooltip("Total length of each sliced one-shot, in seconds.")]
    public float sliceLength = 0.32f;

    [Header("Playback")]
    public float walkStepsPerSecond = 2.0f;
    public float sprintStepsPerSecond = 2.6f;
    [Range(0f, 1f)] public float volume = 0.55f;
    [Range(0f, 0.5f)] public float pitchVariance = 0.12f;

    CharacterController _cc;
    AudioSource _src;
    AudioClip[] _clips;
    float _phase;
    int _lastIndex = -1;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _src = gameObject.AddComponent<AudioSource>();
        _src.playOnAwake = false;
        _src.spatialBlend = 0f;
        BuildClips();
    }

    void BuildClips()
    {
        if (sourceClip == null) { _clips = System.Array.Empty<AudioClip>(); return; }

        int channels = sourceClip.channels;
        int sampleRate = sourceClip.frequency;
        int totalSamples = sourceClip.samples;
        var raw = new float[totalSamples * channels];
        if (!sourceClip.GetData(raw, 0))
        {
            Debug.LogWarning("[FootstepAudio] GetData failed; clip may not be DecompressOnLoad/PCM.");
            _clips = System.Array.Empty<AudioClip>();
            return;
        }

        // Mono envelope
        var mono = new float[totalSamples];
        for (int i = 0; i < totalSamples; i++)
        {
            float s = 0f;
            for (int c = 0; c < channels; c++) s += raw[i * channels + c];
            mono[i] = Mathf.Abs(s / channels);
        }

        int win = Mathf.Max(8, Mathf.RoundToInt(energyWindow * sampleRate));
        var env = new float[totalSamples];
        float acc = 0f;
        for (int i = 0; i < totalSamples; i++)
        {
            acc += mono[i];
            if (i >= win) acc -= mono[i - win];
            env[i] = acc / win;
        }

        int longWin = Mathf.Max(win * 8, Mathf.RoundToInt(0.5f * sampleRate));
        var avg = new float[totalSamples];
        acc = 0f;
        for (int i = 0; i < totalSamples; i++)
        {
            acc += env[i];
            if (i >= longWin) acc -= env[i - longWin];
            avg[i] = acc / Mathf.Min(longWin, i + 1);
        }

        int spacing = Mathf.Max(1, Mathf.RoundToInt(minStepSpacing * sampleRate));
        var peaks = new List<int>();
        int lastPeak = -spacing;
        for (int i = 1; i < totalSamples - 1; i++)
        {
            if (env[i] < avg[i] * peakThreshold) continue;
            if (i - lastPeak < spacing) continue;
            int look = Mathf.Min(win, totalSamples - i - 1);
            float maxV = env[i];
            int maxI = i;
            for (int j = -look; j <= look; j++)
            {
                int k = i + j;
                if (k < 0 || k >= totalSamples) continue;
                if (env[k] > maxV) { maxV = env[k]; maxI = k; }
            }
            if (maxI != i) continue;
            peaks.Add(i);
            lastPeak = i;
        }

        if (peaks.Count == 0)
        {
            int n = Mathf.Max(1, Mathf.FloorToInt(sourceClip.length / sliceLength));
            int len = totalSamples / n;
            int pre = Mathf.RoundToInt(preRoll * sampleRate);
            for (int i = 0; i < n; i++) peaks.Add(Mathf.Min(totalSamples - 1, i * len + pre));
        }

        int sliceSamples = Mathf.Max(64, Mathf.RoundToInt(sliceLength * sampleRate));
        int preSamples = Mathf.RoundToInt(preRoll * sampleRate);
        int fadeOut = Mathf.RoundToInt(sliceSamples * 0.18f);
        var clips = new List<AudioClip>(peaks.Count);
        var slice = new float[sliceSamples * channels];
        for (int p = 0; p < peaks.Count; p++)
        {
            int start = Mathf.Max(0, peaks[p] - preSamples);
            int copyN = Mathf.Min(sliceSamples, totalSamples - start);
            System.Array.Clear(slice, 0, slice.Length);
            for (int i = 0; i < copyN; i++)
            {
                for (int c = 0; c < channels; c++)
                    slice[i * channels + c] = raw[(start + i) * channels + c];
            }
            for (int i = copyN - fadeOut; i < copyN; i++)
            {
                if (i < 0) continue;
                float k = 1f - (i - (copyN - fadeOut)) / (float)fadeOut;
                for (int c = 0; c < channels; c++) slice[i * channels + c] *= k;
            }
            var clip = AudioClip.Create("Footstep_" + p, sliceSamples, channels, sampleRate, false);
            clip.SetData(slice, 0);
            clips.Add(clip);
        }
        _clips = clips.ToArray();
        Debug.Log($"[FootstepAudio] Sliced {_clips.Length} footsteps from {sourceClip.name}.");
    }

    void Update()
    {
        if (_cc == null || !_cc.isGrounded) { _phase = 0.5f; return; }
        Vector3 v = _cc.velocity; v.y = 0f;
        float speed = v.magnitude;
        if (speed < 0.2f) { _phase = 0.5f; return; }

        bool sprinting = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
        float rate = sprinting ? sprintStepsPerSecond : walkStepsPerSecond;
        _phase += Time.deltaTime * rate;
        if (_phase >= 1f) { _phase -= 1f; PlayStep(); }
    }

    void PlayStep()
    {
        if (_clips == null || _clips.Length == 0) return;
        int idx = Random.Range(0, _clips.Length);
        if (_clips.Length > 1 && idx == _lastIndex) idx = (idx + 1) % _clips.Length;
        _lastIndex = idx;
        _src.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
        _src.PlayOneShot(_clips[idx], volume);
    }
}
