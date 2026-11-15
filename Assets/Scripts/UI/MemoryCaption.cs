using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Writes a found memory in the middle of the screen, with a few soft piano notes.
/// Two memories found close together wait in a queue and are shown one after the other.
/// </summary>
public class MemoryCaption : MonoBehaviour
{
    private const int CanvasSortingOrder = 30;

    // The memory is written in the upper middle of the screen, in UI pixels.
    private static readonly Vector2 TextSize = new Vector2(1300f, 200f);
    private static readonly Vector2 TextOffset = new Vector2(0f, 170f);
    private const int FontSize = 46;

    [SerializeField] private TextEvent memoryFound;
    [Tooltip("Seconds a memory stays after it has appeared")]
    [SerializeField] private float showSeconds = 5f;

    [Tooltip("How loud the sound of a memory is")]
    [SerializeField] private float soundVolume = 0.9f;

    [Tooltip("Seconds a memory takes to appear, and to fade away")]
    [SerializeField] private Vector2 fadeSeconds = new Vector2(1.5f, 2f);

    private readonly Queue<string> waiting = new Queue<string>();
    private CanvasGroup group;
    private Text text;
    private bool isShowing;

    private void Awake()
    {
        Canvas canvas = UiFactory.CreateCanvas("Memory", CanvasSortingOrder);
        canvas.transform.SetParent(transform, false);
        group = canvas.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;

        text = UiFactory.CreateTitle(canvas.transform, "Memory", UiFactory.Center, TextSize, FontSize, UiTheme.Hope);
        text.rectTransform.pivot = UiFactory.Center;
        text.rectTransform.anchoredPosition = TextOffset;
        text.font = UiTheme.ItalicFont;
    }

    private void OnEnable()
    {
        memoryFound.Subscribe(OnMemoryFound);
    }

    // Turning the object off stops its coroutines, so the next memory must start a new one.
    private void OnDisable()
    {
        memoryFound.Unsubscribe(OnMemoryFound);
        isShowing = false;
    }

    private void OnMemoryFound(string memory)
    {
        waiting.Enqueue(memory);
        if (!isShowing)
        {
            StartCoroutine(ShowWaiting());
        }
    }

    private IEnumerator ShowWaiting()
    {
        isShowing = true;
        while (waiting.Count > 0)
        {
            text.text = waiting.Dequeue();
            Sfx.Play("Memory", soundVolume);
            yield return UiAnim.Fade(group, 1f, fadeSeconds.x);
            yield return new WaitForSeconds(showSeconds);
            yield return UiAnim.Fade(group, 0f, fadeSeconds.y);
        }
        isShowing = false;
    }
}
