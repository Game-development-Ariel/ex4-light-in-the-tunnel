using NUnit.Framework;

/// <summary>A new asset made from the Create menu should already work, so its defaults are checked too.</summary>
public class DefaultValuesTests : EditModeTest
{
    [Test]
    public void LevelSettings_Defaults()
    {
        var settings = Track(UnityEngine.ScriptableObject.CreateInstance<TunnelLevelSettings>());
        Assert.Greater(settings.maxHope, 0);
        Assert.That(settings.startHope, Is.InRange(1, settings.maxHope));
        Assert.Greater(settings.hopeLostWhenCaught, 0);
        Assert.Greater(settings.caughtFreezeSeconds, 0f);
        Assert.GreaterOrEqual(settings.endFadeSeconds, 0f);
    }

    [Test]
    public void VisionStats_Defaults()
    {
        var stats = Track(UnityEngine.ScriptableObject.CreateInstance<VisionStats>());
        Assert.That(stats.viewAngle, Is.InRange(1f, 359f));
        Assert.Greater(stats.viewDistance, 0f);
        Assert.GreaterOrEqual(stats.alarmSeconds, 0f);
    }

    [Test]
    public void HopeEffects_Defaults()
    {
        var effects = Track(TestData.HopeEffects());
        Assert.Less(effects.speedFactor.x, effects.speedFactor.y);
        Assert.Greater(effects.speedFactor.x, 0f);
        Assert.Less(effects.hearingRange.x, effects.hearingRange.y);
        Assert.Less(effects.steadyHope, effects.faithHope);
        Assert.Greater(effects.surgeSeconds, 0f);
    }

    [Test]
    public void ImpactSounds_Defaults()
    {
        var sounds = Track(TestData.Sounds());
        Assert.That(sounds.minVolume, Is.InRange(0f, 1f));
        Assert.Greater(sounds.fullVolumeHitSpeed, 0f);
        Assert.Greater(sounds.fullVolumeTouchSpeed, 0f);
    }
}
