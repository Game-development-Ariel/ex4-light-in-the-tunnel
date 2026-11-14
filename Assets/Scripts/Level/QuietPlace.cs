using UnityEngine;

/// <summary>
/// A quiet corner where the hero can stop and say a verse. Standing still inside it for a few seconds
/// gives hope, once. Moving breaks the moment and it starts over. From the design: praying takes time.
/// </summary>
[RequireComponent(typeof(Collider))]
public class QuietPlace : MonoBehaviour
{
    // Slower than this counts as standing still.
    private const float StillSpeed = 0.1f;

    [TextArea]
    [SerializeField] private string verse;

    [Tooltip("Hope gained here")]
    [SerializeField] private int hope = 20;

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

        bool isStill = other.GetComponent<CharacterController>().velocity.magnitude < StillSpeed;
        stillTime = isStill ? stillTime + Time.fixedDeltaTime : 0f; // OnTriggerStay runs once every physics step.
        if (stillTime >= stillSeconds)
        {
            playerHope.Add(hope);
            memoryFound.Raise(verse);
            Destroy(gameObject);
        }
    }
}
