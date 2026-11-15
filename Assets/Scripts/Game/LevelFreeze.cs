using UnityEngine;

/// <summary>
/// Decides when the level is played: the player can't move during the opening or for a moment after being caught,
/// and when the level ends the player and the watchers stop at once, and the level stops after the fade to black.
/// This is game logic, so the UI only shows these moments and never turns the player on or off itself.
/// </summary>
public class LevelFreeze : MonoBehaviour
{
    [SerializeField] private TunnelLevelSettings settings;
    [SerializeField] private PlayerMovement player;

    [Tooltip("The parent of everything that can see the player. Its VisionSensors stop looking when the level ends")]
    [SerializeField] private Transform watchers;

    [Tooltip("Turned off when the screen is black after the level ends: the player, the watchers, the sounds")]
    [SerializeField] private GameObject[] stopWhenLevelEnds;

    [Header("Listens to")]
    [SerializeField] private GameEvent openingFinished;
    [SerializeField] private GameEvent playerCaught;
    [SerializeField] private GameEvent levelWon;
    [SerializeField] private GameEvent levelLost;

    private bool opened;
    private bool ended;
    private float freeAt;

    private void Awake()
    {
        player.enabled = false; // Until the opening ends.
    }

    private void OnEnable()
    {
        openingFinished.Subscribe(OnOpeningFinished);
        playerCaught.Subscribe(OnPlayerCaught);
        levelWon.Subscribe(OnLevelEnded);
        levelLost.Subscribe(OnLevelEnded);
    }

    private void OnDisable()
    {
        openingFinished.Unsubscribe(OnOpeningFinished);
        playerCaught.Unsubscribe(OnPlayerCaught);
        levelWon.Unsubscribe(OnLevelEnded);
        levelLost.Unsubscribe(OnLevelEnded);
    }

    // The player can move only when nothing holds him. Setting enabled to the value it already has does nothing.
    private void Update()
    {
        player.enabled = opened && !ended && Time.time >= freeAt;
    }

    private void OnOpeningFinished()
    {
        opened = true;
    }

    private void OnPlayerCaught()
    {
        freeAt = Time.time + settings.caughtFreezeSeconds;
    }

    private void OnLevelEnded()
    {
        ended = true;
        player.enabled = false;
        foreach (VisionSensor sensor in watchers.GetComponentsInChildren<VisionSensor>())
        {
            sensor.enabled = false;
        }
        Invoke(nameof(StopLevel), settings.endFadeSeconds);
    }

    private void StopLevel()
    {
        foreach (GameObject levelObject in stopWhenLevelEnds)
        {
            levelObject.SetActive(false);
        }
    }
}
