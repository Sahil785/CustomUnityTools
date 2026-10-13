using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

[CustomEditor(typeof(CustomImageHitDetection))]
public class CustomImageHitDetectionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDefaultInspector();

        var comp = (CustomImageHitDetection)target;
        if (comp == null)
        {
            serializedObject.ApplyModifiedProperties();
            return;
        }

        Image img = null;
        if (!comp.TryGetComponent(out img))
        {
            EditorGUILayout.HelpBox("Image component missing. This script requires an Image component.", MessageType.Warning);
            serializedObject.ApplyModifiedProperties();
            return;
        }

        if (img.sprite == null)
        {
            EditorGUILayout.HelpBox("No sprite assigned to the Image. Assign a sprite to enable alpha hit testing.", MessageType.Warning);
        }
        else
        {
            var tex = img.sprite.texture;
            if (tex == null)
            {
                EditorGUILayout.HelpBox("Assigned sprite has no underlying texture.", MessageType.Warning);
            }
            else
            {
                string path = AssetDatabase.GetAssetPath(tex);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    EditorGUILayout.HelpBox($"Could not find TextureImporter for '{tex.name}'.", MessageType.Warning);
                }
                else
                {
                    // Detect Crunch compression via reflection (property or field may vary by Unity version)
                    bool isCrunched = false;
                    var importerType = importer.GetType();
                    PropertyInfo prop = null;
                    FieldInfo field = null;
                    PropertyInfo prop2 = null;
                    FieldInfo field2 = null;

                    prop = importerType.GetProperty("crunchedCompression", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (prop != null && prop.PropertyType == typeof(bool))
                    {
                        isCrunched = (bool)prop.GetValue(importer);
                    }
                    else
                    {
                        field = importerType.GetField("crunchedCompression", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (field != null && field.FieldType == typeof(bool))
                        {
                            isCrunched = (bool)field.GetValue(importer);
                        }
                        else
                        {
                            // Some Unity versions used 'crunchCompression' naming
                            prop2 = importerType.GetProperty("crunchCompression", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                            if (prop2 != null && prop2.PropertyType == typeof(bool))
                                isCrunched = (bool)prop2.GetValue(importer);
                            else
                            {
                                field2 = importerType.GetField("crunchCompression", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                                if (field2 != null && field2.FieldType == typeof(bool))
                                    isCrunched = (bool)field2.GetValue(importer);
                            }
                        }
                    }

                    if (isCrunched)
                    {
                        EditorGUILayout.HelpBox("Sprite texture uses Crunch compression. Alpha hit-testing may not work correctly with crunched textures.", MessageType.Warning);
                        if (GUILayout.Button("Disable Crunch Compression"))
                        {
                            // Attempt to disable crunched compression via reflection and reimport
                            bool setSuccess = false;
                            if (prop != null && prop.PropertyType == typeof(bool))
                            {
                                prop.SetValue(importer, false);
                                setSuccess = true;
                            }
                            else if (field != null && field.FieldType == typeof(bool))
                            {
                                field.SetValue(importer, false);
                                setSuccess = true;
                            }
                            else if (prop2 != null && prop2.PropertyType == typeof(bool))
                            {
                                prop2.SetValue(importer, false);
                                setSuccess = true;
                            }
                            else if (field2 != null && field2.FieldType == typeof(bool))
                            {
                                field2.SetValue(importer, false);
                                setSuccess = true;
                            }

                            if (setSuccess)
                            {
                                importer.SaveAndReimport();
                                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                                EditorUtility.SetDirty(comp);
                            }
                            else
                            {
                                Debug.LogWarning($"Unable to modify Crunch compression flag for texture '{tex.name}' on this Unity version.");
                            }
                        }
                    }

                    // Then check Read/Write (isReadable)
                    if (!importer.isReadable)
                    {
                        EditorGUILayout.HelpBox("Sprite texture Read/Write is disabled. Alpha hit-testing will not work unless enabled.", MessageType.Warning);
                        if (GUILayout.Button("Enable Read/Write on Sprite"))
                        {
                            // Call the component helper which will toggle Read/Write and reimport
                            comp.EnableReadWriteOnSprite();
                            // After reimport, force the inspector to refresh so importer state updates
                            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                            EditorUtility.SetDirty(comp);
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Sprite texture Read/Write is enabled.", MessageType.Info);
                    }
                }
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
