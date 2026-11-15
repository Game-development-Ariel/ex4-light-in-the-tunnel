using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>The real scene loads and plays for a few seconds. Any error or exception in the log fails the test.</summary>
public class SmokeTests : TutorialSceneTest
{
    [UnityTest]
    public IEnumerator Scene_RunsThreeSeconds_WithoutErrors()
    {
        yield return TestUtils.WaitGameSeconds(3f);

        LogAssert.NoUnexpectedReceived();
        Assert.IsTrue(Player.gameObject.activeInHierarchy);
    }

    [UnityTest]
    public IEnumerator Scene_PlaysThreeSecondsAfterTheOpening_WithoutErrors()
    {
        yield return SkipTheOpening();
        yield return TestUtils.WaitGameSeconds(3f);

        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator Scene_StartsWithTheBottleOnTheFloor_AndTheHopeFromTheSettings()
    {
        yield return null;

        Assert.IsNotNull(Spawner.Bottle, "BottleSpawner makes the first bottle in Start");
        Assert.IsFalse(Carrier.HasBottle);
        Assert.AreEqual(Mathf.Min(Settings.startHope, Settings.maxHope), Hope.Current);
        Assert.AreEqual(0, Stats.Captures);
    }

    // Regression (cleanup): the player could walk during the title opening.
    [UnityTest]
    public IEnumerator Player_CannotMove_DuringTheOpening()
    {
        yield return TestUtils.WaitGameSeconds(1f);
        Assert.IsFalse(Player.enabled, "The player moved before the eyes opened");

        yield return SkipTheOpening();

        Assert.IsTrue(Player.enabled);
    }

    [UnityTest]
    public IEnumerator Watchers_LookAround_WhileTheLevelIsPlayed()
    {
        CameraSweep sweep = Object.FindAnyObjectByType<CameraSweep>();
        Quaternion before = sweep.transform.rotation;

        yield return TestUtils.WaitGameSeconds(1f);

        Assert.Greater(Quaternion.Angle(before, sweep.transform.rotation), 0.1f);
    }
}
