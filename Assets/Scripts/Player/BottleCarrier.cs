using UnityEngine;

/// <summary>
/// Remembers whether the player carries the water bottle. The bottle is lost when the player is caught.
/// </summary>
public class BottleCarrier : MonoBehaviour
{
    [Tooltip("Raised when the player picks up the bottle")]
    [SerializeField] private GameEvent bottlePicked;

    [Tooltip("Raised when the player loses the bottle because he was caught")]
    [SerializeField] private GameEvent bottleDropped;

    [Tooltip("When this happens the bottle is dropped")]
    [SerializeField] private GameEvent playerCaught;

    public bool HasBottle { get; private set; }

    private void OnEnable()
    {
        playerCaught.Subscribe(Drop);
    }

    private void OnDisable()
    {
        playerCaught.Unsubscribe(Drop);
    }

    public void PickUp()
    {
        HasBottle = true;
        bottlePicked.Raise();
    }

    /// <summary>The bottle was brought to the mattress. It is given away, not lost, so nothing is raised.</summary>
    public void Deliver()
    {
        HasBottle = false;
    }

    private void Drop()
    {
        if (!HasBottle)
        {
            return;
        }
        HasBottle = false;
        bottleDropped.Raise();
    }
}
