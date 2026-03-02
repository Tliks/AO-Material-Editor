using UnityEngine.Rendering;

namespace Aoyon.MaterialEditor.UI;

[CustomPropertyDrawer(typeof(MaterialProperty))]
internal class MaterialPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        position.SetSingleHeight();

        var propertyName = property.FindPropertyRelative(nameof(MaterialProperty.PropertyName));
        var propertyType = property.FindPropertyRelative(nameof(MaterialProperty.PropertyType));

        EditorGUI.PropertyField(position, propertyName, GUIContent.none);
        position.NewLine();

        GUIHelper.SplitRectHorizontally(position, 0.3f, out var typeRect, out var valueRect);

        EditorGUI.PropertyField(typeRect, propertyType, GUIContent.none);
        var type = (ShaderPropertyType)propertyType.enumValueIndex;
        switch (type)
        {
            case ShaderPropertyType.Texture:
                var textureValue = property.FindPropertyRelative(nameof(MaterialProperty.TextureValue));
                var textureOffsetValue = property.FindPropertyRelative(nameof(MaterialProperty.TextureOffsetValue));
                var textureScaleValue = property.FindPropertyRelative(nameof(MaterialProperty.TextureScaleValue));
                EditorGUI.PropertyField(valueRect, textureValue, GUIContent.none);
                position.NewLine();
                var offsetScaleLabel = new GUIContent($"{"Label:MaterialProperty:TextureOffsetValue".LS()}・{"Label:MaterialProperty:TextureScaleValue".LS()}");
                var offsetScaleLabelWidth = GUI.skin.label.CalcSize(offsetScaleLabel).x;
                GUIHelper.SplitRectHorizontallyForLeft(position, offsetScaleLabelWidth, out var labelRect, out var fieldRect);
                EditorGUI.LabelField(labelRect, offsetScaleLabel);
                GUIHelper.SplitRectHorizontally(fieldRect, 0.5f, out var offsetRect, out var scaleRect);
                EditorGUI.PropertyField(offsetRect, textureOffsetValue, GUIContent.none);
                EditorGUI.PropertyField(scaleRect, textureScaleValue, GUIContent.none);
                break;
            case ShaderPropertyType.Color:
                var colorValue = property.FindPropertyRelative(nameof(MaterialProperty.ColorValue));
                EditorGUI.PropertyField(valueRect, colorValue, GUIContent.none);
                break;
            case ShaderPropertyType.Vector:
                var vectorValue = property.FindPropertyRelative(nameof(MaterialProperty.VectorValue));
                EditorGUI.BeginChangeCheck();
                var newValue = EditorGUI.Vector4Field(valueRect, GUIContent.none, vectorValue.vector4Value);
                if (EditorGUI.EndChangeCheck())
                    vectorValue.vector4Value = newValue;
                break;
            case ShaderPropertyType.Int:
                var intValue = property.FindPropertyRelative(nameof(MaterialProperty.IntValue));
                EditorGUI.PropertyField(valueRect, intValue, GUIContent.none);
                break;
            case ShaderPropertyType.Float:
            case ShaderPropertyType.Range:
                var floatValue = property.FindPropertyRelative(nameof(MaterialProperty.FloatValue));
                EditorGUI.PropertyField(valueRect, floatValue, GUIContent.none);
                break;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var height = 0f;
        height += GUIHelper.propertyHeight + GUIHelper.GUI_SPACE;

        var propertyType = property.FindPropertyRelative(nameof(MaterialProperty.PropertyType));
        switch ((ShaderPropertyType)propertyType.enumValueIndex)
        {
            case ShaderPropertyType.Texture:
                height += GUIHelper.propertyHeight + GUIHelper.GUI_SPACE;
                height += GUIHelper.propertyHeight;
                break;
            case ShaderPropertyType.Color:
            case ShaderPropertyType.Vector:
            case ShaderPropertyType.Int:
            case ShaderPropertyType.Float:
            case ShaderPropertyType.Range:
                height += GUIHelper.propertyHeight;
                break;
        }
        return height;
    }
}
