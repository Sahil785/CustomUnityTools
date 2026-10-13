using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Controls the runtime cursor visuals and behavior.
/// - Loads cursor textures from Resources, resizes them to device DPI,
///   and sets the proper hotspot for different cursor types.
/// - Listens for UI pointer state via <see cref="CursorManager"/> and
///   switches cursor appearance for buttons, toggles and input fields.
/// - Provides WebGL interop hooks to detect input device and mouse events.
/// </summary>
public class CursorController : MonoBehaviour
{
    /// <summary>Flag set when running on a mobile/touch input device.</summary>
    bool isMobile = false;

    /// <summary>Hand pointer texture (for hoverable UI like buttons).</summary>
    Texture2D handCursor;

    /// <summary>Default arrow cursor texture.</summary>
    Texture2D defaultCursor;

    /// <summary>Input field cursor texture.</summary>
    Texture2D inputCursor;

    /// <summary>Default hotspot used for most cursors.</summary>
    Vector2 hotspot = new Vector2(0, 0);

    /// <summary>Hotspot used for the hand cursor (offset to better align finger tip).</summary>
    Vector2 handhotspot = new Vector2(5, 3);

    /// <summary>Reference to the currently applied cursor texture.</summary>
    Texture2D CurrentCursor;

    /// <summary>Scale factor computed from screen DPI used to resize cursor textures.</summary>
    float scaleFactor;

    /// <summary>Whether the cursor should currently be highlighted (from UI hover).</summary>
    private bool HighlightCursor = false;

    /// <summary>Currently hovered <see cref="CursorManager"/> instance (if any).</summary>
    public static CursorManager CurrentCursorObj = null;

    /// <summary>
    /// Event invoked when a CursorManager wants to change cursor state.
    /// Params: (highlightOn, cursorManagerInstance)
    /// </summary>
    public static UnityAction<bool, CursorManager> OnCursorStateChange;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void RegisterMouseEvents();

    [DllImport("__Internal")]
    private static extern int detectInputDevice();
#endif

    /// <summary>
    /// Called from WebGL JS when the mouse leaves Unity canvas area.
    /// Hides the cursor inside the canvas area.
    /// </summary>
    public void OnMouseLeaveCanvas()
    {
        DebugHelper.Log($"Cursor entered Unity canvas space");
        Cursor.visible = false;
    }

    /// <summary>
    /// Called from WebGL JS when the mouse enters Unity canvas area.
    /// Shows the cursor and sets it to the hand cursor.
    /// </summary>
    public void OnMouseEnterCanvas()
    {
        DebugHelper.Log($"Cursor left Unity canvas space");
        Cursor.visible = true;
        SetCursorToHand();
    }

    /// <summary>
    /// Unity start callback. Initializes event subscriptions, scale factor,
    /// loads cursor textures and sets initial cursor state.
    /// </summary>
    void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        RegisterMouseEvents();
        isMobile = detectInputDevice() == 0;
        DebugHelper.Log($"Is Mobile: {isMobile}, {detectInputDevice()}");
#endif

        OnCursorStateChange += (onCursorHL, cursorStateManager) =>
        {
            HighlightCursor = onCursorHL;
            CurrentCursorObj = cursorStateManager;
        };
        SetScaleFactor();
        LoadCursorsFormResources();

        if (handCursor == null || defaultCursor == null)
        {
            DebugHelper.LogError("Cursor textures not found. Please ensure they are named correctly and placed in the Resources folder.");
            return;
        }
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.None;
        Invoke(nameof(SetStartupCursor), 0.1f);
    }

    /// <summary>
    /// Sets the initial visible cursor after startup delay.
    /// </summary>
    private void SetStartupCursor()
    {
        Cursor.visible = true;
        SetCursorToHand();
    }

    /// <summary>
    /// Ensures the default cursor is set and visible.
    /// </summary>
    void SetDefaultCursor()
    {
        SetCursorToDefault();
        Cursor.visible = true;
    }

    /// <summary>
    /// Unity LateUpdate callback. Evaluates the current hovered UI element
    /// and switches the cursor texture appropriately.
    /// </summary>
    void LateUpdate()
    {
#if !UNITY_SERVER
        if (CurrentCursorObj != null && CurrentCursorObj.gameObject.activeInHierarchy && HighlightCursor)
        {
            if (CurrentCursorObj.isInputField && CurrentCursorObj.inputField != null)
            {
                if (CurrentCursorObj.inputField.interactable && CurrentCursor != inputCursor)
                {
                    SetCursorToInput();
                }
                else if (!CurrentCursorObj.inputField.interactable && CurrentCursor != defaultCursor)
                {
                    SetCursorToDefault();
                }
            }
            else if (CurrentCursorObj.button != null)
            {
                if (CurrentCursorObj.button.interactable && CurrentCursor != handCursor)
                {
                    SetCursorToHand();
                }
                else if (!CurrentCursorObj.button.interactable && CurrentCursor != defaultCursor)
                {
                    SetCursorToDefault();
                }
            }
            else if (CurrentCursorObj.toggle != null)
            {
                if (CurrentCursorObj.toggle.interactable && CurrentCursor != handCursor)
                {
                    SetCursorToHand();
                }
                else if (!CurrentCursorObj.toggle.interactable && CurrentCursor != defaultCursor)
                {
                    SetCursorToDefault();
                }
            }
            else
            {
                SetCursorToDefault();
            }
        }
        if (!HighlightCursor && CurrentCursor != defaultCursor)
        {
            SetCursorToDefault();
        }
#endif
    }

    /// <summary>
    /// Returns whether the current platform/input is considered mobile (touch).
    /// </summary>
    /// <returns>True when mobile input is detected; otherwise false.</returns>
    bool IsMobile()
    {
        return isMobile;
    }

    /// <summary>
    /// Computes a scale factor from screen DPI to use when resizing cursor textures.
    /// </summary>
    void SetScaleFactor()
    {
        int screenWidth = Screen.width;
        int screenHeight = Screen.height;
        float dpi = Screen.dpi;
        DebugHelper.Log($"Screen Resolution: {screenWidth}x{screenHeight}, DPI: {dpi}");
        scaleFactor = dpi > 0 ? dpi / 140 : 1;
        DebugHelper.Log($"Scale Factor: {scaleFactor}");
    }

    /// <summary>
    /// Loads cursor textures from Resources and rescales them according to computed DPI scale factor.
    /// Expects textures named 'hand_cursor', 'default_cursor' and 'input_cursor'.
    /// </summary>
    void LoadCursorsFormResources()
    {
        handCursor = Resources.Load<Texture2D>("hand_cursor");
        defaultCursor = Resources.Load<Texture2D>("default_cursor");
        inputCursor = Resources.Load<Texture2D>("input_cursor");

        handCursor = ResizeTexture(handCursor, scaleFactor);

        defaultCursor = ResizeTexture(defaultCursor, scaleFactor);

        inputCursor = ResizeTexture(inputCursor, scaleFactor);
        hotspot *= scaleFactor;
        handhotspot *= scaleFactor;
    }

    /// <summary>
    /// Creates a resized copy of the provided texture using a RenderTexture blit.
    /// </summary>
    /// <param name="original">Original texture to resize.</param>
    /// <param name="scaleFactor">Scale factor to apply to width/height.</param>
    /// <returns>A new Texture2D instance containing the resized pixels.</returns>
    private Texture2D ResizeTexture(Texture2D original, float scaleFactor)
    {
        int newWidth = Mathf.CeilToInt(original.width * scaleFactor);
        int newHeight = Mathf.CeilToInt(original.height * scaleFactor);

        // Create a RenderTexture with the new dimensions
        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
        RenderTexture.active = rt;

        // Render the original texture onto the RenderTexture
        Graphics.Blit(original, rt);

        // Create a new Texture2D with the resized dimensions
        Texture2D resizedTexture = new Texture2D(newWidth, newHeight);
        resizedTexture.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        resizedTexture.Apply();

        // Clean up
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        return resizedTexture;
    }

    /// <summary>
    /// Sets the current cursor to the default arrow texture (if not mobile).
    /// </summary>
    public void SetCursorToDefault()
    {
        if (!IsMobile())
        {
            CurrentCursor = defaultCursor;
            Cursor.SetCursor(CurrentCursor, hotspot, CursorMode.ForceSoftware);
        }
    }

    /// <summary>
    /// Sets the current cursor to the hand pointer texture (if not mobile).
    /// </summary>
    public void SetCursorToHand()
    {
        if (!IsMobile())
        {
            CurrentCursor = handCursor;
            Cursor.SetCursor(CurrentCursor, handhotspot, CursorMode.ForceSoftware);
        }
    }

    /// <summary>
    /// Sets the current cursor to the input field texture (if not mobile).
    /// </summary>
    public void SetCursorToInput()
    {
        if (!IsMobile())
        {
            CurrentCursor = inputCursor;
            Cursor.SetCursor(CurrentCursor, hotspot, CursorMode.ForceSoftware);
        }
    }

    /// <summary>
    /// Validates a cursor texture and logs import setting issues.
    /// </summary>
    /// <param name="texture">Texture to validate.</param>
    /// <returns>True when the texture meets expected requirements; otherwise false.</returns>
    private bool IsValidCursorTexture(Texture2D texture)
    {
        Debug.Log($"{texture.name} - Readable: {texture.isReadable}, Format: {texture.format}, Mipmap Count: {texture.mipmapCount}");
        if (texture == null)
        {
            Debug.LogError("Cursor texture is null.");
            return false;
        }

        if (!texture.isReadable)
        {
            Debug.LogError($"Cursor texture '{texture.name}' is not readable. Enable 'Read/Write' in the texture import settings.");
            return false;
        }

        if (texture.format != TextureFormat.RGBA32 && texture.format != TextureFormat.ARGB32)
        {
            Debug.LogError($"Cursor texture '{texture.name}' must be in RGBA32 or ARGB32 format. Current format: {texture.format}");
            return false;
        }

        if (texture.mipmapCount > 1)
        {
            Debug.LogError($"Cursor texture '{texture.name}' has mip maps enabled. Disable 'Generate Mip Maps' in the texture import settings.");
            return false;
        }

        return true;
    }
}