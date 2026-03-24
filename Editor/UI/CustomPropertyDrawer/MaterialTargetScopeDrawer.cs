using UnityEngine.Pool;

namespace Aoyon.MaterialEditor.UI;

[CustomPropertyDrawer(typeof(MaterialTargetScope))]
internal class MaterialTargetScopeDrawer : PropertyDrawer
{
    private static readonly GUIStyle _typeStyle = StyleHelper.CenteredPopupStyle;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        using var _ = new EditorGUI.PropertyScope(position, GUIContent.none, property);

        position.SetSingleHeight();

        var type = property.FindPropertyRelative(nameof(MaterialTargetScope.Type));

        using var _1 = ListPool<string>.Get(out var optionKeys);
        LocalizedUI.GetEnumOptionKeys(typeof(MaterialTargetScope.ScopeType), optionKeys);

        var typeWidth = CalculateTypeWidth(optionKeys);

        GUIHelper.SplitRectHorizontallyForLeft(position, typeWidth, out var typeRect, out var valueRect);

        LocalizedPopup.Field(typeRect, type, null, optionKeys, _typeStyle);

        switch ((MaterialTargetScope.ScopeType)type.enumValueIndex)
        {
            case MaterialTargetScope.ScopeType.Asset:
                var material = property.FindPropertyRelative(nameof(MaterialTargetScope.Material));
                EditorGUI.PropertyField(valueRect, material, GUIContent.none);
                break;
            case MaterialTargetScope.ScopeType.Slot:
                var materialSlotReference = property.FindPropertyRelative(nameof(MaterialTargetScope.MaterialSlotReference));
                EditorGUI.PropertyField(valueRect, materialSlotReference, GUIContent.none);
                break;
        }
    }

    private static float CalculateTypeWidth(List<string> optionKeys)
    {
        return optionKeys.Select(k => _typeStyle.CalcSize(k.LG()).x).Max() + 8f;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return GUIHelper.propertyHeight;
    }
}