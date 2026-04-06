using nadena.dev.ndmf.preview;

namespace Aoyon.MaterialEditor;

internal static class NDMFUtility
{
    public static bool EditorOnlyInHierarchy(this ComputeContext context, GameObject obj)
    {
        const string editorOnlyTag = "EditorOnly";

        foreach (var node in context.ObservePath(obj.transform))
        {
            var isEditorOnly = context.Observe(node.gameObject, g => g.CompareTag(editorOnlyTag), (a, b) => a == b);
            if (isEditorOnly) return true;
        }

        return false;
    }
}
