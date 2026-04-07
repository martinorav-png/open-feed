using UnityEngine;
using UnityEditor;

public class SceneExporter : Editor
{
    [MenuItem("OPEN FEED/Tools/Export Scene to Clipboard", false, 0)]
    static void ExportScene()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        // Get all root objects in the scene
        GameObject[] roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

        foreach (GameObject root in roots)
        {
            ExportTransform(root.transform, sb, 0);
        }

        GUIUtility.systemCopyBuffer = sb.ToString();
        Debug.Log($"Scene data exported to clipboard ({sb.Length} chars). Paste it anywhere.");
    }

    static void ExportTransform(Transform t, System.Text.StringBuilder sb, int depth)
    {
        string indent = new string(' ', depth * 2);
        Vector3 p = t.localPosition;
        Vector3 r = t.localEulerAngles;
        Vector3 s = t.localScale;

        sb.AppendLine($"{indent}[{t.name}]");
        sb.AppendLine($"{indent}  pos: ({p.x:F4}, {p.y:F4}, {p.z:F4})");
        sb.AppendLine($"{indent}  rot: ({r.x:F1}, {r.y:F1}, {r.z:F1})");
        sb.AppendLine($"{indent}  scl: ({s.x:F4}, {s.y:F4}, {s.z:F4})");

        // Log material if has renderer
        Renderer rend = t.GetComponent<Renderer>();
        if (rend != null && rend.sharedMaterial != null)
            sb.AppendLine($"{indent}  mat: {rend.sharedMaterial.name}");

        // Log mesh type
        MeshFilter mf = t.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
            sb.AppendLine($"{indent}  mesh: {mf.sharedMesh.name}");

        // Log collider
        Collider col = t.GetComponent<Collider>();
        if (col != null)
        {
            if (col is BoxCollider box)
                sb.AppendLine($"{indent}  boxcol: center({box.center.x:F3},{box.center.y:F3},{box.center.z:F3}) size({box.size.x:F3},{box.size.y:F3},{box.size.z:F3})");
            else
                sb.AppendLine($"{indent}  col: {col.GetType().Name}");
        }

        // Log light
        Light light = t.GetComponent<Light>();
        if (light != null)
            sb.AppendLine($"{indent}  light: {light.type} color({light.color.r:F2},{light.color.g:F2},{light.color.b:F2}) int:{light.intensity:F2} range:{light.range:F1}");

        // Log InteractableObject
        var interactable = t.GetComponent<InteractableObject>();
        if (interactable != null)
            sb.AppendLine($"{indent}  interactable: {interactable.interactionType} \"{interactable.objectName}\"");

        // Children
        foreach (Transform child in t)
        {
            ExportTransform(child, sb, depth + 1);
        }
    }
}