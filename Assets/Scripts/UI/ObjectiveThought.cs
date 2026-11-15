using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Writes what the hero is thinking under the hope bar, which is also the goal of the moment:
/// first "find water", and after picking up the bottle "bring it back".
/// </summary>
public class ObjectiveThought : MonoBehaviour
{
    private const int CanvasSortingOrder = 10;

    // The line under the hope bar, in UI pixels from the top-left corner of the screen. Down is minus.
    private static readonly Vector2 ThoughtSize = new Vector2(900f, 50f);
    private static readonly Vector2 ThoughtPosition = new Vector2(40f, -112f);
    private const int ThoughtFontSize = 32;

    [Tooltip("For the time the hero is held after being caught. The new goal is written when he can move again")]
    [SerializeField] private TunnelLevelSettings settings;

    [Header("Listens to")]
    [SerializeField] private GameEvent bottlePicked;
    [SerializeField] private GameEvent bottleDropped;

    [Tooltip("Raised with the thought about the bottle, when it is picked up")]
    [SerializeField] private TextEvent hintRequested;

    [Header("The words")]
    [SerializeField] private string findWater = "I need water.";
    [SerializeField] private string goBack = "Back to the mattress. Quietly.";

    [TextArea]
    [SerializeField] private string pickedThought = "Water. Only half a bottle left in it.\nI'll drink it in the cell, where it's safe.";

    [Header("Timing")]
    [Tooltip("Seconds from the start of the level to the first goal, so it comes after the opening")]
    [SerializeField] private float firstThoughtDelay = 14f;

    [Tooltip("Seconds from picking up the bottle to the new goal")]
    [SerializeField] private float pickedThoughtDelay = 0.5f;

    [Tooltip("How fast the goal is typed, in letters per second")]
    [SerializeField] private float lettersPerSecond = 18f;

    private Text thought;
    private Coroutine thinking;

    // The UI is created in Awake, before OnEnable starts listening.
    private void Awake()
    {
        Canvas canvas = UiFactory.CreateCanvas("Thought Canvas", CanvasSortingOrder);
        canvas.transform.SetParent(transform, false);
        thought = UiFactory.CreateText(canvas.transform, "Thought", UiFactory.TopLeft, ThoughtSize, ThoughtFontSize, TextAnchor.MiddleLeft);
        thought.rectTransform.anchoredPosition = ThoughtPosition;
        thought.text = "";
    }

    private void OnEnable()
    {
        bottlePicked.Subscribe(OnBottlePicked);
        bottleDropped.Subscribe(OnBottleDropped);
    }

    private void OnDisable()
    {
        bottlePicked.Unsubscribe(OnBottlePicked);
        bottleDropped.Unsubscribe(OnBottleDropped);
    }

    private void Start()
    {
        StartThought(findWater, firstThoughtDelay);
    }

    private void OnBottlePicked()
    {
        StartThought(goBack, pickedThoughtDelay);
        hintRequested.Raise(pickedThought);
    }

    private void OnBottleDropped()
    {
        StartThought(findWater, settings.caughtFreezeSeconds);
    }

    /// <summary>A new thought replaces the one being written, so two thoughts never mix.</summary>
    private void StartThought(string text, float delay)
    {
        if (thinking != null)
        {
            StopCoroutine(thinking);
        }
        thinking = StartCoroutine(Think(text, delay));
    }

    private IEnumerator Think(string text, float delay)
    {
        yield return new WaitForSeconds(delay);
        yield return UiAnim.Typewriter(thought, text, lettersPerSecond);
    }
}
