using UnityEngine;

/// <summary>
/// Teaches the player to push slowly: the first loud wall hit in the level raises a thought, and only that one.
/// There is one in the level, and every loose object reports its wall hits to it.
/// </summary>
public class NoiseWarning : MonoBehaviour
{
    [Tooltip("A wall hit at this speed or faster raises the warning thought")]
    [SerializeField] private float warnAboveSpeed = 1.2f;

    [TextArea]
    [SerializeField] private string warning = "Too loud. I push slowly, so nobody hears.";

    [Tooltip("Raised with the warning, the first time only")]
    [SerializeField] private TextEvent hintRequested;

    private bool warned;

    /// <summary>Called by a loose object when it bangs into a wall, with how fast it was going.</summary>
    public void WallHit(float speed)
    {
        if (warned || speed < warnAboveSpeed)
        {
            return;
        }
        warned = true;
        hintRequested.Raise(warning);
    }
}
