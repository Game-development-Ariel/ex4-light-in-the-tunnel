using System.Collections;
using UnityEngine;

/// <summary>
/// Small animations for the UI, written as coroutines: start one with StartCoroutine(UiAnim.Fade(...)).
/// Each one changes a value a little every frame until the time is up.
/// They all count real (unscaled) time, so they keep the same speed even if the game is slowed or paused.
/// </summary>
public static class UiAnim
{
    // A short stop after punctuation, in seconds, gives the typed words a human rhythm.
    private const float PunctuationPause = 0.25f;

    /// <summary>Fades a whole group of UI objects (alpha 0 = invisible, 1 = visible).</summary>
    public static IEnumerator Fade(CanvasGroup group, float to, float seconds)
    {
        float from = group.alpha;
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            group.alpha = Mathf.Lerp(from, to, t / seconds);
            yield return null;
        }
        group.alpha = to;
    }

    /// <summary>A quick bump in size and back, to draw the eye to something that changed.</summary>
    public static IEnumerator Punch(Transform target, float amount, float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            // Sin goes up and back down to 0 over half a turn.
            target.localScale = Vector3.one * (1f + Mathf.Sin(t / seconds * Mathf.PI) * amount);
            yield return null;
        }
        target.localScale = Vector3.one;
    }

    /// <summary>
    /// Writes the text one letter at a time, like someone typing or thinking it.
    /// A short stop after punctuation gives the words a human rhythm.
    /// </summary>
    public static IEnumerator Typewriter(UnityEngine.UI.Text target, string text, float lettersPerSecond)
    {
        target.text = "";
        for (int i = 0; i < text.Length; i++)
        {
            target.text = text.Substring(0, i + 1);
            char letter = text[i];
            bool pause = letter == '.' || letter == ',' || letter == '\n';
            yield return new WaitForSecondsRealtime(pause ? PunctuationPause : 1f / lettersPerSecond);
        }
    }

    /// <summary>Fades one UI element (text or image) to an alpha.</summary>
    public static IEnumerator FadeGraphic(UnityEngine.UI.Graphic target, float to, float seconds)
    {
        Color color = target.color;
        float from = color.a;
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            color.a = Mathf.Lerp(from, to, t / seconds);
            target.color = color;
            yield return null;
        }
        color.a = to;
        target.color = color;
    }
}
