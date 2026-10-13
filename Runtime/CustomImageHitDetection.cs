using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Image))]
/// <summary>
/// Ensures Image alpha hit testing is applied when the component is enabled
/// and provides an editor helper to enable Read/Write on the sprite texture.
/// </summary>
public class CustomImageHitDetection : MonoBehaviour
{
    /// <summary>
    /// Alpha threshold used for hit testing. Pixels with alpha greater than this
    /// value are considered hittable. Range typically 0.0 (transparent) to 1.0 (opaque).
    /// </summary>
    [SerializeField] private float AlphaHitThreshold = 0.5f;

    /// <summary>
    /// Called by Unity when the component becomes enabled.
    /// - If the attached Image has a sprite whose texture is readable, sets
    ///   the Image's <see cref="Image.alphaHitTestMinimumThreshold"/> to
    ///   <see cref="AlphaHitThreshold"/> so alpha-based hit testing works.
    /// - If the texture is not readable (or null), logs a warning and skips setup.
    /// </summary>
    private void OnEnable()
    {
        if (TryGetComponent(out Image img) && img.sprite != null)
        {
            var tex = img.sprite.texture;

            // Guard against non-readable or Crunch-compressed textures
            if (tex != null && tex.isReadable)
            {
                img.alphaHitTestMinimumThreshold = AlphaHitThreshold;
            }
            else
            {
                Debug.LogWarning(
                    $"Alpha hit test skipped for {name}: Texture not readable or uses Crunch Compression."
                );
            }
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor-only helper to enable Read/Write on the sprite's underlying texture asset.
    /// Can be called from a custom inspector button.
    /// </summary>
    public void EnableReadWriteOnSprite()
    {
        if (TryGetComponent(out Image img) && img.sprite != null)
        {
            var tex = img.sprite.texture;
            if (tex == null)
            {
                Debug.LogWarning("Sprite has no texture to modify.");
                return;
            }
            string path = AssetDatabase.GetAssetPath(tex);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer != null && !importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
                Debug.Log($"Enabled Read/Write on {tex.name}");
            }
            else if (importer != null)
            {
                Debug.Log($"Texture {tex.name} is already Read/Write enabled.");
            }
            else
            {
                Debug.LogWarning($"Could not find TextureImporter for {tex.name} at path {path}.");
            }
        }
        else
        {
            Debug.LogWarning("No sprite assigned to Image.");
        }
    }

#endif
}