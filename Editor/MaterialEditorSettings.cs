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
}