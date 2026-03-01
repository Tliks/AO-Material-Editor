namespace Aoyon.MaterialEditor.UI;

// https://github.com/lilxyzw/lilycalInventory/blob/52763ab539d59609e63d6974493948ab0614f7c2/Editor/Helper/GUIHelper.cs
internal static partial class GUIHelper
{
    private static readonly float GUI_SPACE = EditorGUIUtility.standardVerticalSpacing;
    internal static readonly float propertyHeight = EditorGUIUtility.singleLineHeight;

    // 汎用的なFoldout
    private static bool Foldout(Rect position, SerializedProperty property, bool drawFoldout, GUIContent content)
    {
        var label = EditorGUI.BeginProperty(position, content, property);
        if(drawFoldout)
        {
            // Foldoutを描画する場合は左にずらして位置調整
            var rect = new Rect(position);
            if(EditorGUIUtility.hierarchyMode) rect.xMin -=  EditorStyles.foldout.padding.left - EditorStyles.label.padding.left;
            if(Event.current.type == EventType.Repaint) EditorStyles.foldoutHeader.Draw(rect, false, false, property.isExpanded, false);
            PropertyFoldout(position, property, label);
        }
        else
        {
            // Foldoutを描画しない場合は普通にラベルを表示
            EditorGUI.LabelField(position, label);
            property.isExpanded = true;
        }
        EditorGUI.EndProperty();
        return property.isExpanded;
    }

    // EditorGUILayout用
    private static bool Foldout(SerializedProperty prop, bool drawFoldout, GUIContent content)
    {
        return Foldout(EditorGUILayout.GetControlRect(), prop, drawFoldout, content);
    }

    // Foldoutの三角形の部分だけ
    internal static bool FoldoutOnly(Rect position, SerializedProperty property)
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
        var isExpanded = EditorGUI.Foldout(position, property.isExpanded, label);
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