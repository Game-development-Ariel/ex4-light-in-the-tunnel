using UnityEngine;

/// <summary>
/// Creates the water bottle from its prefab, at this object's position.
/// If the player is caught while carrying the bottle, the bottle is lost, so a new one appears here.
/// </summary>
public class BottleSpawner : MonoBehaviour
{
    [Tooltip("The water bottle prefab")]
    [SerializeField] private GameObject bottlePrefab;

    [Tooltip("Raised when the player loses the bottle. A new one is made then")]
    [SerializeField] private GameEvent bottleDropped;

    // The bottle that is on the floor now. Null after the player picks it up (it destroys itself).
    private GameObject currentBottle;

    /// <summary>The bottle on the floor, or null while the player carries it.</summary>
    public Transform Bottle => currentBottle != null ? currentBottle.transform : null;

    private void OnEnable()
    {
        bottleDropped.Subscribe(Spawn);
    }

    private void OnDisable()
    {
        bottleDropped.Unsubscribe(Spawn);
    }

    private void Start()
    {
        Spawn();
    }

    // The dropped bottle was picked up from here before, so there is no other bottle on the floor.
    private void Spawn()
    {
        currentBottle = Instantiate(bottlePrefab, transform.position, Quaternion.identity);
    }
}
