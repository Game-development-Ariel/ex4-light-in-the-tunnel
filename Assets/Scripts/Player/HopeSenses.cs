using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Turns hope into ability: with more hope the hero walks faster, sees further in the dark and hears guards
/// from further away. When hope crosses a level, it raises a thought so the player notices the change.
/// </summary>
[RequireComponent(typeof(Hope), typeof(PlayerMovement))]
public class HopeSenses : MonoBehaviour
{
    [SerializeField] private HopeEffects effects;

    [Tooltip("The soft light around the hero. Its radius is how far the eyes see")]
    [SerializeField] private Light2D eyes;

    [Tooltip("The dim light of the whole tunnel")]
    [SerializeField] private Light2D tunnelLight;

    [Tooltip("How fast the body follows a change of hope, in fractions per second")]
    [SerializeField] private float changeSpeed = 0.4f;

    [Tooltip("Raised with a thought when hope crosses a level")]
    [SerializeField] private TextEvent hintRequested;

    [TextArea]
    [SerializeField] private string steadyThought = "My legs are steadier. I see a little further into the dark.";

    [TextArea]
    [SerializeField] private string faithThought = "I am not alone here. I can hear their steps long before they come.";

    [TextArea]
    [SerializeField] private string weakThought = "Everything is heavier now. Darker. I need something to hold on to.";

    private Hope hope;
    private PlayerMovement movement;

    // From 0 (no hope) to 1 (full hope). It follows the real hope slowly, so the change is felt and not sudden.
    private float strength;
    private int lastHope;

    // 1 right after hope is gained, then fades to 0. It makes the light flare, so the moment is felt.
    private float surge;

    /// <summary>From how far the hero hears steps now, in tiles.</summary>
    public float HearingRange => Mathf.Lerp(effects.hearingRange.x, effects.hearingRange.y, strength);

    private void Awake()
    {
        hope = GetComponent<Hope>();
        movement = GetComponent<PlayerMovement>();
    }

    private void OnEnable()
    {
        hope.Changed += OnHopeChanged;
    }

    private void OnDisable()
    {
        hope.Changed -= OnHopeChanged;
    }

    private void Start()
    {
        lastHope = hope.Current;
        strength = (float)hope.Current / hope.Max;
        Apply();
    }

    private void Update()
    {
        float goal = (float)hope.Current / hope.Max;
        strength = Mathf.MoveTowards(strength, goal, changeSpeed * Time.deltaTime);
        surge = Mathf.MoveTowards(surge, 0f, Time.deltaTime / effects.surgeSeconds);
        Apply();
    }

    private void Apply()
    {
        movement.SpeedFactor = Mathf.Lerp(effects.speedFactor.x, effects.speedFactor.y, strength);
        eyes.pointLightOuterRadius = Mathf.Lerp(effects.sightRadius.x, effects.sightRadius.y, strength) + surge * effects.surgeRadius;
        eyes.intensity = Mathf.Lerp(effects.sightBrightness.x, effects.sightBrightness.y, strength) + surge * 0.5f;
        tunnelLight.intensity = Mathf.Lerp(effects.tunnelBrightness.x, effects.tunnelBrightness.y, strength);
    }

    private void OnHopeChanged(int newHope)
    {
        if (newHope > lastHope)
        {
            surge = 1f;
        }

        if (Crossed(effects.faithHope, lastHope, newHope))
        {
            hintRequested.Raise(faithThought);
        }
        else if (Crossed(effects.steadyHope, lastHope, newHope))
        {
            hintRequested.Raise(steadyThought);
        }
        else if (Crossed(effects.steadyHope, newHope, lastHope))
        {
            hintRequested.Raise(weakThought);
        }
        lastHope = newHope;
    }

    // True if the hope went from below the level to the level or above it.
    private static bool Crossed(int level, int from, int to)
    {
        return from < level && to >= level;
    }
}
