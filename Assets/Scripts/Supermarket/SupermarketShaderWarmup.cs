using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Walks every Renderer in the scene at startup and forces Unity to compile and upload
/// all shader variants used by their materials. Eliminates the first-encounter stutter
/// that makes FPS feel half what it should be when supermarket2 is the boot scene.
/// Runs once, distributed across a few frames so the warmup itself doesn't hitch.
/// </summary>
[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public class SupermarketShaderWarmup : MonoBehaviour
{
    [Tooltip("How many materials to warm per frame (higher = faster warmup, bigger first-second hitch).")]
    [SerializeField] int materialsPerFrame = 200;

    [Tooltip("If true, also calls Shader.WarmupAllShaders once. Heavier but catches everything.")]
    [SerializeField] bool warmupAll = false;

    void Start()
    {
        StartCoroutine(WarmRoutine());
    }

    IEnumerator WarmRoutine()
    {
        // Wait one frame so all Awakes/Starts complete.
        yield return null;

        if (warmupAll)
        {
            Shader.WarmupAllShaders();
            yield return null;
        }

        var collected = new HashSet<Material>();
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (r == null) continue;
            foreach (var m in r.sharedMaterials)
            {
                if (m == null) continue;
                collected.Add(m);
            }
        }

        int processed = 0;
        foreach (var mat in collected)
        {
            if (mat == null || mat.shader == null) continue;
            // Force a small CreateGPUProgram-trigger by querying shader properties / running a tiny dummy pass.
            // Most reliable: force a basic Pass setup via Material.SetPass which compiles the variant for this material's keywords.
            if (mat.passCount > 0)
            {
                try { mat.SetPass(0); } catch { /* ignore */ }
            }
            processed++;
            if (processed % materialsPerFrame == 0)
                yield return null;
        }

        Debug.Log("[SupermarketShaderWarmup] Warmed " + processed + " materials.");
    }
}
