using NUnit.Framework;

public class TextEventTests : EditModeTest
{
    private TextEvent textEvent;

    [SetUp]
    public void SetUp()
    {
        textEvent = Track(TestData.TextEvent());
    }

    [TestCase("Too loud.")]
    [TestCase("")]
    [TestCase("Two\nlines")]
    public void Raise_GivesTheTextToTheSubscriber(string text)
    {
        string received = null;
        textEvent.Subscribe(t => received = t);

        textEvent.Raise(text);

        Assert.AreEqual(text, received);
    }

    [Test]
    public void Raise_CallsEverySubscriber()
    {
        var first = new EventSpy(textEvent);
        var second = new EventSpy(textEvent);

        textEvent.Raise("hint");

        Assert.AreEqual("hint", first.Last);
        Assert.AreEqual("hint", second.Last);
    }

    [Test]
    public void Unsubscribe_StopsTheSubscriber()
    {
        int calls = 0;
        void Listener(string text)
        {
            calls++;
        }
        textEvent.Subscribe(Listener);
        textEvent.Unsubscribe(Listener);

        textEvent.Raise("hint");

        Assert.AreEqual(0, calls);
    }

    [Test]
    public void Raise_WithNoSubscribers_IsSafe()
    {
        Assert.DoesNotThrow(() => textEvent.Raise("nobody listens"));
    }

    [Test]
    public void Raise_WithNullText_IsSafe()
    {
        var spy = new EventSpy(textEvent);
        Assert.DoesNotThrow(() => textEvent.Raise(null));
        Assert.AreEqual(1, spy.Count);
    }
}
