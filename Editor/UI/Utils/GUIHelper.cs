namespace Aoyon.MaterialEditor.UI;

internal static partial class GUIHelper
{
    public readonly record struct DropHandler(
        Func<Object, bool> Accepts,
        Action<SerializedProperty, Object[]> Drop,
        string ContentKey = "common.dragAndDropAdd");

    private static readonly float DropAreaHeight = EditorGUIUtility.singleLineHeight * 2f;

    internal static Rect DragAndDropList(
        Rect position,
        SerializedProperty property,
        GUIContent content,
        Action<SerializedProperty> initializeFunction,
        DropHandler? dropHandler,
        ListOptions? options = null)
    {
        var resolvedOptions = options ?? new ListOptions();
        return List(position, property, content, AddDropArea(resolvedOptions, property, dropHandler), initializeFunction);
    }

    internal static float GetDragAndDropListHeight(SerializedProperty property, ListOptions? options, DropHandler? dropHandler)
    {
        return GetListHeight(property, AddDropArea(options ?? new ListOptions(), property, dropHandler));
    }

    private static ListOptions AddDropArea(ListOptions options, SerializedProperty property, DropHandler? dropHandler)
    {
        if (dropHandler == null) return options;

        var existingTailContent = options.TailContent;
        var dropContent = new ListContentOptions(
            rect => DropArea(rect, property, dropHandler.Value),
            () => DropAreaHeight);

        if (existingTailContent == null)
        {
            return new ListOptions(
                options.Foldout,
                options.Nest,
                options.MaxVisibleListHeight,
                options.MiddleContent,
                dropContent);
        }

        var combinedTailContent = new ListContentOptions(
            rect =>
            {
                var existingHeight = existingTailContent.Value.GetHeight.Invoke();
                var existingRect = new Rect(rect.x, rect.y, rect.width, existingHeight);
                existingTailContent.Value.Draw.Invoke(existingRect);

                var dropRect = new Rect(rect.x, existingRect.yMax + GUI_SPACE, rect.width, DropAreaHeight);
                dropContent.Draw.Invoke(dropRect);
            },
            () => existingTailContent.Value.GetHeight.Invoke() + GUI_SPACE + DropAreaHeight);

        return new ListOptions(
            options.Foldout,
            options.Nest,
            options.MaxVisibleListHeight,
            options.MiddleContent,
            combinedTailContent);
    }

    private static readonly Color DropAreaHoverBorderColor = new(0.8f, 0.8f, 0.8f, 0.8f);
    private static readonly Color DropAreaHoverBackgroundColor = new(0.9f, 0.9f, 0.9f, 0.18f);
    private static void DropArea(Rect position, SerializedProperty property, DropHandler dropHandler)
    {
        var e = Event.current;
        var objectReferences = DragAndDrop.objectReferences;
        var items = objectReferences != null
            ? objectReferences.Where(dropHandler.Accepts).SkipDestroyed().ToArray()
            : Array.Empty<Object>();
        var isDraggingOverArea = items.Length > 0 && position.Contains(e.mousePosition);

        var content = dropHandler.ContentKey.LG();
        EditorGUI.LabelField(position, content, StyleHelper.DropStyle);
        if (isDraggingOverArea)
        {
            EditorGUI.DrawRect(position, DropAreaHoverBackgroundColor);
            DrawRectBorder(position, DropAreaHoverBorderColor);
        }

        if (items.Length == 0) return;
        if (e.type is EventType.DragUpdated or EventType.DragPerform or EventType.DragExited)
        {
            HandleUtility.Repaint();
        }
        if (!isDraggingOverArea) return;

        switch (e.type)
        {
            case EventType.DragUpdated:
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                e.Use();
                break;
            case EventType.DragPerform:
                DragAndDrop.AcceptDrag();
                dropHandler.Drop.Invoke(property, items);
                e.Use();
                break;
        }
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
        var toggleWidth = EditorGUIUtility.singleLineHeight;
        var foldRect = new Rect(position.x, position.y, foldWidth, position.height);
        var toggleRect = new Rect(foldRect.xMax, position.y, toggleWidth, position.height);
        var labelRect = new Rect(toggleRect.xMax, position.y, Mathf.Max(0f, position.xMax - toggleRect.xMax), position.height);

        DrawFoldout(position, property, GUIContent.none, false, rectStrict);

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
