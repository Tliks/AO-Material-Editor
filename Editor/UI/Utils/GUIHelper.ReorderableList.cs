using System.Reflection;
using UnityEditorInternal;

namespace Aoyon.MaterialEditor.UI;

// based on https://github.com/lilxyzw/lilycalInventory/blob/52763ab539d59609e63d6974493948ab0614f7c2/Editor/Helper/GUIHelper.ReorderableList.cs
internal static partial class GUIHelper
{
    public readonly record struct ListMiddleContentOptions(Action<Rect> Draw, Func<float> GetHeight);

    public readonly struct ListOptions
    {
        public FoldoutOptions Foldout { get; }
        public bool Nest { get; }
        public float? MaxVisibleListHeight { get; }
        public ListMiddleContentOptions? MiddleContent { get; }

        public ListOptions() : this(null, true, null, null) { }

        public ListOptions(
            FoldoutOptions? foldout = null,
            bool nest = true,
            float? maxVisibleListHeight = null,
            ListMiddleContentOptions? middleContent = null)
        {
            Foldout = foldout ?? new FoldoutOptions();
            Nest = nest;
            MaxVisibleListHeight = maxVisibleListHeight;
            MiddleContent = middleContent;
        }
    }

    private static readonly GUIStyle buttonStyle = EditorStyles.miniButton;
    private static readonly Dictionary<string, Vector2> listScrollPositions = new();
    private static readonly ReorderableList.FooterCallbackDelegate emptyFooterCallback = _ => { };
    public static float DefaultScrollableListHeight => 110f;

    public static Rect List(
        Rect position,
        SerializedProperty property,
        GUIContent content,
        ListOptions? options = null,
        Action<SerializedProperty>? initializeFunction = null)
    {
        return InternalList(position, property, content, initializeFunction, options);
    }

    private static Rect InternalList(
        Rect position,
        SerializedProperty property,
        GUIContent content,
        Action<SerializedProperty>? initializeFunction,
        ListOptions? options)
    {
        var resolvedOptions = options ?? new ListOptions();
        var foldoutOptions = resolvedOptions.Foldout with { GetClickableRect = resolvedOptions.Foldout.GetClickableRect ?? GetListHeaderClickableRect };
        var drawFoldout = foldoutOptions.Draw;
        var shouldNest = resolvedOptions.Nest;
        var middleContent = resolvedOptions.MiddleContent;
        var foldoutRect = position.SetSingleHeight();
        // ReorderableList 用の foldout は、右端の IntField/ボタン領域をクリック対象から除外する
        var isExpanded = Foldout(foldoutRect, property, content, foldoutOptions);
        if (!isExpanded) DrawArraySizeOnLine(foldoutRect, property);
        position.NewLine();
        if (!isExpanded) return position;
        if (shouldNest) position.Indent();

        var reorderableList = PropertyHandlerWrap.GetOrSet(property, initializeFunction);
        reorderableList.drawFooterCallback = emptyFooterCallback;
        var middleContentHeight = middleContent?.GetHeight.Invoke() ?? 0f;

        var fullListHeight = reorderableList.GetHeight();
        var visibleListHeight = ApplyHeightLimit(fullListHeight, resolvedOptions.MaxVisibleListHeight);
        if (middleContentHeight > 0f)
        {
            var middleRect = new Rect(position.x, position.y, position.width, middleContentHeight);
            middleContent?.Draw.Invoke(middleRect);
            position.y += middleContentHeight + GUI_SPACE;
        }

        position.height = visibleListHeight;

        if (resolvedOptions.MaxVisibleListHeight.HasValue && fullListHeight > resolvedOptions.MaxVisibleListHeight.Value)
        {
            DrawScrollableList(position, property, reorderableList, fullListHeight, visibleListHeight);
        }
        else
        {
            reorderableList.DoList(new Rect(position.x, position.y, position.width, fullListHeight));
        }

        DrawFooter(foldoutRect, reorderableList);
        position.NewLine();
        position.SetSingleHeight();
        return position;
    }

    private static Rect GetListHeaderClickableRect(Rect position, SerializedProperty property)
    {
        CalcFooterSize("common.add".LG(), "common.remove".LG(), buttonStyle, position, out var rectNum, out _, out var rectAdd, out _);
        if (property.isExpanded)
        {
            return new Rect(position.x, position.y, rectAdd.x - EditorGUIUtility.standardVerticalSpacing - position.x, position.height);
        }
        else
        {
            return new Rect(position.x, position.y, rectNum.x - EditorGUIUtility.standardVerticalSpacing - position.x, position.height);
        }
    }

    public static float GetListHeight(
        SerializedProperty property,
        ListOptions? options = null)
    {
        return GetListHeightInternal(property, options);
    }

    private static float GetListHeightInternal(SerializedProperty property, ListOptions? options)
    {
        var resolvedOptions = options ?? new ListOptions();
        var drawFoldout = resolvedOptions.Foldout.Draw;
        // drawFoldout=false の場合は Foldout 側で常に展開描画されるため、高さ計算でも展開扱いにする
        var isExpanded = !drawFoldout || property.isExpanded;
        var headerHeight = propertyHeight;
        var headerSpacing = isExpanded ? GUI_SPACE : 0f;
        var middleContentHeight = isExpanded ? resolvedOptions.MiddleContent?.GetHeight.Invoke() ?? 0f : 0f;
        var middleContentSpacing = middleContentHeight > 0f ? GUI_SPACE : 0f;

        float listHeight;
        var list = PropertyHandlerWrap.GetOrSet(property);
        if (list == null) listHeight = EditorGUI.GetPropertyHeight(property);
        else listHeight = isExpanded
            ? ApplyHeightLimit(list.GetHeight(), resolvedOptions.MaxVisibleListHeight)
            : 0f;
        
        return listHeight + middleContentHeight + middleContentSpacing + headerHeight + headerSpacing;
    }

    private static ReorderableList CreateReorderableList(SerializedProperty property, Action<SerializedProperty>? initializeFunction = null)
    {
        var list = new ReorderableList(property.serializedObject, property.Copy(), true, false, true, true)
        {
            draggable = true,
            headerHeight = 0,
            footerHeight = 0,
            multiSelect = true
        };
        list.elementHeightCallback = index => EditorGUI.GetPropertyHeight(list.serializedProperty.GetArrayElementAtIndex(index));
        list.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            rect.x += 8;
            rect.width -= 8;
            rect.y += GUI_SPACE * 0.5f;
            rect.height -= GUI_SPACE;
            EditorGUI.PropertyField(rect, list.serializedProperty.GetArrayElementAtIndex(index), GUIContent.none, true);
        };
        if(initializeFunction != null)
            list.onAddCallback = _ => list.serializedProperty.ResizeArray(list.serializedProperty.arraySize + 1, initializeFunction);

        return list;
    }

    private static void DrawScrollableList(
        Rect rect,
        SerializedProperty property,
        ReorderableList reorderableList,
        float fullListHeight,
        float visibleListHeight)
    {
        var key = ReorderableListWrapper.GetPropertyIdentifier(property);
        listScrollPositions.TryGetValue(key, out var scrollPosition);

        var viewWidth = Mathf.Max(0f, rect.width - 16f);
        var viewRect = new Rect(0f, 0f, viewWidth, fullListHeight);
        var visibleRect = new Rect(0f, scrollPosition.y, viewWidth, visibleListHeight);

        scrollPosition = GUI.BeginScrollView(rect, scrollPosition, viewRect, false, true);
        reorderableList.DoList(new Rect(0f, 0f, viewWidth, fullListHeight), visibleRect);
        GUI.EndScrollView();

        listScrollPositions[key] = scrollPosition;
    }

    private static float ApplyHeightLimit(float fullListHeight, float? maxVisibleListHeight)
    {
        return maxVisibleListHeight.HasValue
            ? Mathf.Min(fullListHeight, maxVisibleListHeight.Value)
            : fullListHeight;
    }

    private static void NormalizeReorderableList(ReorderableList list)
    {
        list.headerHeight = 0f;
        list.footerHeight = 0f;
        list.drawFooterCallback = emptyFooterCallback;
    }

    private static MethodInfo? InvalidateCacheRecursive;
    private static FieldInfo? m_scheduleRemove;
    private static bool isInitialized = false;
    private static void DrawFooter(Rect rect, ReorderableList list)
    {
        // どうしようもないのでReflectionを使用
        if(!isInitialized)
        {
            isInitialized = true;
            InvalidateCacheRecursive = typeof(ReorderableList).GetMethod("InvalidateCacheRecursive", BindingFlags.Instance | BindingFlags.NonPublic);
            m_scheduleRemove = typeof(ReorderableList).GetField("m_scheduleRemove", BindingFlags.Instance | BindingFlags.NonPublic);

            if(InvalidateCacheRecursive == null) Debug.LogError("InvalidateCacheRecursive == null");
            if(m_scheduleRemove == null) Debug.LogError("m_scheduleRemove == null");
        }

        var serializedProperty = list.serializedProperty;
        if (serializedProperty == null) return;

        bool isOverMaxMultiEditLimit = serializedProperty.minArraySize > serializedProperty.serializedObject.maxArraySizeForMultiEditing &&
            serializedProperty.serializedObject.isEditingMultipleObjects;

        var addContent = "common.add".LG();
        var deleteContent = "common.remove".LG();
        CalcFooterSize(addContent, deleteContent, buttonStyle, rect, out var rectNum, out var rectRem, out var rectAdd, out var rectBack);

        // Foldoutのラベルと重なることを防ぐために上からRectを描画
        EditorGUI.DrawRect(rectBack, EditorGUIUtility.isProSkin ? new Color(0.219f,0.219f,0.219f,1) : new Color(0.784f,0.784f,0.784f,1));

        // 配列の要素数を表示
        EditorGUI.BeginChangeCheck();
        var size = EditorGUI.IntField(rectNum, serializedProperty.arraySize);
        if(EditorGUI.EndChangeCheck()) serializedProperty.arraySize = size;

        // 追加ボタン、削除ボタンの再実装
        if(list.displayAdd)
        {
            bool cantAdd = list.onCanAddCallback != null && !list.onCanAddCallback(list) || isOverMaxMultiEditLimit;
            using(new EditorGUI.DisabledScope(cantAdd))
            {
                EditorGUI.DrawRect(rectAdd, new Color(0,0,0,0.1f));
                if(GUI.Button(rectAdd, addContent, buttonStyle))
                {
                    if(list.onAddDropdownCallback != null) list.onAddDropdownCallback(rectAdd, list);
                    else if(list.onAddCallback != null) list.onAddCallback(list);
                    else ReorderableList.defaultBehaviours.DoAddButton(list);

                    list.onChangedCallback?.Invoke(list);
                    InvalidateCacheRecursive?.Invoke(list, null);
                }
            }
        }
        if(list.displayRemove)
        {
            bool cantRemove = list.index < 0 || list.index >= list.count || (list.onCanRemoveCallback != null && !list.onCanRemoveCallback(list)) || isOverMaxMultiEditLimit;
            using(new EditorGUI.DisabledScope(cantRemove))
            {
                EditorGUI.DrawRect(rectRem, new Color(0,0,0,0.1f));
                if(GUI.Button(rectRem, deleteContent, buttonStyle) || GUI.enabled && (bool?)m_scheduleRemove?.GetValue(list) == true)
                {
                    if(list.onRemoveCallback == null) ReorderableList.defaultBehaviours.DoRemoveButton(list);
                    else list.onRemoveCallback(list);

                    list.onChangedCallback?.Invoke(list);
                    InvalidateCacheRecursive?.Invoke(list, null);
                    GUI.changed = true;
                }
            }
        }

        m_scheduleRemove?.SetValue(list, false);
    }

    private static void CalcFooterSize(GUIContent addContent, GUIContent deleteContent, GUIStyle buttonStyle, Rect rect, out Rect rectNum, out Rect rectRem, out Rect rectAdd, out Rect rectBack)
    {
        var addSize     = buttonStyle.CalcSize(addContent);
        var deleteSize  = buttonStyle.CalcSize(deleteContent);

        var buttonSize = Mathf.Max(addSize.x, deleteSize.x) + 8f;
        
        var spacing = EditorGUIUtility.standardVerticalSpacing;

        float numWidth = EditorGUIUtility.fieldWidth;
        rectNum = new Rect(
            rect.xMax - numWidth,
            rect.y,
            numWidth,
            rect.height
        );

        rectRem = new Rect(
            rectNum.x - spacing - buttonSize,
            rect.y,
            buttonSize,
            rect.height
        );

        rectAdd = new Rect(
            rectRem.x - spacing - buttonSize,
            rect.y,
            buttonSize,
            rect.height
        );

        rectBack = new Rect(
            rectAdd.x,
            rect.y,
            rectNum.xMax - rectAdd.x,
            EditorGUIUtility.singleLineHeight
        );
    }

    private static void DrawArraySizeOnLine(Rect rect, SerializedProperty property)
    {
        if(property == null || !property.isArray || property.hasMultipleDifferentValues) return;

        CalcFooterSize("common.add".LG(), "common.remove".LG(), buttonStyle, rect, out var rectNum, out _, out _, out _);

        EditorGUI.BeginChangeCheck();
        var size = EditorGUI.IntField(rectNum, property.arraySize);
        if(EditorGUI.EndChangeCheck()) property.arraySize = size;
    }

    private class ReorderableListWrapper
    {
        private static Type TYPE = typeof(ReorderableList).Assembly.GetType("UnityEditorInternal.ReorderableListWrapper")!;
        private static ConstructorInfo CI = TYPE.GetConstructor(new Type[]{typeof(SerializedProperty), typeof(GUIContent), typeof(bool)})!;
        private static MethodInfo MI_GetPropertyIdentifier = TYPE.GetMethod("GetPropertyIdentifier", BindingFlags.Public | BindingFlags.Static)!;
        private static PropertyInfo PI_Property = TYPE.GetProperty("Property", BindingFlags.NonPublic | BindingFlags.Instance)!;
        private static FieldInfo FI_m_ReorderableList = TYPE.GetField("m_ReorderableList", BindingFlags.NonPublic | BindingFlags.Instance)!;
        internal object instance;

        internal static string GetPropertyIdentifier(SerializedProperty serializedProperty)
            => (string)MI_GetPropertyIdentifier.Invoke(null, new object[]{serializedProperty})!;

        internal ReorderableListWrapper(SerializedProperty property, GUIContent label, bool reorderable = true)
            => instance = CI.Invoke(new object[]{property, label, reorderable});

        internal ReorderableListWrapper(object instance)
            => this.instance = instance;

        internal SerializedProperty Property
        {
            get => (SerializedProperty)PI_Property.GetValue(instance)!;
            set => PI_Property.SetValue(instance, value);
        }

        private ReorderableList? m_ReorderableListBuf;
        internal ReorderableList m_ReorderableList
        {
            get => m_ReorderableListBuf ??= (ReorderableList)FI_m_ReorderableList.GetValue(instance)!;
            set => FI_m_ReorderableList.SetValue(instance, m_ReorderableListBuf = value);
        }
    }

    private class PropertyHandlerWrap
    {
        private static Type TYPE = typeof(Editor).Assembly.GetType("UnityEditor.PropertyHandler")!;
        private static FieldInfo FI_s_reorderableLists = TYPE.GetField("s_reorderableLists", BindingFlags.NonPublic | BindingFlags.Static)!;
        private static IDictionary? s_reorderableLists;
        internal static ReorderableList GetOrSet(SerializedProperty property, Action<SerializedProperty>? initializeFunction = null)
        {
            var reorderableLists = s_reorderableLists ??= (IDictionary)FI_s_reorderableLists.GetValue(null)!;

            var name = ReorderableListWrapper.GetPropertyIdentifier(property);
            ReorderableListWrapper wrapper;
            if(reorderableLists.Contains(name))
            {
                wrapper = new(reorderableLists[name]!);
            }
            else
            {
                wrapper = new(property, GUIContent.none, true);
                wrapper.m_ReorderableList = CreateReorderableList(property, initializeFunction);
                reorderableLists[name] = wrapper.instance;
            }
            wrapper.Property = property.Copy();
            var reorderableList = wrapper.m_ReorderableList;
            NormalizeReorderableList(reorderableList);
            if(initializeFunction != null && reorderableList.onAddCallback == null)
                reorderableList.onAddCallback = _ => reorderableList.serializedProperty.ResizeArray(reorderableList.serializedProperty.arraySize + 1, initializeFunction);

            return reorderableList;
        }
    }
}
