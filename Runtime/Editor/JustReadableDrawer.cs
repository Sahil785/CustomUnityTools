using System;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(JustReadableAttribute))]
public class JustReadableDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (property == null)
            return base.GetPropertyHeight(property, label);

        // Arrays: show one line for the array and one line per element
        if (property.isArray && property.propertyType != SerializedPropertyType.String)
        {
            float height = EditorGUIUtility.singleLineHeight; // header line
            for (int i = 0; i < property.arraySize; i++)
            {
                var element = property.GetArrayElementAtIndex(i);
                // Use Unity's property height for each element for correctness
                height += EditorGUI.GetPropertyHeight(element, GUIContent.none, true) + EditorGUIUtility.standardVerticalSpacing;
            }
            return height + EditorGUIUtility.standardVerticalSpacing;
        }

        // For complex/managed types, delegate to Unity to compute height
        if (property.propertyType == SerializedPropertyType.Generic || property.propertyType == SerializedPropertyType.ManagedReference)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        return EditorGUIUtility.singleLineHeight;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property == null)
            return;

        EditorGUI.BeginProperty(position, label, property);

        // Use PrefixLabel so Unity handles folding, tooltip, and drag/drop properly
        Rect contentRect = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        // read attribute options if present
        var opts = attribute as JustReadableAttribute;
        int maxInline = opts != null ? opts.MaxInlineLength : 64;
        bool showTooltip = opts != null ? opts.ShowTooltip : true;

        if (property.isArray && property.propertyType != SerializedPropertyType.String)
        {
            // Draw header line (array size summary)
            string header = $"Array ({property.arraySize})";
            EditorGUI.LabelField(new Rect(contentRect.x, contentRect.y, contentRect.width, EditorGUIUtility.singleLineHeight), header, EditorStyles.boldLabel);

            // Draw each element on subsequent lines as read-only fields
            float y = contentRect.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.indentLevel++;
            for (int i = 0; i < property.arraySize; i++)
            {
                var element = property.GetArrayElementAtIndex(i);
                float h = EditorGUI.GetPropertyHeight(element, GUIContent.none, true);
                Rect elemRect = new Rect(contentRect.x, y, contentRect.width, h);

                // Render element label and value; show object fields as disabled object fields
                GUI.enabled = false;
                EditorGUI.PropertyField(elemRect, element, new GUIContent($"Element {i}"), true);
                GUI.enabled = true;

                y += h + EditorGUIUtility.standardVerticalSpacing;
            }
            EditorGUI.indentLevel--;
        }
        else
        {
            // Single-line display for supported simple types
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    DrawSingleLine(contentRect, property.intValue.ToString());
                    break;
                case SerializedPropertyType.Boolean:
                    DrawSingleLine(contentRect, property.boolValue.ToString());
                    break;
                case SerializedPropertyType.Float:
                    DrawSingleLine(contentRect, property.floatValue.ToString("0.00000"));
                    break;
                case SerializedPropertyType.String:
                    DrawSingleLineWithTooltip(contentRect, property.stringValue, maxInline, showTooltip);
                    break;
                case SerializedPropertyType.Enum:
                    DrawEnumSafe(contentRect, property);
                    break;
                case SerializedPropertyType.Vector3:
                    DrawSingleLine(contentRect, property.vector3Value.ToString());
                    break;
                case SerializedPropertyType.Vector2:
                    DrawSingleLine(contentRect, property.vector2Value.ToString());
                    break;
                case SerializedPropertyType.Vector3Int:
                    DrawSingleLine(contentRect, property.vector3IntValue.ToString());
                    break;
                case SerializedPropertyType.Vector2Int:
                    DrawSingleLine(contentRect, property.vector2IntValue.ToString());
                    break;
                case SerializedPropertyType.Quaternion:
                    DrawSingleLine(contentRect, property.quaternionValue.ToString());
                    break;
                case SerializedPropertyType.ObjectReference:
                    DrawObjectReference(contentRect, property);
                    break;
                case SerializedPropertyType.Generic:
                case SerializedPropertyType.ManagedReference:
                    // Let Unity render the complex type, read-only
                    GUI.enabled = false;
                    EditorGUI.PropertyField(contentRect, property, GUIContent.none, true);
                    GUI.enabled = true;
                    break;
                default:
                    DrawSingleLine(contentRect, "(not supported)");
                    break;
            }
        }

        EditorGUI.EndProperty();
    }

    private void DrawSingleLine(Rect rect, string value)
    {
        EditorGUI.LabelField(rect, value);
    }

    private void DrawSingleLineWithTooltip(Rect rect, string value, int maxInline, bool showTooltip)
    {
        if (string.IsNullOrEmpty(value))
        {
            EditorGUI.LabelField(rect, "");
            return;
        }

        if (value.Length > maxInline)
        {
            string truncated = value.Substring(0, maxInline) + "...";
            var content = showTooltip ? new GUIContent(truncated, value) : new GUIContent(truncated);
            EditorGUI.LabelField(rect, content);
        }
        else
        {
            var content = new GUIContent(value);
            EditorGUI.LabelField(rect, content);
        }
    }

    private void DrawEnumSafe(Rect rect, SerializedProperty prop)
    {
        string[] names = prop.enumNames;
        int idx = prop.enumValueIndex;
        string value = "(invalid)";
        if (names != null && idx >= 0 && idx < names.Length)
            value = names[idx];
        EditorGUI.LabelField(rect, value);
    }

    private void DrawObjectReference(Rect rect, SerializedProperty prop)
    {
        UnityEngine.Object obj = prop.objectReferenceValue;
        // Show object field in disabled state so user can see icon and name but cannot change it here
        GUI.enabled = false;
        EditorGUI.ObjectField(rect, obj, typeof(UnityEngine.Object), true);
        GUI.enabled = true;
    }
}
