using System.Reflection;
using UnityEditor;

namespace Aoyon.MaterialEditor;

internal static class SerializedPropertyExtensions
{
    public static SerializedProperty FPR(this SerializedProperty property, string name) =>
        property.FindPropertyRelative(name);

    /// <summary>
    /// オブジェクトのシリアライズ可能なフィールド値を SerializedProperty にコピーする。
    /// createDefault で作ったインスタンスをそのまま要素に反映できる。
    /// </summary>
    public static void CopyFrom(this SerializedProperty prop, object source)
    {
        if (source == null) return;
        var type = source.GetType();
        var child = prop.Copy();
        if (!child.Next(enterChildren: true)) return;
        var startDepth = child.depth;
        do
        {
            if (child.depth != startDepth) break;
            var field = type.GetField(child.name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null) continue;
            var value = field.GetValue(source);
            if (child.propertyType == SerializedPropertyType.Generic)
            {
                if (value != null) CopyFrom(child, value);
            }
            else
            {
                SetPropertyValue(child, value);
            }
        }
        while (child.Next(enterChildren: false));
    }

    private static void SetPropertyValue(SerializedProperty prop, object? value)
    {
        switch (prop.propertyType)
        {
            case SerializedPropertyType.Integer:
                prop.intValue = value is int i ? i : (int)(value ?? 0);
                break;
            case SerializedPropertyType.Boolean:
                prop.boolValue = value is bool b && b;
                break;
            case SerializedPropertyType.Float:
                prop.floatValue = value is float f ? f : (float)(value ?? 0f);
                break;
            case SerializedPropertyType.String:
                prop.stringValue = value?.ToString() ?? "";
                break;
            case SerializedPropertyType.Enum:
                prop.enumValueIndex = value != null ? (int)value : 0;
                break;
            case SerializedPropertyType.ObjectReference:
                prop.objectReferenceValue = value as UnityEngine.Object;
                break;
        }
    }

    public static void ResizeArray(this SerializedProperty prop, int size, Action<SerializedProperty>? initializeFunction = null)
    {
        var arraySize = prop.arraySize;
        if (arraySize == size) return;
        prop.arraySize = size;
        if (initializeFunction != null && size > arraySize)
        {
            for (var i = arraySize; i < size; i++)
            {
                var element = prop.GetArrayElementAtIndex(i);
                initializeFunction(element);
            }
        }
    }
}
