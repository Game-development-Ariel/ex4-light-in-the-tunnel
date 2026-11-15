using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// A quiet corner where the hero can stop and say a verse. Standing still inside it for a few seconds
/// gives hope, once. Moving breaks the moment and it starts over. From the design: praying takes time.
/// </summary>
[RequireComponent(typeof(Collider))]
public class QuietPlace : MonoBehaviour
{
    [TextArea]
    [SerializeField] private string verse;

    [Tooltip("Hope gained here")]
    [FormerlySerializedAs("hope")]
    [SerializeField] private int hopeGained = 20;

    [Tooltip("Seconds the hero has to stand still")]
    [SerializeField] private float stillSeconds = 3f;

    [Tooltip("Raised with the verse. The memory caption listens to it")]
    [SerializeField] private TextEvent memoryFound;

    private float stillTime;

    private void OnTriggerStay(Collider other)
    {
        Hope playerHope = other.GetComponent<Hope>();
        if (playerHope == null)
        {
            return;
        }

        bool isStill = !other.GetComponent<PlayerMovement>().IsWalking;
        stillTime = isStill ? stillTime + Time.fixedDeltaTime : 0f; // OnTriggerStay runs once every physics step.
        if (stillTime >= stillSeconds)
        {
            playerHope.Add(hopeGained);
            memoryFound.Raise(verse);
            Destroy(gameObject);
        }
    }

    // Leaving the place breaks the moment, so coming back starts the count over.
    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<Hope>() != null)
        {
            stillTime = 0f;
        }
    }
}
