using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The screen after the level, built like the last shot of a film: darkness, then a beam of light from above
/// (the "light in the tunnel"), the day's title, the hero's last thoughts written one by one,
/// and the results of the day. It waits for the "level won" or "level lost" event, and then covers the level.
/// It only shows: LevelFreeze is the one that stops the level. The UI is created here in code.
/// </summary>
public class EndScreen : MonoBehaviour
{
    // Above everything else in the level, below the scene fader.
    private const int CanvasSortingOrder = 60;

    // The dark that covers the level. It starts see-through and fades to its full alpha (1).
    private static readonly Color BackgroundColor = new Color(0.02f, 0.02f, 0.03f, 0f);

    // The beam of light from the top of the screen, in UI pixels.
    private static readonly Vector2 BeamSize = new Vector2(900f, 1100f);

    // The icons of the results are a bit see-through, so the numbers stand out.
    private const float ResultIconAlpha = 0.75f;

    // White and see-through: the beam starts hidden and fades in.
    private static readonly Color BeamStartColor = new Color(1f, 1f, 1f, 0f);

    // Layout in UI pixels, from the middle of the screen. Up is plus, down is minus.
    private const float TextWidth = 1400f;
    private const float TitleHeight = 140f;
    private const int TitleFontSize = 104;
    private const float TitleY = 300f;
    private const float SubtitleHeight = 70f;
    private const int SubtitleFontSize = 44;
    private const float SubtitleY = 210f;

    // The thoughts go down the screen from FirstThoughtY, one line every ThoughtLineGap pixels.
    private const float ThoughtHeight = 50f;
    private const int ThoughtFontSize = 34;
    private const float FirstThoughtY = 70f;
    private const float ThoughtLineGap = 58f;

    // The row of results, and the gaps in it, in UI pixels.
    private const float ResultsY = -170f;
    private static readonly Vector2 ResultsSize = new Vector2(1200f, 60f);
    private const float ResultsSpacing = 70f;
    private const float ResultIconTextSpacing = 14f;
    private static readonly Vector2 ResultIconSize = new Vector2(40f, 40f);

    // The result text sizes itself to its width, so only its height is set here.
    private static readonly Vector2 ResultTextSize = new Vector2(0f, 50f);
    private const int ResultFontSize = 30;

    // The "Play again" button, centered under the results, in UI pixels.
    private static readonly Vector2 ButtonSize = new Vector2(380f, 84f);
    private const int ButtonFontSize = 40;
    private const float ButtonY = -320f;

    [Header("The words")]
    [SerializeField] private string title = "Day 1";
    [SerializeField] private string winSubtitle = "I made it through the first day.";
    [SerializeField] private string loseSubtitle = "Not today.";
    [SerializeField] private string[] winThoughts = { "I drank slowly. Small sips.", "Somewhere above, people are looking for us.", "Tomorrow I'll look for the way out." };
    [SerializeField] private string[] loseThoughts = { "The darkness was too heavy today.", "I'll rest. Tomorrow I'll try again." };

    [Header("Timing, in seconds")]
    [Tooltip("How long the beam takes to fade in, in seconds")]
    [SerializeField] private float beamSeconds = 4f;
    [Tooltip("The wait after the beam starts, before the title, in seconds")]
    [SerializeField] private float afterBeam = 1.2f;
    [Tooltip("How long the title takes to fade in, in seconds")]
    [SerializeField] private float titleFade = 1.6f;
    [Tooltip("How long the subtitle takes to fade in, in seconds")]
    [SerializeField] private float subtitleFade = 1.2f;
    [Tooltip("The wait after the subtitle, before the thoughts, in seconds")]
    [SerializeField] private float afterTitle = 0.6f;
    [Tooltip("How fast the thoughts are typed, in letters per second")]
    [SerializeField] private float lettersPerSecond = 26f;
    [Tooltip("The wait between two thoughts, in seconds")]
    [SerializeField] private float betweenThoughts = 0.7f;
    [Tooltip("How long the results and the button take to fade in, in seconds")]
    [SerializeField] private float resultsFade = 1.2f;

    [Header("The light")]
    [Tooltip("How bright the beam gets after a win, and after a loss. The brighter the ending, the more light comes in")]
    [SerializeField] private Vector2 beamBrightness = new Vector2(0.4f, 0.12f);

    [Tooltip("The volume of the win or lose sound, from 0 to 1")]
    [SerializeField] private float endSoundVolume = 0.9f;

    [Header("Listens to")]
    [SerializeField] private GameEvent levelWon;
    [SerializeField] private GameEvent levelLost;

    [Header("Where the results come from")]
    [Tooltip("The fade to black takes endFadeSeconds, the time LevelFreeze waits before it stops the level")]
    [SerializeField] private TunnelLevelSettings settings;
    [SerializeField] private RoundStats stats;
    [SerializeField] private Hope hope;

    [Tooltip("UI under the end screen that is hidden once the screen is black, so it doesn't keep talking")]
    [SerializeField] private GameObject[] hideWhenLevelEnds;

    private Transform canvas;

    private void OnEnable()
    {
        levelWon.Subscribe(ShowWin);
        levelLost.Subscribe(ShowLose);
    }

    private void OnDisable()
    {
        levelWon.Unsubscribe(ShowWin);
        levelLost.Unsubscribe(ShowLose);
    }

    private void ShowWin()
    {
        StartCoroutine(Show(true));
    }

    private void ShowLose()
    {
        StartCoroutine(Show(false));
    }

    private IEnumerator Show(bool won)
    {
        UiFactory.EnsureEventSystem();
        canvas = UiFactory.CreateCanvas("End Canvas", CanvasSortingOrder).transform;
        Image background = UiFactory.CreateImage(canvas, "Background", null, BackgroundColor);
        UiFactory.Stretch(background.rectTransform);
        background.raycastTarget = true;
        yield return UiAnim.FadeGraphic(background, 1f, settings.endFadeSeconds);

        foreach (GameObject ui in hideWhenLevelEnds)
        {
            ui.SetActive(false);
        }
        Sfx.Play(won ? "Win" : "Lose", endSoundVolume);

        Image beam = UiFactory.CreateImage(canvas, "Beam", UiTheme.Sprite("Beam"), BeamStartColor);
        UiFactory.Place(beam.rectTransform, UiFactory.TopCenter, Vector2.zero, BeamSize);
        StartCoroutine(UiAnim.FadeGraphic(beam, won ? beamBrightness.x : beamBrightness.y, beamSeconds));
        Image vignette = UiFactory.CreateImage(canvas, "Vignette", UiTheme.Sprite("Vignette"), Color.white);
        UiFactory.Stretch(vignette.rectTransform);
        yield return new WaitForSeconds(afterBeam);

        Text titleText = AddCentered(UiFactory.CreateTitle(canvas, "Title", UiFactory.Center, new Vector2(TextWidth, TitleHeight), TitleFontSize, UiTheme.TextColor), TitleY);
        Text subtitleText = AddCentered(UiFactory.CreateTitle(canvas, "Subtitle", UiFactory.Center, new Vector2(TextWidth, SubtitleHeight), SubtitleFontSize, UiTheme.TextColor), SubtitleY);
        subtitleText.font = UiTheme.ItalicFont;
        titleText.text = title;
        subtitleText.text = won ? winSubtitle : loseSubtitle;
        yield return FadeIn(titleText, titleFade);
        yield return FadeIn(subtitleText, subtitleFade);
        yield return new WaitForSeconds(afterTitle);

        string[] thoughts = won ? winThoughts : loseThoughts;
        for (int i = 0; i < thoughts.Length; i++)
        {
            Text line = AddCentered(UiFactory.CreateText(canvas, "Thought", UiFactory.Center, new Vector2(TextWidth, ThoughtHeight), ThoughtFontSize, TextAnchor.MiddleCenter), FirstThoughtY - i * ThoughtLineGap);
            yield return UiAnim.Typewriter(line, thoughts[i], lettersPerSecond);
            yield return new WaitForSeconds(betweenThoughts);
        }

        yield return ShowResults(ResultsY);

        string thisScene = SceneManager.GetActiveScene().name;
        yield return ShowButton("Play again", () => ScreenFader.FadeAndLoad(thisScene));
    }

    private IEnumerator ShowButton(string label, UnityAction onClick)
    {
        Button button = UiFactory.CreateButton(canvas, label, UiFactory.Center, ButtonSize, ButtonFontSize, Color.white);
        ((RectTransform)button.transform).anchoredPosition = new Vector2(0f, ButtonY);
        button.onClick.AddListener(onClick);
        var group = button.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        yield return UiAnim.Fade(group, 1f, resultsFade);
    }

    /// <summary>
    /// One quiet row: hope, times seen, and the time, each with a small icon.
    /// Layout groups place them: each result sizes itself to its text, and the row keeps equal gaps between them.
    /// </summary>
    private IEnumerator ShowResults(float y)
    {
        RectTransform row = UiFactory.CreateRow(canvas, "Results", ResultsSpacing);
        UiFactory.Place(row, UiFactory.Center, new Vector2(0f, y), ResultsSize);
        row.pivot = UiFactory.Center;

        string seen = stats.Captures == 0 ? "never seen" : stats.Captures == 1 ? "seen once" : $"seen {stats.Captures} times";
        AddResult(row, "Hope", $"hope {hope.Current}");
        AddResult(row, "Eye", seen);
        AddResult(row, "Clock", $"{stats.Seconds:0} seconds");

        var group = row.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        yield return UiAnim.Fade(group, 1f, resultsFade);
    }

    private static void AddResult(Transform row, string icon, string value)
    {
        RectTransform item = UiFactory.CreateRow(row, icon, ResultIconTextSpacing);

        Image image = UiFactory.CreateIcon(item, icon, ResultIconSize);
        image.color = new Color(1f, 1f, 1f, ResultIconAlpha);

        Text text = UiFactory.CreateText(item, icon + " Value", UiFactory.Center, ResultTextSize, ResultFontSize, TextAnchor.MiddleLeft);
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.color = UiTheme.Muted;
        text.text = value;
        text.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private static Text AddCentered(Text text, float y)
    {
        text.rectTransform.pivot = UiFactory.Center;
        text.rectTransform.anchoredPosition = new Vector2(0f, y);
        return text;
    }

    private static IEnumerator FadeIn(Text text, float seconds)
    {
        Color color = text.color;
        color.a = 0f;
        text.color = color;
        yield return UiAnim.FadeGraphic(text, 1f, seconds);
    }
}
