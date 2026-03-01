namespace Aoyon.MaterialEditor.UI;

[CustomPropertyDrawer(typeof(RendererReference))]
internal class RendererReferenceDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var objectReference = property.FindPropertyRelative(nameof(RendererReference.ObjectReference));
        var rendererIndex = property.FindPropertyRelative(nameof(RendererReference.RendererIndex));
        
        float spacing = 4f;
        float rendererIndexWidth = 48f;

        var objectRect = new Rect(position.x, position.y, position.width - rendererIndexWidth - spacing, position.height);
        var indexRect = new Rect(position.x + position.width - rendererIndexWidth, position.y, rendererIndexWidth, position.height);

        EditorGUI.PropertyField(objectRect, objectReference, label);
        EditorGUI.PropertyField(indexRect, rendererIndex, GUIContent.none);

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }
}