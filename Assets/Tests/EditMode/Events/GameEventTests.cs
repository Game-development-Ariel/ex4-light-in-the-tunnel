using NUnit.Framework;

public class GameEventTests : EditModeTest
{
    private GameEvent gameEvent;

    [SetUp]
    public void SetUp()
    {
        gameEvent = Track(TestData.Event());
    }

    [Test]
    public void Raise_CallsEverySubscriber()
    {
        int first = 0;
        int second = 0;
        gameEvent.Subscribe(() => first++);
        gameEvent.Subscribe(() => second++);

        gameEvent.Raise();

        Assert.AreEqual(1, first);
        Assert.AreEqual(1, second);
    }

    [Test]
    public void Raise_TwiceCallsTheSubscriberTwice()
    {
        int calls = 0;
        gameEvent.Subscribe(() => calls++);

        gameEvent.Raise();
        gameEvent.Raise();

        Assert.AreEqual(2, calls);
    }

    [Test]
    public void Unsubscribe_StopsTheSubscriber()
    {
        int calls = 0;
        void Listener()
        {
            calls++;
        }
        gameEvent.Subscribe(Listener);
        gameEvent.Unsubscribe(Listener);

        gameEvent.Raise();

        Assert.AreEqual(0, calls);
    }

    [Test]
    public void Unsubscribe_LeavesTheOtherSubscribers()
    {
        int kept = 0;
        int removed = 0;
        void Removed()
        {
            removed++;
        }
        gameEvent.Subscribe(() => kept++);
        gameEvent.Subscribe(Removed);
        gameEvent.Unsubscribe(Removed);

        gameEvent.Raise();

        Assert.AreEqual(1, kept);
        Assert.AreEqual(0, removed);
    }

    [Test]
    public void Raise_WithNoSubscribers_IsSafe()
    {
        Assert.DoesNotThrow(() => gameEvent.Raise());
    }

    [Test]
    public void Unsubscribe_ThatWasNeverSubscribed_IsSafe()
    {
        Assert.DoesNotThrow(() => gameEvent.Unsubscribe(() => { }));
        Assert.DoesNotThrow(() => gameEvent.Raise());
    }

    [Test]
    public void EventSpy_CountsRaises()
    {
        var spy = new EventSpy(gameEvent);
        gameEvent.Raise();
        gameEvent.Raise();
        spy.Stop();
        gameEvent.Raise();

        Assert.AreEqual(2, spy.Count);
    }
}
