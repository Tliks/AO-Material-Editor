using System.Reflection;

namespace Aoyon.MaterialEditor.UI;

internal static partial class GUIHelper
{
    private const BindingFlags InstanceNonPublic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private const BindingFlags StaticNonPublic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

    private static PropertyInfo? _leftMarginProp;
    private static PropertyInfo? LeftMarginProp =>
        _leftMarginProp ??= typeof(EditorGUIUtility).GetProperty("leftMarginCoord", StaticNonPublic);

    private static PropertyInfo? _topmostRectProp;
    private static PropertyInfo? TopmostRectProp =>
        _topmostRectProp ??= typeof(GUI).Assembly
            .GetType("UnityEngine.GUIClip")?.GetProperty("topmostRect", StaticNonPublic);

    public static bool TryGetMarginX(out float marginX)
    {
        marginX = 0f;

        if (LeftMarginProp == null || TopmostRectProp == null)
            return false;

        if (LeftMarginProp.GetValue(null) is not float leftMargin)
            return false;

        if (TopmostRectProp.GetValue(null) is not Rect topmostRect)
            return false;

        marginX = leftMargin - Mathf.Max(0f, topmostRect.xMin);
        return true;
    }

    private static Type? _sceneHierarchyWindowType;
    private static Type? SceneHierarchyWindowType =>
        _sceneHierarchyWindowType ??= typeof(Editor).Assembly.GetType("UnityEditor.SceneHierarchyWindow");

    private static PropertyInfo? _sceneHierarchyProp;
    private static PropertyInfo? SceneHierarchyProp =>
        _sceneHierarchyProp ??= SceneHierarchyWindowType?.GetProperty("sceneHierarchy", InstanceNonPublic)
            ?? SceneHierarchyWindowType?.GetProperty("m_SceneHierarchy", InstanceNonPublic);

    private static MethodInfo? _expandTreeViewItemMethod;
    private static MethodInfo? ExpandTreeViewItemMethod =>
        _expandTreeViewItemMethod ??= SceneHierarchyProp?.PropertyType.GetMethod("ExpandTreeViewItem", InstanceNonPublic);

    public static void SetHierarchyExpanded(GameObject go, bool expand)
    {
        if (SceneHierarchyWindowType == null || SceneHierarchyProp == null || ExpandTreeViewItemMethod == null)
            return;

        var window = EditorWindow.GetWindow(SceneHierarchyWindowType);
        if (window == null) return;

        if (SceneHierarchyProp.GetValue(window) is not { } sceneHierarchy) return;

        ExpandTreeViewItemMethod.Invoke(sceneHierarchy, new object[] { go.GetInstanceID(), expand });
    }
}