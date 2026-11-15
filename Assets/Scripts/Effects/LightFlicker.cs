using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Makes a light bulb flicker like a weak bulb on a tired generator:
/// a small restless shimmer all the time, and sometimes a short dip almost to darkness.
/// </summary>
[RequireComponent(typeof(Light2D))]
public class LightFlicker : MonoBehaviour
{
    [Tooltip("How strong the constant shimmer is. 0.1 = 10 percent")]
    [SerializeField] private float shimmer = 0.12f;

    [Tooltip("On average, seconds between two dips")]
    [SerializeField] private float secondsBetweenDips = 7f;

    [Tooltip("How fast the shimmer changes")]
    [SerializeField] private float shimmerSpeed = 3f;

    [Tooltip("How bright the bulb is during a dip. 0.15 = 15 percent of its normal light")]
    [SerializeField] private float dipBrightness = 0.15f;

    [Tooltip("Seconds a dip lasts")]
    [SerializeField] private float dipSeconds = 0.12f;

    [Tooltip("The first dip comes at the earliest after this many seconds")]
    [SerializeField] private float firstDipAfter = 2f;

    // Each bulb reads the noise from a random place between 0 and this, so no two bulbs flicker the same.
    private const float MaxSeed = 100f;

    // After a dip, the next one comes after secondsBetweenDips times a random factor between these,
    // so the dips don't keep a beat.
    private const float MinDipGapFactor = 0.5f;
    private const float MaxDipGapFactor = 1.5f;

    private Light2D bulb;
    private float baseIntensity;
    private float seed;
    private float dipTimer;

    private void Start()
    {
        bulb = GetComponent<Light2D>();
        baseIntensity = bulb.intensity;
        seed = Random.value * MaxSeed;
        dipTimer = Random.Range(firstDipAfter, secondsBetweenDips * 2f);
    }

    private void Update()
    {
        // Perlin noise changes smoothly, so the shimmer looks natural and not random jumps.
        float noise = Mathf.PerlinNoise(seed, Time.time * shimmerSpeed) - 0.5f;
        float intensity = baseIntensity * (1f + noise * 2f * shimmer);

        dipTimer -= Time.deltaTime;
        if (dipTimer < 0f)
        {
            intensity *= dipBrightness;
            if (dipTimer < -dipSeconds)
            {
                dipTimer = Random.Range(secondsBetweenDips * MinDipGapFactor, secondsBetweenDips * MaxDipGapFactor);
            }
        }
        bulb.intensity = intensity;
    }
}
