using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The tutorial's quiet teacher: the hero's thoughts, written at the bottom of the screen like subtitles.
/// The text types itself out, stays a few seconds, and fades away. Keyboard keys can be shown above it.
/// It listens to the "hint requested" event of the hint zones, and other UI scripts can call Show directly.
/// </summary>
public class HintToast : MonoBehaviour
{
    // Above the HUD (10), below the title card and the fader.
    private const int CanvasSortingOrder = 20;

    // The keyboard keys shown above the text: their size, the gap between them and their font, in UI pixels.
    private const float KeySize = 58f;
    private const float KeySpacing = 10f;
    private const int KeyFontSize = 26;

    // The font size of the thought itself.
    private const int MessageFontSize = 34;

    // The dark band at the bottom of the screen, the message in it, and the row of keys above it, in UI pixels.
    private static readonly Vector2 BandOffset = new Vector2(0f, 40f);
    private static readonly Vector2 BandSize = new Vector2(1500f, 150f);
    private static readonly Vector2 MessageSize = new Vector2(1250f, 140f);
    private static readonly Vector2 KeyRowOffset = new Vector2(0f, 45f);
    private static readonly Vector2 KeyRowSize = new Vector2(600f, KeySize);

    [Tooltip("Seconds a thought stays after it is fully written")]
    [SerializeField] private float showSeconds = 5f;

    [Tooltip("How fast the words are written")]
    [SerializeField] private float lettersPerSecond = 40f;

    [SerializeField] private TextEvent hintRequested;

    [Tooltip("How loud the soft sound of a new thought is")]
    [SerializeField] private float soundVolume = 0.5f;

    [Tooltip("Seconds a thought takes to appear, and to fade away")]
    [SerializeField] private Vector2 fadeSeconds = new Vector2(0.4f, 1.2f);

    private CanvasGroup group;
    private Text message;
    private RectTransform keyRow;
    private Coroutine current;

    private void Awake()
    {
        Canvas canvas = UiFactory.CreateCanvas("Hint Canvas", CanvasSortingOrder);
        canvas.transform.SetParent(transform, false);
        group = canvas.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;

        // A dark band that fades out at its ends, so the words are readable without a hard box.
        Image band = UiFactory.CreateImage(canvas.transform, "Band", UiTheme.Sprite("Band"), Color.white);
        UiFactory.Place(band.rectTransform, UiFactory.BottomCenter, BandOffset, BandSize);

        message = UiFactory.CreateText(band.transform, "Message", UiFactory.Center, MessageSize, MessageFontSize, TextAnchor.MiddleCenter);
        message.rectTransform.pivot = UiFactory.Center;

        keyRow = UiFactory.CreateRow(band.transform, "Keys", KeySpacing);
        UiFactory.Place(keyRow, UiFactory.TopCenter, KeyRowOffset, KeyRowSize);
        keyRow.pivot = UiFactory.Center;
    }

    private void OnEnable()
    {
        hintRequested.Subscribe(ShowHint);
    }

    private void OnDisable()
    {
        hintRequested.Unsubscribe(ShowHint);
    }

    private void ShowHint(string text)
    {
        Show(text);
    }

    /// <summary>Shows a thought. Keys are labels like "W", drawn as keyboard keys above the text.</summary>
    public void Show(string text, params string[] keys)
    {
        if (current != null)
        {
            StopCoroutine(current);
        }
        SetKeys(keys);
        current = StartCoroutine(ShowRoutine(text));
    }

    private IEnumerator ShowRoutine(string text)
    {
        Sfx.Play("Hint", soundVolume);
        message.text = "";
        yield return UiAnim.Fade(group, 1f, fadeSeconds.x);
        yield return UiAnim.Typewriter(message, text, lettersPerSecond);
        yield return new WaitForSeconds(showSeconds);
        yield return UiAnim.Fade(group, 0f, fadeSeconds.y);
    }

    private void SetKeys(string[] keys)
    {
        foreach (Transform old in keyRow)
        {
            Destroy(old.gameObject);
        }
        foreach (string key in keys)
        {
            Image cap = UiFactory.CreatePanel(keyRow, "Key " + key, "Key");
            cap.rectTransform.sizeDelta = Vector2.one * KeySize;
            Text label = UiFactory.CreateText(cap.transform, "Label", UiFactory.Center, Vector2.one * KeySize, KeyFontSize, TextAnchor.MiddleCenter);
            label.rectTransform.pivot = UiFactory.Center;
            label.text = key;
        }
    }
}
