using UnityEngine;

/// <summary>
/// Lets the player push loose things (crates, sandbags, the cell door) by walking into them.
/// A CharacterController doesn't push rigidbodies by itself, so the push is added here as a force.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerPush : MonoBehaviour
{
    [Tooltip("How hard the player pushes, in newtons")]
    [SerializeField] private float pushForce = 14f;

    // Unity calls this every time the controller touches a collider while moving.
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody body = hit.collider.attachedRigidbody;
        if (body == null || body.isKinematic)
        {
            return; // A wall, or something that is not meant to move.
        }

        // Pushing at the touched point (and not at the center) also turns the object, like a real shove.
        body.AddForceAtPosition(hit.moveDirection * pushForce, hit.point);
    }
}
