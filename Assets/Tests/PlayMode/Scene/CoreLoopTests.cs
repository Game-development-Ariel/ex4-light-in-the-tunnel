using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>The rules of the level, played in the real scene: being caught, winning and losing.</summary>
public class CoreLoopTests : TutorialSceneTest
{
    [UnityTest]
    public IEnumerator Caught_SendsThePlayerBackToTheStart()
    {
        yield return SkipTheOpening();
        BlindTheWatchers();
        Teleport(StartPoint.position + new Vector3(3f, -2f, 0f));

        PlayerCaught.Raise();
        yield return null;

        Assert.AreEqual(Flat(StartPoint.position).x, Player.transform.position.x, 0.01f);
        Assert.AreEqual(Flat(StartPoint.position).y, Player.transform.position.y, 0.01f);
    }

    [UnityTest]
    public IEnumerator Caught_LowersHopeByTheConfiguredAmount()
    {
        yield return SkipTheOpening();
        int before = Hope.Current;

        PlayerCaught.Raise();
        yield return null;

        Assert.AreEqual(Mathf.Max(0, before - Settings.hopeLostWhenCaught), Hope.Current);
        Assert.AreEqual(1, Stats.Captures);
    }

    [UnityTest]
    public IEnumerator Caught_FreezesThePlayerForTheConfiguredTime()
    {
        yield return SkipTheOpening();
        BlindTheWatchers();
        Time.timeScale = 4f; // The freeze is a few seconds long. Faster time keeps the test short.

        PlayerCaught.Raise();
        float caughtAt = Time.time;
        yield return null;
        Assert.IsFalse(Player.enabled, "Frozen right after the capture");

        while (Time.time < caughtAt + Settings.caughtFreezeSeconds - 0.2f)
        {
            Assert.IsFalse(Player.enabled, $"Free too early, {Time.time - caughtAt:0.00} s after the capture");
            yield return null;
        }
        yield return TestUtils.WaitGameSeconds(0.4f);
        Assert.IsTrue(Player.enabled, "Still frozen after caughtFreezeSeconds");
    }

    [UnityTest]
    public IEnumerator StandingInACamerasLight_GetsThePlayerCaught()
    {
        yield return SkipTheOpening();
        BlindTheWatchers();
        VisionSensor camera = Object.FindObjectsByType<VisionSensor>(FindObjectsSortMode.InstanceID)
            .Where(s => s.GetComponent<CameraSweep>() != null)
            .OrderBy(s => s.name)
            .First();
        camera.GetComponent<CameraSweep>().enabled = false; // Holds the camera still while the player stands in its light.
        float clear = camera.DistanceToWall(camera.transform.up);
        Assert.Greater(clear, 1f, $"{camera.name} looks straight at a wall");
        int before = Hope.Current;

        Teleport(camera.transform.position + camera.transform.up * Mathf.Min(1.5f, clear * 0.5f));
        camera.enabled = true;
        yield return TestUtils.WaitUntil(() => Hope.Current < before, 5f, $"{camera.name} to see the player");

        Assert.AreEqual(1, Stats.Captures);
        Assert.AreEqual(StartPoint.position.x, Player.transform.position.x, 0.01f);
    }

    [UnityTest]
    public IEnumerator TouchingTheBottle_ThenTheMattress_WinsTheLevel()
    {
        yield return SkipTheOpening();
        BlindTheWatchers();
        var won = new EventSpy(LevelWon);
        var lost = new EventSpy(LevelLost);

        Teleport(Spawner.Bottle.position);
        yield return TestUtils.WaitUntil(() => Carrier.HasBottle, 2f, "the player to pick up the bottle");
        Assert.IsTrue(Spawner.Bottle == null, "The picked bottle leaves the floor");

        Teleport(Goal.transform.position);
        yield return TestUtils.WaitUntil(() => won.Count > 0, 2f, "the level to be won at the mattress");
        yield return null;

        Assert.AreEqual(1, won.Count);
        Assert.AreEqual(0, lost.Count);
        Assert.IsFalse(Carrier.HasBottle, "The bottle was given away");
        Assert.IsFalse(Player.enabled, "The player stops the moment the level is won");
        won.Stop();
        lost.Stop();
    }

    [UnityTest]
    public IEnumerator AfterTheWin_TheLevelStopsAfterTheFade()
    {
        yield return SkipTheOpening();
        Carrier.PickUp();
        BottleDelivered.Raise();

        yield return TestUtils.WaitGameSeconds(Settings.endFadeSeconds + 0.2f);

        Assert.IsFalse(Player.gameObject.activeSelf, "LevelFreeze turns the player off after the fade");
        Assert.IsTrue(Object.FindObjectsByType<VisionSensor>(FindObjectsSortMode.None).All(s => !s.isActiveAndEnabled));
    }

    // Regression (cleanup): during the end screen's fade the player could still be caught and lose hope.
    [UnityTest]
    public IEnumerator BeingSeenAfterTheWin_ChangesNothing()
    {
        yield return SkipTheOpening();
        var lost = new EventSpy(LevelLost);
        Carrier.PickUp();
        BottleDelivered.Raise();
        yield return null;
        int hopeAtWin = Hope.Current;

        PlayerCaught.Raise();
        PlayerCaught.Raise();
        yield return null;

        Assert.AreEqual(hopeAtWin, Hope.Current);
        Assert.AreEqual(0, lost.Count);
        Assert.IsFalse(Player.enabled);
        Assert.IsTrue(Object.FindObjectsByType<VisionSensor>(FindObjectsSortMode.None).All(s => !s.enabled), "The watchers stop looking at the win");
        lost.Stop();
    }

    [UnityTest]
    public IEnumerator CaughtUntilNoHope_LosesTheLevel()
    {
        yield return SkipTheOpening();
        BlindTheWatchers();
        var lost = new EventSpy(LevelLost);
        int captures = Mathf.CeilToInt((float)Hope.Current / Settings.hopeLostWhenCaught);

        for (int i = 0; i < captures; i++)
        {
            Assert.AreEqual(0, lost.Count, "Lost before the hope ran out");
            PlayerCaught.Raise();
            yield return null;
        }

        Assert.AreEqual(1, lost.Count);
        Assert.AreEqual(0, Hope.Current);
        Assert.AreEqual(captures, Stats.Captures);
        Assert.IsFalse(Player.enabled);
        lost.Stop();
    }

    [UnityTest]
    public IEnumerator CaughtWithTheBottle_PutsANewBottleInTheStorageRoom()
    {
        yield return SkipTheOpening();
        BlindTheWatchers();
        Vector3 bottlePlace = Spawner.transform.position;
        Teleport(Spawner.Bottle.position);
        yield return TestUtils.WaitUntil(() => Carrier.HasBottle, 2f, "the player to pick up the bottle");

        PlayerCaught.Raise();
        yield return null;

        Assert.IsFalse(Carrier.HasBottle);
        Assert.IsNotNull(Spawner.Bottle, "A new bottle appears");
        Assert.Less(Vector2.Distance(bottlePlace, Spawner.Bottle.position), 0.1f, "At the bottle's place in the storage room (it bobs a little)");
    }

    [UnityTest]
    public IEnumerator RoundClock_RunsAfterTheOpening_AndStopsAtTheEnd()
    {
        yield return TestUtils.WaitGameSeconds(0.3f);
        Assert.AreEqual(0f, Stats.Seconds, "The clock waits for the opening");

        yield return SkipTheOpening();
        yield return TestUtils.WaitGameSeconds(0.5f);
        Assert.That(Stats.Seconds, Is.InRange(0.4f, 1f));

        Carrier.PickUp();
        BottleDelivered.Raise();
        float atTheEnd = Stats.Seconds;
        yield return TestUtils.WaitGameSeconds(0.3f);
        Assert.AreEqual(atTheEnd, Stats.Seconds, 1e-4f);
    }
}
