#nullable disable

using System.Reflection;
using UnityEditorInternal;

namespace Aoyon.MaterialEditor.UI;

// based on https://github.com/lilxyzw/lilycalInventory/blob/52763ab539d59609e63d6974493948ab0614f7c2/Editor/Helper/GUIHelper.ReorderableList.cs
internal static partial class GUIHelper
{
    internal static Rect List(Rect position, SerializedProperty property, bool drawFoldout, GUIContent content, Action<SerializedProperty> initializeFunction = null)
    {
        return InternalList(position, property, drawFoldout, content, initializeFunction);
    }

    internal static Rect List(Rect position, SerializedProperty property, GUIContent content, Action<SerializedProperty> initializeFunction = null)
    {
        return InternalList(position, property, true, content, initializeFunction);
    }

    private static Rect InternalList(Rect position, SerializedProperty property, bool drawFoldout, GUIContent content, Action<SerializedProperty> initializeFunction)
    {
        var foldoutRect = position.SetSingleHeight();
        // ReorderableList 用の foldout は、右端の IntField/ボタン領域をクリック対象から除外する
        var isExpanded = Foldout(foldoutRect, property, content, drawFoldout, true, GetListHeaderClickableRect);
        if (!isExpanded) DrawArraySizeOnLine(foldoutRect, property);
        position.NewLine();
        if (!isExpanded) return position;

        var reorderableList = PropertyHandlerWrap.GetOrSet(property, initializeFunction);
        position.height = reorderableList.GetHeight() - propertyHeight; // フッターをずらした分リスト自体の高さは小さくなっている
        reorderableList.DoList(position);
        position.NewLineWithSingleHeight();
        return position;
    }

    private static void List(SerializedProperty property, bool drawFoldout, GUIContent content, Action<SerializedProperty> initializeFunction = null)
    {
        InternalList(property, drawFoldout, content, initializeFunction);
    }

    private static void List(SerializedProperty property, GUIContent content, Action<SerializedProperty> initializeFunction = null)
    {
        InternalList(property, true, content, initializeFunction);
    }

    private static void InternalList(SerializedProperty property, bool drawFoldout, GUIContent content, Action<SerializedProperty> initializeFunction)
    {
        var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
        var isExpanded = Foldout(rect, property, content, drawFoldout, true, GetListHeaderClickableRect);

        if (isExpanded)
            PropertyHandlerWrap.GetOrSet(property, initializeFunction).DoLayoutList();
        else
            DrawArraySizeOnLine(rect, property);
    }

    private static Rect GetListHeaderClickableRect(Rect position, SerializedProperty property)
    {
        CalcFooterSize("List:Add".LG(), "List:Delete".LG(), ReorderableList.defaultBehaviours.preButton, position, out var rectNum, out _, out var rectAdd, out _);
        if (property.isExpanded)
        {
            return new Rect(position.x, position.y, rectAdd.x - EditorGUIUtility.standardVerticalSpacing - position.x, position.height);
        }
        else
        {
            return new Rect(position.x, position.y, rectNum.x - EditorGUIUtility.standardVerticalSpacing - position.x, position.height);
        }
    }

    internal static float GetListHeight(SerializedProperty property, bool drawFoldout = true)
    {
        var foldoutHeight = drawFoldout ? propertyHeight : 0f;

        float listHeight;
        var list = PropertyHandlerWrap.GetOrSet(property);
        if (list == null) listHeight = EditorGUI.GetPropertyHeight(property);
        else listHeight = property.isExpanded
            ? list.GetHeight() - propertyHeight // フッターをずらした分リスト自体の高さは小さくなっている
            : 0f;
        
        return listHeight + foldoutHeight;
    }

    internal static float GetListHeight(SerializedProperty parent, string propertyName, bool drawFoldout = true)
    {
        using var property = parent.FindPropertyRelative(propertyName);
        return GetListHeight(property, drawFoldout);
    }

    private static ReorderableList CreateReorderableList(SerializedProperty property, Action<SerializedProperty> initializeFunction = null)
    {
        Rect headerRect = default;
        var list = new ReorderableList(property.serializedObject, property.Copy(), true, false, true, true)
        {
            draggable = true,
            headerHeight = 0,
            //footerHeight = 0, // みやすさのためにあえて余白を残す
            multiSelect = true,
            drawHeaderCallback = rect => headerRect = rect
        };
        list.elementHeightCallback = index => EditorGUI.GetPropertyHeight(list.serializedProperty.GetArrayElementAtIndex(index));
        list.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            rect.x += 8;
            rect.width -= 8;
            rect.y += GUI_SPACE * 0.5f;
            rect.height -= GUI_SPACE;
            EditorGUI.PropertyField(rect, list.serializedProperty.GetArrayElementAtIndex(index));
        };
        if(initializeFunction != null)
            list.onAddCallback = _ => list.serializedProperty.ResizeArray(list.serializedProperty.arraySize + 1, initializeFunction);

        // フッターはヘッダーの位置にずらして操作しやすく
        // ついでに表示もカスタマイズ
        list.drawFooterCallback = rect =>
        {
            headerRect.height = EditorGUIUtility.singleLineHeight;
            headerRect.y -= headerRect.height + EditorGUIUtility.standardVerticalSpacing;
            DrawFooter(headerRect, list);
        };
        return list;
    }

    private static MethodInfo InvalidateCacheRecursive;
    private static FieldInfo m_scheduleRemove;
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

        bool isOverMaxMultiEditLimit = list.serializedProperty != null &&
            list.serializedProperty.minArraySize > list.serializedProperty.serializedObject.maxArraySizeForMultiEditing &&
            list.serializedProperty.serializedObject.isEditingMultipleObjects;

        var addContent = "List:Add".LG();
        var deleteContent = "List:Delete".LG();
        var buttonStyle = ReorderableList.defaultBehaviours.preButton;
        CalcFooterSize(addContent, deleteContent, buttonStyle, rect, out var rectNum, out var rectRem, out var rectAdd, out var rectBack);

        // Foldoutのラベルと重なることを防ぐために上からRectを描画
        EditorGUI.DrawRect(rectBack, EditorGUIUtility.isProSkin ? new Color(0.219f,0.219f,0.219f,1) : new Color(0.784f,0.784f,0.784f,1));

        // 配列の要素数を表示
        EditorGUI.BeginChangeCheck();
        var size = EditorGUI.IntField(rectNum, list.serializedProperty.arraySize);
        if(EditorGUI.EndChangeCheck()) list.serializedProperty.arraySize = size;

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
                if(GUI.Button(rectRem, deleteContent, buttonStyle) || GUI.enabled && (bool)m_scheduleRemove?.GetValue(list))
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

        var buttonSize = Mathf.Max(addSize.x, deleteSize.x) + 16f;
        
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

        var rectNum = new Rect(rect.xMax - EditorGUIUtility.fieldWidth + EditorGUIUtility.standardVerticalSpacing * 3, rect.y, EditorGUIUtility.fieldWidth, rect.height);

        EditorGUI.BeginChangeCheck();
        var size = EditorGUI.IntField(rectNum, property.arraySize);
        if(EditorGUI.EndChangeCheck()) property.arraySize = size;
    }

    private class ReorderableListWrapper
    {
        private static Type TYPE = typeof(ReorderableList).Assembly.GetType("UnityEditorInternal.ReorderableListWrapper");
        private static ConstructorInfo CI = TYPE.GetConstructor(new Type[]{typeof(SerializedProperty), typeof(GUIContent), typeof(bool)});
        private static MethodInfo MI_GetPropertyIdentifier = TYPE.GetMethod("GetPropertyIdentifier", BindingFlags.Public | BindingFlags.Static);
        private static PropertyInfo PI_Property = TYPE.GetProperty("Property", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo FI_m_ReorderableList = TYPE.GetField("m_ReorderableList", BindingFlags.NonPublic | BindingFlags.Instance);
        internal object instance;

        internal static string GetPropertyIdentifier(SerializedProperty serializedProperty)
            => MI_GetPropertyIdentifier.Invoke(null, new object[]{serializedProperty}) as string;

        internal ReorderableListWrapper(SerializedProperty property, GUIContent label, bool reorderable = true)
            => instance = CI.Invoke(new object[]{property, label, reorderable});

        internal ReorderableListWrapper(object instance)
            => this.instance = instance;

        internal SerializedProperty Property
        {
            get => PI_Property.GetValue(instance) as SerializedProperty;
            set => PI_Property.SetValue(instance, value);
        }

        private ReorderableList m_ReorderableListBuf;
        internal ReorderableList m_ReorderableList
        {
            get => m_ReorderableListBuf != null ? m_ReorderableListBuf : m_ReorderableListBuf = FI_m_ReorderableList.GetValue(instance) as ReorderableList;
            set => FI_m_ReorderableList.SetValue(instance, m_ReorderableListBuf = value);
        }
    }

    private class PropertyHandlerWrap
    {
        private static Type TYPE = typeof(Editor).Assembly.GetType("UnityEditor.PropertyHandler");
        private static FieldInfo FI_s_reorderableLists = TYPE.GetField("s_reorderableLists", BindingFlags.NonPublic | BindingFlags.Static);
        private static System.Collections.IDictionary s_reorderableLists;
        internal static ReorderableList GetOrSet(SerializedProperty property, Action<SerializedProperty> initializeFunction = null)
        {
            s_reorderableLists ??= FI_s_reorderableLists.GetValue(null) as System.Collections.IDictionary;

            var name = ReorderableListWrapper.GetPropertyIdentifier(property);
            ReorderableListWrapper wrapper;
            if(s_reorderableLists.Contains(name))
            {
                wrapper = new(s_reorderableLists[name]);
            }
            else
            {
                wrapper = new(property, GUIContent.none, true);
                wrapper.m_ReorderableList = CreateReorderableList(property, initializeFunction);
                s_reorderableLists[name] = wrapper.instance;
            }
            wrapper.Property = property.Copy();
            var reorderableList = wrapper.m_ReorderableList;
            if(initializeFunction != null && reorderableList.onAddCallback == null)
                reorderableList.onAddCallback = _ => reorderableList.serializedProperty.ResizeArray(reorderableList.serializedProperty.arraySize + 1, initializeFunction);

            return reorderableList;
        }
    }
}