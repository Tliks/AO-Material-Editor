namespace Aoyon.MaterialEditor.UI;

internal static partial class GUIHelper
{
    internal static Rect DragAndDropList(
        Rect position,
        SerializedProperty property,
        GUIContent content,
        Action<SerializedProperty> initializeFunction,
        Func<Object, bool> selectFunc,
        Action<SerializedProperty, Object[]> onItemsDropped,
        ListOptions? options = null)
    {
        var resolvedOptions = options ?? new ListOptions();
        var drawFoldout = resolvedOptions.Foldout.Draw;
        var shouldNest = resolvedOptions.Nest;
        Object[] items = new Object[]{};

        // D&D中のものの中から型が一致しているものだけ抽出
        if(DragAndDrop.objectReferences != null) items = DragAndDrop.objectReferences.Where(selectFunc).SkipDestroyed().ToArray();

        if (items.Length <= 1)
        {
            return List(position, property, content, resolvedOptions, initializeFunction);
        }

        var listHeight = GetListHeightInternal(property, resolvedOptions);
        var listRect = position;
        if (drawFoldout)
        {
            listRect.SetSingleHeight();
            listRect.NewLine();
            listHeight -= propertyHeight;
            if (shouldNest) listRect.Indent();
        }
        else if (shouldNest)
        {
            listRect.Indent();
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
            position = List(position, property, content, resolvedOptions, initializeFunction);
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
        EditorGUI.LabelField(position, Localization.G("common.dragAndDropAdd"), StyleHelper.DropStyle);
    }

    public static void DrawFullWidthHorizontalLine(Color color, float thickness = 1.0f)
    {
        var lineRect = EditorGUILayout.GetControlRect(false, thickness);
        lineRect.x = 0;
        lineRect.width = EditorGUIUtility.currentViewWidth;
        DrawHorizontalLine(lineRect, color);
    }

      public static void DrawFullWidthHorizontalLine(Rect position, Color color, float thickness = 1.0f)
    {
        position.x = 0;
        position.width = EditorGUIUtility.currentViewWidth;
        position.height = thickness;
        DrawHorizontalLine(position, color);
    }
  
    public static void DrawHorizontalLine(Color color, float thickness = 1.0f, float verticalPadding = 4.0f)
    {
        var rect = EditorGUILayout.GetControlRect(GUILayout.Height(thickness + verticalPadding * 2));
        rect.y += verticalPadding;
        rect.height = thickness;
        DrawHorizontalLine(rect, color);
    }

    public static void DrawHorizontalLine(Rect position, Color color)
    {
        EditorGUI.DrawRect(position, color);
    }

    /// <summary>
    /// <paramref name="rect"/> を基準に枠線を描画する。
    /// <paramref name="edge"/> は基準矩形の各辺を「どれだけ内側へ寄せるか」。正でインセット、負でアウトセット（外側へ広げる）。
    /// </summary>
    public static void DrawRectBorder(Rect rect, Color color, float borderWidth = 1f, RectOffset? edge = null)
    {
        var e = edge ?? new RectOffset();
        DrawRectBorderImpl(rect, color, borderWidth, e.left, e.right, e.top, e.bottom);
    }

    public static void DrawRectBorder(Rect rect, Color color, float borderWidth, float edge)
    {
        DrawRectBorderImpl(rect, color, borderWidth, edge, edge, edge, edge);
    }

    /// <summary>EditorGUI 座標を物理ピクセルへ揃え、DPI スケール時の線のにじみを抑える。</summary>
    private static Rect AlignRectToPixelGrid(Rect rect)
    {
        var pixelsPerPoint = Mathf.Max(1f, EditorGUIUtility.pixelsPerPoint);
        var xMin = SnapToPixel(rect.xMin, pixelsPerPoint);
        var yMin = SnapToPixel(rect.yMin, pixelsPerPoint);
        var xMax = SnapToPixel(rect.xMax, pixelsPerPoint);
        var yMax = SnapToPixel(rect.yMax, pixelsPerPoint);
        if (xMax < xMin) xMax = xMin;
        if (yMax < yMin) yMax = yMin;
        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    private static float SnapToPixel(float value, float pixelsPerPoint)
    {
        return Mathf.Round(value * pixelsPerPoint) / pixelsPerPoint;
    }

    private static void DrawRectBorderImpl(
        Rect rect,
        Color color,
        float borderWidth,
        float left,
        float right,
        float top,
        float bottom)
    {
        if (Event.current.type != EventType.Repaint) return;

        var r = rect;
        r.xMin += left;
        r.xMax -= right;
        r.yMin += top;
        r.yMax -= bottom;

        r = AlignRectToPixelGrid(r);

        if (r.width <= 0f || r.height <= 0f) return;

        var pixelsPerPoint = Mathf.Max(1f, EditorGUIUtility.pixelsPerPoint);
        var wRaw = Mathf.Max(0f, borderWidth);
        if (wRaw <= 0f) return;
        var w = Mathf.Max(1f / pixelsPerPoint, SnapToPixel(wRaw, pixelsPerPoint));

        // 上下が四隅を占有する。左右は角を除いた縦帯のみ（角で二重描画しない）
        EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, w), color);
        EditorGUI.DrawRect(new Rect(r.x, r.yMax - w, r.width, w), color);

        var sideHeight = r.height - 2f * w;
        if (sideHeight > 0f)
        {
            EditorGUI.DrawRect(new Rect(r.x, r.y + w, w, sideHeight), color);
            EditorGUI.DrawRect(new Rect(r.xMax - w, r.y + w, w, sideHeight), color);
        }
    }
    
    private static bool ToggleLeftInternal(Rect position, bool value, GUIContent label, Action<bool>? setValue = null)
    {
        SplitRectHorizontallyForLeft(position, EditorGUIUtility.singleLineHeight, out var toggleRect, out var labelRect);

        var newValue = EditorGUI.Toggle(toggleRect, value);
        if (newValue != value) setValue?.Invoke(newValue);

        EditorGUI.LabelField(labelRect, label);
        
        return newValue;
    }

    public static bool ToggleLeft(Rect position, SerializedProperty property, GUIContent label)
    {
        using var _ = new EditorGUI.PropertyScope(position, label, property);
        var result = ToggleLeftInternal(
            position,
            property.boolValue,
            label,
            newValue => property.boolValue = newValue
        );
        return result;
    }

    public static bool ToggleLeft(Rect position, bool value, GUIContent label)
    {
        return ToggleLeftInternal(position, value, label);
    }

    public static bool ToggleLeft(SerializedProperty property, GUIContent label)
    {
        var position = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
        return ToggleLeft(position, property, label);
    }

    public static bool ToggleLeft(bool value, GUIContent label)
    {
        var position = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
        return ToggleLeft(position, value, label);
    }

    public static (bool isExpanded, bool isEnabled) FoldoutAndToggleLeft(
        Rect position,
        SerializedProperty property,
        GUIContent label,
        bool rectStrict = false,
        bool toggleOnLabelClick = true)
    {
        var foldWidth = EditorStyles.foldout.CalcSize(GUIContent.none).x;
        SplitRectHorizontallyForLeft(position, foldWidth, out var foldRect, out var toggleAndLabelRect);
        SplitRectHorizontallyForLeft(toggleAndLabelRect, EditorGUIUtility.singleLineHeight, out var toggleRect, out var labelRect);

        Foldout(
            foldRect,
            property,
            GUIContent.none,
            new FoldoutOptions(
                RectStrict: rectStrict,
                ToggleOnLabelClick: false));

        using (var _ = new EditorGUI.PropertyScope(position, label, property))
        {
            var newValue = EditorGUI.Toggle(toggleRect, property.boolValue);
            if (newValue != property.boolValue) property.boolValue = newValue;
            EditorGUI.LabelField(labelRect, label);
        }

        var e = Event.current;
        if (toggleOnLabelClick && e.type == EventType.MouseDown && e.button == 0 && labelRect.Contains(e.mousePosition))
        {
            ApplyExpandedState(property, !property.isExpanded);
            e.Use();
        }
        return (property.isExpanded, property.boolValue);
    }
}
