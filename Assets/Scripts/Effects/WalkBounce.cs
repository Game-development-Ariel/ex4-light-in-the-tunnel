using UnityEngine;

/// <summary>
/// Squashes and stretches the player's body a little while walking, like tired, careful steps.
/// The character is a single picture, so this small motion is what makes it feel like it walks.
/// Put it on the Body child, and give it the player's PlayerMovement.
/// </summary>
public class WalkBounce : MonoBehaviour
{
    [SerializeField] private PlayerMovement movement;

    [Tooltip("Bounces per second while walking")]
    [SerializeField] private float stepsPerSecond = 4f;

    [Tooltip("How strong the squash is. 0.08 = 8 percent")]
    [SerializeField] private float amount = 0.045f;

    [Tooltip("How fast the body goes back to its rest shape when the player stops")]
    [SerializeField] private float settleSpeed = 10f;

    private Vector3 baseScale;
    private float phase;

    private void Start()
    {
        baseScale = transform.localScale;
    }

    private void Update()
    {
        if (movement.IsWalking)
        {
            phase += Time.deltaTime * stepsPerSecond * Mathf.PI;
        }
        else
        {
            // Eases back to the rest shape instead of stopping in the middle of a squash.
            phase = Mathf.MoveTowards(phase, Mathf.Round(phase / Mathf.PI) * Mathf.PI, Time.deltaTime * settleSpeed);
        }

        // Abs(Sin) gives one bump per step. Wider and shorter on the bump, like weight landing on a foot.
        float bump = Mathf.Abs(Mathf.Sin(phase)) * amount;
        transform.localScale = new Vector3(baseScale.x * (1f + bump), baseScale.y * (1f - bump), baseScale.z);
    }
}
