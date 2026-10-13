using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

[RequireComponent(typeof(Image))]
/// <summary>
/// ImageHighlighter - a small, clear component that switches an Image's sprite while
/// the pointer is over it. It supports three modes: Normal, DisabledAware and Special.
/// - Normal: simple hover -> hoverSprite.
/// - DisabledAware: only show hover if an optional parent Button is interactable; if it becomes non-interactable while hovered, the sprite resets.
/// - Special: uses the special sprites when specialCaseOn is true; useful for alternative visuals.
/// The component is lightweight and uses the EventSystem pointer callbacks plus a short coroutine to detect pointer exit.
/// </summary>
public class ImageHighlighter : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public enum HighlightMode { Normal, DisabledAware, Special }

    [Header("Sprites")]
    [Tooltip("Sprite used when not hovered (default state). If empty the Image's existing sprite is preserved on enable.")]
    public Sprite defaultSprite;

    [Tooltip("Sprite to show while the pointer is over this element.")]
    public Sprite hoverSprite;

    [Tooltip("(Special mode) Default sprite when specialCaseOn is true.")]
    public Sprite specialDefaultSprite;

    [Tooltip("(Special mode) Hover sprite when specialCaseOn is true.")]
    public Sprite specialHoverSprite;

    [Header("Settings")]
    [Tooltip("Choosing DisabledAware will respect the parent Button's interactable state if a parentButton is assigned.")]
    public HighlightMode highlightMode = HighlightMode.Normal;

    [Tooltip("When in Special mode and this flag is true, special sprites are used.")]
    public bool specialCaseOn;

    [Tooltip("Optional parent Button. Used by DisabledAware mode to determine whether hover visuals should be shown.")]
    public Button parentButton;

    // Cached references and state
    private Image sourceImg;
    private bool isHovered;
    private Coroutine hoverCheckCoroutine;

    private void Awake()
    {
        sourceImg = GetComponent<Image>();
    }

    private void OnEnable()
    {
        // If no explicit default sprite provided, keep whatever is currently set on the Image.
        if (defaultSprite != null)
            sourceImg.sprite = defaultSprite;
    }

    /// <summary>
    /// Pointer entered - apply hover sprite depending on the selected mode.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!gameObject.activeInHierarchy || sourceImg == null)
            return;

        isHovered = true;

        // Decide whether we should show hover based on mode and optional parent button state
        switch (highlightMode)
        {
            case HighlightMode.DisabledAware:
                if (parentButton == null || parentButton.interactable)
                    sourceImg.sprite = GetHoverSprite();
                else
                    sourceImg.sprite = GetDefaultSprite();
                break;

            case HighlightMode.Special:
                sourceImg.sprite = GetHoverSprite();
                break;

            default:
                sourceImg.sprite = GetHoverSprite();
                break;
        }

        // Start monitoring pointer exit and parent button state while hovered
        hoverCheckCoroutine ??= StartCoroutine(HoverMonitorCoroutine());
    }

    /// <summary>
    /// Pointer exited - reset visuals and stop monitor.
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (sourceImg == null)
            return;

        isHovered = false;
        ResetSprite();

        if (hoverCheckCoroutine != null)
        {
            StopCoroutine(hoverCheckCoroutine);
            hoverCheckCoroutine = null;
        }
    }

    /// <summary>
    /// Monitors pointer position and parent button state while hovered.
    /// If the pointer leaves the RectTransform or a parent button becomes non-interactable (when relevant), sprite resets.
    /// </summary>
    private IEnumerator HoverMonitorCoroutine()
    {
        var rect = transform as RectTransform;
        while (isHovered)
        {
            // If we have a parentButton and we're in DisabledAware mode, reset when it becomes disabled
            if (highlightMode == HighlightMode.DisabledAware && parentButton != null && !parentButton.interactable)
            {
                isHovered = false;
                ResetSprite();
                break;
            }

            // Check whether the mouse is still inside this UI element. Use ScreenPoint check with current EventSystem camera when available.
            bool contains = false;
            if (rect != null)
            {
                Camera cam = null;
                var canvas = GetComponentInParent<Canvas>();
                if (canvas != null)
                    cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

                contains = RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, cam);
            }

            if (!contains)
            {
                // Trigger exit behavior
                isHovered = false;
                ResetSprite();
                break;
            }

            yield return null;
        }

        hoverCheckCoroutine = null;
    }

    /// <summary>
    /// Resets the sprite back to the appropriate default for the selected mode.
    /// </summary>
    public void ResetSprite()
    {
        if (sourceImg == null)
            return;

        sourceImg.sprite = GetDefaultSprite();
    }

    private Sprite GetDefaultSprite()
    {
        return highlightMode switch
        {
            HighlightMode.Special => specialCaseOn && specialDefaultSprite != null ? specialDefaultSprite : defaultSprite != null ? defaultSprite : sourceImg.sprite,
            _ => defaultSprite != null ? defaultSprite : sourceImg.sprite,
        };
    }

    private Sprite GetHoverSprite()
    {
        return highlightMode switch
        {
            HighlightMode.Special => specialCaseOn && specialHoverSprite != null ? specialHoverSprite : hoverSprite != null ? hoverSprite : sourceImg.sprite,
            _ => hoverSprite != null ? hoverSprite : sourceImg.sprite,
        };
    }
}
