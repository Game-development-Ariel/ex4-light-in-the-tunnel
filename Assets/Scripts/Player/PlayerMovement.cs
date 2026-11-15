using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Moves the player around the map in a top-down view.
/// The movement goes through a CharacterController, so the wall colliders stop the player
/// and the player slides along a wall instead of getting stuck on it.
/// The map is flat on the X-Y plane, so the controller's height equals twice its radius, which makes it a ball.
/// The body (the sprite) turns to the walking direction, so the hero faces where he goes.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    // Angle 0 in Unity points right, but the player sprite is drawn facing up.
    private const float SpriteForwardOffsetDegrees = -90f;

    // Input weaker than this counts as "no input", so a small gamepad drift doesn't turn the body.
    // It is compared with the squared length of the input, so 0.01 means a tilt of 0.1.
    private const float MinInputToTurn = 0.01f;

    // Slower than this, in world units per second, the player counts as standing (for example when pushing against a wall).
    private const float MinSpeedToWalk = 0.1f;

    [Tooltip("Movement speed, in world units per second")]
    [SerializeField] private float moveSpeed = 5f;

    [Tooltip("How fast the body turns, in degrees per second")]
    [SerializeField] private float turnSpeed = 720f;

    [Tooltip("The child object that turns: the sprite")]
    [SerializeField] private Transform body;

    [Tooltip("The movement direction. Arrows, WASD and the left gamepad stick by default")]
    [SerializeField]
    private InputAction moveAction = new InputAction(type: InputActionType.Value, expectedControlType: nameof(Vector2));

    private CharacterController controller;

    /// <summary>Multiplies the walking speed. HopeSenses sets it: more hope, faster steps.</summary>
    public float SpeedFactor { get; set; } = 1f;

    public float CurrentSpeed => moveSpeed * SpeedFactor;

    private Vector2 moveInput;

    /// <summary>How fast the player is walking right now: 0 when standing, less when the joystick is only tilted a little.</summary>
    public float WalkingSpeed => Mathf.Min(moveInput.magnitude, 1f) * CurrentSpeed;

    /// <summary>
    /// True while the player really moves across the floor. Footsteps, the walking bounce and the quiet place read it.
    /// Walking into a wall doesn't count, and neither does a frozen player (the controller keeps its last velocity).
    /// </summary>
    public bool IsWalking => enabled && controller.velocity.magnitude > MinSpeedToWalk;

    /// <summary>
    /// Adds the default keys, so the action works without setting it in the Inspector.
    /// Unity runs this in the editor when the component is loaded or changed.
    /// </summary>
    private void OnValidate()
    {
        if (moveAction.bindings.Count == 0)
        {
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            moveAction.AddBinding("<Gamepad>/leftStick");
        }
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    // An InputAction reads nothing until it is enabled.
    private void OnEnable()
    {
        moveAction.Enable();
    }

    // While the movement is off the player stands still, so nothing should think he still walks.
    private void OnDisable()
    {
        moveAction.Disable();
        moveInput = Vector2.zero;
    }

    /// <summary>Reads the input every frame, moves, and turns the body toward the walking direction.</summary>
    private void Update()
    {
        moveInput = moveAction.ReadValue<Vector2>();

        // Move wants the distance for this frame. It stops at walls by itself.
        controller.Move(moveInput * CurrentSpeed * Time.deltaTime);
        TurnBodyTowardMovement();
    }

    /// <summary>Turns the body a little each frame toward the walking direction.</summary>
    private void TurnBodyTowardMovement()
    {
        if (body == null || moveInput.sqrMagnitude <= MinInputToTurn)
        {
            return; // When the player stops, the body keeps facing the last direction.
        }

        // Atan2 gives the angle of the direction vector, in radians.
        float angle = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg + SpriteForwardOffsetDegrees;
        Quaternion facing = Quaternion.Euler(0f, 0f, angle);

        // RotateTowards never passes the goal, so the turn is smooth and stops exactly on it.
        body.rotation = Quaternion.RotateTowards(body.rotation, facing, turnSpeed * Time.deltaTime);
    }
}
