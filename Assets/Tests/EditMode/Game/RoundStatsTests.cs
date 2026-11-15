using NUnit.Framework;

public class RoundStatsTests : EditModeTest
{
    private RoundStats stats;
    private GameEvent openingFinished;
    private GameEvent playerCaught;
    private GameEvent levelWon;
    private GameEvent levelLost;

    [SetUp]
    public void SetUp()
    {
        openingFinished = Track(TestData.Event("OpeningFinished"));
        playerCaught = Track(TestData.Event("PlayerCaught"));
        levelWon = Track(TestData.Event("LevelWon"));
        levelLost = Track(TestData.Event("LevelLost"));

        stats = Make("Round").AddComponent<RoundStats>();
        TestUtils.Set(stats, "openingFinished", openingFinished);
        TestUtils.Set(stats, "playerCaught", playerCaught);
        TestUtils.Set(stats, "levelWon", levelWon);
        TestUtils.Set(stats, "levelLost", levelLost);
        TestUtils.Call(stats, "OnEnable");
    }

    private bool Counting => TestUtils.Get<bool>(stats, "counting");

    [Test]
    public void StartsWithNothingCounted()
    {
        Assert.AreEqual(0, stats.Captures);
        Assert.AreEqual(0f, stats.Seconds);
    }

    [TestCase(1)]
    [TestCase(3)]
    [TestCase(10)]
    public void CountsEveryCapture(int captures)
    {
        for (int i = 0; i < captures; i++)
        {
            playerCaught.Raise();
        }

        Assert.AreEqual(captures, stats.Captures);
    }

    [Test]
    public void TheCaptureThatLosesTheLevel_IsCounted()
    {
        playerCaught.Raise();
        levelLost.Raise();
        playerCaught.Raise();

        Assert.AreEqual(2, stats.Captures);
    }

    [Test]
    public void Clock_DoesNotRunBeforeTheOpeningEnds()
    {
        for (int i = 0; i < 10; i++)
        {
            TestUtils.Call(stats, "Update");
        }

        Assert.IsFalse(Counting);
        Assert.AreEqual(0f, stats.Seconds);
    }

    [Test]
    public void Clock_StartsWhenTheOpeningEnds()
    {
        openingFinished.Raise();

        Assert.IsTrue(Counting);
    }

    [Test]
    public void Clock_StopsWhenTheLevelIsWon()
    {
        openingFinished.Raise();
        levelWon.Raise();
        float seconds = stats.Seconds;

        TestUtils.Call(stats, "Update");

        Assert.IsFalse(Counting);
        Assert.AreEqual(seconds, stats.Seconds);
    }

    [Test]
    public void Clock_StopsWhenTheLevelIsLost()
    {
        openingFinished.Raise();
        levelLost.Raise();

        Assert.IsFalse(Counting);
    }

    [Test]
    public void OnDisable_StopsListening()
    {
        TestUtils.Call(stats, "OnDisable");

        playerCaught.Raise();
        openingFinished.Raise();

        Assert.AreEqual(0, stats.Captures);
        Assert.IsFalse(Counting);
    }
}
