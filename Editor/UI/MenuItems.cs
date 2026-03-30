using UnityEditorInternal;

namespace Aoyon.MaterialEditor.UI;

internal static class MenuItems
{
    // GameObject
    private const string GameObjectPath = "GameObject/AO Material Editor";

    private const string GeneratePath = GameObjectPath; // エントリーポイントなので階層を浅く
    private const int GeneratePriority = 100;

    [MenuItem(GeneratePath, true, GeneratePriority)]
    private static bool ValidateGenerate()
    {
        return Selection.activeGameObject != null;
    }

    [MenuItem(GeneratePath, false, GeneratePriority)]
    private static void Generate()
    {
        var selected = Selection.activeGameObject;
        if (selected == null) throw new Exception("No selected game object");

        var materials = Utils.GetAllTargetMaterials(selected).ToList();
        if (materials.Count == 0) throw new Exception("No materials found");

        var root = new GameObject("Material Editor");
        root.transform.SetParent(selected.transform, false);

        GameObject? firstChild = null;
        foreach (var material in materials)
        {
            var child = new GameObject(material.name);
            if (firstChild == null) firstChild = child;
            child.transform.SetParent(root.transform, false);
            var component = child.AddComponent<MaterialEditorComponent>();
            component.TargetSettings.Mode = MaterialTargetSettings.SelectionMode.SingleMaterial;
            component.TargetSettings.SingleMaterial.TargetMaterial = material;
        }

        Undo.RegisterCreatedObjectUndo(root, "Create AO Material Editor");

        // GUIHelper.SetHierarchyExpanded(root, true);
        EditorGUIUtility.PingObject(firstChild);
        Selection.activeGameObject = firstChild;
    }

    // Tools
    private const string ToolsPath = "Tools/AO Material Editor";

    private const string EnableMaterialEditorPatcherPath = ToolsPath + "/Enable Material Editor Patcher";

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
        InternalEditorUtility.RepaintAllViews();
    }

    private const string ShowInspectorDescriptionPath = ToolsPath + "/Show Inspector Description";

    [MenuItem(ShowInspectorDescriptionPath, true)]
    private static bool ValidateShowInspectorDescription()
    {
        Menu.SetChecked(ShowInspectorDescriptionPath, MaterialEditorSettings.ShowInspectorDescription);
        return true;
    }

    [MenuItem(ShowInspectorDescriptionPath, false)]
    private static void ToggleShowInspectorDescription()
    {
        MaterialEditorSettings.ShowInspectorDescription = !MaterialEditorSettings.ShowInspectorDescription;
        InternalEditorUtility.RepaintAllViews();
    }

}