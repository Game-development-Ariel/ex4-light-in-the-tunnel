using UnityEngine;

/// <summary>
/// Raises a thought when hope crosses a level (the levels are in HopeEffects), so the player notices
/// that the body changed: steadier and more faithful going up, heavier going down.
/// </summary>
[RequireComponent(typeof(Hope))]
public class HopeThoughts : MonoBehaviour
{
    [SerializeField] private HopeEffects effects;

    [Tooltip("Raised with the thought. The hint UI shows it")]
    [SerializeField] private TextEvent hintRequested;

    [TextArea]
    [SerializeField] private string steadyThought = "My legs are steadier. I see a little further into the dark.";

    [TextArea]
    [SerializeField] private string faithThought = "I am not alone here. I can hear their steps long before they come.";

    [TextArea]
    [SerializeField] private string weakThought = "Everything is heavier now. Darker. I need something to hold on to.";

    private Hope hope;
    private int lastHope;

    private void Awake()
    {
        hope = GetComponent<Hope>();
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
    }

    private void OnHopeChanged(int newHope)
    {
        if (CrossedUp(effects.faithHope, lastHope, newHope))
        {
            hintRequested.Raise(faithThought);
        }
        else if (CrossedUp(effects.steadyHope, lastHope, newHope))
        {
            hintRequested.Raise(steadyThought);
        }
        else if (CrossedDown(effects.steadyHope, lastHope, newHope))
        {
            hintRequested.Raise(weakThought);
        }
        lastHope = newHope;
    }

    // True if the hope went from below the level to the level or above it.
    private static bool CrossedUp(int level, int from, int to)
    {
        return from < level && to >= level;
    }

    // True if the hope went from the level or above it to below it.
    private static bool CrossedDown(int level, int from, int to)
    {
        return from >= level && to < level;
    }
}
