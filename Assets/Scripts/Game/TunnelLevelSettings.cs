using UnityEngine;

/// <summary>
/// The numbers of one tunnel level, saved as an asset so they can be tuned without touching code.
/// </summary>
[CreateAssetMenu(menuName = "Tunnel/Level Settings")]
public class TunnelLevelSettings : ScriptableObject
{
    public int maxHope = 100;

    [Tooltip("Hope at the start. The hero wakes up weak, so memories have room to fill it")]
    public int startHope = 60;

    [Tooltip("Hope lost every time the player is caught. At 0 hope the level is lost")]
    public int hopeLostWhenCaught = 25;
}
