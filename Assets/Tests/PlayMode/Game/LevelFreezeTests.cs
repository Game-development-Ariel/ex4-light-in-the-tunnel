using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>LevelFreeze decides when the level is played: not in the opening, not right after a capture, not after the end.</summary>
public class LevelFreezeTests : PlayModeTest
{
    private const float FreezeSeconds = 0.3f;
    private const float FadeSeconds = 0.2f;

    private PlayerMovement player;
    private VisionSensor watcher;
    private GameObject sounds;
    private GameEvent openingFinished;
    private GameEvent playerCaught;
    private GameEvent levelWon;
    private GameEvent levelLost;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        openingFinished = Track(TestData.Event());
        playerCaught = Track(TestData.Event());
        levelWon = Track(TestData.Event());
        levelLost = Track(TestData.Event());

        GameObject hero = MakeOff("Player");
        player = TestHero.Create(hero);
        hero.SetActive(true);

        GameObject watchers = Make("Watchers");
        GameObject camera = Make("Camera");
        camera.transform.SetParent(watchers.transform);
        watcher = camera.AddComponent<VisionSensor>();
        sounds = Make("Sound");

        GameObject round = MakeOff("Round");
        LevelFreeze freeze = round.AddComponent<LevelFreeze>();
        TestUtils.Set(freeze, "settings", Track(TestData.Settings(caughtFreezeSeconds: FreezeSeconds, endFadeSeconds: FadeSeconds)));
        TestUtils.Set(freeze, "player", player);
        TestUtils.Set(freeze, "watchers", watchers.transform);
        TestUtils.Set(freeze, "stopWhenLevelEnds", new[] { hero, watchers, sounds });
        TestUtils.Set(freeze, "openingFinished", openingFinished);
        TestUtils.Set(freeze, "playerCaught", playerCaught);
        TestUtils.Set(freeze, "levelWon", levelWon);
        TestUtils.Set(freeze, "levelLost", levelLost);
        round.SetActive(true);
        yield return null;
    }

    [UnityTest]
    public IEnumerator ThePlayerIsHeld_UntilTheOpeningEnds()
    {
        yield return TestUtils.WaitFrames(10);
        Assert.IsFalse(player.enabled);

        openingFinished.Raise();
        yield return null;

        Assert.IsTrue(player.enabled);
    }

    [UnityTest]
    public IEnumerator Caught_HoldsThePlayer_ForCaughtFreezeSeconds()
    {
        openingFinished.Raise();
        yield return null;

        playerCaught.Raise();
        float caughtAt = Time.time;
        yield return null;
        Assert.IsFalse(player.enabled);

        yield return TestUtils.WaitUntil(() => player.enabled, 2f, "the player to be free again");
        Assert.That(Time.time - caughtAt, Is.InRange(FreezeSeconds - 0.02f, FreezeSeconds + 0.1f));
    }

    [UnityTest]
    public IEnumerator CaughtDuringTheOpening_StillWaitsForTheOpening()
    {
        playerCaught.Raise();
        yield return TestUtils.WaitGameSeconds(FreezeSeconds + 0.1f);

        Assert.IsFalse(player.enabled);
    }

    [UnityTest]
    public IEnumerator SecondCapture_StartsTheFreezeOver()
    {
        openingFinished.Raise();
        playerCaught.Raise();
        yield return TestUtils.WaitGameSeconds(FreezeSeconds * 0.7f);

        playerCaught.Raise();
        yield return TestUtils.WaitGameSeconds(FreezeSeconds * 0.7f);

        Assert.IsFalse(player.enabled, "The second capture holds the player its own full time");
    }

    [UnityTest]
    public IEnumerator Win_StopsThePlayerAndTheWatchers_AtOnce()
    {
        openingFinished.Raise();
        yield return null;

        levelWon.Raise();

        Assert.IsFalse(player.enabled);
        Assert.IsFalse(watcher.enabled);
        yield return null;
        Assert.IsFalse(player.enabled, "Update doesn't let the player go after the end");
    }

    [UnityTest]
    public IEnumerator Loss_StopsThePlayerAndTheWatchers_AtOnce()
    {
        openingFinished.Raise();
        yield return null;

        levelLost.Raise();

        Assert.IsFalse(player.enabled);
        Assert.IsFalse(watcher.enabled);
    }

    [UnityTest]
    public IEnumerator LevelEnd_TurnsTheLevelOff_AfterTheFade()
    {
        openingFinished.Raise();
        levelWon.Raise();
        yield return null;
        Assert.IsTrue(sounds.activeSelf, "Still on during the fade");

        yield return TestUtils.WaitGameSeconds(FadeSeconds + 0.1f);

        Assert.IsFalse(sounds.activeSelf);
        Assert.IsFalse(player.gameObject.activeSelf);
        Assert.IsFalse(watcher.gameObject.activeInHierarchy);
    }

    [UnityTest]
    public IEnumerator OpeningAfterTheEnd_DoesNotFreeThePlayer()
    {
        openingFinished.Raise();
        levelLost.Raise();

        openingFinished.Raise();
        yield return null;

        Assert.IsFalse(player.enabled);
    }
}
