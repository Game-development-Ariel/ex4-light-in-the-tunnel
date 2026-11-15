using UnityEngine;

/// <summary>A pushable thing for the tests: it only counts how many pushes started, and how fast.</summary>
public class PushableSpy : MonoBehaviour, IPushable
{
    public int Pushes { get; private set; }
    public float LastSpeed { get; private set; }

    public void PushStarted(float playerSpeed)
    {
        Pushes++;
        LastSpeed = playerSpeed;
    }
}
