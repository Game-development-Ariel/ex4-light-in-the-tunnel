using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

/// <summary>The small effects that make the game feel alive: shake, bob, dust, flicker and the walking bounce.</summary>
public class FeelTests : PlayModeTest
{
    private ScreenShake MakeShake(out GameEvent shakeOn, out Camera cam)
    {
        shakeOn = Track(TestData.Event());
        GameObject view = MakeOff("Camera");
        cam = view.AddComponent<Camera>();
        ScreenShake shake = view.AddComponent<ScreenShake>();
        TestUtils.Set(shake, "shakeOn", shakeOn);
        TestUtils.Set(shake, "seconds", 0.3f);
        view.SetActive(true);
        return shake;
    }

    private static void Draw(ScreenShake shake, Camera cam, bool before)
    {
        TestUtils.Call(shake, before ? "BeforeDraw" : "AfterDraw", default(ScriptableRenderContext), cam);
    }

    [UnityTest]
    public IEnumerator ScreenShake_MovesTheCameraOnlyForTheDrawing()
    {
        ScreenShake shake = MakeShake(out GameEvent shakeOn, out Camera cam);
        Vector3 rest = cam.transform.position;
        yield return null;

        shakeOn.Raise();
        Random.InitState(1);
        Draw(shake, cam, true);
        float moved = Vector3.Distance(rest, cam.transform.position);
        Draw(shake, cam, false);

        Assert.Greater(moved, 0f);
        Assert.LessOrEqual(moved, TestUtils.Get<float>(shake, "strength") + 1e-4f);
        Assert.AreEqual(rest, cam.transform.position, "The camera is back where it was after the drawing");
    }

    [UnityTest]
    public IEnumerator ScreenShake_Stops_AfterItsSeconds()
    {
        ScreenShake shake = MakeShake(out GameEvent shakeOn, out Camera cam);
        shakeOn.Raise();
        yield return TestUtils.WaitGameSeconds(0.4f);
        Vector3 rest = cam.transform.position;

        Draw(shake, cam, true);

        Assert.AreEqual(rest, cam.transform.position);
        Draw(shake, cam, false);
    }

    [UnityTest]
    public IEnumerator ScreenShake_IgnoresOtherCameras()
    {
        ScreenShake shake = MakeShake(out GameEvent shakeOn, out Camera cam);
        Camera minimap = Make("Minimap Camera").AddComponent<Camera>();
        yield return null;
        shakeOn.Raise();
        Vector3 rest = cam.transform.position;

        Draw(shake, minimap, true);

        Assert.AreEqual(rest, cam.transform.position);
    }

    [UnityTest]
    public IEnumerator Bob_FloatsAroundWhereItStarted()
    {
        GameObject arrow = Make("Arrow");
        arrow.transform.localPosition = new Vector3(2f, 3f, 0f);
        Bob bob = arrow.AddComponent<Bob>();
        float height = TestUtils.Get<float>(bob, "height");
        float lowest = float.MaxValue;
        float highest = float.MinValue;

        for (int i = 0; i < 120; i++)
        {
            yield return null;
            lowest = Mathf.Min(lowest, arrow.transform.localPosition.y);
            highest = Mathf.Max(highest, arrow.transform.localPosition.y);
            Assert.AreEqual(2f, arrow.transform.localPosition.x, 1e-4f);
        }

        Assert.GreaterOrEqual(lowest, 3f - height - 1e-4f);
        Assert.LessOrEqual(highest, 3f + height + 1e-4f);
        Assert.Greater(highest - lowest, 0f);
    }

    [UnityTest]
    public IEnumerator DustMotes_SetUpTheParticles()
    {
        GameObject dust = MakeOff("Dust");
        dust.AddComponent<ParticleSystem>();
        DustMotes motes = dust.AddComponent<DustMotes>();
        TestUtils.Set(motes, "maxSpecks", 30);
        TestUtils.Set(motes, "area", new Vector2(4f, 2f));
        dust.SetActive(true);
        yield return null;

        var particles = dust.GetComponent<ParticleSystem>();
        Assert.AreEqual(30, particles.main.maxParticles);
        Assert.IsTrue(particles.main.loop);
        Assert.AreEqual(new Vector3(4f, 2f, 0.1f), particles.shape.scale);
        Assert.IsTrue(particles.isPlaying);
        Assert.IsTrue(particles.noise.enabled);
    }

    [UnityTest]
    public IEnumerator LightFlicker_ShimmersAroundTheBrightness_AndDips()
    {
        GameObject bulb = MakeOff("Bulb");
        Light2D light = bulb.AddComponent<Light2D>();
        light.intensity = 1f;
        LightFlicker flicker = bulb.AddComponent<LightFlicker>();
        TestUtils.Set(flicker, "firstDipAfter", 0.05f);
        TestUtils.Set(flicker, "secondsBetweenDips", 0.1f);
        TestUtils.Set(flicker, "dipSeconds", 0.05f);
        bulb.SetActive(true);
        float shimmer = TestUtils.Get<float>(flicker, "shimmer");
        float dip = TestUtils.Get<float>(flicker, "dipBrightness");
        bool dipped = false;

        for (int i = 0; i < 120; i++)
        {
            yield return null;
            Assert.LessOrEqual(light.intensity, 1f + shimmer + 0.01f);
            Assert.GreaterOrEqual(light.intensity, dip * (1f - shimmer) - 0.01f);
            dipped |= light.intensity < 0.5f;
        }

        Assert.IsTrue(dipped, "The bulb dipped at least once");
    }

    [UnityTest]
    public IEnumerator WalkBounce_SquashesWhileWalking_AndSettlesWhenStopped()
    {
        var keyboard = new VirtualKeyboard();
        try
        {
            GameObject hero = MakeOff("Player");
            PlayerMovement movement = TestHero.Create(hero);
            GameObject body = Make("Body");
            body.transform.SetParent(hero.transform, false);
            WalkBounce bounce = body.AddComponent<WalkBounce>();
            TestUtils.Set(bounce, "movement", movement);
            hero.SetActive(true);
            Time.captureFramerate = 60;
            yield return null;

            keyboard.Hold(Key.D);
            float widest = 1f;
            for (int i = 0; i < 30; i++)
            {
                yield return null;
                widest = Mathf.Max(widest, body.transform.localScale.x);
            }
            Assert.Greater(widest, 1.01f, "Wider on each step");

            keyboard.ReleaseAll();
            yield return TestUtils.WaitFrames(60);
            Assert.AreEqual(Vector3.one, body.transform.localScale, "Back to the rest shape");
        }
        finally
        {
            keyboard.Remove();
        }
    }
}
