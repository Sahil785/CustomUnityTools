using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System;

// Fallback editor that applies only when the inspected object is CustomContentSizeFitter (by name)
[CustomEditor(typeof(MonoBehaviour), true)]
public class CustomContentSizeFitterEditor : Editor
{
    SerializedProperty targetObjectsProp;
    SerializedProperty paddingProp;
    SerializedProperty fitVerticallyProp;
    SerializedProperty fitHorizontalProp;
    SerializedProperty includeSelfTextProp;
    SerializedProperty useTextPreferredSizeProp;
    SerializedProperty maxWidthProp;
    SerializedProperty maxHeightProp;

    void OnEnable()
    {
        targetObjectsProp = serializedObject.FindProperty("TargetObjects");
        paddingProp = serializedObject.FindProperty("Padding");
        fitVerticallyProp = serializedObject.FindProperty("FitVertically");
        fitHorizontalProp = serializedObject.FindProperty("FitHorizontal");
        includeSelfTextProp = serializedObject.FindProperty("IncludeSelfText");
        useTextPreferredSizeProp = serializedObject.FindProperty("UseTextPreferredSize");
        maxWidthProp = serializedObject.FindProperty("MaxWidth");
        maxHeightProp = serializedObject.FindProperty("MaxHeight");
    }

    public override void OnInspectorGUI()
    {
        // Only customize inspector for CustomContentSizeFitter (avoid affecting other MonoBehaviours)
        if (target == null || target.GetType().Name != "CustomContentSizeFitter")
        {
            base.OnInspectorGUI();
            return;
        }

        serializedObject.Update();

        EditorGUILayout.PropertyField(targetObjectsProp, true);
        EditorGUILayout.PropertyField(paddingProp, true);
        EditorGUILayout.PropertyField(fitHorizontalProp);
        EditorGUILayout.PropertyField(fitVerticallyProp);

        // Determine whether to show the Text/TMP specific options.
        var t = target as MonoBehaviour;
        bool hasTextOrTmp = false;

        if (t != null)
        {
            // Check same GameObject for UnityEngine.UI.Text
            var uiText = t.GetComponent<Text>();
            if (uiText != null)
                hasTextOrTmp = true;

            // Check TMP by type name to avoid hard dependency issues
            if (!hasTextOrTmp)
            {
                var tmpType = Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro");
                if (tmpType != null)
                {
                    var comp = t.GetComponent(tmpType);
                    if (comp != null)
                        hasTextOrTmp = true;
                }
            }
        }

        if (hasTextOrTmp)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Optional Text/TMP Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(includeSelfTextProp);
            EditorGUILayout.PropertyField(useTextPreferredSizeProp);
        }

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(maxWidthProp);
        EditorGUILayout.PropertyField(maxHeightProp);

        serializedObject.ApplyModifiedProperties();
    }
}
