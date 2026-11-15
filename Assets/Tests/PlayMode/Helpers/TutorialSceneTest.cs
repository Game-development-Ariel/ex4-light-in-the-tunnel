using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// The base of the tests that play the real Tutorial scene. Each test loads it fresh, and the long title
/// opening is skipped by raising OpeningFinished, which is what the TitleCard does when the eyes open.
/// </summary>
public abstract class TutorialSceneTest
{
    protected const string SceneName = "Tutorial";

    private float volumeBefore;

    protected PlayerMovement Player { get; private set; }
    protected Hope Hope { get; private set; }
    protected BottleCarrier Carrier { get; private set; }
    protected TunnelRound Round { get; private set; }
    protected RoundStats Stats { get; private set; }
    protected LevelFreeze Freeze { get; private set; }
    protected BottleSpawner Spawner { get; private set; }
    protected MattressGoal Goal { get; private set; }
    protected EndScreen End { get; private set; }
    protected TunnelLevelSettings Settings { get; private set; }
    protected Transform StartPoint { get; private set; }

    protected GameEvent PlayerCaught { get; private set; }
    protected GameEvent OpeningFinished { get; private set; }
    protected GameEvent BottleDelivered { get; private set; }
    protected GameEvent LevelWon { get; private set; }
    protected GameEvent LevelLost { get; private set; }

    [UnitySetUp]
    public IEnumerator LoadTheScene()
    {
        Time.timeScale = 1f;
        Time.captureFramerate = 60; // The same frame length every run, as in PlayModeTest.
        volumeBefore = AudioListener.volume;
        AudioListener.volume = 0f; // Silent: the music and the steps don't play through the speakers.
        yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
        yield return null;
        FindTheParts();
    }

    [UnityTearDown]
    public IEnumerator UnloadTheScene()
    {
        Time.timeScale = 1f;
        Time.captureFramerate = 0;
        AudioListener.volume = volumeBefore;
        // An empty scene for the next test, so its objects don't land in the Tutorial scene.
        Scene empty = SceneManager.CreateScene("Empty Test Scene " + Time.frameCount);
        SceneManager.SetActiveScene(empty);
        foreach (Scene scene in Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Where(s => s.name == SceneName).ToList())
        {
            yield return SceneManager.UnloadSceneAsync(scene);
        }
    }

    protected void FindTheParts()
    {
        Player = Object.FindAnyObjectByType<PlayerMovement>(FindObjectsInactive.Include);
        Assert.IsNotNull(Player, "The scene has no player");
        Hope = Player.GetComponent<Hope>();
        Carrier = Player.GetComponent<BottleCarrier>();
        Round = Object.FindAnyObjectByType<TunnelRound>();
        Stats = Object.FindAnyObjectByType<RoundStats>();
        Freeze = Object.FindAnyObjectByType<LevelFreeze>();
        Spawner = Object.FindAnyObjectByType<BottleSpawner>();
        Goal = Object.FindAnyObjectByType<MattressGoal>();
        End = Object.FindAnyObjectByType<EndScreen>();
        Settings = TestUtils.Get<TunnelLevelSettings>(Round, "settings");
        StartPoint = TestUtils.Get<Transform>(Player.GetComponent<PlayerRespawn>(), "startPoint");

        PlayerCaught = TestUtils.Get<GameEvent>(Round, "playerCaught");
        BottleDelivered = TestUtils.Get<GameEvent>(Round, "bottleDelivered");
        LevelWon = TestUtils.Get<GameEvent>(Round, "levelWon");
        LevelLost = TestUtils.Get<GameEvent>(Round, "levelLost");
        OpeningFinished = TestUtils.Get<GameEvent>(Freeze, "openingFinished");
    }

    /// <summary>Lets the player move now, as if the title opening had ended.</summary>
    protected IEnumerator SkipTheOpening()
    {
        OpeningFinished.Raise();
        yield return null;
        yield return null;
    }

    /// <summary>Turns off every camera and the guard, for tests that move the player around the map.</summary>
    protected static void BlindTheWatchers()
    {
        foreach (VisionSensor sensor in Object.FindObjectsByType<VisionSensor>(FindObjectsSortMode.None))
        {
            sensor.enabled = false;
        }
    }

    /// <summary>Puts the player somewhere at once. The controller is off for the jump, like PlayerRespawn does.</summary>
    protected void Teleport(Vector3 position)
    {
        var controller = Player.GetComponent<CharacterController>();
        controller.enabled = false;
        Player.transform.position = new Vector3(position.x, position.y, Player.transform.position.z);
        controller.enabled = true;
        Physics.SyncTransforms();
    }

    protected static Vector2 Flat(Vector3 position)
    {
        return new Vector2(position.x, position.y);
    }
}
