using System.Collections;
using UnityEngine;

/// <summary>
/// House scene: keep the fragmented biped hidden until the i-dog is clicked, then show it at a fixed
/// world pose and run it away with a skeletal run clip on the Animator.
/// </summary>
public class HouseWindowPeekScare : MonoBehaviour
{
    const string CreatureName = "Meshy_AI_Fragmented_Form_biped_Character_output";
    const string IdogName = "RBX_irobotdog_r1";

    public Transform creature;

    [Header("Appear (world space)")]
    public Vector3 spawnWorldPosition = new Vector3(17.75806f, 1.868998f, 14.89784f);
    public Quaternion spawnWorldRotation = new Quaternion(-0.00165768f, 0.9393029f, -0.00242253f, -0.3430763f);

    [Header("Flee")]
    [Tooltip("How far to move along the character forward axis while fleeing.")]
    public float fleeDistanceMeters = 5.5f;
    public float preFleeHoldSeconds = 0.12f;
    public float fleeSeconds = 1.05f;

    [Header("Animator (humanoid / skinned mesh)")]
    public RuntimeAnimatorController runAnimatorController;
    public string runStateName = "Run";
    public float runAnimatorSpeed = 1.05f;

    Renderer[] renderers;
    Animator animator;
    bool running;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreateIfPresent()
    {
        if (GameObject.Find(CreatureName) == null || GameObject.Find(IdogName) == null)
            return;

        if (FindAnyObjectByType<HouseWindowPeekScare>() != null)
            return;

        var go = new GameObject("HouseWindowPeekScare_Controller");
        go.AddComponent<HouseWindowPeekScare>();
    }

    void Awake()
    {
        ResolveReferences();
        CacheCreatureParts();
        HideCreature();
    }

    void Start()
    {
        ResolveReferences();
        CacheCreatureParts();
        HideCreature();
    }

    public static void TriggerFromIdog(InteractableObject obj)
    {
        if (obj == null || !obj.name.Contains(IdogName))
            return;

        var scare = FindAnyObjectByType<HouseWindowPeekScare>();
        if (scare == null)
        {
            var go = new GameObject("HouseWindowPeekScare_Controller");
            scare = go.AddComponent<HouseWindowPeekScare>();
        }

        scare.Trigger();
    }

    public void Trigger()
    {
        if (running)
            return;

        ResolveReferences();
        CacheCreatureParts();
        if (creature == null)
            return;

        StartCoroutine(AppearAndFlee());
    }

    IEnumerator AppearAndFlee()
    {
        running = true;

        Quaternion rot = spawnWorldRotation.normalized;
        Vector3 start = spawnWorldPosition;
        Vector3 end = start + rot * Vector3.forward * fleeDistanceMeters;

        creature.SetPositionAndRotation(start, rot);
        creature.gameObject.SetActive(true);
        SetRenderersVisible(true);
        ConfigureAnimatorForRun();

        if (preFleeHoldSeconds > 0f)
            yield return new WaitForSeconds(preFleeHoldSeconds);

        yield return MoveCreature(start, end, fleeSeconds, false);

        HideCreature();
        running = false;
    }

    void ConfigureAnimatorForRun()
    {
        if (animator == null)
            return;

        animator.enabled = true;
        if (runAnimatorController != null)
            animator.runtimeAnimatorController = runAnimatorController;

        animator.applyRootMotion = false;
        animator.speed = runAnimatorSpeed;

        if (!string.IsNullOrEmpty(runStateName) && animator.runtimeAnimatorController != null)
            animator.CrossFadeInFixedTime(runStateName, 0.12f, 0, 0f);
    }

    IEnumerator MoveCreature(Vector3 from, Vector3 to, float seconds, bool easeOut)
    {
        float elapsed = 0f;
        seconds = Mathf.Max(0.01f, seconds);

        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / seconds);
            float eased = easeOut ? 1f - Mathf.Pow(1f - t, 3f) : t * t;
            creature.position = Vector3.LerpUnclamped(from, to, eased);
            yield return null;
        }

        creature.position = to;
    }

    void ResolveReferences()
    {
        if (creature == null)
        {
            var foundCreature = GameObject.Find(CreatureName);
            if (foundCreature != null)
                creature = foundCreature.transform;
        }
    }

    void CacheCreatureParts()
    {
        if (creature == null)
            return;

        renderers = creature.GetComponentsInChildren<Renderer>(true);
        animator = creature.GetComponentInChildren<Animator>(true);
    }

    void HideCreature()
    {
        if (creature == null)
            return;

        creature.SetPositionAndRotation(spawnWorldPosition, spawnWorldRotation.normalized);
        SetRenderersVisible(false);
        creature.gameObject.SetActive(false);

        if (animator != null)
            animator.enabled = false;
    }

    void SetRenderersVisible(bool visible)
    {
        if (renderers == null)
            return;

        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
                renderer.enabled = visible;
        }
    }
}
