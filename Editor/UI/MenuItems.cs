using Aoyon.MaterialEditor.Migration;
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
        MaterialEditorGenerator.Generate(selected);
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

    // CONTEXT
    private const string ContextPath = "CONTEXT/" + nameof(MaterialEditorComponent) + "/";

    private const string MigratePath = ContextPath + "Migrate";

    [MenuItem(MigratePath, true)]
    static bool ValidateMigrate(MenuCommand command)
    {
        var component = command.context as MaterialEditorComponent;
        if (component == null) return false;
        return !component.IsLatestDataVersion();
    }
    
    [MenuItem(MigratePath, false)]
    static void Migrate(MenuCommand command)
    {
        var component = command.context as MaterialEditorComponent;
        if (component == null) throw new Exception("MaterialEditorComponent not found");
        Migrator.Migrate(component);
    }
}
