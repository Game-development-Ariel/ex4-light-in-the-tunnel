using System.Collections.Generic;

/// <summary>Listens to a GameEvent or a TextEvent and remembers every time it was raised.</summary>
public sealed class EventSpy
{
    private readonly GameEvent gameEvent;
    private readonly TextEvent textEvent;

    public int Count { get; private set; }
    public List<string> Texts { get; } = new List<string>();
    public string Last => Texts.Count > 0 ? Texts[Texts.Count - 1] : null;

    public EventSpy(GameEvent watched)
    {
        gameEvent = watched;
        gameEvent.Subscribe(OnRaised);
    }

    public EventSpy(TextEvent watched)
    {
        textEvent = watched;
        textEvent.Subscribe(OnText);
    }

    public void Stop()
    {
        gameEvent?.Unsubscribe(OnRaised);
        textEvent?.Unsubscribe(OnText);
    }

    private void OnRaised()
    {
        Count++;
    }

    private void OnText(string text)
    {
        Count++;
        Texts.Add(text);
    }
}
