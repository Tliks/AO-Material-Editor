using System;
using System.Collections.Generic;
using nadena.dev.ndmf.preview;

namespace Aoyon.MaterialEditor;

internal static class NDMFUtility
{
    public static bool EditorOnlyInHierarchy(this ComputeContext context, GameObject obj)
    {
        const string editorOnlyTag = "EditorOnly";
        var current = obj;
        while (current != null)
        {
            var isEditorOnly = context.Observe(current, g => g.CompareTag(editorOnlyTag), (a, b) => a == b);
            if (isEditorOnly) return true;
            var parent = context.Observe(current, g => g.transform.parent, (a, b) => a == b);
            current = parent != null ? parent.gameObject : null;
        }
        return false;
    }
}
