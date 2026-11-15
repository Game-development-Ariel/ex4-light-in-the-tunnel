using UnityEngine;

/// <summary>
/// How well a watcher (a security camera or a guard) sees. Saved as an asset,
/// so a new kind of watcher is a new asset and not new code.
/// </summary>
[CreateAssetMenu(menuName = "Tunnel/Vision Stats")]
public class VisionStats : ScriptableObject
{
    [Tooltip("How wide it sees, in degrees")]
    [Range(1f, 359f)]
    public float viewAngle = 40f;

    [Tooltip("How far it sees, in world units (tiles)")]
    [Min(0.1f)]
    public float viewDistance = 9f;

    [Tooltip("Seconds it stays alarmed after seeing the player. It doesn't look during this time")]
    public float alarmSeconds = 1f;
}
