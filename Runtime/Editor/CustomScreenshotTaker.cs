using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

public class ScreenshotWindow : EditorWindow
{
    private Camera TargetCamera;
    private int SuperSize = 1;
    private string BaseFileName = "Screenshot";
    private bool AppendSceneName = true;
    private bool AppendCameraName = true;
    private bool AppendTimestamp = true;

    private double LastRefreshTime = 0;
    private const double RefreshInterval = 1.0;
    private string ScreenshotsFolder = "Assets/Resources/Screenshots";
    private string LastImportedFile = null;

    private const int MaxPreviewCount = 10;
    private List<string> PreviewAssetPaths = new List<string>();
    private List<Texture2D> PreviewTextures = new List<Texture2D>();
    private Vector2 previewScroll = Vector2.zero;

    // Window sizing helpers
    private float baseWindowHeight = 280f;
    private bool prevHadPreviews = false;

    [MenuItem("Tools/Custom Screenshot %#k")]
    public static void ShowWindow()
    {
        var window = GetWindow<ScreenshotWindow>("Screenshot Tool", true);
        window.position = new Rect(Screen.width / 2f, Screen.height / 2f, 360, 280);
        window.TargetCamera = Camera.main;
        window.Show();
    }

    private void OnEnable()
    {
        EditorApplication.update += PollForNewScreenshots;
        // Populate previews on enable
        AssetDatabase.Refresh();
        RefreshPreviewList();
    }

    private void OnDisable()
    {
        EditorApplication.update -= PollForNewScreenshots;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("📷 Custom Screenshot Tool", EditorStyles.boldLabel);

        BaseFileName = EditorGUILayout.TextField("Base File Name", BaseFileName);
        AppendSceneName = EditorGUILayout.ToggleLeft("Append Scene Name", AppendSceneName);
        AppendCameraName = EditorGUILayout.ToggleLeft("Append Camera Name", AppendCameraName);
        AppendTimestamp = EditorGUILayout.ToggleLeft("Append Timestamp", AppendTimestamp);

        TargetCamera = (Camera)EditorGUILayout.ObjectField(
            "Target Camera",
            TargetCamera != null ? TargetCamera : Camera.main,
            typeof(Camera),
            true
        );

        SuperSize = EditorGUILayout.IntSlider("Super Size", SuperSize, 1, 4);

        Vector2 gv = GameViewSizeUtil.GetMainGameViewSize();
        int capW = Mathf.RoundToInt(gv.x * SuperSize);
        int capH = Mathf.RoundToInt(gv.y * SuperSize);

        EditorGUILayout.HelpBox(
            $"Base File Name: {BaseFileName}\n" +
            $"Super Size: x{SuperSize} — final resolution {capW} × {capH}px\n" +
            $"Output: Assets/Resources/Screenshots",
            MessageType.Info
        );

        if (GUILayout.Button("📸 Take Screenshot", GUILayout.Height(28)))
            TakeScreenshot();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Last refresh: {(LastImportedFile != null ? Path.GetFileName(LastImportedFile) : "None")}", EditorStyles.miniLabel);

        // Previews: only show if we have at least one preview
        bool hasPreviews = PreviewAssetPaths != null && PreviewAssetPaths.Count > 0;
        if (hasPreviews != prevHadPreviews)
        {
            // adjust window height when previews appear/disappear
            float width = Mathf.Max(position.width, 360f);
            float newHeight = baseWindowHeight;
            if (hasPreviews)
            {
                int perRow = 5;
                int rows = Mathf.CeilToInt(PreviewAssetPaths.Count / (float)perRow);
                float previewAreaHeight = rows * 96f + 60f; // approximate per-row height + padding
                newHeight = baseWindowHeight + previewAreaHeight;
            }
            this.position = new Rect(position.x, position.y, width, newHeight);
            prevHadPreviews = hasPreviews;
        }

        if (PreviewAssetPaths != null && PreviewAssetPaths.Count > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Previews", EditorStyles.boldLabel);

            previewScroll = EditorGUILayout.BeginScrollView(previewScroll, GUILayout.Height(180));
            int perRow = 5;
            for (int i = 0; i < PreviewAssetPaths.Count; i++)
            {
                if (i % perRow == 0)
                    GUILayout.BeginHorizontal();

                // Ensure texture is loaded
                var tex = PreviewTextures[i];
                if (tex == null && !string.IsNullOrEmpty(PreviewAssetPaths[i]))
                {
                    tex = AssetDatabase.LoadAssetAtPath<Texture2D>(PreviewAssetPaths[i]);
                    PreviewTextures[i] = tex;
                }
                if (tex != null)
                {
                    if (GUILayout.Button(tex, GUILayout.Width(160), GUILayout.Height(90)))
                    {
                        var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(PreviewAssetPaths[i]);
                        if (obj != null)
                        {
                            Selection.activeObject = obj;
                            EditorGUIUtility.PingObject(obj);
                        }
                    }
                }
                else
                {
                    // If texture missing (deleted), skip showing an empty placeholder; render a small box to keep layout stable
                    GUILayout.Box("[missing]", GUILayout.Width(160), GUILayout.Height(90));
                }

                if (i % perRow == perRow - 1 || i == PreviewAssetPaths.Count - 1)
                    GUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }
    }

    private void TakeScreenshot()
    {
        if (TargetCamera == null)
            TargetCamera = Camera.main;

        string filename = BaseFileName;
        if (AppendSceneName)
            filename += $"_{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}";
        if (AppendCameraName && TargetCamera != null)
            filename += $"_{TargetCamera.name}";
        if (AppendTimestamp)
            filename += $"_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
        filename += ".png";

        string folder = Path.Combine(Application.dataPath, "Resources", "Screenshots");
        Directory.CreateDirectory(folder);

        string fullPath = Path.Combine(folder, filename).Replace("\\", "/");

        ScreenCapture.CaptureScreenshot(fullPath, SuperSize);
        Debug.Log($"Screenshot triggered: {fullPath}");
    }

    private void PollForNewScreenshots()
    {
        if (!focusedWindow || focusedWindow != this)
            return;

        double now = EditorApplication.timeSinceStartup;
        if (now - LastRefreshTime < RefreshInterval)
            return;

        LastRefreshTime = now;

        string fullFolderPath = Path.Combine(Application.dataPath, "Resources", "Screenshots");
        if (!Directory.Exists(fullFolderPath))
            return;

        var files = Directory.GetFiles(fullFolderPath, "*.png");
        if (files.Length == 0)
        {
            // Clear previews if folder empty
            PreviewAssetPaths.Clear();
            PreviewTextures.Clear();
            LastImportedFile = null;
            return;
        }

        Array.Sort(files); // Sort by name
        string latest = files[files.Length - 1];

        if (latest != LastImportedFile)
        {
            // Refresh assets then rebuild preview list from newest -> oldest (up to MaxPreviewCount)
            AssetDatabase.Refresh();
            RefreshPreviewList();

            // Select the newest asset if available
            string newestAssetPath = latest.Replace(Application.dataPath, "Assets").Replace("\\", "/");
            var newestAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(newestAssetPath);
            if (newestAsset != null)
                Selection.activeObject = newestAsset;

            LastImportedFile = latest;
            Debug.Log($"Screenshots refreshed. Latest: {newestAssetPath}");
        }
    }

    private void RefreshPreviewList()
    {
        string fullFolderPath = Path.Combine(Application.dataPath, "Resources", "Screenshots");
        if (!Directory.Exists(fullFolderPath))
        {
            PreviewAssetPaths.Clear();
            PreviewTextures.Clear();
            return;
        }

        var files = Directory.GetFiles(fullFolderPath, "*.png");
        if (files.Length == 0)
        {
            PreviewAssetPaths.Clear();
            PreviewTextures.Clear();
            return;
        }

        Array.Sort(files);

        PreviewAssetPaths.Clear();
        PreviewTextures.Clear();

        int take = Math.Min(files.Length, MaxPreviewCount);
        for (int i = 0; i < take; i++)
        {
            string file = files[files.Length - 1 - i]; // newest first
            string assetPath = file.Replace(Application.dataPath, "Assets").Replace("\\", "/");
            PreviewAssetPaths.Add(assetPath);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            PreviewTextures.Add(tex);
        }

        LastImportedFile = files[files.Length - 1];
    }
}

static class GameViewSizeUtil
{
    public static Vector2 GetMainGameViewSize()
    {
        var T = Type.GetType("UnityEditor.GameView,UnityEditor");
        var method = T.GetMethod("GetSizeOfMainGameView", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return (Vector2)method.Invoke(null, null);
    }
}
