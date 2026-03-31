using UnityEngine;

public class WindowCreature : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 0.15f;
    public float startX = -3.5f;
    public float endX = 3.5f;
    public float zPosition = -2.5f;

    [Header("Timing")]
    public float minWaitTime = 30f;
    public float maxWaitTime = 90f;
    public float firstAppearDelay = 15f;

    [Header("Behavior")]
    public bool canPause = true;
    public float pauseChance = 0.3f;
    public float minPauseDuration = 1.5f;
    public float maxPauseDuration = 4f;
    public float headTurnChance = 0.5f;

    // State
    private enum CreatureState { Waiting, Walking, Paused, Turning }
    private CreatureState state = CreatureState.Waiting;
    private float timer = 0f;
    private float targetTime = 0f;
    private bool movingRight = true;
    private bool hasPaused = false;
    private Transform headTransform;
    private Quaternion headOriginalRot;
    private float turnProgress = 0f;
    private bool firstPass = true;

    void Start()
    {
        // Find head for turning
        Transform head = transform.Find("CreatureHead");
        if (head != null)
        {
            headTransform = head;
            headOriginalRot = head.localRotation;
        }

        // Start hidden
        SetVisible(false);
        targetTime = firstAppearDelay;
        state = CreatureState.Waiting;
    }

    void Update()
    {
        timer += Time.deltaTime;

        switch (state)
        {
            case CreatureState.Waiting:
                if (timer >= targetTime)
                {
                    StartWalking();
                }
                break;

            case CreatureState.Walking:
                float step = moveSpeed * Time.deltaTime;
                float newX = transform.position.x + (movingRight ? step : -step);
                transform.position = new Vector3(newX, transform.position.y, zPosition);

                // Check if creature should pause (near window center)
                if (canPause && !hasPaused)
                {
                    float distFromCenter = Mathf.Abs(newX);
                    if (distFromCenter < 0.5f && Random.value < pauseChance * Time.deltaTime)
                    {
                        StartPause();
                        break;
                    }
                }

                // Check if crossed the whole window
                if ((movingRight && newX > endX) || (!movingRight && newX < startX))
                {
                    FinishCrossing();
                }
                break;

            case CreatureState.Paused:
                if (timer >= targetTime)
                {
                    // Maybe turn head toward window before resuming
                    if (headTransform != null && Random.value < headTurnChance)
                    {
                        state = CreatureState.Turning;
                        timer = 0f;
                        targetTime = 2f;
                        turnProgress = 0f;
                    }
                    else
                    {
                        state = CreatureState.Walking;
                        hasPaused = true;
                    }
                }
                break;

            case CreatureState.Turning:
                turnProgress += Time.deltaTime / targetTime;
                float t = turnProgress;

                if (t < 0.3f)
                {
                    // Turn head toward window (toward player)
                    float turnT = t / 0.3f;
                    Quaternion lookIn = headOriginalRot * Quaternion.Euler(0, 90, 0);
                    headTransform.localRotation = Quaternion.Slerp(headOriginalRot, lookIn, turnT * turnT);
                }
                else if (t > 0.7f)
                {
                    // Turn back
                    float turnBack = (t - 0.7f) / 0.3f;
                    Quaternion lookIn = headOriginalRot * Quaternion.Euler(0, 90, 0);
                    headTransform.localRotation = Quaternion.Slerp(lookIn, headOriginalRot, turnBack * turnBack);
                }

                if (t >= 1f)
                {
                    headTransform.localRotation = headOriginalRot;
                    state = CreatureState.Walking;
                    hasPaused = true;
                }
                break;
        }
    }

    void StartWalking()
    {
        // Randomize direction
        movingRight = Random.value > 0.5f;

        // Position off-screen on the starting side
        float startXPos = movingRight ? startX : endX;
        transform.position = new Vector3(startXPos, transform.position.y, zPosition);

        // Slightly randomize speed each pass
        moveSpeed = Random.Range(0.08f, 0.2f);

        SetVisible(true);
        hasPaused = false;
        state = CreatureState.Walking;
        timer = 0f;

        // After first pass, use slower speed for more subtle appearances
        if (!firstPass)
            moveSpeed = Random.Range(0.06f, 0.15f);
    }

    void StartPause()
    {
        state = CreatureState.Paused;
        timer = 0f;
        targetTime = Random.Range(minPauseDuration, maxPauseDuration);
    }

    void FinishCrossing()
    {
        SetVisible(false);
        state = CreatureState.Waiting;
        timer = 0f;
        targetTime = Random.Range(minWaitTime, maxWaitTime);
        firstPass = false;
    }

    void SetVisible(bool visible)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
            r.enabled = visible;
    }
}