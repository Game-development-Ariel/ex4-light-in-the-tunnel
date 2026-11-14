using UnityEngine;

/// <summary>
/// A loose object that makes noise when it bangs into a wall. The first loud bang also raises a thought,
/// to teach the player to push slowly. Needs a Rigidbody.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ImpactNoise : MonoBehaviour
{
    [Tooltip("Hits slower than this, in units per second, are silent")]
    [SerializeField] private float loudSpeed = 1.2f;

    [Tooltip("Seconds before the same object can make noise again")]
    [SerializeField] private float quietSeconds = 1f;

    [TextArea]
    [SerializeField] private string warning = "Too loud. I push slowly, so nobody hears.";

    [Tooltip("Raised with the warning, the first time only")]
    [SerializeField] private TextEvent hintRequested;

    private float nextNoiseTime;
    private bool warned;

    private void OnCollisionEnter(Collision collision)
    {
        // Only a hit against something that doesn't move (a wall) is loud. Another crate or the player is soft.
        bool hitWall = collision.rigidbody == null && collision.collider.GetComponent<CharacterController>() == null;
        float speed = collision.relativeVelocity.magnitude;
        if (!hitWall || speed < loudSpeed || Time.time < nextNoiseTime)
        {
            return;
        }

        nextNoiseTime = Time.time + quietSeconds;
        Sfx.Play("Step1", Mathf.Clamp01(speed / (loudSpeed * 3f)));
        if (!warned)
        {
            warned = true;
            hintRequested.Raise(warning);
        }
    }
}
