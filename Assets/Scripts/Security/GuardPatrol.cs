using System.Collections;
using UnityEngine;

/// <summary>
/// Walks a guard back and forth between points. At each point the guard turns to look at the wall,
/// waits a moment, and then turns to the next point. The object's "up" is where the guard looks,
/// so a VisionSensor on the same object sees what the guard sees.
/// </summary>
public class GuardPatrol : MonoBehaviour
{
    [SerializeField] private Transform[] points;

    [Tooltip("Tiles per second")]
    [SerializeField] private float walkSpeed = 1.2f;

    [Tooltip("Degrees per second")]
    [SerializeField] private float turnSpeed = 180f;

    [Tooltip("Seconds the guard stands at each point")]
    [SerializeField] private float pauseSeconds = 1.5f;

    [Tooltip("Where the guard looks while standing. Looking at the wall gives the player a moment to pass")]
    [SerializeField] private Vector2 pauseLook = Vector2.up;

    /// <summary>True while the guard is walking, for the sound of the steps.</summary>
    public bool IsWalking { get; private set; }

    private IEnumerator Start()
    {
        int next = 0;
        while (true)
        {
            Vector3 goal = points[next].position;
            yield return TurnTo(goal - transform.position);

            IsWalking = true;
            while (transform.position != goal)
            {
                transform.position = Vector3.MoveTowards(transform.position, goal, walkSpeed * Time.deltaTime);
                yield return null;
            }
            IsWalking = false;

            yield return TurnTo(pauseLook);
            yield return new WaitForSeconds(pauseSeconds);
            next = (next + 1) % points.Length; // After the last point comes the first again.
        }
    }

    private IEnumerator TurnTo(Vector2 direction)
    {
        Quaternion goal = Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.up, direction));
        while (Quaternion.Angle(transform.rotation, goal) > 0.5f)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, goal, turnSpeed * Time.deltaTime);
            yield return null;
        }
        transform.rotation = goal;
    }
}
