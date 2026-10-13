using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CustomShadowAndOutline))]
[CanEditMultipleObjects]
public class CustomOutlineEditor : Editor
{
    SerializedProperty useOutline;
    SerializedProperty outlineColor;
    SerializedProperty outlineThickness;

    SerializedProperty useShadow;
    SerializedProperty shadowColor;
    SerializedProperty shadowOffset;
    SerializedProperty shadowSoftness;

    CustomShadowAndOutline outline;

    void OnEnable()
    {
        outline = (CustomShadowAndOutline)target;

        useOutline = serializedObject.FindProperty("useOutline");
        outlineColor = serializedObject.FindProperty("outlineColor");
        outlineThickness = serializedObject.FindProperty("outlineThickness");

        useShadow = serializedObject.FindProperty("useShadow");
        shadowColor = serializedObject.FindProperty("shadowColor");
        shadowOffset = serializedObject.FindProperty("shadowOffset");
        shadowSoftness = serializedObject.FindProperty("shadowSoftness");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.PropertyField(useOutline);
        EditorGUILayout.PropertyField(outlineColor);
        EditorGUILayout.PropertyField(outlineThickness);

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(useShadow);
        EditorGUILayout.PropertyField(shadowColor);
        EditorGUILayout.PropertyField(shadowOffset);
        EditorGUILayout.PropertyField(shadowSoftness);

        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            outline.ApplyEffects();
        }
    }
}