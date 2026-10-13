using System;
using UnityEngine;

/// <summary>
/// Marks a serialized field to be rendered as a compact, read-only
/// representation by the JustReadableDrawer in the Inspector.
/// Optional parameters allow per-field customization of tooltip behavior and
/// the maximum inline string length before truncation.
/// </summary>
[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
public sealed class JustReadableAttribute : PropertyAttribute
{
    /// <summary>
    /// Maximum number of characters to show inline before truncating and using a tooltip.
    /// </summary>
    public int MaxInlineLength { get; }

    /// <summary>
    /// Whether to show a tooltip containing the full value when a truncated value is displayed.
    /// </summary>
    public bool ShowTooltip { get; }

    public JustReadableAttribute(int maxInlineLength = 64, bool showTooltip = true)
    {
        MaxInlineLength = Mathf.Max(1, maxInlineLength);
        ShowTooltip = showTooltip;
    }
}
