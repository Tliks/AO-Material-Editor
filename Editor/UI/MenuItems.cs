using Aoyon.MaterialEditor.Migration;
using System.IO;
using UnityEditorInternal;

namespace Aoyon.MaterialEditor.UI;

internal static class MenuItems
{
    // GameObject
    private const string GameObjectPath = "GameObject/" + Constants.DisplayName;

    private const string GeneratePath = GameObjectPath; // エントリーポイントなので階層を浅く
    private const int GeneratePriority = 100;

    [MenuItem(GeneratePath, true, GeneratePriority)]
    private static bool ValidateGenerate()
    {
        return Selection.objects.OfType<GameObject>().Any();
    }

    [MenuItem(GeneratePath, false, GeneratePriority)]
    private static void Generate()
    {
        var selection = Selection.objects.OfType<GameObject>();;
        MaterialEditorGenerator.Generate(selection);
    }

    // Tools
    private const string ToolsPath = "Tools/" + Constants.DisplayName;

    private const string SettingsPath = ToolsPath + "/Settings";

    private const string EnableMaterialEditorPatcherPath = SettingsPath + "/Enable Material Editor Patcher";

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

    private const string ShowInspectorDescriptionPath = SettingsPath + "/Show Inspector Description";

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
    private const string ContextPath = "CONTEXT";

    private const string MaterialContextPath = ContextPath + "/" + nameof(Material);

    private const string GenerateContextPath = MaterialContextPath + "/" + Constants.DisplayName;
    private const int GenerateContextPriority = 300;

    [MenuItem(GenerateContextPath, true, GenerateContextPriority)]
    static bool ValidateGenerateContext(MenuCommand command)
    {
        return command.context is Material;
    }

    [MenuItem(GenerateContextPath, false, GenerateContextPriority)]
    static void GenerateContext(MenuCommand command)
    {
        var material = (Material)command.context;
        var parent = Selection.activeGameObject?.transform?.parent;
        MaterialEditorGenerator.Generate(material, parent);
    }

    private const string ComponentContextPath = ContextPath + "/" + nameof(MaterialEditorComponent);

    private const string MigratePath = ComponentContextPath + "/Migrate";
    private const string CreateRecordingMaterialPath = ComponentContextPath + "/Create Recording Material";

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
        if (component == null) throw new Exception($"{nameof(MaterialEditorComponent)} not found");
        Migrator.Migrate(component);
    }

    [MenuItem(CreateRecordingMaterialPath, true)]
    static bool ValidateCreateRecordingMaterial(MenuCommand command)
    {
        var component = command.context as MaterialEditorComponent;
        return component != null && MaterialEditoEditorContext.TryGetRecordingMaterial(component, out _);
    }

    [MenuItem(CreateRecordingMaterialPath, false)]
    static void CreateRecordingMaterial(MenuCommand command)
    {
        var component = command.context as MaterialEditorComponent;
        if (component == null) throw new Exception($"{nameof(MaterialEditorComponent)} not found");
        if (!MaterialEditoEditorContext.TryGetRecordingMaterial(component, out var recordingMaterial))
        {
            throw new Exception("Recording material not found");
        }

        var path = EditorUtility.SaveFilePanelInProject(
            "Create Recording Material",
            $"{component.gameObject.name} Recording.mat",
            "mat",
            "Select a path for the generated material asset");
        if (string.IsNullOrEmpty(path)) return;

        var material = new Material(recordingMaterial)
        {
            name = Path.GetFileNameWithoutExtension(path),
        };

        AssetDatabase.CreateAsset(material, path);
        AssetDatabase.SaveAssets();
        EditorGUIUtility.PingObject(material);
        Selection.activeObject = material;
    }
}
