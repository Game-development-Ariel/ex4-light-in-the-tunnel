using UnityEngine;

/// <summary>
/// How much danger the hero feels, from 0 (safe) to 1 (right next to a watcher).
/// It grows as the player gets closer to a security camera (anything with a VisionSensor). The heartbeat and the breathing
/// get louder with it, and MoodVolume darkens the edges of the screen, so the player feels the danger
/// before seeing it.
/// </summary>
public class Tension : MonoBehaviour
{
    /// <summary>The danger now, for other scripts (like MoodVolume) to read.</summary>
    public float Danger { get; private set; }

    [Tooltip("Closer than this to a camera, in tiles, the danger is full")]
    [SerializeField] private float fullDangerDistance = 6f;

    [Tooltip("Farther than this from every camera, in tiles, there is no danger")]
    [SerializeField] private float noDangerDistance = 14f;

    [Tooltip("How fast the feeling changes, so it rises and calms down smoothly")]
    [SerializeField] private float smoothing = 1.5f;

    [SerializeField] private AudioSource heartbeat;
    [SerializeField] private AudioSource breathing;

    [Tooltip("The heartbeat's volume at full danger, from 0 to 1. With no danger it is silent")]
    [SerializeField] private float maxHeartbeatVolume = 0.7f;
    [Tooltip("The breathing's volume with no danger and with full danger, from 0 to 1")]
    [SerializeField] private Vector2 breathingVolume = new Vector2(0.18f, 0.45f);

    [Tooltip("The heartbeat's pitch with no danger and with full danger. A faster heart when the danger is closer")]
    [SerializeField] private Vector2 heartbeatPitch = new Vector2(0.9f, 1.2f);

    [Tooltip("The breathing's pitch with no danger and with full danger")]
    [SerializeField] private Vector2 breathingPitch = new Vector2(1f, 1.25f);

    [SerializeField] private Transform player;

    private VisionSensor[] watchers;

    private void Start()
    {
        watchers = FindObjectsByType<VisionSensor>(FindObjectsSortMode.None);
    }

    private void Update()
    {
        float closest = float.MaxValue;
        foreach (VisionSensor watcher in watchers)
        {
            closest = Mathf.Min(closest, Vector2.Distance(player.position, watcher.transform.position));
        }

        // InverseLerp: 0 at noDangerDistance, 1 at fullDangerDistance.
        float goal = Mathf.InverseLerp(noDangerDistance, fullDangerDistance, closest);
        Danger = Mathf.MoveTowards(Danger, goal, smoothing * Time.deltaTime);

        heartbeat.volume = Danger * maxHeartbeatVolume;
        heartbeat.pitch = Mathf.Lerp(heartbeatPitch.x, heartbeatPitch.y, Danger);
        breathing.volume = Mathf.Lerp(breathingVolume.x, breathingVolume.y, Danger);
        breathing.pitch = Mathf.Lerp(breathingPitch.x, breathingPitch.y, Danger);
    }
}
