using UnityEngine;

/// <summary>
/// Shows the guard's footsteps as a faint ring that pulses on the floor, even behind a wall,
/// but only when the guard is close enough for the hero to hear. More hope means better hearing.
/// </summary>
[RequireComponent(typeof(GuardPatrol))]
public class HeardSteps : MonoBehaviour
{
    [SerializeField] private HopeSenses listener;

    [Tooltip("The ring that is drawn at the guard's feet")]
    [SerializeField] private SpriteRenderer ring;

    [Tooltip("Steps per second")]
    [SerializeField] private float stepRate = 1.6f;

    [SerializeField] private float maxAlpha = 0.5f;
    [SerializeField] private Vector2 ringSize = new Vector2(0.6f, 1.8f);

    private GuardPatrol patrol;

    private void Awake()
    {
        patrol = GetComponent<GuardPatrol>();
    }

    private void Update()
    {
        float distance = Vector2.Distance(transform.position, listener.transform.position);
        bool heard = patrol.IsWalking && distance <= listener.HearingRange;
        ring.enabled = heard;
        if (!heard)
        {
            return;
        }

        // Each step is a ring that grows and fades. Repeat(t, 1) counts from 0 to 1 again and again.
        float step = Mathf.Repeat(Time.time * stepRate, 1f);
        ring.transform.localScale = Vector3.one * Mathf.Lerp(ringSize.x, ringSize.y, step);
        Color color = ring.color;
        color.a = maxAlpha * (1f - step);
        ring.color = color;
    }
}
