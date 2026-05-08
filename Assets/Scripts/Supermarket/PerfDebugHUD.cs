using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Drop into the supermarket2 scene; it logs scene + pipeline state once after 1.5s of play
/// and shows a tiny live FPS counter in the top-left corner. Use it to compare two play paths.
/// </summary>
[DisallowMultipleComponent]
public class PerfDebugHUD : MonoBehaviour
{
    [Tooltip("When off, skips the one-shot console dump (can hitch / pause the Editor on large projects).")]
    public bool logPerformanceSnapshot;

    [Tooltip("Seconds to wait after entering play before logging the snapshot.")]
    public float logDelay = 1.5f;

    float _smoothedDt = 0.016f;

    void Start()
    {
        if (logPerformanceSnapshot)
            Invoke(nameof(LogSnapshot), logDelay);
    }

    void Update()
    {
        _smoothedDt = Mathf.Lerp(_smoothedDt, Time.unscaledDeltaTime, 0.05f);
    }

    void LogSnapshot()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== PERF SNAPSHOT (entry path: " +
            (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name) + ") ===");

        var rp = GraphicsSettings.currentRenderPipeline;
        sb.AppendLine("Active URP asset: " + (rp != null ? rp.name : "NULL"));
        sb.AppendLine("Quality level: " + QualitySettings.GetQualityLevel() + " ('" + QualitySettings.names[QualitySettings.GetQualityLevel()] + "')");

        var cams = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        sb.AppendLine("Total cameras: " + cams.Length + " — enabled: " + cams.Count(c => c.enabled));
        foreach (var c in cams)
        {
            if (!c.enabled) continue;
            sb.AppendLine("  CAM '" + c.name + "' depth=" + c.depth + " mask=0x" + c.cullingMask.ToString("X") + " scene=" + c.gameObject.scene.name);
        }

        var listeners = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        sb.AppendLine("AudioListeners (enabled): " + listeners.Count(l => l.enabled));

        var srcs = Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int playing = srcs.Count(s => s != null && s.isPlaying);
        sb.AppendLine("AudioSources total: " + srcs.Length + " — playing: " + playing);

        // Volume + post processing on cameras
        var volumes = Object.FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        sb.AppendLine("Volumes in scene: " + volumes.Length);
        foreach (var v in volumes)
            sb.AppendLine("  vol '" + v.name + "' isGlobal=" + v.isGlobal + " weight=" + v.weight + " profile=" + (v.sharedProfile ? v.sharedProfile.name : "null"));

        sb.AppendLine("Lights total: " + Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length);
        sb.AppendLine("Renderers total: " + Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length);

        // Frame-pacing settings
        sb.AppendLine("vSyncCount: " + QualitySettings.vSyncCount + "  targetFrameRate: " + Application.targetFrameRate + "  maxQueuedFrames: " + QualitySettings.maxQueuedFrames);
        sb.AppendLine("Time.timeScale: " + Time.timeScale + "  fixedDeltaTime: " + Time.fixedDeltaTime + "  maxDelta: " + Time.maximumDeltaTime);
        sb.AppendLine("Screen current: " + Screen.width + "x" + Screen.height + "  fullscreen: " + Screen.fullScreen);
        sb.AppendLine("Display.main: " + Display.main.renderingWidth + "x" + Display.main.renderingHeight);

        // Cumulative MonoBehaviour counts that matter for Update tick
        var mbs = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        sb.AppendLine("Active MonoBehaviours: " + mbs.Length);

        // Active SkinnedMeshRenderers (skinning cost)
        var smrs = Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        long skinTris = 0; foreach (var s in smrs) { if (s != null && s.sharedMesh != null) skinTris += s.sharedMesh.triangles.Length / 3; }
        sb.AppendLine("Active SkinnedMeshRenderers: " + smrs.Length + " (skinned tris: " + skinTris + ")");

        // PSX-specific: render features state on the active URP renderer
        var rpAsset = GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
        if (rpAsset != null)
        {
            var rdField = rpAsset.GetType().GetField("m_RendererDataList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var data = rdField?.GetValue(rpAsset) as UnityEngine.Rendering.Universal.ScriptableRendererData[];
            if (data != null)
            {
                foreach (var rd in data)
                {
                    if (rd == null) continue;
                    foreach (var rf in rd.rendererFeatures)
                    {
                        if (rf != null) sb.AppendLine("  RF '" + rf.GetType().Name + "' isActive=" + rf.isActive);
                    }
                }
            }
            sb.AppendLine("  URP renderScale=" + rpAsset.renderScale + "  perObjectLightLimit=" + rpAsset.maxAdditionalLightsCount);
        }

        // DontDestroyOnLoad GameObjects (heuristic: scene name = "DontDestroyOnLoad")
        var allGos = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var ddolRoots = allGos.Where(g => g.scene.name == "DontDestroyOnLoad" && g.transform.parent == null).ToList();
        sb.AppendLine("DontDestroyOnLoad roots: " + ddolRoots.Count);
        foreach (var g in ddolRoots) sb.AppendLine("  DDOL '" + g.name + "'");

        Debug.Log(sb.ToString());
    }

    void OnGUI()
    {
        GUI.color = Color.green;
        float fps = 1f / Mathf.Max(1e-5f, _smoothedDt);
        GUI.Label(new Rect(8, 8, 320, 22), "FPS " + Mathf.RoundToInt(fps) + "   (" + (_smoothedDt * 1000f).ToString("F1") + " ms)");
    }
}
