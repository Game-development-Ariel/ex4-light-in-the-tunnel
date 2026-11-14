using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The level's HUD, created in code when the level starts. It is kept small and quiet:
/// - top left: a small flame and a thin bar for hope, and a drop that fills when the hero carries water;
/// - under it: what the hero is thinking, which is also the goal ("I need water.");
/// - when caught: the screen goes dark for a moment, and the hero wakes up back in the cell.
/// It listens to the game's events (caught, bottle picked, hope changed), so it reacts the moment something happens.
/// The game scripts don't know this class exists.
/// </summary>
public class StatusHud : MonoBehaviour
{
    // Higher than the minimap's canvas, so the HUD is drawn on top. The vignette is under everything.
    private const int CanvasSortingOrder = 10;
    private const int VignetteSortingOrder = -10;
    private const int BlackoutSortingOrder = 40;

    private static readonly Vector2 TopLeft = new Vector2(0f, 1f);
    private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

    private const string FindWater = "I need water.";
    private const string GoBack = "Back to the mattress. Quietly.";

    [Tooltip("How fast the hope bar slides to the new value, in fractions of the bar per second")]
    [SerializeField] private float barSpeed = 0.6f;

    [Tooltip("Seconds of darkness after being caught")]
    [SerializeField] private float blackoutSeconds = 2.2f;

    [Header("The game objects it shows")]
    [SerializeField] private Hope hope;
    [SerializeField] private BottleCarrier carrier;
    [SerializeField] private PlayerMovement player;
    [SerializeField] private HintToast hints;

    [Header("Listens to")]
    [SerializeField] private GameEvent playerCaught;
    [SerializeField] private GameEvent bottlePicked;

    private RectTransform flame;
    private Image hopeFill;
    private Image waterIcon;
    private Text thought;
    private Text gainText;
    private CanvasGroup blackout;
    private Text blackoutText;
    private Coroutine thinking;

    // The values shown now, to notice when something changes.
    private bool shownHasBottle;
    private int shownHope;
    private int captures;

    // The UI is created in Awake, before OnEnable starts listening.
    private void Awake()
    {
        CreateVignette();
        Canvas canvas = UiFactory.CreateCanvas("HUD Canvas", CanvasSortingOrder);
        canvas.transform.SetParent(transform, false);
        CreateHope(canvas.transform);
        CreateThought(canvas.transform);
        CreateBlackout();
    }

    private void OnEnable()
    {
        playerCaught.Subscribe(OnCaptured);
        bottlePicked.Subscribe(OnBottlePicked);
        hope.Changed += OnHopeChanged;
    }

    private void OnDisable()
    {
        playerCaught.Unsubscribe(OnCaptured);
        bottlePicked.Unsubscribe(OnBottlePicked);
        hope.Changed -= OnHopeChanged;
    }

    private void Start()
    {
        shownHope = hope.Current;
        hopeFill.fillAmount = (float)hope.Current / hope.Max;
        StartThought(FindWater, 14f); // After the opening, the first thought appears.
    }

    private void Update()
    {
        // The flame breathes a little, like a real flame in still air.
        float flicker = 1f + (Mathf.PerlinNoise(Time.time * 2f, 0f) - 0.5f) * 0.12f;
        flame.localScale = new Vector3(1f, flicker, 1f);

        // The bar slides to the new value instead of jumping.
        float goal = (float)shownHope / hope.Max;
        hopeFill.fillAmount = Mathf.MoveTowards(hopeFill.fillAmount, goal, barSpeed * Time.deltaTime);

        if (carrier.HasBottle != shownHasBottle)
        {
            shownHasBottle = carrier.HasBottle;
            StartCoroutine(UiAnim.FadeGraphic(waterIcon, shownHasBottle ? 1f : 0.18f, 0.6f));
        }
    }

    private void OnHopeChanged(int newHope)
    {
        if (newHope > shownHope)
        {
            StartCoroutine(UiAnim.Punch(flame, 0.35f, 0.6f)); // Hope grew: the flame flares up.
            StartCoroutine(ShowGain(newHope - shownHope));
        }
        shownHope = newHope;
    }

    /// <summary>Writes how much hope was gained next to the bar, and lets it fade.</summary>
    private IEnumerator ShowGain(int amount)
    {
        gainText.text = "+" + amount + " hope";
        gainText.color = UiTheme.Hope;
        yield return new WaitForSeconds(1.5f);
        yield return UiAnim.FadeGraphic(gainText, 0f, 1.5f);
    }

    private void OnBottlePicked()
    {
        Sfx.Play("Water", 0.8f);
        StartCoroutine(UiAnim.Punch(waterIcon.transform, 0.3f, 0.5f));
        StartThought(GoBack, 0.5f);
        ShowHint("Water. Only half a bottle left in it.\nI'll drink it in the cell, where it's safe.");
    }

    private void OnCaptured()
    {
        // The carrier may have dropped the bottle already, so the last shown value tells if there was one.
        bool droppedBottle = shownHasBottle;
        captures++;
        Sfx.Play("Caught", 0.9f);
        StartCoroutine(Blackout(droppedBottle));
    }

    /// <summary>
    /// The player was already sent back to the cell. The darkness hides the jump,
    /// and gives the player a moment, like the hero, to breathe before trying again.
    /// </summary>
    private IEnumerator Blackout(bool droppedBottle)
    {
        SetPlayerFree(false);
        blackoutText.text = captures == 1 ? "They saw me." : "Again...";
        yield return UiAnim.Fade(blackout, 1f, 0.5f);
        yield return new WaitForSeconds(blackoutSeconds);
        yield return UiAnim.Fade(blackout, 0f, 1.5f);
        SetPlayerFree(true);

        if (droppedBottle)
        {
            StartThought(FindWater, 0f);
            ShowHint("They took the bottle back to the storage room.\nI'll try again. Slower this time.");
        }
        else if (captures == 1)
        {
            ShowHint("Back in the cell. Breathe.\nThe pale light is the camera's eye. I wait in the dark until it turns away.");
        }
        else
        {
            ShowHint("Breathe. Watch the light. Wait for it to turn.");
        }
    }

    private void SetPlayerFree(bool free)
    {
        player.enabled = free; // PlayerMovement moves only while it is enabled.
    }

    private void ShowHint(string text)
    {
        hints.Show(text);
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

    /// <summary>Writes a new thought under the hope bar, after a delay.</summary>
    private IEnumerator Think(string text, float delay)
    {
        yield return new WaitForSeconds(delay);
        yield return UiAnim.Typewriter(thought, text, 18f);
    }

    private void CreateVignette()
    {
        Canvas canvas = UiFactory.CreateCanvas("Vignette Canvas", VignetteSortingOrder);
        canvas.transform.SetParent(transform, false);
        Image vignette = UiFactory.CreateImage(canvas.transform, "Vignette", UiTheme.Sprite("Vignette"), new Color(1f, 1f, 1f, 0.7f));
        UiFactory.Stretch(vignette.rectTransform);
    }

    /// <summary>A small flame, a thin bar beside it, and the water drop at the end of the bar.</summary>
    private void CreateHope(Transform canvas)
    {
        Image flameIcon = UiFactory.CreateIcon(canvas, "Hope", new Vector2(64f, 64f));
        flame = flameIcon.rectTransform;
        UiFactory.Place(flame, TopLeft, new Vector2(70f, -76f), new Vector2(64f, 64f));
        flame.pivot = new Vector2(0.5f, 0f); // It flickers from its base, like a real flame.

        Text label = UiFactory.CreateText(canvas, "Label", TopLeft, new Vector2(200f, 30f), 26, TextAnchor.MiddleLeft);
        label.rectTransform.anchoredPosition = new Vector2(112f, -36f);
        label.color = UiTheme.Muted;
        label.text = "hope";

        Image barBack = UiFactory.CreatePanel(canvas, "Hope Bar", "BarBack");
        UiFactory.Place(barBack.rectTransform, TopLeft, new Vector2(112f, -74f), new Vector2(280f, 12f));

        hopeFill = UiFactory.CreateImage(barBack.transform, "Fill", UiTheme.Sprite("BarFill"), UiTheme.Hope);
        UiFactory.Stretch(hopeFill.rectTransform);
        hopeFill.type = Image.Type.Filled; // fillAmount from 0 to 1 cuts the picture from the right.
        hopeFill.fillMethod = Image.FillMethod.Horizontal;

        gainText = UiFactory.CreateTitle(canvas, "Gain", TopLeft, new Vector2(260f, 44f), 34, UiTheme.Hope);
        gainText.rectTransform.anchoredPosition = new Vector2(480f, -50f);
        gainText.alignment = TextAnchor.MiddleLeft;
        gainText.text = "";

        waterIcon = UiFactory.CreateIcon(canvas, "Drop", new Vector2(40f, 40f));
        UiFactory.Place(waterIcon.rectTransform, TopLeft, new Vector2(428f, -60f), new Vector2(40f, 40f));
        waterIcon.rectTransform.pivot = Center;
        waterIcon.color = new Color(1f, 1f, 1f, 0.18f); // Faint = the hero has no water.
    }

    private void CreateThought(Transform canvas)
    {
        thought = UiFactory.CreateText(canvas, "Thought", TopLeft, new Vector2(900f, 50f), 32, TextAnchor.MiddleLeft);
        thought.rectTransform.anchoredPosition = new Vector2(40f, -112f);
        thought.text = "";
    }

    /// <summary>A black screen with one line of text, hidden until the player is caught.</summary>
    private void CreateBlackout()
    {
        Canvas canvas = UiFactory.CreateCanvas("Blackout", BlackoutSortingOrder);
        canvas.transform.SetParent(transform, false);
        blackout = canvas.gameObject.AddComponent<CanvasGroup>();
        blackout.alpha = 0f;
        blackout.blocksRaycasts = false;

        Image black = UiFactory.CreateImage(canvas.transform, "Black", null, new Color(0.02f, 0f, 0f, 0.96f));
        UiFactory.Stretch(black.rectTransform);

        blackoutText = UiFactory.CreateTitle(canvas.transform, "Line", Center, new Vector2(1200f, 120f), 56, UiTheme.TextColor);
        blackoutText.rectTransform.pivot = Center;
        blackoutText.font = UiTheme.ItalicFont;
    }
}
