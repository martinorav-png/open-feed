using System.Collections;
using UnityEngine;

public class StoreFlowIntroController : MonoBehaviour
{
    [Header("Rig")]
    public StoreFirstPersonController playerController;
    public Camera playerCamera;

    [Header("Views")]
    public Transform menuView;
    public Transform carSeatView;
    public Transform carExitView;
    public Transform exploreView;
    public bool overrideMenuViewPose;
    public Vector3 menuViewOverridePosition;
    public Vector3 menuViewOverrideEuler;
    public bool overrideCarSeatViewPose;
    public Vector3 carSeatViewOverridePosition;
    public Vector3 carSeatViewOverrideEuler;

    [Header("Doors")]
    public Transform leftDoor;
    public Transform rightDoor;
    public Vector3 leftDoorClosedLocalPosition;
    public Vector3 leftDoorOpenLocalPosition;
    public Vector3 rightDoorClosedLocalPosition;
    public Vector3 rightDoorOpenLocalPosition;
    public StoreEntranceLock entranceLock;
    public bool lockEntranceAfterIntro = true;

    [Header("Car Side Door")]
    public Transform carSideDoor;
    public string carSideDoorName = "doorleftsmd";
    public float carSideDoorOpenAngleX = -55f;
    public float carSideDoorOpenDuration = 0.5f;
    public float carSideDoorCloseDuration = 0.45f;
    public float carSideDoorOpenHoldTime = 0.2f;

    [Header("Timing")]
    public float menuToCarDuration = 1.2f;
    public float exitCarDuration = 1.0f;
    public float walkToStoreDuration = 3.2f;
    public float holdInCarTime = 0.75f;
    public float sideDoorPauseTime = 0.35f;
    public float beforeDoorCloseDelay = 0.35f;
    public float doorAnimDuration = 0.65f;

    bool hasStarted;
    Quaternion carDoorClosedRotation;
    Quaternion carDoorOpenRotation;

    void Start()
    {
        ApplyViewOverrides();
        ResolveCarSideDoor();

        if (playerController != null)
        {
            playerController.SetControlEnabled(false);
            playerController.SetCinematicMode(true);

            if (menuView != null)
                playerController.SetPose(menuView.position, menuView.rotation);
            else if (carSeatView != null)
                playerController.SetPose(carSeatView.position, carSeatView.rotation);
        }

        EnsureEntranceLock();
        if (entranceLock != null)
            entranceLock.UnlockEntrance();

        SetDoorOpenAmount(0f);
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

        Coroutine carDoorOpen = null;
        if (carSideDoor != null)
            carDoorOpen = StartCoroutine(AnimateCarSideDoor(true, carSideDoorOpenDuration));

        if (carSeatView != null && carExitView != null)
            yield return StartCoroutine(MoveRig(carSeatView, carExitView, exitCarDuration));

        if (carDoorOpen != null)
            yield return carDoorOpen;

        if (carSideDoor != null && carSideDoorOpenHoldTime > 0f)
            yield return new WaitForSeconds(carSideDoorOpenHoldTime);

        if (sideDoorPauseTime > 0f)
            yield return new WaitForSeconds(sideDoorPauseTime);

        if (carSideDoor != null)
            yield return StartCoroutine(AnimateCarSideDoor(false, carSideDoorCloseDuration));

        Coroutine openDoors = StartCoroutine(AnimateDoors(true, doorAnimDuration));

        if (carExitView != null && exploreView != null)
            yield return StartCoroutine(MoveRig(carExitView, exploreView, walkToStoreDuration));

        if (openDoors != null)
            yield return openDoors;

        if (beforeDoorCloseDelay > 0f)
            yield return new WaitForSeconds(beforeDoorCloseDelay);
        yield return StartCoroutine(AnimateDoors(false, doorAnimDuration));

        if (lockEntranceAfterIntro && entranceLock != null)
            entranceLock.LockEntrance();

        if (exploreView != null)
            playerController.SetPose(exploreView.position, exploreView.rotation);

        playerController.SetCinematicMode(false);
        playerController.SetControlEnabled(true);

        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.FinishIntroToStore();
    }

    void EnsureEntranceLock()
    {
        if (entranceLock == null && leftDoor != null && leftDoor.parent != null)
            entranceLock = leftDoor.parent.GetComponent<StoreEntranceLock>();

        if (entranceLock == null && leftDoor != null && leftDoor.parent != null)
            entranceLock = leftDoor.parent.gameObject.AddComponent<StoreEntranceLock>();

        if (entranceLock != null)
            entranceLock.ConfigureUsingDoors(leftDoor, rightDoor);
    }

    void ApplyViewOverrides()
    {
        if (overrideMenuViewPose && menuView != null)
            menuView.SetPositionAndRotation(menuViewOverridePosition, Quaternion.Euler(menuViewOverrideEuler));

        if (overrideCarSeatViewPose && carSeatView != null)
            carSeatView.SetPositionAndRotation(carSeatViewOverridePosition, Quaternion.Euler(carSeatViewOverrideEuler));
    }

    void ResolveCarSideDoor()
    {
        if (carSideDoor == null && !string.IsNullOrEmpty(carSideDoorName))
        {
            GameObject doorObj = GameObject.Find(carSideDoorName);
            if (doorObj == null)
                doorObj = FindByNameRecursive(carSideDoorName, transform, false);
            if (doorObj == null)
                doorObj = FindByNameRecursive(carSideDoorName, transform, true);

            if (doorObj != null)
                carSideDoor = doorObj.transform;
        }

        if (carSideDoor != null)
        {
            carDoorClosedRotation = carSideDoor.localRotation;
            carDoorOpenRotation = carDoorClosedRotation * Quaternion.Euler(carSideDoorOpenAngleX, 0f, 0f);
        }
    }

    IEnumerator AnimateCarSideDoor(bool open, float duration)
    {
        if (carSideDoor == null)
            yield break;

        Quaternion from = open ? carDoorClosedRotation : carDoorOpenRotation;
        Quaternion to = open ? carDoorOpenRotation : carDoorClosedRotation;

        if (duration <= 0f)
        {
            carSideDoor.localRotation = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smooth = t * t * (3f - 2f * t);
            carSideDoor.localRotation = Quaternion.Slerp(from, to, smooth);
            yield return null;
        }

        carSideDoor.localRotation = to;
    }

    static GameObject FindByNameRecursive(string targetName, Transform root, bool allowContains)
    {
        if (root == null)
            return null;

        foreach (Transform child in root)
        {
            bool isMatch = string.Equals(child.name, targetName, System.StringComparison.OrdinalIgnoreCase);
            if (!isMatch && allowContains)
                isMatch = child.name.ToLowerInvariant().Contains(targetName.ToLowerInvariant());

            if (isMatch)
                return child.gameObject;

            GameObject nested = FindByNameRecursive(targetName, child, allowContains);
            if (nested != null)
                return nested;
        }

        return null;
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

    IEnumerator AnimateDoors(bool open, float duration)
    {
        float start = open ? 0f : 1f;
        float end = open ? 1f : 0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smooth = t * t * (3f - 2f * t);
            SetDoorOpenAmount(Mathf.Lerp(start, end, smooth));
            yield return null;
        }

        SetDoorOpenAmount(end);
    }

    void SetDoorOpenAmount(float amount)
    {
        if (leftDoor != null)
            leftDoor.localPosition = Vector3.Lerp(leftDoorClosedLocalPosition, leftDoorOpenLocalPosition, amount);
        if (rightDoor != null)
            rightDoor.localPosition = Vector3.Lerp(rightDoorClosedLocalPosition, rightDoorOpenLocalPosition, amount);
    }
}
