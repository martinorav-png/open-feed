using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class HousePlayerSpawnBootstrap : MonoBehaviour
{
    public StoreFirstPersonController controller;
    public Camera playerCamera;
    public Vector3 spawnCameraPosition = new Vector3(17.9f, 1.62f, -2.6f);
    public Vector3 spawnCameraEuler = new Vector3(4f, 225f, 0f);

    IEnumerator Start()
    {
        if (controller == null)
            controller = FindAnyObjectByType<StoreFirstPersonController>();

        if (playerCamera == null)
            playerCamera = Camera.main;

        yield return null;

        if (controller != null)
        {
            controller.SetCinematicMode(false);
            controller.SetPose(spawnCameraPosition, Quaternion.Euler(spawnCameraEuler));
            controller.SetControlEnabled(true);
        }
    }
}
