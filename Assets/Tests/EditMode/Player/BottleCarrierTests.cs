using NUnit.Framework;

public class BottleCarrierTests : EditModeTest
{
    private BottleCarrier carrier;
    private GameEvent playerCaught;
    private EventSpy picked;
    private EventSpy dropped;

    [SetUp]
    public void SetUp()
    {
        playerCaught = Track(TestData.Event("PlayerCaught"));
        GameEvent bottlePicked = Track(TestData.Event("BottlePicked"));
        GameEvent bottleDropped = Track(TestData.Event("BottleDropped"));
        picked = new EventSpy(bottlePicked);
        dropped = new EventSpy(bottleDropped);

        carrier = Make("Player").AddComponent<BottleCarrier>();
        TestUtils.Set(carrier, "bottlePicked", bottlePicked);
        TestUtils.Set(carrier, "bottleDropped", bottleDropped);
        TestUtils.Set(carrier, "playerCaught", playerCaught);
        TestUtils.Call(carrier, "OnEnable");
    }

    [Test]
    public void StartsWithoutTheBottle()
    {
        Assert.IsFalse(carrier.HasBottle);
    }

    [Test]
    public void PickUp_SetsHasBottle_AndRaisesBottlePicked()
    {
        carrier.PickUp();

        Assert.IsTrue(carrier.HasBottle);
        Assert.AreEqual(1, picked.Count);
        Assert.AreEqual(0, dropped.Count);
    }

    [Test]
    public void CaughtWithTheBottle_DropsIt_AndRaisesBottleDropped()
    {
        carrier.PickUp();

        playerCaught.Raise();

        Assert.IsFalse(carrier.HasBottle);
        Assert.AreEqual(1, dropped.Count);
    }

    [Test]
    public void CaughtWithoutTheBottle_RaisesNothing()
    {
        playerCaught.Raise();

        Assert.IsFalse(carrier.HasBottle);
        Assert.AreEqual(0, dropped.Count);
    }

    [Test]
    public void CaughtTwice_DropsTheBottleOnlyOnce()
    {
        carrier.PickUp();

        playerCaught.Raise();
        playerCaught.Raise();

        Assert.AreEqual(1, dropped.Count);
    }

    // Regression: delivering the bottle used to look like losing it to the code that polled HasBottle.
    [Test]
    public void Deliver_GivesTheBottleAway_WithoutRaisingBottleDropped()
    {
        carrier.PickUp();

        carrier.Deliver();

        Assert.IsFalse(carrier.HasBottle);
        Assert.AreEqual(0, dropped.Count);
    }

    [Test]
    public void CaughtAfterDelivering_RaisesNothing()
    {
        carrier.PickUp();
        carrier.Deliver();

        playerCaught.Raise();

        Assert.AreEqual(0, dropped.Count);
    }

    [Test]
    public void PickUpAgainAfterDropping_Works()
    {
        carrier.PickUp();
        playerCaught.Raise();

        carrier.PickUp();

        Assert.IsTrue(carrier.HasBottle);
        Assert.AreEqual(2, picked.Count);
    }

    [Test]
    public void OnDisable_StopsListeningToPlayerCaught()
    {
        carrier.PickUp();
        TestUtils.Call(carrier, "OnDisable");

        playerCaught.Raise();

        Assert.IsTrue(carrier.HasBottle);
    }
}
