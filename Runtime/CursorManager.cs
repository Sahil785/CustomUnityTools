using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

/// <summary>
/// Manages cursor interactions for UI elements.  
/// - Notifies <c>CursorController</c> when the pointer enters or exits this element.
/// - Optionally caches a <c>TMP_InputField</c> when <see cref="isInputField"/> is true.
/// - Automatically discovers attached <c>Button</c> and <c>Toggle</c> components.
/// </summary>
public class CursorManager : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    /// <summary>
    /// When true, this GameObject is treated as an input field and the
    /// <see cref="TMP_InputField"/> component will be cached in <see cref="inputField"/>.
    /// </summary>
    public bool isInputField;

    /// <summary>
    /// Cached reference to the attached <see cref="TMP_InputField"/> when <see cref="isInputField"/> is true.
    /// </summary>
    public TMP_InputField inputField;

    /// <summary>
    /// Cached reference to an attached <see cref="Button"/> (if present).
    /// </summary>
    public Button button;

    /// <summary>
    /// Cached reference to an attached <see cref="Toggle"/> (if present).
    /// </summary>
    public Toggle toggle;

    /// <summary>
    /// Unity callback invoked on startup.
    /// Caches references to <see cref="Button"/>, <see cref="Toggle"/>, and optionally <see cref="TMP_InputField"/>.
    /// </summary>
    void Start()
    {
        button = GetComponent<Button>();
        toggle = GetComponent<Toggle>();
        if (isInputField)
        {
            inputField = GetComponent<TMP_InputField>();
        }
    }

    /// <summary>
    /// Unity callback invoked when the component is disabled.
    /// If this instance currently owns the cursor state, notifies <c>CursorController</c> to clear it.
    /// </summary>
    private void OnDisable()
    {
        if (CursorController.CurrentCursorObj == this)
            CursorController.OnCursorStateChange?.Invoke(false, null);
    }

    /// <summary>
    /// Called when the pointer enters the UI element.
    /// Waits one frame (to avoid race conditions with UI state changes) and then notifies <c>CursorController</c>.
    /// </summary>
    /// <param name="eventData">Pointer event data supplied by the EventSystem.</param>
    public async void OnPointerEnter(PointerEventData eventData)
    {
        await UniTask.DelayFrame(1);
        CursorController.OnCursorStateChange?.Invoke(true, this);
    }

    /// <summary>
    /// Called when the pointer exits the UI element.
    /// Notifies <c>CursorController</c> to clear the current cursor state.
    /// </summary>
    /// <param name="eventData">Pointer event data supplied by the EventSystem.</param>
    public void OnPointerExit(PointerEventData eventData)
    {
        CursorController.OnCursorStateChange?.Invoke(false, null);
    }
}