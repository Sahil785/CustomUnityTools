using UnityEngine;
using TMPro;

[ExecuteAlways]
[RequireComponent(typeof(TMP_Text))]
/// <summary>
/// Combines outline and shadow/underlay settings for a TextMeshPro text component
/// into a single material and applies those effects at edit-time and runtime.
/// </summary>
public class CustomShadowAndOutline : MonoBehaviour
{
    /// <summary>
    /// Cached reference to the TextMeshPro text component on this GameObject.
    /// </summary>
    private TMP_Text _tmpText;

    /// <summary>
    /// Combined material instance that receives outline and underlay (shadow) properties.
    /// </summary>
    private Material _combinedMaterial;

    [Header("Outline Settings")]
    /// <summary>
    /// Enable or disable the outline effect.
    /// </summary>
    public bool useOutline = true;

    /// <summary>
    /// Color used for the outline when <see cref="useOutline"/> is true.
    /// </summary>
    public Color outlineColor = Color.black;

    /// <summary>
    /// Thickness of the outline. Value range is 0 (no outline) to 1 (maximum thickness).
    /// </summary>
    [Range(0, 1)] public float outlineThickness = 0.2f;

    [Header("Shadow Settings")]
    /// <summary>
    /// Enable or disable the shadow/underlay effect.
    /// </summary>
    public bool useShadow = true;

    /// <summary>
    /// Color used for the shadow/underlay when <see cref="useShadow"/> is true.
    /// </summary>
    public Color shadowColor = Color.black;

    /// <summary>
    /// Offset of the shadow/underlay in local text units. X/Y values typically in range [-1, 1].
    /// </summary>
    [Vector2Range(-1f, 1f)]
    public Vector2 shadowOffset = new Vector2(0, -1f);

    /// <summary>
    /// Softness of the shadow/underlay. 0 = hard, 1 = soft.
    /// </summary>
    [Range(0, 1)] public float shadowSoftness = 0.5f;

    /// <summary>
    /// Unity Awake callback. Ensures material is initialized when the component wakes.
    /// </summary>
    void Awake()
    {
        InitializeMaterial();
    }

    /// <summary>
    /// Unity editor callback invoked when properties are edited in the inspector.
    /// Reinitializes material so inspector changes are visible immediately.
    /// </summary>
    void OnValidate()
    {
        InitializeMaterial();
    }

    /// <summary>
    /// Ensures a combined material instance exists and is tied to the current font atlas.
    /// If the font atlas changes, a new combined material is created from the font's default material.
    /// </summary>
    private void InitializeMaterial()
    {
        if (_tmpText == null)
            _tmpText = GetComponent<TMP_Text>();

        if (_tmpText != null && _tmpText.font != null)
        {
            if (_combinedMaterial == null || _combinedMaterial.mainTexture != _tmpText.font.atlas)
            {
                Material defaultMaterial = _tmpText.font.material;
                if (defaultMaterial != null)
                {
                    _combinedMaterial = new Material(defaultMaterial)
                    {
                        name = "CombinedMaterial"
                    };
                }
            }

            ApplyEffects();
        }
    }

    /// <summary>
    /// Set the outline color and immediately apply the change to the combined material.
    /// </summary>
    /// <param name="color">New outline color.</param>
    public void SetOutlineColor(Color color)
    {
        outlineColor = color;
        ApplyEffects();
    }

    /// <summary>
    /// Set the shadow color and immediately apply the change to the combined material.
    /// </summary>
    /// <param name="color">New shadow color.</param>
    public void SetShadowColor(Color color)
    {
        shadowColor = color;
        ApplyEffects();
    }

    /// <summary>
    /// Set both outline and shadow colors and apply them immediately.
    /// </summary>
    /// <param name="outlineColor">New outline color.</param>
    /// <param name="shadowColor">New shadow color.</param>
    public void SetOutlineAndShadowColor(Color outlineColor, Color shadowColor)
    {
        this.outlineColor = outlineColor;
        this.shadowColor = shadowColor;
        ApplyEffects();
    }

    /// <summary>
    /// Applies the current outline and shadow settings to the combined material
    /// and assigns it to the TextMeshPro component's <c>fontSharedMaterial</c>.
    /// This method is safe to call at edit-time and runtime.
    /// </summary>
    public void ApplyEffects()
    {
        if (_tmpText == null || _combinedMaterial == null)
            return;

        if (useOutline)
        {
            _combinedMaterial.SetColor(ShaderUtilities.ID_OutlineColor, outlineColor);
            _combinedMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, outlineThickness);
        }
        else
        {
            _combinedMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0f);
        }

        if (useShadow)
        {
            _combinedMaterial.EnableKeyword("UNDERLAY_ON");
            _combinedMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, shadowColor);
            _combinedMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, shadowOffset.x);
            _combinedMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, shadowOffset.y);
            _combinedMaterial.SetFloat(ShaderUtilities.ID_UnderlaySoftness, shadowSoftness);
        }
        else
        {
            _combinedMaterial.DisableKeyword("UNDERLAY_ON");
            _combinedMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, Color.clear);
        }

        _tmpText.fontSharedMaterial = _combinedMaterial;
    }
}