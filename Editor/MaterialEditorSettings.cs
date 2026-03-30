namespace Aoyon.MaterialEditor;

internal class MaterialEditorSettings
{
    private const string EnableMaterialEditorPatcherKey = "aoyon.material-editor.enable-material-editor-patcher";
    public static bool EnableMaterialEditorPatcher
    {
        get => EditorPrefs.GetBool(EnableMaterialEditorPatcherKey, true);
        set
        {
            if (EnableMaterialEditorPatcher == value) return;
            EditorPrefs.SetBool(EnableMaterialEditorPatcherKey, value);
            EnableMaterialEditorPatcherChanged?.Invoke(value);
        }
    }
    public static event Action<bool>? EnableMaterialEditorPatcherChanged;

    private const string ShowInspectorDescriptionKey = "aoyon.material-editor.show-inspector-description";
    public static bool ShowInspectorDescription
    {
        get => EditorPrefs.GetBool(ShowInspectorDescriptionKey, true);
        set
        {
            if (ShowInspectorDescription == value) return;
            EditorPrefs.SetBool(ShowInspectorDescriptionKey, value);
            ShowInspectorDescriptionChanged?.Invoke(value);
        }
    }
    public static event Action<bool>? ShowInspectorDescriptionChanged;
}