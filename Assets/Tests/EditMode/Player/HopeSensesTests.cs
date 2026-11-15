using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class HopeSensesTests : EditModeTest
{
    private HopeEffects effects;
    private PlayerMovement movement;
    private HopeSenses senses;
    private Light2D eyes;
    private Light2D tunnelLight;

    private void MakeHero(int startHope)
    {
        effects = Track(TestData.HopeEffects());
        var player = Make("Player");
        Hope hope = player.AddComponent<Hope>();
        TestUtils.Set(hope, "settings", Track(TestData.Settings(100, startHope)));
        movement = player.AddComponent<PlayerMovement>();
        senses = player.AddComponent<HopeSenses>();
        eyes = Make("Eyes").AddComponent<Light2D>();
        tunnelLight = Make("Tunnel Light").AddComponent<Light2D>();
        TestUtils.Set(senses, "effects", effects);
        TestUtils.Set(senses, "eyes", eyes);
        TestUtils.Set(senses, "tunnelLight", tunnelLight);

        TestUtils.Call(hope, "Awake");
        TestUtils.Call(movement, "Awake");
        TestUtils.Call(senses, "Awake");
        TestUtils.Call(senses, "Start");
    }

    [TestCase(0, 0f)]
    [TestCase(30, 0.3f)]
    [TestCase(50, 0.5f)]
    [TestCase(100, 1f)]
    public void Start_SetsTheBodyFromTheHope(int startHope, float strength)
    {
        MakeHero(startHope);

        Assert.AreEqual(Mathf.Lerp(effects.speedFactor.x, effects.speedFactor.y, strength), movement.SpeedFactor, 1e-4f);
        Assert.AreEqual(Mathf.Lerp(effects.sightRadius.x, effects.sightRadius.y, strength), eyes.pointLightOuterRadius, 1e-4f);
        Assert.AreEqual(Mathf.Lerp(effects.sightBrightness.x, effects.sightBrightness.y, strength), eyes.intensity, 1e-4f);
        Assert.AreEqual(Mathf.Lerp(effects.tunnelBrightness.x, effects.tunnelBrightness.y, strength), tunnelLight.intensity, 1e-4f);
        Assert.AreEqual(Mathf.Lerp(effects.hearingRange.x, effects.hearingRange.y, strength), senses.HearingRange, 1e-4f);
    }

    [Test]
    public void MoreHope_MeansFasterAndHearsFurther()
    {
        MakeHero(10);
        float weakSpeed = movement.SpeedFactor;
        float weakHearing = senses.HearingRange;

        MakeHero(90);

        Assert.Greater(movement.SpeedFactor, weakSpeed);
        Assert.Greater(senses.HearingRange, weakHearing);
    }

    [Test]
    public void MovementSpeed_IsTheBaseSpeedTimesTheFactor()
    {
        MakeHero(100);
        TestUtils.Set(movement, "moveSpeed", 2f);

        Assert.AreEqual(2f * effects.speedFactor.y, movement.CurrentSpeed, 1e-4f);
    }
}
