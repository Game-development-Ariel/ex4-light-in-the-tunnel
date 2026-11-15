using UnityEngine;

/// <summary>
/// Remembers whether the player carries the water bottle. The bottle is lost when the player is caught.
/// </summary>
public class BottleCarrier : MonoBehaviour
{
    [Tooltip("Raised when the player picks up the bottle")]
    [SerializeField] private GameEvent bottlePicked;

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

    public void Drop()
    {
        HasBottle = false;
    }
}
