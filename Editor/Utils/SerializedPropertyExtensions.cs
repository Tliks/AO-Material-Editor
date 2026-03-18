using System.Reflection;

namespace Aoyon.MaterialEditor;

internal static class SerializedPropertyExtensions
{
    public static void CopyFrom(this SerializedProperty prop, object source)
    {
        if (source == null) return;
        CopyValue(prop, source);
    }

    private static void CopyValue(SerializedProperty prop, object source)
    {
        if (prop.isArray && prop.propertyType != SerializedPropertyType.String)
        {
            CopyArray(prop, source);
            return;
        }

        if (prop.propertyType == SerializedPropertyType.Generic)
        {
            CopyGeneric(prop, source);
            return;
        }

        SetPropertyValue(prop, source);
    }

    private static void CopyGeneric(SerializedProperty prop, object source)
    {
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
            if (value == null)
            {
                if (child.isArray && child.propertyType != SerializedPropertyType.String)
                {
                    child.ClearArray();
                }
                else
                {
                    SetPropertyValue(child, null);
                }
                continue;
            }

            CopyValue(child, value);
        }
        while (child.Next(enterChildren: false));
    }

    private static void CopyArray(SerializedProperty prop, object source)
    {
        if (source is not IList list) return;

        prop.ResizeArray(list.Count);
        for (var i = 0; i < list.Count; i++)
        {
            var element = prop.GetArrayElementAtIndex(i);
            var value = list[i];
            if (value == null)
            {
                SetPropertyValue(element, null);
                continue;
            }

            CopyValue(element, value);
        }
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
            case SerializedPropertyType.Color:
                prop.colorValue = value is Color color ? color : default;
                break;
            case SerializedPropertyType.Vector2:
                prop.vector2Value = value is Vector2 vector2 ? vector2 : default;
                break;
            case SerializedPropertyType.Vector3:
                prop.vector3Value = value is Vector3 vector3 ? vector3 : default;
                break;
            case SerializedPropertyType.Vector4:
                prop.vector4Value = value is Vector4 vector4 ? vector4 : default;
                break;
            case SerializedPropertyType.Rect:
                prop.rectValue = value is Rect rect ? rect : default;
                break;
            case SerializedPropertyType.Bounds:
                prop.boundsValue = value is Bounds bounds ? bounds : default;
                break;
            case SerializedPropertyType.Quaternion:
                prop.quaternionValue = value is Quaternion quaternion ? quaternion : default;
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
