using UnityEngine;

/// <summary>
/// The area of the mattress in the cell. Bringing the water bottle here raises the "bottle delivered" event.
/// Needs a collider with "Is Trigger" on, covering the mattress.
/// </summary>
[RequireComponent(typeof(Collider))]
public class MattressGoal : MonoBehaviour
{
    [SerializeField] private GameEvent bottleDelivered;

    // Stay and not Enter: the player starts next to the mattress, so the trigger may already be touched
    // when the player gets the bottle. Stay keeps checking every physics step.
    private void OnTriggerStay(Collider other)
    {
        BottleCarrier carrier = other.GetComponent<BottleCarrier>();
        if (carrier == null || !carrier.HasBottle)
        {
            return;
        }

        carrier.Drop();
        bottleDelivered.Raise();
    }
}
