using UnityEngine.Rendering;

namespace Aoyon.MaterialEditor.UI;

[CustomPropertyDrawer(typeof(MaterialProperty))]
internal class MaterialPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var propertyName = property.FindPropertyRelative(nameof(MaterialProperty.PropertyName));
        var propertyType = property.FindPropertyRelative(nameof(MaterialProperty.PropertyType));
        var spacing = EditorGUIUtility.standardVerticalSpacing;
        var rect = position;

        rect.height = EditorGUI.GetPropertyHeight(propertyName, GUIContent.none, true);
        EditorGUI.PropertyField(rect, propertyName, true);
        rect.y += rect.height + spacing;
        rect.height = EditorGUI.GetPropertyHeight(propertyType, GUIContent.none, true);
        EditorGUI.PropertyField(rect, propertyType, true);
        rect.y += rect.height + spacing;

        var type = (ShaderPropertyType)propertyType.enumValueIndex;
        switch (type)
        {
            case ShaderPropertyType.Texture:
                var textureValue = property.FindPropertyRelative(nameof(MaterialProperty.TextureValue));
                var textureOffsetValue = property.FindPropertyRelative(nameof(MaterialProperty.TextureOffsetValue));
                var textureScaleValue = property.FindPropertyRelative(nameof(MaterialProperty.TextureScaleValue));
                rect.height = EditorGUI.GetPropertyHeight(textureValue, GUIContent.none, true);
                EditorGUI.PropertyField(rect, textureValue, true);
                rect.y += rect.height + spacing;
                rect.height = EditorGUI.GetPropertyHeight(textureOffsetValue, GUIContent.none, true);
                EditorGUI.PropertyField(rect, textureOffsetValue, true);
                rect.y += rect.height + spacing;
                rect.height = EditorGUI.GetPropertyHeight(textureScaleValue, GUIContent.none, true);
                EditorGUI.PropertyField(rect, textureScaleValue, true);
                break;
            case ShaderPropertyType.Color:
                var colorValue = property.FindPropertyRelative(nameof(MaterialProperty.ColorValue));
                rect.height = EditorGUI.GetPropertyHeight(colorValue, GUIContent.none, true);
                EditorGUI.PropertyField(rect, colorValue, true);
                break;
            case ShaderPropertyType.Vector:
                var vectorValue = property.FindPropertyRelative(nameof(MaterialProperty.VectorValue));
                rect.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.BeginChangeCheck();
                Vector4 newValue = EditorGUI.Vector4Field(rect, "VectorValue", vectorValue.vector4Value);
                if (EditorGUI.EndChangeCheck())
                    vectorValue.vector4Value = newValue;
                break;
            case ShaderPropertyType.Int:
                var intValue = property.FindPropertyRelative(nameof(MaterialProperty.IntValue));
                rect.height = EditorGUI.GetPropertyHeight(intValue, GUIContent.none, true);
                EditorGUI.PropertyField(rect, intValue, true);
                break;
            case ShaderPropertyType.Float:
            case ShaderPropertyType.Range:
                var floatValue = property.FindPropertyRelative(nameof(MaterialProperty.FloatValue));
                rect.height = EditorGUI.GetPropertyHeight(floatValue, GUIContent.none, true);
                EditorGUI.PropertyField(rect, floatValue, true);
                break;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var spacing = EditorGUIUtility.standardVerticalSpacing;
        var propertyName = property.FindPropertyRelative(nameof(MaterialProperty.PropertyName));
        var propertyType = property.FindPropertyRelative(nameof(MaterialProperty.PropertyType));
        var height = EditorGUI.GetPropertyHeight(propertyName, GUIContent.none, true) + spacing
            + EditorGUI.GetPropertyHeight(propertyType, GUIContent.none, true) + spacing;
        switch ((ShaderPropertyType)propertyType.enumValueIndex)
        {
            case ShaderPropertyType.Texture:
                var textureValue = property.FindPropertyRelative(nameof(MaterialProperty.TextureValue));
                var textureOffsetValue = property.FindPropertyRelative(nameof(MaterialProperty.TextureOffsetValue));
                var textureScaleValue = property.FindPropertyRelative(nameof(MaterialProperty.TextureScaleValue));
                height += EditorGUI.GetPropertyHeight(textureValue, GUIContent.none, true) + spacing
                    + EditorGUI.GetPropertyHeight(textureOffsetValue, GUIContent.none, true) + spacing
                    + EditorGUI.GetPropertyHeight(textureScaleValue, GUIContent.none, true);
                break;
            case ShaderPropertyType.Color:
                var colorValue = property.FindPropertyRelative(nameof(MaterialProperty.ColorValue));
                height += EditorGUI.GetPropertyHeight(colorValue, GUIContent.none, true);
                break;
            case ShaderPropertyType.Vector:
            case ShaderPropertyType.Int:
            case ShaderPropertyType.Float:
            case ShaderPropertyType.Range:
                height += EditorGUIUtility.singleLineHeight;
                break;
        }
        return height;
    }
}
