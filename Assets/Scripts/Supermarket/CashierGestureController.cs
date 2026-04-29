using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class CashierGestureController : MonoBehaviour
{
    [Header("Wiring")]
    public Animator animator;
    public SupermarketCashierRigPose pose;
    public ClerkHeadLook headLook;

    [Header("Idle state")]
    public string idleState = "Idle";

    [Header("Gesture state names (must match AnimatorController states)")]
    public string[] clickGestures = new string[]
    {
        "Acknowledging", "BeingCocky", "Dismissing", "HappyHand", "HardNod",
        "NodYes", "LengthyNod", "LookAway", "RelievedSigh", "SarcasticNod",
        "ShakingNo", "ThoughtfulShake", "WeightShift", "AnnoyedShake", "AngryGesture"
    };

    [Header("Tuning")]
    public float crossFadeIn = 0.25f;
    public float crossFadeOut = 0.6f;
    public float minHold = 2.5f;
    public float postHold = 0.4f;

    int _lastIndex = -1;
    Coroutine _co;
    public bool IsPlaying => _co != null;

    void Reset()
    {
        animator = GetComponentInChildren<Animator>();
        pose = GetComponent<SupermarketCashierRigPose>();
        headLook = GetComponent<ClerkHeadLook>();
    }

    public void PlayRandom()
    {
        if (clickGestures == null || clickGestures.Length == 0) return;
        int idx = Random.Range(0, clickGestures.Length);
        if (clickGestures.Length > 1 && idx == _lastIndex)
            idx = (idx + 1) % clickGestures.Length;
        _lastIndex = idx;
        Play(clickGestures[idx]);
    }

    public void PlayForLine(string lineKey)
    {
        string state = MapLine(lineKey);
        if (!string.IsNullOrEmpty(state)) Play(state);
        else PlayRandom();
    }

    string MapLine(string key)
    {
        switch (key)
        {
            case "good_evening":      return "Acknowledging";
            case "is_that_all":       return "AnnoyedShake";
            case "are_you_sure":      return "BeingCocky";

            case "anything_else_1":   return "WeightShift";
            case "what_on_it":        return "ThoughtfulShake";
            case "anything_else_2":   return "NodYes";
            case "five_twenty":       return "Dismissing";
            case "long_night":        return "LookAway";
            case "yeah":              return "RelievedSigh";
        }
        return null;
    }

    public void Play(string stateName)
    {
        if (animator == null || string.IsNullOrEmpty(stateName)) return;
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(PlayCo(stateName));
    }

    IEnumerator PlayCo(string stateName)
    {
        animator.CrossFadeInFixedTime(stateName, crossFadeIn, 0);

        int targetHash = Animator.StringToHash(stateName);
        float guard = 0f;
        while (animator.GetCurrentAnimatorStateInfo(0).shortNameHash != targetHash && guard < 0.5f)
        {
            guard += Time.unscaledDeltaTime;
            yield return null;
        }

        var info = animator.GetCurrentAnimatorStateInfo(0);
        float length = info.length > 0.05f ? info.length : 2.0f;
        float hold = Mathf.Max(minHold, length + postHold);
        float t = 0f;
        while (t < hold)
        {
            t += Time.deltaTime;
            yield return null;
        }

        // Smoothly blend back to idle. If a new gesture is requested mid-hold,
        // this coroutine is stopped before reaching here, so the new gesture
        // crossfades in from wherever the body is currently — no snap.
        animator.CrossFadeInFixedTime(idleState, crossFadeOut, 0);
        _co = null;
    }
}
