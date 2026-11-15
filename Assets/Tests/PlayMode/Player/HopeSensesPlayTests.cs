using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

/// <summary>The body follows a change of hope slowly, and a gain makes the light flare for a moment.</summary>
public class HopeSensesPlayTests : PlayModeTest
{
    private HopeEffects effects;
    private Hope hope;
    private PlayerMovement movement;
    private HopeSenses senses;
    private Light2D eyes;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        effects = Track(TestData.HopeEffects());
        GameObject hero = MakeOff("Player");
        hope = hero.AddComponent<Hope>();
        TestUtils.Set(hope, "settings", Track(TestData.Settings(100, 30)));
        movement = TestHero.Create(hero);
        senses = hero.AddComponent<HopeSenses>();
        eyes = Make("Eyes").AddComponent<Light2D>();
        TestUtils.Set(senses, "effects", effects);
        TestUtils.Set(senses, "eyes", eyes);
        TestUtils.Set(senses, "tunnelLight", Make("Tunnel Light").AddComponent<Light2D>());
        TestUtils.Set(senses, "changeSpeed", 0.4f);
        hero.SetActive(true);
        Time.captureFramerate = 60;
        yield return null;
    }

    private float SpeedAt(float strength)
    {
        return Mathf.Lerp(effects.speedFactor.x, effects.speedFactor.y, strength);
    }

    [UnityTest]
    public IEnumerator GainedHope_SpeedsTheBodyUpSlowly()
    {
        hope.Add(40); // 30 -> 70.
        yield return TestUtils.WaitFrames(30); // Half a second: 0.2 of the way at 0.4 per second.

        Assert.Greater(movement.SpeedFactor, SpeedAt(0.3f));
        Assert.Less(movement.SpeedFactor, SpeedAt(0.7f));

        yield return TestUtils.WaitFrames(60);
        Assert.AreEqual(SpeedAt(0.7f), movement.SpeedFactor, 1e-3f);
    }

    [UnityTest]
    public IEnumerator LostHope_SlowsTheBodyDown()
    {
        hope.Lose(30);
        yield return TestUtils.WaitFrames(60 * 2);

        Assert.AreEqual(SpeedAt(0f), movement.SpeedFactor, 1e-3f);
    }

    [UnityTest]
    public IEnumerator GainedHope_MakesTheLightFlare_ThenFade()
    {
        yield return TestUtils.WaitFrames(2);
        float before = eyes.pointLightOuterRadius;

        hope.Add(1);
        yield return null;
        Assert.Greater(eyes.pointLightOuterRadius, before + effects.surgeRadius * 0.8f, "The flare");

        yield return TestUtils.WaitFrames(Mathf.CeilToInt(effects.surgeSeconds * 60f) + 5);
        Assert.Less(eyes.pointLightOuterRadius, before + 0.1f, "The flare faded");
    }

    [UnityTest]
    public IEnumerator LostHope_DoesNotFlare()
    {
        yield return TestUtils.WaitFrames(2);
        float before = eyes.pointLightOuterRadius;

        hope.Lose(10);
        yield return null;

        Assert.LessOrEqual(eyes.pointLightOuterRadius, before);
    }
}
