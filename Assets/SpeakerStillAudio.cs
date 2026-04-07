using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Desk speakers (Cube.009 / Cube.010): crosshair click toggles Still.mp3 from Resources — play → pause → resume.
/// </summary>
[DefaultExecutionOrder(-25)]
public class SpeakerStillAudio : MonoBehaviour
{
    const string ResourcesClipPath = "Still";

    static readonly string[] SpeakerObjectNames = { "Cube.009", "Cube.010" };

    [SerializeField] float interactDistance = 3f;
    [SerializeField] [Range(0f, 1f)] float playbackVolume = 0.5f;
    [SerializeField] float spatialMinDistance = 0.65f;
    [SerializeField] float spatialMaxDistance = 16f;
    [Tooltip("Degrees; lower = more directional.")]
    [SerializeField] float spatialSpread = 38f;

    Camera playerCamera;
    AudioSource playbackSource;
    AudioClip clip;

    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            Camera[] cams = FindObjectsByType<Camera>();
            if (cams.Length > 0)
                playerCamera = cams[0];
        }

        clip = Resources.Load<AudioClip>(ResourcesClipPath);
        if (clip == null)
            Debug.LogError($"SpeakerStillAudio: No AudioClip at Resources/{ResourcesClipPath} (expected Assets/Audio/Resources/Still.mp3).");

        Transform sp0 = FindNamedObjectInLoadedScenes(SpeakerObjectNames[0])?.transform;
        Transform sp1 = FindNamedObjectInLoadedScenes(SpeakerObjectNames[1])?.transform;

        Vector3 worldPos;
        Transform attachParent;
        if (sp0 != null && sp1 != null)
        {
            worldPos = (sp0.position + sp1.position) * 0.5f;
            attachParent = sp0.parent != null ? sp0.parent : sp0;
        }
        else if (sp0 != null)
        {
            worldPos = sp0.position;
            attachParent = sp0.parent != null ? sp0.parent : sp0;
        }
        else if (sp1 != null)
        {
            worldPos = sp1.position;
            attachParent = sp1.parent != null ? sp1.parent : sp1;
        }
        else
        {
            worldPos = transform.position;
            attachParent = transform;
        }

        GameObject host = new GameObject("StillTrackAudio");
        host.transform.position = worldPos;
        host.transform.SetParent(attachParent, true);
        playbackSource = host.AddComponent<AudioSource>();
        playbackSource.playOnAwake = false;
        playbackSource.loop = true;
        playbackSource.volume = playbackVolume;
        playbackSource.spatialBlend = 1f;
        playbackSource.minDistance = spatialMinDistance;
        playbackSource.maxDistance = spatialMaxDistance;
        playbackSource.rolloffMode = AudioRolloffMode.Logarithmic;
        playbackSource.dopplerLevel = 0f;
        playbackSource.spread = spatialSpread;

        foreach (string speakerName in SpeakerObjectNames)
            EnsureSpeakerCollider(FindNamedObjectInLoadedScenes(speakerName));
    }

    static GameObject FindNamedObjectInLoadedScenes(string objectName)
    {
        foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            Transform found = FindChildRecursive(root.transform, objectName);
            if (found != null)
                return found.gameObject;
        }

        GameObject direct = GameObject.Find(objectName);
        return direct;
    }

    static Transform FindChildRecursive(Transform parent, string objectName)
    {
        if (parent.name == objectName)
            return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform r = FindChildRecursive(parent.GetChild(i), objectName);
            if (r != null)
                return r;
        }

        return null;
    }

    static void EnsureSpeakerCollider(GameObject speaker)
    {
        if (speaker == null)
        {
            Debug.LogWarning("SpeakerStillAudio: speaker object not found in scene.");
            return;
        }

        if (speaker.GetComponent<Collider>() != null)
            return;

        MeshFilter mf = speaker.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            var box = speaker.AddComponent<BoxCollider>();
            box.center = mf.sharedMesh.bounds.center;
            box.size = mf.sharedMesh.bounds.size;
            return;
        }

        speaker.AddComponent<BoxCollider>();
    }

    void Update()
    {
        if (playerCamera == null || clip == null || playbackSource == null)
            return;

        MonitorInteraction monitor = FindAnyObjectByType<MonitorInteraction>();
        if (monitor != null && monitor.IsZoomed())
            return;

        PhoneInteraction phone = FindAnyObjectByType<PhoneInteraction>();
        if (phone != null && phone.IsActive())
            return;

        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out RaycastHit hit, interactDistance))
            return;

        if (!IsSpeakerTransform(hit.transform))
            return;

        playbackSource.clip = clip;

        if (playbackSource.isPlaying)
        {
            playbackSource.Pause();
            return;
        }

        if (playbackSource.time < 0.001f)
            playbackSource.Play();
        else
            playbackSource.UnPause();
    }

    public static bool IsSpeakerTransform(Transform t)
    {
        Transform c = t;
        while (c != null)
        {
            for (int i = 0; i < SpeakerObjectNames.Length; i++)
            {
                if (c.name == SpeakerObjectNames[i])
                    return true;
            }

            c = c.parent;
        }

        return false;
    }
}
