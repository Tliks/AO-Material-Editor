using Aoyon.MaterialEditor.Processor;

namespace Aoyon.MaterialEditor.UI;

internal static class MaterialEditorGenerator
{
    private const string UndoMessage = "Create " + Constants.DisplayName;

    public static void Generate(IEnumerable<GameObject> selectedObjects)
    {
       var targetRenderers = selectedObjects
            .SelectMany(s => s.GetComponents<Renderer>())
            .Where(r => MaterialEditorProcessor.IsTargetRenderer(r))
            .ToArray();
        
        if (targetRenderers.Length == 0)
        {
            Debug.LogWarning(string.Format("generator.noTargetRenderers.warning".LS(), Constants.DisplayName));
            return;
        }

        var targetMaterials = targetRenderers
            .SelectMany(r => r.sharedMaterials)
            .SkipDestroyed()
            .Distinct()
            .ToArray();
        
        if (targetMaterials.Length == 0)
        {
            Debug.LogWarning(string.Format("generator.noTargetMaterials.warning".LS(), Constants.DisplayName));
            return;
        }
        
        var parent = targetRenderers[0].transform.parent;
        if (targetMaterials.Length > 1) {
            var transform = new GameObject("Material").transform;
            transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(transform.gameObject, UndoMessage);
            parent = transform;
        }

        GameObject firstCreated = null!;
        
        foreach (var material in targetMaterials)
        {
            var entry = GenerateImpl(material);
            entry.transform.SetParent(parent, false);
            if (firstCreated == null) firstCreated = entry;
        }

        EditorGUIUtility.PingObject(firstCreated);
        Selection.activeGameObject = firstCreated;
    }

    public static void Generate(Material material, Transform? parent = null)
    {
        var entry = GenerateImpl(material);

        if (parent != null) entry.transform.SetParent(parent, false);

        EditorGUIUtility.PingObject(entry);
        Selection.activeGameObject = entry;
    }

    public static GameObject GenerateImpl(Material material)
    {
        var entry = new GameObject(material.name);
        var component = entry.AddComponent<MaterialEditorComponent>();
        component.TargetSettings.Mode = MaterialTargetSettings.SelectionMode.SingleMaterial;
        component.TargetSettings.SingleMaterial.TargetMaterial = material;
        Undo.RegisterCreatedObjectUndo(entry, UndoMessage);
        return entry;
    }
}
