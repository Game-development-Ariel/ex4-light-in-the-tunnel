using UnityEngine;

/// <summary>
/// How well a watcher (a security camera, later a guard) sees. Saved as an asset,
/// so a new kind of watcher is a new asset and not new code.
/// </summary>
[CreateAssetMenu(menuName = "Tunnel/Vision Stats")]
public class VisionStats : ScriptableObject
{
    [Tooltip("How wide it sees, in degrees")]
    public float viewAngle = 40f;

    [Tooltip("How far it sees, in world units (tiles)")]
    public float viewDistance = 9f;

    [Tooltip("Seconds it stays alarmed after seeing the player. It doesn't look during this time")]
    public float alarmSeconds = 1f;
}
