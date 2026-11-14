using UnityEngine;

/// <summary>
/// Turns the security camera from side to side, with a pause at each end.
/// </summary>
public class CameraSweep : MonoBehaviour
{
    [Tooltip("The direction in the middle of the sweep, in degrees. 0 looks up, 90 left, 180 down")]
    [SerializeField] private float centerAngle = 180f;

    [Tooltip("How far the camera turns to each side of the center, in degrees")]
    [SerializeField] private float sweepAngle = 60f;

    [Tooltip("Seconds it takes to turn from one side to the other")]
    [SerializeField] private float turnSeconds = 5f;

    [Tooltip("Seconds the camera stays still at each side before turning back")]
    [SerializeField] private float pauseSeconds = 2.5f;

    private float sweepTime;

    private void Update()
    {
        sweepTime += Time.deltaTime;
        transform.rotation = Quaternion.Euler(0f, 0f, centerAngle + CurrentSide() * sweepAngle);
    }

    /// <summary>
    /// Where the camera is in its cycle: -1 is the left end of the sweep and +1 is the right end.
    /// One full cycle: turn right, pause, turn left, pause.
    /// SmoothStep makes the camera speed up and slow down at the ends, like a real motor.
    /// </summary>
    private float CurrentSide()
    {
        float cycle = 2f * (turnSeconds + pauseSeconds);
        float t = sweepTime % cycle;

        if (t < turnSeconds)
        {
            return Mathf.Lerp(-1f, 1f, Mathf.SmoothStep(0f, 1f, t / turnSeconds));
        }
        if (t < turnSeconds + pauseSeconds)
        {
            return 1f;
        }
        if (t < 2f * turnSeconds + pauseSeconds)
        {
            return Mathf.Lerp(1f, -1f, Mathf.SmoothStep(0f, 1f, (t - turnSeconds - pauseSeconds) / turnSeconds));
        }
        return -1f;
    }
}
