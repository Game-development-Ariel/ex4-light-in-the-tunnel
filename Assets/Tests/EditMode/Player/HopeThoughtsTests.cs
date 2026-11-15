using System.Collections.Generic;
using NUnit.Framework;

public class HopeThoughtsTests : EditModeTest
{
    private Hope hope;
    private HopeThoughts thoughts;
    private EventSpy hints;

    private void MakeHero(int startHope)
    {
        TextEvent hintRequested = Track(TestData.TextEvent());
        hints = new EventSpy(hintRequested);

        var player = Make("Player");
        hope = player.AddComponent<Hope>();
        TestUtils.Set(hope, "settings", Track(TestData.Settings(100, startHope)));
        thoughts = player.AddComponent<HopeThoughts>();
        TestUtils.Set(thoughts, "effects", Track(TestData.HopeEffects())); // steady at 50, faith at 75.
        TestUtils.Set(thoughts, "hintRequested", hintRequested);

        TestUtils.Call(hope, "Awake");
        TestUtils.Call(thoughts, "Awake");
        TestUtils.Call(thoughts, "OnEnable");
        TestUtils.Call(thoughts, "Start");
    }

    private string Thought(string field)
    {
        return TestUtils.Get<string>(thoughts, field);
    }

    // start hope, change, the thought field that should be raised (null = no thought).
    private static IEnumerable<TestCaseData> Crossings()
    {
        yield return new TestCaseData(30, 20, "steadyThought").SetName("Up to exactly steady says steady");
        yield return new TestCaseData(30, 19, null).SetName("Up to just below steady says nothing");
        yield return new TestCaseData(49, 1, "steadyThought").SetName("From just below steady to steady says steady");
        yield return new TestCaseData(30, 45, "faithThought").SetName("Up past both levels says only faith");
        yield return new TestCaseData(60, 15, "faithThought").SetName("Up to exactly faith says faith");
        yield return new TestCaseData(60, 14, null).SetName("Up to just below faith says nothing");
        yield return new TestCaseData(74, 1, "faithThought").SetName("From just below faith to faith says faith");
        yield return new TestCaseData(60, -11, "weakThought").SetName("Down below steady says weak");
        yield return new TestCaseData(50, -1, "weakThought").SetName("From steady to just below says weak");
        yield return new TestCaseData(60, -10, null).SetName("Down to exactly steady says nothing");
        yield return new TestCaseData(80, -5, null).SetName("Down to exactly faith says nothing");
        yield return new TestCaseData(80, -40, "weakThought").SetName("Down past both levels says weak");
        yield return new TestCaseData(100, -100, "weakThought").SetName("Down to zero says weak");
        yield return new TestCaseData(30, -10, null).SetName("Down while already weak says nothing");
        yield return new TestCaseData(90, 5, null).SetName("Up while already faithful says nothing");
        yield return new TestCaseData(30, 0, null).SetName("No change says nothing");
    }

    [TestCaseSource(nameof(Crossings))]
    public void ThoughtWhenHopeCrossesALevel(int startHope, int change, string expectedField)
    {
        MakeHero(startHope);

        hope.Add(change);

        if (expectedField == null)
        {
            Assert.AreEqual(0, hints.Count, "No thought expected, got: " + hints.Last);
        }
        else
        {
            Assert.AreEqual(1, hints.Count);
            Assert.AreEqual(Thought(expectedField), hints.Last);
        }
    }

    [Test]
    public void RemembersTheLastHope_BetweenChanges()
    {
        MakeHero(30);

        hope.Add(20);
        hope.Lose(10);
        hope.Add(10);

        CollectionAssert.AreEqual(new[] { Thought("steadyThought"), Thought("weakThought"), Thought("steadyThought") }, hints.Texts);
    }

    [Test]
    public void OnDisable_StopsListening()
    {
        MakeHero(30);
        TestUtils.Call(thoughts, "OnDisable");

        hope.Add(50);

        Assert.AreEqual(0, hints.Count);
    }
}
