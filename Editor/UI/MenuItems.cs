namespace Aoyon.MaterialEditor.UI;

internal static class MenuItems
{
    // GameObject

    private const string GeneratePath = "GameObject/AO Material Editor/Generate";
    private const int GeneratePriority = 100;

    [MenuItem(GeneratePath, false, GeneratePriority)]
    private static void Generate()
    {
        var selected = Selection.activeGameObject;
        if (selected == null) throw new Exception("No selected game object");

        var materials = Utils.GetTargetMaterials(selected);
        if (materials.Count == 0) throw new Exception("No materials found");

        var root = new GameObject("AO Material Editor");
        root.transform.SetParent(selected.transform, false);

        foreach (var material in materials)
        {
            var child = new GameObject(material.name);
            child.transform.SetParent(root.transform, false);
            var component = child.AddComponent<MaterialEditorComponent>();
            var entrySettings = component.EntrySettings;
            entrySettings.Mode = MaterialEntrySettings.ApplyMode.Basic;
            entrySettings.BasicMaterial = material;
        }

        Undo.RegisterCreatedObjectUndo(root, "Create AO Material Editor");

        EditorGUIUtility.PingObject(root);
    }

    // Tools

    private const string EnableMaterialEditorPatcherPath = "Tools/AO Material Editor/Enable Material Editor Patcher";

    [MenuItem(EnableMaterialEditorPatcherPath, true)]
    private static bool ValidateEnableMaterialEditorPatcher()
    {
        Menu.SetChecked(EnableMaterialEditorPatcherPath, MaterialEditorSettings.EnableMaterialEditorPatcher);
        return true;
    }

    [MenuItem(EnableMaterialEditorPatcherPath, false)]
    private static void ToggleMaterialEditorPatcher()
    {
        MaterialEditorSettings.EnableMaterialEditorPatcher = !MaterialEditorSettings.EnableMaterialEditorPatcher;
    }
}