using UnityEngine;

/// <summary>
/// The numbers of one tunnel level, saved as an asset so they can be tuned without touching code.
/// </summary>
[CreateAssetMenu(menuName = "Tunnel/Level Settings")]
public class TunnelLevelSettings : ScriptableObject
{
    [Min(1)]
    public int maxHope = 100;

    [Tooltip("Hope at the start. The hero wakes up weak, so memories have room to fill it")]
    public int startHope = 30;

    [Tooltip("Hope lost every time the player is caught. At 0 hope the level is lost")]
    public int hopeLostWhenCaught = 20;

    [Tooltip("Seconds the player can't move after being caught. The screen is dark for this time, and the hero breathes")]
    [Min(0f)]
    public float caughtFreezeSeconds = 4.2f;

    [Tooltip("Seconds from the end of the level until the level stops. The end screen fades to black in this time")]
    [Min(0f)]
    public float endFadeSeconds = 0.8f;
}
