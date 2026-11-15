using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Shows or hides the minimap when the player presses its key (M by default).
/// </summary>
[RequireComponent(typeof(Minimap))]
public class MinimapToggle : MonoBehaviour
{
    [Tooltip("Shows or hides the minimap. The M key by default")]
    [SerializeField]
    private InputAction toggleAction = new InputAction(type: InputActionType.Button);

    private Minimap minimap;

    /// <summary>
    /// Adds the default key, so the action works without setting it in the Inspector.
    /// Unity runs this in the editor when the component is loaded or changed.
    /// </summary>
    private void OnValidate()
    {
        if (toggleAction.bindings.Count == 0)
        {
            toggleAction.AddBinding("<Keyboard>/m");
        }
    }

    private void Awake()
    {
        minimap = GetComponent<Minimap>();
    }

    // An InputAction reads nothing until it is enabled.
    private void OnEnable()
    {
        toggleAction.Enable();
    }

    private void OnDisable()
    {
        toggleAction.Disable();
    }

    private void Update()
    {
        if (toggleAction.WasPressedThisFrame())
        {
            minimap.SetVisible(!minimap.IsVisible);
        }
    }
}
