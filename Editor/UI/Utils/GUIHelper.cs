namespace Aoyon.MaterialEditor.UI;

internal static partial class GUIHelper
{
    internal static Rect DragAndDropList(Rect position, SerializedProperty property, bool drawFoldout, GUIContent content, Action<SerializedProperty> initializeFunction, Func<Object, bool> selectFunc, Action<SerializedProperty, Object[]> onItemsDropped)
    {
        Object[] items = new Object[]{};

        // D&D中のものの中から型が一致しているものだけ抽出
        if(DragAndDrop.objectReferences != null) items = DragAndDrop.objectReferences.Where(selectFunc).SkipDestroyed().ToArray();

        if (items.Length <= 1)
        {
            return List(position, property, drawFoldout, content, initializeFunction);
        }

        var listHeight = GetListHeight(property, drawFoldout);
        var listRect = position;
        if (drawFoldout)
        {
            listRect.SetSingleHeight();
            listRect.NewLine();
            listHeight -= propertyHeight;
        }
        listRect.height = listHeight;

        var e = Event.current;

        if (e.type is EventType.DragUpdated or EventType.DragPerform or EventType.DragExited)
        {
            HandleUtility.Repaint();
        }

        var isDraggingOverList = listRect.Contains(e.mousePosition);
        using (var disableScope = new EditorGUI.DisabledScope(isDraggingOverList))
        {
            position = List(position, property, drawFoldout, content, initializeFunction);
        }
        if (isDraggingOverList) DrawDropLect(listRect);

        if (!isDraggingOverList) return position;

        switch(e.type)
        {
            case EventType.DragUpdated:
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                break;
            case EventType.DragPerform:
                DragAndDrop.AcceptDrag();
                onItemsDropped?.Invoke(property, items);
                e.Use();
                break;
        }

        return position;
    }

    private static void DrawDropLect(Rect position)
    {
        StyleHelper.DropStyle.fontSize = (int)Mathf.Min(24, position.height);
        EditorGUI.LabelField(position, Localization.G("label.dragAndDrop"), StyleHelper.DropStyle);
    }

    public static void DrawHorizontalWhiteLine(float thickness = 1.0f, float verticalPadding = 4.0f)
    {
        var rect = EditorGUILayout.GetControlRect(GUILayout.Height(thickness + verticalPadding * 2));
        rect.y += verticalPadding;
        rect.height = thickness;
        EditorGUI.DrawRect(rect, Color.white);
    }
}