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
        Open        // drawer, book
    }

    public InteractionType interactionType;
    public string objectName = "";
    public string interactHint = "";

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