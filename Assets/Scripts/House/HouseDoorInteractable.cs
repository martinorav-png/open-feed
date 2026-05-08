using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class HouseDoorInteractable : MonoBehaviour
{
    public Transform doorVisual;
    public Transform hingePivot;
    public float openAngle = -92f;
    public float duration = 0.7f;
    public AudioClip openSound;
    public AudioClip closeSound;
    [Range(0f, 1f)] public float soundVolume = 0.65f;

    bool _isOpen;
    bool _isAnimating;
    bool _rotateVisualAroundHinge;
    Vector3 _closedWorldPosition;
    Vector3 _openWorldPosition;
    Quaternion _closedWorldRotation;
    Quaternion _openWorldRotation;
    Quaternion _closedLocalRotation;
    Quaternion _openLocalRotation;
    AudioSource _audio;

    public bool IsBusy => _isAnimating;

    void Awake()
    {
        if (doorVisual == null)
            doorVisual = transform;

        if (hingePivot == null)
            hingePivot = transform;

        _rotateVisualAroundHinge = doorVisual != hingePivot && !doorVisual.IsChildOf(hingePivot);
        if (_rotateVisualAroundHinge)
        {
            Quaternion swing = Quaternion.AngleAxis(openAngle, hingePivot.up);
            _closedWorldPosition = doorVisual.position;
            _closedWorldRotation = doorVisual.rotation;
            _openWorldPosition = hingePivot.position + swing * (doorVisual.position - hingePivot.position);
            _openWorldRotation = swing * doorVisual.rotation;
        }
        else
        {
            _closedLocalRotation = hingePivot.localRotation;
            _openLocalRotation = _closedLocalRotation * Quaternion.Euler(0f, openAngle, 0f);
        }

        _audio = hingePivot.GetComponent<AudioSource>();
        if (_audio == null)
            _audio = hingePivot.gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake = false;
        _audio.spatialBlend = 1f;
        _audio.volume = soundVolume;
    }

    public void Toggle()
    {
        if (_isAnimating)
            return;

        StopAllCoroutines();
        StartCoroutine(AnimateDoor(!_isOpen));
    }

    IEnumerator AnimateDoor(bool open)
    {
        _isAnimating = true;

        Quaternion from = _rotateVisualAroundHinge ? doorVisual.rotation : hingePivot.localRotation;
        Quaternion to = open
            ? (_rotateVisualAroundHinge ? _openWorldRotation : _openLocalRotation)
            : (_rotateVisualAroundHinge ? _closedWorldRotation : _closedLocalRotation);
        Vector3 fromPosition = _rotateVisualAroundHinge ? doorVisual.position : Vector3.zero;
        Vector3 toPosition = open ? _openWorldPosition : _closedWorldPosition;
        TryPlay(open ? openSound : closeSound, open);

        float elapsed = 0f;
        float d = Mathf.Max(0.05f, duration);
        while (elapsed < d)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / d));
            if (_rotateVisualAroundHinge)
            {
                doorVisual.SetPositionAndRotation(
                    Vector3.Lerp(fromPosition, toPosition, t),
                    Quaternion.Slerp(from, to, t));
            }
            else
            {
                hingePivot.localRotation = Quaternion.Slerp(from, to, t);
            }
            yield return null;
        }

        if (_rotateVisualAroundHinge)
            doorVisual.SetPositionAndRotation(toPosition, to);
        else
            hingePivot.localRotation = to;
        _isOpen = open;
        _isAnimating = false;
    }

    void TryPlay(AudioClip clip, bool opening)
    {
        if (_audio == null)
            return;

        if (clip != null)
        {
            _audio.PlayOneShot(clip, soundVolume);
            return;
        }

        _audio.PlayOneShot(CreateDoorTone(opening), soundVolume * 0.45f);
    }

    static AudioClip CreateDoorTone(bool opening)
    {
        int sampleRate = 22050;
        float duration = opening ? 0.18f : 0.13f;
        int samples = Mathf.Max(1, Mathf.RoundToInt(sampleRate * duration));
        var data = new float[samples];
        float freq = opening ? 115f : 85f;
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float env = Mathf.Exp(-t * 7f);
            data[i] = Mathf.Sin(t * Mathf.PI * 2f * freq) * env * 0.28f;
        }

        var clip = AudioClip.Create(opening ? "HouseDoorOpenTone" : "HouseDoorCloseTone", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
