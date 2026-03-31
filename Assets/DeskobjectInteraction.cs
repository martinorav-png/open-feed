using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class DeskObjectInteraction : MonoBehaviour
{
    [Header("Settings")]
    public float interactDistance = 3f;
    public Camera playerCamera;
    private FirstPersonCamera cameraController;

    // Currently hovered
    private InteractableObject hoveredObject = null;

    void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
        if (playerCamera == null)
        {
            Camera[] cams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            if (cams.Length > 0) playerCamera = cams[0];
        }

        if (playerCamera != null)
            cameraController = playerCamera.GetComponent<FirstPersonCamera>();
    }

    void Update()
    {
        if (playerCamera == null) return;

        // Don't interact if monitor browser is active
        MonitorInteraction monitorInt = FindAnyObjectByType<MonitorInteraction>();
        if (monitorInt != null && monitorInt.IsBrowsing()) return;

        // Don't interact if phone is being held
        PhoneInteraction phone = FindAnyObjectByType<PhoneInteraction>();
        if (phone != null)
        {
            var stateField = phone.GetType().GetField("state",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (stateField != null)
            {
                int stateVal = (int)stateField.GetValue(phone);
                if (stateVal != 0) return; // not idle
            }
        }

        // Raycast from screen center
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        hoveredObject = null;

        if (Physics.Raycast(ray, out hit, interactDistance))
        {
            // Search up the entire hierarchy for an InteractableObject
            InteractableObject obj = FindInteractableInParents(hit.transform);
            hoveredObject = obj;
        }

        // Click to interact
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            if (hoveredObject != null && !hoveredObject.isAnimating)
            {
                StartCoroutine(DoInteraction(hoveredObject));
            }
        }
    }

    InteractableObject FindInteractableInParents(Transform t)
    {
        Transform current = t;
        while (current != null)
        {
            InteractableObject obj = current.GetComponent<InteractableObject>();
            if (obj != null) return obj;
            current = current.parent;
        }
        return null;
    }

    IEnumerator DoInteraction(InteractableObject obj)
    {
        obj.isAnimating = true;

        switch (obj.interactionType)
        {
            case InteractableObject.InteractionType.Squeeze:
                yield return StartCoroutine(AnimateSqueeze(obj));
                break;

            case InteractableObject.InteractionType.Sip:
                yield return StartCoroutine(AnimateSip(obj));
                break;

            case InteractableObject.InteractionType.Spin:
                yield return StartCoroutine(AnimateSpin(obj));
                break;

            case InteractableObject.InteractionType.Toggle:
                yield return StartCoroutine(AnimateToggle(obj));
                break;

            case InteractableObject.InteractionType.Crumple:
                yield return StartCoroutine(AnimateCrumple(obj));
                break;

            case InteractableObject.InteractionType.Open:
                yield return StartCoroutine(AnimateOpen(obj));
                break;
        }

        obj.isAnimating = false;
    }

    // ============================
    // STRESS BALL - squish and bounce back
    // ============================
    IEnumerator AnimateSqueeze(InteractableObject obj)
    {
        Transform t = obj.transform;
        Vector3 original = obj.originalScale;
        Vector3 squished = new Vector3(original.x * 1.3f, original.y * 0.6f, original.z * 1.3f);

        Debug.Log("*squeezes stress ball*");

        // Squish down
        float elapsed = 0f;
        float dur = 0.15f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t2 = Mathf.Clamp01(elapsed / dur);
            t.localScale = Vector3.Lerp(original, squished, t2);
            yield return null;
        }

        // Hold squish
        yield return new WaitForSeconds(0.1f);

        // Bounce back (overshoot slightly)
        Vector3 overshoot = new Vector3(original.x * 0.9f, original.y * 1.15f, original.z * 0.9f);
        elapsed = 0f;
        dur = 0.12f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t2 = Mathf.Clamp01(elapsed / dur);
            t.localScale = Vector3.Lerp(squished, overshoot, t2);
            yield return null;
        }

        // Settle
        elapsed = 0f;
        dur = 0.15f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t2 = Mathf.Clamp01(elapsed / dur);
            t.localScale = Vector3.Lerp(overshoot, original, t2);
            yield return null;
        }

        t.localScale = original;
    }

    // ============================
    // ENERGY DRINK - pick up, bring to camera, sip, put back
    // ============================
    IEnumerator AnimateSip(InteractableObject obj)
    {
        Transform t = obj.transform;
        Vector3 origLocalPos = obj.originalPosition;
        Quaternion origLocalRot = obj.originalRotation;
        Transform origParent = t.parent;

        Debug.Log("*picks up energy drink*");

        // Freeze camera look
        if (cameraController != null) cameraController.enabled = false;

        // Parent to camera
        t.SetParent(playerCamera.transform);

        // Current local pos in camera space (will be weird, we lerp from it)
        Vector3 startPos = t.localPosition;
        Quaternion startRot = t.localRotation;

        // Target: centered, close to camera, slightly below center
        Vector3 holdPos = new Vector3(0f, -0.08f, 0.25f);
        Quaternion holdRot = Quaternion.Euler(0, 0, 0);

        // Pick up - bring to camera
        float elapsed = 0f;
        float dur = 0.4f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float s = Mathf.SmoothStep(0, 1, elapsed / dur);
            t.localPosition = Vector3.Lerp(startPos, holdPos, s);
            t.localRotation = Quaternion.Slerp(startRot, holdRot, s);
            yield return null;
        }

        // Brief pause holding
        yield return new WaitForSeconds(0.15f);

        // Tilt to sip
        Vector3 sipPos = holdPos + new Vector3(0, 0.03f, -0.02f);
        Quaternion sipRot = Quaternion.Euler(-35, 0, 8);

        elapsed = 0f;
        dur = 0.3f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float s = Mathf.SmoothStep(0, 1, elapsed / dur);
            t.localPosition = Vector3.Lerp(holdPos, sipPos, s);
            t.localRotation = Quaternion.Slerp(holdRot, sipRot, s);
            yield return null;
        }

        // Hold sip
        yield return new WaitForSeconds(0.5f);

        // Tilt back to hold position
        elapsed = 0f;
        dur = 0.25f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float s = Mathf.SmoothStep(0, 1, elapsed / dur);
            t.localPosition = Vector3.Lerp(sipPos, holdPos, s);
            t.localRotation = Quaternion.Slerp(sipRot, holdRot, s);
            yield return null;
        }

        // Brief pause before putting back
        yield return new WaitForSeconds(0.1f);

        // Re-parent to desk
        t.SetParent(origParent);

        // Lerp back to original desk position
        startPos = t.localPosition;
        startRot = t.localRotation;

        elapsed = 0f;
        dur = 0.35f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float s = Mathf.SmoothStep(0, 1, elapsed / dur);
            t.localPosition = Vector3.Lerp(startPos, origLocalPos, s);
            t.localRotation = Quaternion.Slerp(startRot, origLocalRot, s);
            yield return null;
        }

        t.localPosition = origLocalPos;
        t.localRotation = origLocalRot;

        // Re-enable camera look
        if (cameraController != null) cameraController.enabled = true;

        Debug.Log("*puts drink back*");
    }

    // ============================
    // PEN - spin in place
    // ============================
    IEnumerator AnimateSpin(InteractableObject obj)
    {
        Transform t = obj.transform;

        Debug.Log("*spins pen*");

        float elapsed = 0f;
        float dur = 0.8f;
        float spins = 2f;

        Quaternion startRot = t.localRotation;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / dur;
            float angle = progress * 360f * spins;
            // Ease out
            float ease = 1f - (1f - progress) * (1f - progress);
            t.localRotation = startRot * Quaternion.Euler(0, angle * (1f - ease * 0.5f), 0);
            yield return null;
        }

        t.localRotation = startRot;
    }

    // ============================
    // TOGGLE - click (like a lighter flick or pen click)
    // ============================
    IEnumerator AnimateToggle(InteractableObject obj)
    {
        Transform t = obj.transform;
        obj.toggleState = !obj.toggleState;

        Debug.Log(obj.toggleState ? $"*clicks {obj.objectName} on*" : $"*clicks {obj.objectName} off*");

        // Quick press down
        Vector3 origPos = obj.originalPosition;
        Vector3 pressPos = origPos + new Vector3(0, -0.005f, 0);

        float elapsed = 0f;
        while (elapsed < 0.06f)
        {
            elapsed += Time.deltaTime;
            t.localPosition = Vector3.Lerp(origPos, pressPos, elapsed / 0.06f);
            yield return null;
        }

        // Pop back
        elapsed = 0f;
        while (elapsed < 0.08f)
        {
            elapsed += Time.deltaTime;
            t.localPosition = Vector3.Lerp(pressPos, origPos, elapsed / 0.08f);
            yield return null;
        }

        t.localPosition = origPos;
    }

    // ============================
    // CRUMPLE - shrink slightly and nudge
    // ============================
    IEnumerator AnimateCrumple(InteractableObject obj)
    {
        Transform t = obj.transform;
        Vector3 origScale = obj.originalScale;
        Vector3 origPos = obj.originalPosition;

        Debug.Log("*crumples paper*");

        // Crush smaller
        Vector3 crushed = origScale * 0.8f;
        float randomX = Random.Range(-0.02f, 0.02f);
        float randomZ = Random.Range(-0.02f, 0.02f);
        Vector3 nudgedPos = origPos + new Vector3(randomX, 0, randomZ);

        float elapsed = 0f;
        float dur = 0.2f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float s = Mathf.Clamp01(elapsed / dur);
            t.localScale = Vector3.Lerp(origScale, crushed, s);
            t.localPosition = Vector3.Lerp(origPos, nudgedPos, s);
            yield return null;
        }

        // Bounce back to original size
        elapsed = 0f;
        dur = 0.3f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float s = Mathf.Clamp01(elapsed / dur);
            t.localScale = Vector3.Lerp(crushed, origScale, s);
            yield return null;
        }

        t.localScale = origScale;
        // Keep the nudged position for variety
        obj.originalPosition = nudgedPos;
    }

    // ============================
    // OPEN - tilt lid/cover
    // ============================
    IEnumerator AnimateOpen(InteractableObject obj)
    {
        Transform t = obj.transform;
        obj.toggleState = !obj.toggleState;

        Debug.Log(obj.toggleState ? $"*opens {obj.objectName}*" : $"*closes {obj.objectName}*");

        Quaternion closedRot = obj.originalRotation;
        Quaternion openRot = closedRot * Quaternion.Euler(-45, 0, 0);

        Quaternion from = obj.toggleState ? closedRot : openRot;
        Quaternion to = obj.toggleState ? openRot : closedRot;

        float elapsed = 0f;
        float dur = 0.3f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float s = Mathf.SmoothStep(0, 1, elapsed / dur);
            t.localRotation = Quaternion.Slerp(from, to, s);
            yield return null;
        }

        t.localRotation = to;
    }
}