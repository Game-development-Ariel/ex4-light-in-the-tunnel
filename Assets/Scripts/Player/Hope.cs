using System;
using UnityEngine;

/// <summary>
/// The hero's hope, which is the "health" of this game. Memories add to it and being caught takes from it.
/// </summary>
public class Hope : MonoBehaviour
{
    [SerializeField] private TunnelLevelSettings settings;

    public int Current { get; private set; }
    public int Max => settings.maxHope;
    public bool IsEmpty => Current <= 0;

    /// <summary>Happens every time the hope changes. Gives the new value.</summary>
    public event Action<int> Changed;

    private void Awake()
    {
        Current = Mathf.Min(settings.startHope, settings.maxHope);
    }

    public void Add(int amount)
    {
        Set(Current + amount);
    }

    public void Lose(int amount)
    {
        Set(Current - amount);
    }

    private void Set(int value)
    {
        Current = Mathf.Clamp(value, 0, settings.maxHope);
        Changed?.Invoke(Current);
    }
}
