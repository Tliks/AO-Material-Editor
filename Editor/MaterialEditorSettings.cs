namespace Aoyon.MaterialEditor;

internal class MaterialEditorSettings
{
    private const string EnableMaterialEditorPatcherKey = Constants.QualifiedName + ".enable-material-editor-patcher";
    private const bool EnableMaterialEditorPatcherDefault = true;
    public static bool EnableMaterialEditorPatcher
    {
        get => EditorPrefs.GetBool(EnableMaterialEditorPatcherKey, EnableMaterialEditorPatcherDefault);
        set
        {
            if (EnableMaterialEditorPatcher == value) return;
            EditorPrefs.SetBool(EnableMaterialEditorPatcherKey, value);
            EnableMaterialEditorPatcherChanged?.Invoke(value);
        }
    }
    public static event Action<bool>? EnableMaterialEditorPatcherChanged;

    private const string ShowInspectorDescriptionKey = Constants.QualifiedName + ".show-inspector-description";
    private const bool ShowInspectorDescriptionDefault = true;
    public static bool ShowInspectorDescription
    {
        get => EditorPrefs.GetBool(ShowInspectorDescriptionKey, ShowInspectorDescriptionDefault);
        set
        {
            if (ShowInspectorDescription == value) return;
            EditorPrefs.SetBool(ShowInspectorDescriptionKey, value);
            ShowInspectorDescriptionChanged?.Invoke(value);
        }
    }
    public static event Action<bool>? ShowInspectorDescriptionChanged;
}
