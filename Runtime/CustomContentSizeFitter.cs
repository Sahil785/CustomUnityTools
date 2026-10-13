using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways] // run in edit mode too
[RequireComponent(typeof(RectTransform))]
/// <summary>
/// CustomContentSizeFitter
/// -----------------------
/// Dynamically adjusts this RectTransform’s size to enclose a set of target RectTransforms,
/// with optional padding.
/// 
/// Key behavior:
/// • Bounding box calculation → always computes the true enclosing bounds of all targets
///   relative to this RectTransform.
/// • FitHorizontal → width matches the bounding box width (+ horizontal padding).
/// • FitVertically → height matches the bounding box height (+ vertical padding).
/// • Both enabled → full bounding box (width and height).
/// • Runs in edit mode and at runtime; only updates when the raw target bounds change.
/// 
/// Usage:
/// - Add to a GameObject with a RectTransform.
/// - Assign target RectTransforms in the inspector.
/// - Toggle FitHorizontal / FitVertically depending on which axes should resize.
/// - Adjust CustomPadding to add extra spacing around the fitted bounds.
/// - Enable UseTextPreferredSize to respect Text / TMP preferred content sizes.
/// - Set MaxWidth / MaxHeight to limit expansion (0 = unlimited).
/// - Enable IncludeSelfText to let TMP/Text on the same GameObject contribute.
/// </summary>
public class CustomContentSizeFitter : MonoBehaviour
{
    [Tooltip("List of RectTransforms to enclose. Leave empty to use IncludeSelfText if enabled.")]
    public List<RectTransform> TargetObjects = new();

    [Tooltip("Padding applied around the computed bounds (horizontal/vertical).")]
    public CustomPadding Padding = new();

    [Tooltip("If true, adjust this RectTransform's height to fit the targets.")]
    public bool FitVertically = false;

    [Tooltip("If true, adjust this RectTransform's width to fit the targets.")]
    public bool FitHorizontal = false;

    // NEW: when true, include TMP/Text on this GameObject even if not listed in TargetObjects.
    [Tooltip("Include Text/TMP on this GameObject as a target even if not listed in TargetObjects.")]
    public bool IncludeSelfText = false;

    [Tooltip("When true, use Text/TMP preferred sizes instead of the target RectTransform's rect size.")]
    public bool UseTextPreferredSize = true;

    [Tooltip("Maximum width after fitting (0 = no limit).")]
    public float MaxWidth = 0f;

    [Tooltip("Maximum height after fitting (0 = no limit).")]
    public float MaxHeight = 0f;

    private RectTransform MyRT;
    private Vector2 LastTargetSize = Vector2.zero;

    private void Awake()
    {
        MyRT = GetComponent<RectTransform>();
    }

    private void LateUpdate()
    {
        if ((TargetObjects == null || TargetObjects.Count == 0) && !IncludeSelfText)
            return;

        if (MyRT == null)
            return;

        Vector2 combinedSize = CalculateCombinedSize();

        if ((FitHorizontal && !Mathf.Approximately(LastTargetSize.x, combinedSize.x)) ||
            (FitVertically && !Mathf.Approximately(LastTargetSize.y, combinedSize.y)))
        {
            ApplyFit(combinedSize);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (MyRT == null)
            MyRT = GetComponent<RectTransform>();
        if ((TargetObjects != null && TargetObjects.Count > 0) || IncludeSelfText)
            ApplyFit(CalculateCombinedSize());
    }
#endif

    private void ApplyFit(Vector2 rawTargetSize)
    {
        if ((TargetObjects == null || TargetObjects.Count == 0) && !IncludeSelfText)
            return;
        if (MyRT == null)
            return;

        Vector2 paddedSize = rawTargetSize;
        paddedSize.x += Padding.Horizontal * 2;
        paddedSize.y += Padding.Vertical * 2;

        if (MaxWidth > 0f)
            paddedSize.x = Mathf.Min(paddedSize.x, MaxWidth);
        if (MaxHeight > 0f)
            paddedSize.y = Mathf.Min(paddedSize.y, MaxHeight);

        Vector2 newSize = MyRT.sizeDelta;
        if (FitHorizontal)
            newSize.x = paddedSize.x;
        if (FitVertically)
            newSize.y = paddedSize.y;

        MyRT.sizeDelta = newSize;
        LastTargetSize = rawTargetSize;
    }

    private Vector2 CalculateCombinedSize()
    {
        if ((TargetObjects == null || TargetObjects.Count == 0) && !IncludeSelfText)
            return Vector2.zero;
        if (MyRT == null)
            return Vector2.zero;

        Vector2 boundingBox = CalculateBoundingBox();

        if (FitHorizontal && !FitVertically)
            return new Vector2(boundingBox.x, MyRT.sizeDelta.y);
        else if (FitVertically && !FitHorizontal)
            return new Vector2(MyRT.sizeDelta.x, boundingBox.y);
        else
            return boundingBox;
    }

    private Vector2 CalculateBoundingBox()
    {
        if (MyRT == null)
            return Vector2.zero;

        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;

        // Build effective target list: either TargetObjects (if any) or optionally the MyRT itself
        var effectiveTargets = new List<RectTransform>();
        if (TargetObjects != null && TargetObjects.Count > 0)
        {
            effectiveTargets.AddRange(TargetObjects);
        }
        else if (IncludeSelfText)
        {
            effectiveTargets.Add(MyRT);
        }

        foreach (var rt in effectiveTargets)
        {
            if (rt == null)
                continue;

            // Skip self only when explicitly not allowed
            if (rt == MyRT && !IncludeSelfText)
                continue;

            Vector2 targetSize = rt.rect.size;

            if (UseTextPreferredSize)
            {
                var tmp = rt.GetComponent<TMP_Text>();
                if (tmp != null)
                {
                    tmp.ForceMeshUpdate();
                    float prefW = tmp.preferredWidth;
                    float prefH = tmp.preferredHeight;
                    if (prefW > 0f)
                        targetSize.x = prefW;
                    if (prefH > 0f)
                        targetSize.y = prefH;
                }
                else
                {
                    var utext = rt.GetComponent<Text>();
                    if (utext != null)
                    {
                        var genSettingsW = utext.GetGenerationSettings(new Vector2(1000f, utext.rectTransform.rect.size.y));
                        float prefW = utext.cachedTextGenerator.GetPreferredWidth(utext.text, genSettingsW) / utext.pixelsPerUnit;

                        var genSettingsH = utext.GetGenerationSettings(new Vector2(prefW * utext.pixelsPerUnit, 1000f));
                        float prefH = utext.cachedTextGenerator.GetPreferredHeight(utext.text, genSettingsH) / utext.pixelsPerUnit;

                        if (prefW > 0f)
                            targetSize.x = prefW;
                        if (prefH > 0f)
                            targetSize.y = prefH;
                    }
                }
            }

            Vector3[] localCorners = new Vector3[4]
            {
                new Vector3(-targetSize.x * 0.5f, -targetSize.y * 0.5f, 0f) + (Vector3)rt.rect.center,
                new Vector3(-targetSize.x * 0.5f,  targetSize.y * 0.5f, 0f) + (Vector3)rt.rect.center,
                new Vector3( targetSize.x * 0.5f,  targetSize.y * 0.5f, 0f) + (Vector3)rt.rect.center,
                new Vector3( targetSize.x * 0.5f, -targetSize.y * 0.5f, 0f) + (Vector3)rt.rect.center
            };

            foreach (var cornerLocal in localCorners)
            {
                Vector3 worldCorner = rt.TransformPoint(cornerLocal);
                Vector3 localCorner = MyRT.InverseTransformPoint(worldCorner);

                if (localCorner.x < minX)
                    minX = localCorner.x;
                if (localCorner.y < minY)
                    minY = localCorner.y;
                if (localCorner.x > maxX)
                    maxX = localCorner.x;
                if (localCorner.y > maxY)
                    maxY = localCorner.y;
            }
        }

        if (minX == float.MaxValue || minY == float.MaxValue || maxX == float.MinValue || maxY == float.MinValue)
            return Vector2.zero;

        float width = maxX - minX;
        float height = maxY - minY;

        if (float.IsNaN(width) || width < 0f)
            width = 0f;
        if (float.IsNaN(height) || height < 0f)
            height = 0f;

        return new Vector2(width, height);
    }

    [Serializable]
    public class CustomPadding
    {
        [Tooltip("Horizontal padding (added to both left and right).")]
        public float Horizontal = 0;

        [Tooltip("Vertical padding (added to both top and bottom).")]
        public float Vertical = 0;
    }
}