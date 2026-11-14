using UnityEngine;

/// <summary>
/// The water bottle. When the player touches it, the player picks it up and the bottle disappears from the floor.
/// Needs a collider with "Is Trigger" on, so the player walks into it instead of bumping into it.
/// </summary>
[RequireComponent(typeof(Collider))]
public class WaterBottle : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        BottleCarrier carrier = other.GetComponent<BottleCarrier>();
        if (carrier == null)
        {
            return;
        }

        carrier.PickUp();
        Destroy(gameObject);
    }
}
