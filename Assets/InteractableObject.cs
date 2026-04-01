using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    public enum InteractionType
    {
        Squeeze,    // stress ball
        Sip,        // energy drink
        Spin,       // fidget/pen
        Toggle,     // click pen, lighter
        Crumple,    // paper
        Open,       // drawer, book
        PlayNudge   // toy / iDog: sound + small motion
    }

    public InteractionType interactionType;
    public string objectName = "";
    public string interactHint = "";

    [Header("Play Nudge (PlayNudge type)")]
    public AudioClip nudgeSound;
    [Range(0f, 1f)] public float nudgeSoundVolume = 0.65f;
    public float nudgeMoveMeters = 0.035f;
    public float nudgeDuration = 0.22f;

    // State
    [HideInInspector] public bool isAnimating = false;
    [HideInInspector] public Vector3 originalScale;
    [HideInInspector] public Vector3 originalPosition;
    [HideInInspector] public Quaternion originalRotation;
    [HideInInspector] public bool toggleState = false;

    void Awake()
    {
        originalScale = transform.localScale;
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;
    }
}