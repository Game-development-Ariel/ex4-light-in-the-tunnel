using NUnit.Framework;

public class CameraSweepTests : EditModeTest
{
    private CameraSweep sweep;

    [SetUp]
    public void SetUp()
    {
        sweep = Make("Camera").AddComponent<CameraSweep>();
        TestUtils.Set(sweep, "turnSeconds", 5f);
        TestUtils.Set(sweep, "pauseSeconds", 2.5f);
    }

    private float SideAt(float seconds)
    {
        TestUtils.Set(sweep, "sweepTime", seconds);
        return (float)TestUtils.Call(sweep, "CurrentSide");
    }

    // One cycle is 15 seconds: turn right (0-5), pause (5-7.5), turn left (7.5-12.5), pause (12.5-15).
    [TestCase(0f, -1f)]
    [TestCase(2.5f, 0f)]
    [TestCase(5f, 1f)]
    [TestCase(6f, 1f)]
    [TestCase(7.5f, 1f)]
    [TestCase(10f, 0f)]
    [TestCase(12.5f, -1f)]
    [TestCase(14f, -1f)]
    [TestCase(15f, -1f)]
    [TestCase(17.5f, 0f)]
    [TestCase(150f, -1f)]
    public void CurrentSide_FollowsTheCycle(float seconds, float expected)
    {
        Assert.AreEqual(expected, SideAt(seconds), 1e-4f);
    }

    [Test]
    public void CurrentSide_StaysBetweenTheEnds()
    {
        for (float t = 0f; t < 30f; t += 0.37f)
        {
            float side = SideAt(t);
            Assert.That(side, Is.InRange(-1f, 1f), $"at {t} s");
        }
    }

    [Test]
    public void Turning_IsSmooth_SlowAtTheEnds()
    {
        float nearStart = SideAt(0.5f) - SideAt(0f);
        float inMiddle = SideAt(3f) - SideAt(2.5f);

        Assert.Less(nearStart, inMiddle);
    }

    [Test]
    public void NoPause_TurnsBackRightAway()
    {
        TestUtils.Set(sweep, "pauseSeconds", 0f);

        Assert.AreEqual(1f, SideAt(5f), 1e-4f);
        Assert.Less(SideAt(6f), 1f);
    }
}
