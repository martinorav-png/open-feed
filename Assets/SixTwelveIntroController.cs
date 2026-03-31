using System.Collections;
using UnityEngine;

public class SixTwelveIntroController : MonoBehaviour
{
    [Header("Rig")]
    public StoreFirstPersonController playerController;
    public Camera playerCamera;

    [Header("Poses")]
    public Transform menuView;
    public Transform carSeatView;
    public Transform carExitView;
    public Transform storeEntranceView;

    [Header("Timing")]
    public float menuToCarDuration = 1.2f;
    public float exitCarDuration = 1.6f;
    public float walkToStoreDuration = 3.6f;
    public float holdInCarTime = 0.9f;

    bool hasStarted;

    void Start()
    {
        if (playerController != null && menuView != null)
        {
            playerController.SetControlEnabled(false);
            playerController.SetCinematicMode(true);
            playerController.SetPose(menuView.position, menuView.rotation);
        }
    }

    public bool BeginIntroSequence()
    {
        if (hasStarted || playerController == null)
            return false;

        hasStarted = true;
        StopAllCoroutines();
        StartCoroutine(PlayIntro());
        return true;
    }

    IEnumerator PlayIntro()
    {
        playerController.SetControlEnabled(false);
        playerController.SetCinematicMode(true);

        if (menuView != null && carSeatView != null)
            yield return StartCoroutine(MoveRig(menuView, carSeatView, menuToCarDuration));

        yield return new WaitForSeconds(holdInCarTime);

        if (carSeatView != null && carExitView != null)
            yield return StartCoroutine(MoveRig(carSeatView, carExitView, exitCarDuration));

        if (carExitView != null && storeEntranceView != null)
            yield return StartCoroutine(MoveRig(carExitView, storeEntranceView, walkToStoreDuration));

        if (storeEntranceView != null)
            playerController.SetPose(storeEntranceView.position, storeEntranceView.rotation);

        playerController.SetCinematicMode(false);
        playerController.SetControlEnabled(true);

        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.FinishIntroToStore();
    }

    IEnumerator MoveRig(Transform from, Transform to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smooth = t * t * (3f - 2f * t);

            Vector3 pos = Vector3.Lerp(from.position, to.position, smooth);
            Quaternion rot = Quaternion.Slerp(from.rotation, to.rotation, smooth);
            playerController.SetPose(pos, rot);
            yield return null;
        }

        playerController.SetPose(to.position, to.rotation);
    }
}
