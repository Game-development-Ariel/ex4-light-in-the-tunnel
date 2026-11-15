using UnityEngine;

/// <summary>
/// What hope does to the hero's body, saved as an asset. Each pair is the value with no hope and with full hope,
/// and the real value slides between them. From the design: without hope you can't escape.
/// </summary>
[CreateAssetMenu(menuName = "Tunnel/Hope Effects")]
public class HopeEffects : ScriptableObject
{
    [Tooltip("Walking speed, as a multiplier of the normal speed")]
    public Vector2 speedFactor = new Vector2(0.6f, 1.6f);

    [Tooltip("How far the hero's eyes see in the dark, in tiles")]
    public Vector2 sightRadius = new Vector2(1.6f, 7.5f);

    [Tooltip("How bright the light around the hero is")]
    public Vector2 sightBrightness = new Vector2(0.45f, 1.1f);

    [Tooltip("How bright the whole tunnel looks. With hope the darkness itself is less heavy")]
    public Vector2 tunnelBrightness = new Vector2(0.1f, 0.42f);

    [Tooltip("When hope is gained, the light flares for a moment: tiles added to the sight radius")]
    public float surgeRadius = 2.5f;

    [Tooltip("When hope is gained, the light flares for a moment: brightness added to the light around the hero")]
    public float surgeBrightness = 0.5f;

    [Tooltip("Seconds the flare takes to fade")]
    [Min(0.01f)]
    public float surgeSeconds = 2f;

    [Tooltip("From how far the hero hears a guard's steps, in tiles")]
    public Vector2 hearingRange = new Vector2(1.5f, 14f);

    [Tooltip("At this hope and above the hero is steady. HopeThoughts says it when hope crosses it")]
    public int steadyHope = 50;

    [Tooltip("At this hope and above the hero is full of faith")]
    public int faithHope = 75;
}
