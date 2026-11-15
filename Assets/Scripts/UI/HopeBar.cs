using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows the hero's hope in the top-left corner: a small flame, a thin bar that slides to the new value,
/// and "+N hope" next to it when hope is gained.
/// </summary>
public class HopeBar : MonoBehaviour
{
    // Higher than the minimap's canvas, so the HUD is drawn on top.
    private const int CanvasSortingOrder = 10;

    [SerializeField] private Hope hope;

    [Tooltip("How fast the bar slides to the new value, in fractions of the bar per second")]
    [SerializeField] private float barSpeed = 0.6f;

    [Tooltip("How much the flame's height breathes, like a real flame in still air. 0.12 = 12 percent")]
    [SerializeField] private float flameFlicker = 0.12f;

    [Tooltip("How fast the flame breathes")]
    [SerializeField] private float flameFlickerSpeed = 2f;

    [Tooltip("How much the flame grows for a moment when hope is gained, and for how many seconds")]
    [SerializeField] private Vector2 gainPunch = new Vector2(0.35f, 0.6f);

    [Tooltip("Seconds the \"+N hope\" text stays, and then seconds it takes to fade")]
    [SerializeField] private Vector2 gainTextSeconds = new Vector2(1.5f, 1.5f);

    private RectTransform flame;
    private Image hopeFill;
    private Text gainText;

    // The hope shown now, to notice when it grows.
    private int shownHope;

    // The UI is created in Awake, before OnEnable starts listening.
    private void Awake()
    {
        Canvas canvas = UiFactory.CreateCanvas("Hope Canvas", CanvasSortingOrder);
        canvas.transform.SetParent(transform, false);
        Create(canvas.transform);
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
        shownHope = hope.Current;
        hopeFill.fillAmount = (float)hope.Current / hope.Max;
    }

    private void Update()
    {
        float flicker = 1f + (Mathf.PerlinNoise(Time.time * flameFlickerSpeed, 0f) - 0.5f) * flameFlicker;
        flame.localScale = new Vector3(1f, flicker, 1f);

        // The bar slides to the new value instead of jumping.
        float goal = (float)shownHope / hope.Max;
        hopeFill.fillAmount = Mathf.MoveTowards(hopeFill.fillAmount, goal, barSpeed * Time.deltaTime);
    }

    private void OnHopeChanged(int newHope)
    {
        if (newHope > shownHope)
        {
            StartCoroutine(UiAnim.Punch(flame, gainPunch.x, gainPunch.y)); // Hope grew: the flame flares up.
            StartCoroutine(ShowGain(newHope - shownHope));
        }
        shownHope = newHope;
    }

    /// <summary>Writes how much hope was gained next to the bar, and lets it fade.</summary>
    private IEnumerator ShowGain(int amount)
    {
        gainText.text = "+" + amount + " hope";
        gainText.color = UiTheme.Hope;
        yield return new WaitForSeconds(gainTextSeconds.x);
        yield return UiAnim.FadeGraphic(gainText, 0f, gainTextSeconds.y);
    }

    /// <summary>A small flame, the word "hope", a thin bar beside it, and the text of the gain.</summary>
    private void Create(Transform canvas)
    {
        Image flameIcon = UiFactory.CreateIcon(canvas, "Hope", new Vector2(64f, 64f));
        flame = flameIcon.rectTransform;
        UiFactory.Place(flame, UiFactory.TopLeft, new Vector2(70f, -76f), new Vector2(64f, 64f));
        flame.pivot = new Vector2(0.5f, 0f); // It flickers from its base, like a real flame.

        Text label = UiFactory.CreateText(canvas, "Label", UiFactory.TopLeft, new Vector2(200f, 30f), 26, TextAnchor.MiddleLeft);
        label.rectTransform.anchoredPosition = new Vector2(112f, -36f);
        label.color = UiTheme.Muted;
        label.text = "hope";

        Image barBack = UiFactory.CreatePanel(canvas, "Hope Bar", "BarBack");
        UiFactory.Place(barBack.rectTransform, UiFactory.TopLeft, new Vector2(112f, -74f), new Vector2(280f, 12f));

        hopeFill = UiFactory.CreateImage(barBack.transform, "Fill", UiTheme.Sprite("BarFill"), UiTheme.Hope);
        UiFactory.Stretch(hopeFill.rectTransform);
        hopeFill.type = Image.Type.Filled; // fillAmount from 0 to 1 cuts the picture from the right.
        hopeFill.fillMethod = Image.FillMethod.Horizontal;

        gainText = UiFactory.CreateTitle(canvas, "Gain", UiFactory.TopLeft, new Vector2(260f, 44f), 34, UiTheme.Hope);
        gainText.rectTransform.anchoredPosition = new Vector2(480f, -50f);
        gainText.alignment = TextAnchor.MiddleLeft;
        gainText.text = "";
    }
}
