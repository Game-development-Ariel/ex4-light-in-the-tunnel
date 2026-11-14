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

    [SerializeField] private TextEvent memoryFound;
    [SerializeField] private float showSeconds = 5f;

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

        text = UiFactory.CreateTitle(canvas.transform, "Memory", new Vector2(0.5f, 0.5f), new Vector2(1300f, 200f), 46, UiTheme.Hope);
        text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        text.rectTransform.anchoredPosition = new Vector2(0f, 170f);
        text.font = UiTheme.ItalicFont;
    }

    private void OnEnable()
    {
        memoryFound.Subscribe(OnMemoryFound);
    }

    private void OnDisable()
    {
        memoryFound.Unsubscribe(OnMemoryFound);
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
            Sfx.Play("Memory", 0.9f);
            yield return UiAnim.Fade(group, 1f, 1.5f);
            yield return new WaitForSeconds(showSeconds);
            yield return UiAnim.Fade(group, 0f, 2f);
        }
        isShowing = false;
    }
}
