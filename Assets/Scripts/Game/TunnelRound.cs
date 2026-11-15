using UnityEngine;

/// <summary>
/// The rules of the level: being caught costs hope, no hope left loses, and bringing the water back wins.
/// It only raises events about the result. The end screen listens to them.
/// </summary>
public class TunnelRound : MonoBehaviour
{
    [SerializeField] private TunnelLevelSettings settings;
    [SerializeField] private Hope hope;

    [Header("Listens to")]
    [SerializeField] private GameEvent playerCaught;
    [SerializeField] private GameEvent bottleDelivered;

    [Header("Raises")]
    [SerializeField] private GameEvent levelWon;
    [SerializeField] private GameEvent levelLost;

    private bool ended;

    /// <summary>How many times the player was caught in this level.</summary>
    public int Captures { get; private set; }

    /// <summary>Seconds since the level started. Stops counting when the level ends.</summary>
    public float Seconds { get; private set; }

    private void OnEnable()
    {
        playerCaught.Subscribe(OnPlayerCaught);
        bottleDelivered.Subscribe(OnBottleDelivered);
    }

    private void OnDisable()
    {
        playerCaught.Unsubscribe(OnPlayerCaught);
        bottleDelivered.Unsubscribe(OnBottleDelivered);
    }

    private void Update()
    {
        if (!ended)
        {
            Seconds += Time.deltaTime;
        }
    }

    private void OnPlayerCaught()
    {
        if (ended)
        {
            return;
        }
        Captures++;
        hope.Lose(settings.hopeLostWhenCaught);
        if (hope.IsEmpty)
        {
            End(levelLost);
        }
    }

    private void OnBottleDelivered()
    {
        if (!ended)
        {
            End(levelWon);
        }
    }

    private void End(GameEvent result)
    {
        ended = true;
        result.Raise();
    }
}
