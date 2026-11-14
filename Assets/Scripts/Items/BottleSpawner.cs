using UnityEngine;

/// <summary>
/// Creates the water bottle from its prefab, at this object's position.
/// If the player is caught while carrying the bottle, the bottle is lost, so a new one appears here.
/// </summary>
public class BottleSpawner : MonoBehaviour
{
    [Tooltip("The water bottle prefab")]
    [SerializeField] private GameObject bottlePrefab;

    [SerializeField] private GameEvent playerCaught;

    [Tooltip("The player's carrier. A new bottle is made only when the player doesn't hold one")]
    [SerializeField] private BottleCarrier carrier;

    // The bottle that is on the floor now. Null after the player picks it up.
    private GameObject currentBottle;

    private void OnEnable()
    {
        playerCaught.Subscribe(SpawnNextFrame);
    }

    private void OnDisable()
    {
        playerCaught.Unsubscribe(SpawnNextFrame);
    }

    private void Start()
    {
        SpawnNextFrame();
    }

    private void SpawnNextFrame()
    {
        // Checked on the next frame, after the carrier dropped the bottle and a picked-up bottle was destroyed.
        Invoke(nameof(Spawn), 0f);
    }

    private void Spawn()
    {
        if (currentBottle == null && !carrier.HasBottle)
        {
            currentBottle = Instantiate(bottlePrefab, transform.position, Quaternion.identity);
        }
    }
}
