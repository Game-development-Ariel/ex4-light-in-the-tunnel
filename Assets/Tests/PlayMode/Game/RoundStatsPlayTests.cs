using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class RoundStatsPlayTests : PlayModeTest
{
    private RoundStats stats;
    private GameEvent openingFinished;
    private GameEvent levelWon;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        openingFinished = Track(TestData.Event());
        levelWon = Track(TestData.Event());
        GameObject round = MakeOff("Round");
        stats = round.AddComponent<RoundStats>();
        TestUtils.Set(stats, "openingFinished", openingFinished);
        TestUtils.Set(stats, "playerCaught", Track(TestData.Event()));
        TestUtils.Set(stats, "levelWon", levelWon);
        TestUtils.Set(stats, "levelLost", Track(TestData.Event()));
        round.SetActive(true);
        Time.captureFramerate = 50;
        yield return null;
    }

    [UnityTest]
    public IEnumerator Seconds_CountThePlayTime()
    {
        openingFinished.Raise();
        yield return TestUtils.WaitFrames(50);

        Assert.AreEqual(1f, stats.Seconds, 0.03f);
    }

    [UnityTest]
    public IEnumerator Seconds_StopAtTheWin()
    {
        openingFinished.Raise();
        yield return TestUtils.WaitFrames(25);
        levelWon.Raise();
        float atWin = stats.Seconds;

        yield return TestUtils.WaitFrames(25);

        Assert.AreEqual(atWin, stats.Seconds);
        Assert.AreEqual(0.5f, atWin, 0.03f);
    }
}
