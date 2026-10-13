using UnityEditor;
using UnityEngine;

public class Vector2RangeAttribute : PropertyAttribute
{
    public float min;
    public float max;

    public Vector2RangeAttribute(float min, float max)
    {
        this.min = min;
        this.max = max;
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(Vector2RangeAttribute))]
public class Vector2RangeDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight * 3f + EditorGUIUtility.standardVerticalSpacing * 2f;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.Vector2)
        {
            EditorGUI.LabelField(position, label.text, "Use Vector2Range with Vector2.");
            return;
        }

        Vector2RangeAttribute range = (Vector2RangeAttribute)attribute;

        EditorGUI.BeginProperty(position, label, property);

        // Draw label
        Rect labelRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.PrefixLabel(labelRect, label);

        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        // Slider rects
        Rect xSliderRect = new Rect(position.x, position.y + lineHeight + spacing, position.width, lineHeight);
        Rect ySliderRect = new Rect(position.x, position.y + 2 * (lineHeight + spacing), position.width, lineHeight);

        // Get and draw values
        Vector2 value = property.vector2Value;

        EditorGUI.BeginChangeCheck();
        value.x = EditorGUI.Slider(xSliderRect, "X", value.x, range.min, range.max);
        value.y = EditorGUI.Slider(ySliderRect, "Y", value.y, range.min, range.max);
        if (EditorGUI.EndChangeCheck())
        {
            property.vector2Value = value;
        }

        EditorGUI.EndProperty();
    }
} 
#endif
