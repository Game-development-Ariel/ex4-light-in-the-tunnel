using UnityEngine;

/// <summary>
/// Lets the player push loose things (crates, sandbags, the cell door) by walking into them.
/// A CharacterController doesn't push rigidbodies by itself, so the push is added here as a force.
/// Unity calls OnControllerColliderHit once per frame, and frames are not all the same length, so each frame
/// gives an impulse of (force * frame time). Over one second that adds up to the same push at any frame rate.
/// When a push starts, the pushed object is told about it (if it is an IPushable), so it can make a sound.
/// </summary>
[RequireComponent(typeof(CharacterController), typeof(PlayerMovement))]
public class PlayerPush : MonoBehaviour
{
    [Tooltip("How hard the player pushes, in newtons")]
    [SerializeField] private float pushStrength = 16.8f;

    [Tooltip("Seconds without touching an object before touching it again counts as a new push")]
    [SerializeField] private float newPushAfterSeconds = 0.25f;

    private PlayerMovement movement;
    private Rigidbody lastPushed;
    private float lastPushTime;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
    }

    // Unity calls this every time the controller touches a collider while moving.
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody body = hit.collider.attachedRigidbody;
        if (body == null || body.isKinematic)
        {
            return; // A wall, or something that is not meant to move.
        }

        // Pushing at the touched point (and not at the center) also turns the object, like a real shove.
        body.AddForceAtPosition(hit.moveDirection * pushStrength * Time.deltaTime, hit.point, ForceMode.Impulse);

        // This is called every frame while pushing, so only the first touch is a new push.
        bool isNewPush = body != lastPushed || Time.time - lastPushTime > newPushAfterSeconds;
        lastPushed = body;
        lastPushTime = Time.time;
        if (isNewPush && body.TryGetComponent(out IPushable pushable))
        {
            pushable.PushStarted(movement.WalkingSpeed);
        }
    }
}
