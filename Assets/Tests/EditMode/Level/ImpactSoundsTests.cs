using NUnit.Framework;

public class ImpactSoundsTests : EditModeTest
{
    private ImpactSounds sounds;

    [SetUp]
    public void SetUp()
    {
        sounds = Track(TestData.Sounds());
        sounds.minVolume = 0.25f;
        sounds.fullVolumeHitSpeed = 2f;
        sounds.fullVolumeTouchSpeed = 4f;
    }

    [TestCase(0f, 0.25f)]
    [TestCase(-3f, 0.25f)]
    [TestCase(1f, 0.625f)]
    [TestCase(1.999f, 0.9996f)]
    [TestCase(2f, 1f)]
    [TestCase(2.001f, 1f)]
    [TestCase(50f, 1f)]
    public void HitVolume_GrowsFromMinToOneAtFullSpeed(float speed, float expected)
    {
        Assert.AreEqual(expected, sounds.HitVolume(speed), 1e-3f);
    }

    [TestCase(0f, 0.25f)]
    [TestCase(2f, 0.625f)]
    [TestCase(4f, 1f)]
    [TestCase(8f, 1f)]
    public void TouchVolume_UsesTheTouchSpeed(float speed, float expected)
    {
        Assert.AreEqual(expected, sounds.TouchVolume(speed), 1e-3f);
    }

    [Test]
    public void Volume_NeverGoesDownWithSpeed()
    {
        float previous = 0f;
        for (float speed = 0f; speed <= 3f; speed += 0.1f)
        {
            float volume = sounds.HitVolume(speed);
            Assert.GreaterOrEqual(volume, previous);
            previous = volume;
        }
    }

    [TestCase(0f)]
    [TestCase(1f)]
    public void MinVolume_IsTheQuietest(float minVolume)
    {
        sounds.minVolume = minVolume;
        Assert.AreEqual(minVolume, sounds.HitVolume(0f), 1e-5f);
        Assert.AreEqual(1f, sounds.HitVolume(100f), 1e-5f);
    }

    [Test]
    public void Defaults_HaveSounds()
    {
        ImpactSounds defaults = Track(TestData.Sounds());
        Assert.IsNotEmpty(defaults.wallHitSounds);
        Assert.IsNotEmpty(defaults.touchSounds);
        Assert.Greater(defaults.fullVolumeHitSpeed, defaults.silentBelowSpeed);
    }
}
