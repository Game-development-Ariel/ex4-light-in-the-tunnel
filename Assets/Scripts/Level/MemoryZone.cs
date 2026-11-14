using UnityEngine;

/// <summary>
/// A memory: a small thing in the world (a note, marks on a wall) that reminds the hero of home.
/// The first time the player steps on it, hope grows and the memory's text is sent to be shown.
/// From the design: memories fill the hope meter.
/// </summary>
[RequireComponent(typeof(Collider))]
public class MemoryZone : MonoBehaviour
{
    [TextArea]
    [SerializeField] private string memory;

    [Tooltip("Hope gained from this memory")]
    [SerializeField] private int hope = 15;

    [Tooltip("Raised with the memory's text. The memory caption listens to it")]
    [SerializeField] private TextEvent memoryFound;

    private void OnTriggerEnter(Collider other)
    {
        Hope playerHope = other.GetComponent<Hope>();
        if (playerHope == null)
        {
            return;
        }

        playerHope.Add(hope);
        memoryFound.Raise(memory);
        Destroy(gameObject); // Each memory is found once.
    }
}
