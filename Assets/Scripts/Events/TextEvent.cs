using System;
using UnityEngine;

/// <summary>
/// Like GameEvent, but it also carries a text, for example a hint that the UI should show.
/// </summary>
[CreateAssetMenu(menuName = "Events/Text Event")]
public class TextEvent : ScriptableObject
{
    private event Action<string> Raised;

    public void Raise(string text)
    {
        Raised?.Invoke(text);
    }

    public void Subscribe(Action<string> listener)
    {
        Raised += listener;
    }

    public void Unsubscribe(Action<string> listener)
    {
        Raised -= listener;
    }
}
