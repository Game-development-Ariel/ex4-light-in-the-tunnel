using UnityEngine;

/// <summary>
/// A trigger that teaches: the first time the player walks into it, it asks to show its tip.
/// The tutorial puts these where the player needs to learn something, like before the camera.
/// </summary>
[RequireComponent(typeof(Collider))]
public class HintZone : MonoBehaviour
{
    [TextArea]
    [SerializeField] private string hint;

    [Tooltip("Raised with the hint's text. The hint UI listens to it")]
    [SerializeField] private TextEvent hintRequested;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerMovement>() == null)
        {
            return;
        }

        hintRequested.Raise(hint);
        Destroy(gameObject); // Shown once. Walking here again doesn't repeat it.
    }
}
