namespace Aoyon.MaterialEditor.UI;

// based on https://github.com/lilxyzw/lilycalInventory/blob/52763ab539d59609e63d6974493948ab0614f7c2/Editor/Helper/GUIHelper.cs
internal static partial class GUIHelper
{
    // 汎用的なFoldout
    public static bool Foldout(Rect position, SerializedProperty property, GUIContent content, bool drawFoldout = true, bool toggleOnLabelClick = true, Func<Rect, SerializedProperty, Rect>? getClickableRect = null)
    {
        using var scope = new EditorGUI.PropertyScope(position, content, property);
        var label = scope.content;
        if(drawFoldout)
        {
            var clickableRect = getClickableRect != null ? getClickableRect(position, property) : position;
            PropertyFoldout(clickableRect, property, label);
        }
        else
        {
            // Foldoutを描画しない場合は普通にラベルを表示
            EditorGUI.LabelField(position, label);
            property.isExpanded = true;
        }
        return property.isExpanded;
    }

    // EditorGUILayout用
    public static bool Foldout(SerializedProperty prop, GUIContent content, bool drawFoldout = true, bool toggleOnLabelClick = true, Func<Rect, SerializedProperty, Rect>? getClickableRect = null)
    {
        return Foldout(EditorGUILayout.GetControlRect(), prop, content, drawFoldout, toggleOnLabelClick, getClickableRect);
    }

    // Foldoutの三角形の部分だけ
    public static bool FoldoutOnly(Rect position, SerializedProperty property)
    {
        position.width = 12;
        position.height = propertyHeight;
        position.x -= 12;
        var label = EditorGUI.BeginProperty(position, GUIContent.none, property);
        position.x += 12;
        PropertyFoldout(position, property, label);
        EditorGUI.EndProperty();
        return property.isExpanded;
    }

    private static bool PropertyFoldout(Rect position, SerializedProperty property, GUIContent label)
    {
        var isExpanded = EditorGUI.Foldout(position, property.isExpanded, label, true);
        if(property.isExpanded != isExpanded)
        {
            // altキーが押されている場合は再帰的に開く
            if(Event.current.alt) SetExpandedRecurse(property, isExpanded);
            else property.isExpanded = isExpanded;
        }
        return isExpanded;
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