using UnityEngine;

/// <summary>
/// Counts what happened in the level, for the end screen: how many times the player was caught, and how long the
/// level took. The time starts when the opening ends and stops when the level ends.
/// </summary>
public class RoundStats : MonoBehaviour
{
    [Header("Listens to")]
    [SerializeField] private GameEvent openingFinished;
    [SerializeField] private GameEvent playerCaught;
    [SerializeField] private GameEvent levelWon;
    [SerializeField] private GameEvent levelLost;

    private bool counting;

    /// <summary>How many times the player was caught in this level.</summary>
    public int Captures { get; private set; }

    /// <summary>Seconds of play, from the end of the opening to the end of the level.</summary>
    public float Seconds { get; private set; }

    private void OnEnable()
    {
        openingFinished.Subscribe(StartCounting);
        playerCaught.Subscribe(CountCapture);
        levelWon.Subscribe(StopCounting);
        levelLost.Subscribe(StopCounting);
    }

    private void OnDisable()
    {
        openingFinished.Unsubscribe(StartCounting);
        playerCaught.Unsubscribe(CountCapture);
        levelWon.Unsubscribe(StopCounting);
        levelLost.Unsubscribe(StopCounting);
    }

    private void Update()
    {
        if (counting)
        {
            Seconds += Time.deltaTime;
        }
    }

    private void StartCounting()
    {
        counting = true;
    }

    // Not stopped by the end of the level: the capture that loses the level counts too.
    // After the end LevelFreeze turns the watchers off, so no capture comes later.
    private void CountCapture()
    {
        Captures++;
    }

    private void StopCounting()
    {
        counting = false;
    }
}
