using NUnit.Framework;

public class HopeTests : EditModeTest
{
    private Hope MakeHope(int startHope, int maxHope = 100)
    {
        Hope hope = Make("Player").AddComponent<Hope>();
        TestUtils.Set(hope, "settings", Track(TestData.Settings(maxHope, startHope)));
        TestUtils.Call(hope, "Awake");
        return hope;
    }

    [TestCase(30, 100, 30)]
    [TestCase(0, 100, 0)]
    [TestCase(100, 100, 100)]
    [TestCase(101, 100, 100)]
    [TestCase(150, 100, 100)]
    [TestCase(5, 1, 1)]
    public void StartsAtStartHope_ClampedToMax(int startHope, int maxHope, int expected)
    {
        Assert.AreEqual(expected, MakeHope(startHope, maxHope).Current);
    }

    [Test]
    public void Max_IsTheSettingsMaxHope()
    {
        Assert.AreEqual(80, MakeHope(30, 80).Max);
    }

    [TestCase(10, 40)]
    [TestCase(0, 30)]
    [TestCase(70, 100)]
    [TestCase(71, 100)]
    [TestCase(1000, 100)]
    [TestCase(-10, 20)]
    [TestCase(-31, 0)]
    public void Add_IsClampedBetweenZeroAndMax(int amount, int expected)
    {
        Hope hope = MakeHope(30);
        hope.Add(amount);
        Assert.AreEqual(expected, hope.Current);
    }

    [TestCase(10, 20)]
    [TestCase(0, 30)]
    [TestCase(29, 1)]
    [TestCase(30, 0)]
    [TestCase(31, 0)]
    [TestCase(1000, 0)]
    [TestCase(-10, 40)]
    [TestCase(-100, 100)]
    public void Lose_IsClampedBetweenZeroAndMax(int amount, int expected)
    {
        Hope hope = MakeHope(30);
        hope.Lose(amount);
        Assert.AreEqual(expected, hope.Current);
    }

    [TestCase(30, false)]
    [TestCase(29, false)]
    [TestCase(1, false)]
    public void IsEmpty_IsFalseWhileSomeHopeIsLeft(int start, bool expected)
    {
        Assert.AreEqual(expected, MakeHope(start).IsEmpty);
    }

    [TestCase(30)]
    [TestCase(31)]
    public void IsEmpty_IsTrueAtZero(int lost)
    {
        Hope hope = MakeHope(30);
        hope.Lose(lost);
        Assert.IsTrue(hope.IsEmpty);
    }

    [Test]
    public void Changed_FiresWithTheNewValue()
    {
        Hope hope = MakeHope(30);
        int received = -1;
        hope.Changed += value => received = value;

        hope.Add(15);

        Assert.AreEqual(45, received);
    }

    [Test]
    public void Changed_FiresWithTheClampedValue()
    {
        Hope hope = MakeHope(90);
        int received = -1;
        hope.Changed += value => received = value;

        hope.Add(50);

        Assert.AreEqual(100, received);
    }

    [Test]
    public void Changed_FiresOncePerChange()
    {
        Hope hope = MakeHope(30);
        int calls = 0;
        hope.Changed += value => calls++;

        hope.Add(5);
        hope.Lose(5);
        hope.Lose(0);

        Assert.AreEqual(3, calls);
    }

    [Test]
    public void StartingValue_DoesNotFireChanged()
    {
        Hope hope = Make("Player").AddComponent<Hope>();
        TestUtils.Set(hope, "settings", Track(TestData.Settings()));
        int calls = 0;
        hope.Changed += value => calls++;

        TestUtils.Call(hope, "Awake");

        Assert.AreEqual(0, calls);
    }
}
