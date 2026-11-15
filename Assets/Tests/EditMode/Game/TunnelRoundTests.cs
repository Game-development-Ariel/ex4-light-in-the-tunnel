using NUnit.Framework;

public class TunnelRoundTests : EditModeTest
{
    private Hope hope;
    private TunnelRound round;
    private GameEvent playerCaught;
    private GameEvent bottleDelivered;
    private EventSpy won;
    private EventSpy lost;

    private void MakeRound(int startHope = 30, int hopeLostWhenCaught = 20)
    {
        TunnelLevelSettings settings = Track(TestData.Settings(100, startHope, hopeLostWhenCaught));
        hope = Make("Player").AddComponent<Hope>();
        TestUtils.Set(hope, "settings", settings);
        TestUtils.Call(hope, "Awake");

        playerCaught = Track(TestData.Event("PlayerCaught"));
        bottleDelivered = Track(TestData.Event("BottleDelivered"));
        GameEvent levelWon = Track(TestData.Event("LevelWon"));
        GameEvent levelLost = Track(TestData.Event("LevelLost"));
        won = new EventSpy(levelWon);
        lost = new EventSpy(levelLost);

        round = Make("Round").AddComponent<TunnelRound>();
        TestUtils.Set(round, "settings", settings);
        TestUtils.Set(round, "hope", hope);
        TestUtils.Set(round, "playerCaught", playerCaught);
        TestUtils.Set(round, "bottleDelivered", bottleDelivered);
        TestUtils.Set(round, "levelWon", levelWon);
        TestUtils.Set(round, "levelLost", levelLost);
        TestUtils.Call(round, "OnEnable");
    }

    [Test]
    public void Caught_CostsHopeLostWhenCaught()
    {
        MakeRound(30, 20);

        playerCaught.Raise();

        Assert.AreEqual(10, hope.Current);
    }

    [Test]
    public void Caught_WithHopeLeft_DoesNotEndTheLevel()
    {
        MakeRound(30, 20);

        playerCaught.Raise();

        Assert.AreEqual(0, lost.Count);
        Assert.AreEqual(0, won.Count);
    }

    [TestCase(30, 20, 2)]
    [TestCase(30, 10, 3)]
    [TestCase(30, 30, 1)]
    [TestCase(30, 29, 2)]
    [TestCase(100, 20, 5)]
    [TestCase(1, 20, 1)]
    public void CaughtUntilHopeIsZero_LosesOnTheRightCapture(int startHope, int hopeLost, int capturesToLose)
    {
        MakeRound(startHope, hopeLost);

        for (int i = 1; i < capturesToLose; i++)
        {
            playerCaught.Raise();
        }
        Assert.AreEqual(0, lost.Count, "Lost too early");

        playerCaught.Raise();
        Assert.AreEqual(1, lost.Count);
        Assert.AreEqual(0, hope.Current);
    }

    [Test]
    public void LevelLost_IsRaisedOnlyOnce()
    {
        MakeRound(30, 20);

        for (int i = 0; i < 5; i++)
        {
            playerCaught.Raise();
        }

        Assert.AreEqual(1, lost.Count);
    }

    [Test]
    public void HopeLostZero_NeverLoses()
    {
        MakeRound(30, 0);

        for (int i = 0; i < 10; i++)
        {
            playerCaught.Raise();
        }

        Assert.AreEqual(30, hope.Current);
        Assert.AreEqual(0, lost.Count);
    }

    [Test]
    public void BottleDelivered_RaisesLevelWonOnce()
    {
        MakeRound();

        bottleDelivered.Raise();
        bottleDelivered.Raise();

        Assert.AreEqual(1, won.Count);
        Assert.AreEqual(0, lost.Count);
    }

    // Regression (cleanup): after the win the watchers still saw the player, and each capture took hope
    // and could even lose the level that was already won.
    [Test]
    public void CaughtAfterWin_ChangesNothing()
    {
        MakeRound(30, 20);
        bottleDelivered.Raise();

        for (int i = 0; i < 5; i++)
        {
            playerCaught.Raise();
        }

        Assert.AreEqual(30, hope.Current);
        Assert.AreEqual(0, lost.Count);
        Assert.AreEqual(1, won.Count);
    }

    [Test]
    public void DeliveredAfterLoss_DoesNotWin()
    {
        MakeRound(30, 30);
        playerCaught.Raise();

        bottleDelivered.Raise();

        Assert.AreEqual(1, lost.Count);
        Assert.AreEqual(0, won.Count);
    }

    [Test]
    public void HopeGainedBetweenCaptures_DelaysTheLoss()
    {
        MakeRound(30, 20);
        playerCaught.Raise();
        hope.Add(15);

        playerCaught.Raise();

        Assert.AreEqual(5, hope.Current);
        Assert.AreEqual(0, lost.Count);
    }

    [Test]
    public void OnDisable_StopsListening()
    {
        MakeRound();
        TestUtils.Call(round, "OnDisable");

        playerCaught.Raise();
        bottleDelivered.Raise();

        Assert.AreEqual(30, hope.Current);
        Assert.AreEqual(0, won.Count);
    }
}
