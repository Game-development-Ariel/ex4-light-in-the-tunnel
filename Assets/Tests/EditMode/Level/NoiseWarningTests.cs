using NUnit.Framework;

public class NoiseWarningTests : EditModeTest
{
    private NoiseWarning warning;
    private EventSpy hints;

    [SetUp]
    public void SetUp()
    {
        warning = MakeWarning();
    }

    private NoiseWarning MakeWarning()
    {
        TextEvent hintRequested = Track(TestData.TextEvent());
        hints = new EventSpy(hintRequested);
        NoiseWarning made = Make("Noise Warning").AddComponent<NoiseWarning>();
        TestUtils.Set(made, "hintRequested", hintRequested);
        TestUtils.Set(made, "warnAboveSpeed", 1.2f);
        return made;
    }

    [TestCase(0f)]
    [TestCase(1.19f)]
    [TestCase(-5f)]
    public void SoftHit_DoesNotWarn(float speed)
    {
        warning.WallHit(speed);

        Assert.AreEqual(0, hints.Count);
    }

    [TestCase(1.2f)]
    [TestCase(1.21f)]
    [TestCase(20f)]
    public void LoudHit_WarnsWithTheWarningText(float speed)
    {
        warning.WallHit(speed);

        Assert.AreEqual(1, hints.Count);
        Assert.AreEqual(TestUtils.Get<string>(warning, "warning"), hints.Last);
    }

    // Regression (cleanup): every crate had its own warning, so the thought could come once per crate.
    [Test]
    public void ManyLoudHits_FromManyCrates_WarnOnlyOncePerLevel()
    {
        for (int crate = 0; crate < 5; crate++)
        {
            warning.WallHit(3f);
        }

        Assert.AreEqual(1, hints.Count);
    }

    [Test]
    public void SoftHitsFirst_DoNotUseUpTheWarning()
    {
        warning.WallHit(0.5f);
        warning.WallHit(0.5f);

        warning.WallHit(2f);

        Assert.AreEqual(1, hints.Count);
    }

    // The warning is a scene object and not a static, so a reloaded level warns again.
    [Test]
    public void ANewLevel_WarnsAgain()
    {
        warning.WallHit(3f);

        NoiseWarning nextLevel = MakeWarning();
        nextLevel.WallHit(3f);

        Assert.AreEqual(1, hints.Count);
    }
}
