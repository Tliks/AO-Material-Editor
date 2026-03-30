namespace Aoyon.MaterialEditor.UI;

// based on https://github.com/lilxyzw/lilycalInventory/blob/52763ab539d59609e63d6974493948ab0614f7c2/Editor/Helper/GUIHelper.cs
internal static partial class GUIHelper
{
    public readonly record struct FoldoutOptions(
        bool Draw = true,
        bool RectStrict = false,
        bool ToggleOnLabelClick = true,
        Func<Rect, SerializedProperty, Rect>? GetClickableRect = null)
    {
        public FoldoutOptions() : this(true, false, true, null) { }
    }

    // 汎用的なFoldout
    public static bool Foldout(
        Rect position,
        SerializedProperty property,
        GUIContent content,
        FoldoutOptions? options = null)
    {
        var resolvedOptions = options ?? new FoldoutOptions();
        var drawFoldout = resolvedOptions.Draw;
        using var scope = new EditorGUI.PropertyScope(position, content, property);
        var label = scope.content;
        if (drawFoldout)
        {
            var clickableRect = resolvedOptions.GetClickableRect != null ? resolvedOptions.GetClickableRect(position, property) : position;
            DrawFoldout(clickableRect, property, label, resolvedOptions.ToggleOnLabelClick, resolvedOptions.RectStrict);
        }
        else
        {
            // Foldoutを描画しない場合は普通にラベルを表示
            EditorGUI.LabelField(position, label);
            return true;
        }
        return property.isExpanded;
    }

    // EditorGUILayout用
    public static bool Foldout(
        SerializedProperty prop,
        GUIContent content,
        FoldoutOptions? options = null)
    {
        return Foldout(EditorGUILayout.GetControlRect(), prop, content, options);
    }

    private static bool DrawFoldout(Rect position, SerializedProperty property, GUIContent label, bool toggleOnLabelClick, bool rectStrict)
    {
        bool isExpanded;
        if (rectStrict)
        {
            var prevHierarchy = EditorGUIUtility.hierarchyMode;
            var prevIndent = EditorGUI.indentLevel;
            EditorGUIUtility.hierarchyMode = false;
            EditorGUI.indentLevel = 0;
            try { isExpanded = EditorGUI.Foldout(position, property.isExpanded, label, toggleOnLabelClick); }
            finally
            {
                EditorGUI.indentLevel = prevIndent;
                EditorGUIUtility.hierarchyMode = prevHierarchy;
            }
        }
        else
        {
            isExpanded = EditorGUI.Foldout(position, property.isExpanded, label, toggleOnLabelClick);
        }

        ApplyExpandedState(property, isExpanded);
        return property.isExpanded;
    }

    private static void ApplyExpandedState(SerializedProperty property, bool isExpanded)
    {
        if (property.isExpanded == isExpanded) return;

        // altキーが押されている場合は再帰的に開く
        if (Event.current.alt) SetExpandedRecurse(property, isExpanded);
        else property.isExpanded = isExpanded;
    }

    // 再帰的にFoldoutを開く
    private static void SetExpandedRecurse(SerializedProperty property, bool expanded)
    {
        using var iter = property.Copy();
        iter.isExpanded = expanded;
        int depth = iter.depth;
        bool visitChild = true;
        while(iter.NextVisible(visitChild) && iter.depth > depth)
        {
            visitChild = iter.propertyType != SerializedPropertyType.String;
            if(iter.hasVisibleChildren) iter.isExpanded = expanded;
        }
    }
}
