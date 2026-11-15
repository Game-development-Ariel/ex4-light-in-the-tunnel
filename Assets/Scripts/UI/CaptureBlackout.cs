using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// When the player is caught, the screen goes dark with one line ("They saw me."), and when it clears
/// the hero thinks about what to do now. The dark lasts as long as LevelFreeze holds the player.
/// </summary>
public class CaptureBlackout : MonoBehaviour
{
    // Above the HUD and the hints, below the title card and the end screen.
    private const int CanvasSortingOrder = 40;

    // Almost black with a little red, and not fully opaque, so the dark feels like closed eyes.
    private static readonly Color DarkColor = new Color(0.02f, 0f, 0f, 0.96f);

    // The line in the middle of the dark, in UI pixels.
    private static readonly Vector2 LineSize = new Vector2(1200f, 120f);
    private const int LineFontSize = 56;

    [Tooltip("How long the player is held after being caught. The dark lasts exactly that long")]
    [SerializeField] private TunnelLevelSettings settings;

    [Header("Listens to")]
    [SerializeField] private GameEvent playerCaught;
    [SerializeField] private GameEvent bottleDropped;

    [Tooltip("Raised with the thought after waking up")]
    [SerializeField] private TextEvent hintRequested;

    [Header("The words")]
    [SerializeField] private string firstLine = "They saw me.";
    [SerializeField] private string againLine = "Again...";

    [TextArea]
    [SerializeField] private string firstThought = "Back in the cell. Breathe.\nThe pale light is the camera's eye. I wait in the dark until it turns away.";

    [TextArea]
    [SerializeField] private string againThought = "Breathe. Watch the light. Wait for it to turn.";

    [TextArea]
    [SerializeField] private string lostBottleThought = "They took the bottle back to the storage room.\nI'll try again. Slower this time.";

    [Header("Timing")]
    [Tooltip("How long the screen takes to go dark, in seconds")]
    [SerializeField] private float fadeInSeconds = 0.5f;
    [Tooltip("How long the dark takes to clear, in seconds")]
    [SerializeField] private float fadeOutSeconds = 1.5f;
    [Tooltip("The volume of the caught sound, from 0 to 1")]
    [SerializeField] private float caughtVolume = 0.9f;

    private CanvasGroup blackout;
    private Text line;
    private bool caughtBefore;
    private bool lostBottle;

    // The UI is created in Awake, before OnEnable starts listening.
    private void Awake()
    {
        Canvas canvas = UiFactory.CreateCanvas("Blackout", CanvasSortingOrder);
        canvas.transform.SetParent(transform, false);
        blackout = canvas.gameObject.AddComponent<CanvasGroup>();
        blackout.alpha = 0f;
        blackout.blocksRaycasts = false;

        Image black = UiFactory.CreateImage(canvas.transform, "Black", null, DarkColor);
        UiFactory.Stretch(black.rectTransform);

        line = UiFactory.CreateTitle(canvas.transform, "Line", UiFactory.Center, LineSize, LineFontSize, UiTheme.TextColor);
        line.rectTransform.pivot = UiFactory.Center;
        line.font = UiTheme.ItalicFont;
    }

    private void OnEnable()
    {
        playerCaught.Subscribe(OnCaught);
        bottleDropped.Subscribe(OnBottleDropped);
    }

    private void OnDisable()
    {
        playerCaught.Unsubscribe(OnCaught);
        bottleDropped.Unsubscribe(OnBottleDropped);
    }

    private void OnCaught()
    {
        Sfx.Play("Caught", caughtVolume);
        StopAllCoroutines(); // A capture during the dark starts it over.
        StartCoroutine(Blackout());
    }

    // The bottle is dropped in the same moment as the capture, so the thought after waking can mention it.
    private void OnBottleDropped()
    {
        lostBottle = true;
    }

    /// <summary>
    /// The player was already sent back to the cell. The dark gives the player a moment, like the hero,
    /// to breathe before trying again.
    /// </summary>
    private IEnumerator Blackout()
    {
        bool first = !caughtBefore;
        caughtBefore = true;
        line.text = first ? firstLine : againLine;
        yield return UiAnim.Fade(blackout, 1f, fadeInSeconds);
        float darkSeconds = Mathf.Max(0f, settings.caughtFreezeSeconds - fadeInSeconds - fadeOutSeconds);
        yield return new WaitForSeconds(darkSeconds);
        yield return UiAnim.Fade(blackout, 0f, fadeOutSeconds);

        if (lostBottle)
        {
            hintRequested.Raise(lostBottleThought);
        }
        else
        {
            hintRequested.Raise(first ? firstThought : againThought);
        }
        lostBottle = false;
    }
}
