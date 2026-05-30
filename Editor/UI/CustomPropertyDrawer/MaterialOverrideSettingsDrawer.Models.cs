using UnityEngine.Rendering;

namespace Aoyon.MaterialEditor.UI;

[CustomPropertyDrawer(typeof(MaterialProperty))]
internal class MaterialPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        using var _ = new EditorGUI.PropertyScope(position, GUIContent.none, property);

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
                var offsetScaleLabel = new GUIContent($"{"materialProperty.textureOffset".LS()}・{"materialProperty.textureScale".LS()}");
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
                using (new EditorGUI.PropertyScope(valueRect, GUIContent.none, vectorValue))
                {
                    EditorGUI.BeginChangeCheck();
                    var newValue = EditorGUI.Vector4Field(valueRect, GUIContent.none, vectorValue.vector4Value);
                    if (EditorGUI.EndChangeCheck())
                        vectorValue.vector4Value = newValue;
                }
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

[CustomPropertyDrawer(typeof(MaterialKeywordStateOverride))]
internal class MaterialKeywordStateOverrideDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        using var _ = new EditorGUI.PropertyScope(position, GUIContent.none, property);

        MaterialStateOverrideDrawerGUI.DrawNameAndAction(
            position,
            property.FindPropertyRelative(nameof(MaterialKeywordStateOverride.Keyword)),
            property.FindPropertyRelative(nameof(MaterialKeywordStateOverride.Enabled)),
            MaterialStateOverrideDrawerGUI.KeywordActionOptions);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return GUIHelper.propertyHeight;
    }
}

[CustomPropertyDrawer(typeof(MaterialStringTagOverride))]
internal class MaterialStringTagOverrideDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        using var _ = new EditorGUI.PropertyScope(position, GUIContent.none, property);

        var tagName = property.FindPropertyRelative(nameof(MaterialStringTagOverride.TagName));
        var value = property.FindPropertyRelative(nameof(MaterialStringTagOverride.Value));
        var remove = property.FindPropertyRelative(nameof(MaterialStringTagOverride.Remove));

        GUIHelper.SplitRectHorizontallyForLeft(position, MaterialStateOverrideDrawerGUI.ActionFieldWidth, out var actionRect, out var contentRect);
        MaterialStateOverrideDrawerGUI.DrawStringTagAction(actionRect, remove);
        if (!remove.boolValue)
        {
            GUIHelper.SplitRectHorizontally(contentRect, 0.5f, out var tagNameRect, out var valueRect);
            EditorGUI.PropertyField(tagNameRect, tagName, GUIContent.none);
            EditorGUI.PropertyField(valueRect, value, GUIContent.none);
        }
        else
        {
            EditorGUI.PropertyField(contentRect, tagName, GUIContent.none);
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return GUIHelper.propertyHeight;
    }
}

[CustomPropertyDrawer(typeof(MaterialShaderPassStateOverride))]
internal class MaterialShaderPassStateOverrideDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        using var _ = new EditorGUI.PropertyScope(position, GUIContent.none, property);

        MaterialStateOverrideDrawerGUI.DrawNameAndAction(
            position,
            property.FindPropertyRelative(nameof(MaterialShaderPassStateOverride.PassName)),
            property.FindPropertyRelative(nameof(MaterialShaderPassStateOverride.Enabled)),
            MaterialStateOverrideDrawerGUI.ShaderPassActionOptions);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return GUIHelper.propertyHeight;
    }
}

internal static class MaterialStateOverrideDrawerGUI
{
    public const float ActionFieldWidth = 80f;

    public static readonly GUIContent[] KeywordActionOptions =
    {
        new("Remove"),
        new("Add"),
    };

    public static readonly GUIContent[] ShaderPassActionOptions =
    {
        new("Disable"),
        new("Enable"),
    };

    private static readonly GUIContent[] StringTagActionOptions =
    {
        new("Add・Edit"),
        new("Remove"),
    };

    public static void DrawNameAndAction(Rect position, SerializedProperty nameProperty, SerializedProperty enabledProperty, GUIContent[] options)
    {
        GUIHelper.SplitRectHorizontallyForLeft(position, ActionFieldWidth, out var actionRect, out var nameRect);
        var index = enabledProperty.boolValue ? 1 : 0;
        var newIndex = EditorGUI.Popup(actionRect, index, options, StyleHelper.MiddleCenteredPopupStyle);
        if (newIndex != index) enabledProperty.boolValue = newIndex == 1;
        EditorGUI.PropertyField(nameRect, nameProperty, GUIContent.none);
    }

    public static void DrawStringTagAction(Rect position, SerializedProperty removeProperty)
    {
        var index = removeProperty.boolValue ? 1 : 0;
        var newIndex = EditorGUI.Popup(position, index, StringTagActionOptions, StyleHelper.MiddleCenteredPopupStyle);
        if (newIndex != index) removeProperty.boolValue = newIndex == 1;
    }
}
