using System;
using UnityEngine;

/// <summary>
/// JSON shape from OPEN FEED → Tools → Scene Camera Path Recorder (openfeed-scene-camera-path-v1).
/// Consumed at runtime by SupermarketDriveInIntro when playing a recorded drive path.
/// </summary>
[Serializable]
public class OpenFeedDriveCameraPathSample
{
    public float t;
    public float px, py, pz;
    public float qx, qy, qz, qw;
    public float eulerX, eulerY, eulerZ;
    public float fov;
}

[Serializable]
public class OpenFeedDriveCameraPathFile
{
    public string format;
    public string sceneName;
    public string unityVersion;
    public float recordedDurationSeconds;
    public float sampleIntervalSeconds;
    public OpenFeedDriveCameraPathSample[] samples;
}
