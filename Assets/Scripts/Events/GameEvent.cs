using System;
using UnityEngine;

/// <summary>
/// An event that is saved as an asset. One script raises it and other scripts listen to it,
/// and they only need to hold the same asset, so they don't have to know each other.
/// For example the security camera raises PlayerCaught, and the round rules, the respawn, the HUD and the sound all listen.
/// </summary>
[CreateAssetMenu(menuName = "Events/Game Event")]
public class GameEvent : ScriptableObject
{
    private event Action Raised;

    public void Raise()
    {
        Raised?.Invoke(); // ?. calls it only if someone listens.
    }

    public void Subscribe(Action listener)
    {
        Raised += listener;
    }

    public void Unsubscribe(Action listener)
    {
        Raised -= listener;
    }
}
