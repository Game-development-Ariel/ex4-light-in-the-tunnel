using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

/// <summary>The parts around the sensor: the camera's sweep and its light and hum, the guard's walk and his steps.</summary>
public class WatcherPartsTests : PlayModeTest
{
    private VisionSensor MakeSensor(GameObject watcher)
    {
        VisionSensor sensor = watcher.AddComponent<VisionSensor>();
        TestUtils.Set(sensor, "stats", Track(TestData.Vision()));
        TestUtils.Set(sensor, "targetSeen", Track(TestData.Event()));
        return sensor;
    }

    [UnityTest]
    public IEnumerator CameraSweep_TurnsTheCamera_WithinItsSweep()
    {
        GameObject camera = MakeOff("Camera");
        CameraSweep sweep = camera.AddComponent<CameraSweep>();
        TestUtils.Set(sweep, "centerAngle", 180f);
        TestUtils.Set(sweep, "sweepAngle", 60f);
        TestUtils.Set(sweep, "turnSeconds", 0.5f);
        TestUtils.Set(sweep, "pauseSeconds", 0.1f);
        camera.SetActive(true);
        float smallest = float.MaxValue;
        float largest = float.MinValue;

        for (int i = 0; i < 90; i++)
        {
            yield return null;
            float angle = Mathf.DeltaAngle(0f, camera.transform.eulerAngles.z);
            angle = angle < 0f ? angle + 360f : angle;
            smallest = Mathf.Min(smallest, angle);
            largest = Mathf.Max(largest, angle);
        }

        Assert.GreaterOrEqual(smallest, 120f - 0.5f);
        Assert.LessOrEqual(largest, 240f + 0.5f);
        Assert.Greater(largest - smallest, 100f, "It really sweeps from side to side");
    }

    [UnityTest]
    public IEnumerator CameraIndicators_MakeABlinkingLightAndAHum()
    {
        GameObject camera = MakeOff("Camera");
        MakeSensor(camera);
        camera.AddComponent<CameraIndicators>();
        camera.SetActive(true);
        yield return null;

        SpriteRenderer led = camera.transform.Find("Led").GetComponent<SpriteRenderer>();
        Assert.IsNotNull(led.sprite);
        var hum = camera.GetComponent<AudioSource>();
        Assert.IsTrue(hum.loop);
        Assert.IsNotNull(hum.clip);
        Assert.AreEqual(1f, hum.spatialBlend);

        bool wasOn = false;
        bool wasOff = false;
        for (int i = 0; i < 120 && !(wasOn && wasOff); i++)
        {
            wasOn |= led.enabled;
            wasOff |= !led.enabled;
            yield return null;
        }
        Assert.IsTrue(wasOn && wasOff, "The light blinks");
    }

    [UnityTest]
    public IEnumerator CameraIndicators_LightStaysOn_DuringTheAlarm()
    {
        GameObject camera = MakeOff("Camera");
        VisionSensor sensor = MakeSensor(camera);
        camera.AddComponent<CameraIndicators>();
        TestUtils.Set(camera.GetComponent<CameraIndicators>(), "blinkOnPart", 0f); // Off unless alarmed.
        camera.SetActive(true);
        yield return null;
        SpriteRenderer led = camera.transform.Find("Led").GetComponent<SpriteRenderer>();
        Assert.IsFalse(led.enabled);

        sensor.enabled = false;
        TestUtils.Set(sensor, "alarmTimer", 5f);
        yield return null;

        Assert.IsTrue(led.enabled);
    }

    private GuardPatrol MakeGuard(out Transform[] points)
    {
        GameObject guard = MakeOff("Guard");
        GuardPatrol patrol = guard.AddComponent<GuardPatrol>();
        points = new[] { Make("Point A").transform, Make("Point B").transform };
        points[0].position = new Vector3(1f, 0f, 0f);
        points[1].position = new Vector3(1f, 2f, 0f);
        TestUtils.Set(patrol, "points", points);
        TestUtils.Set(patrol, "walkSpeed", 4f);
        TestUtils.Set(patrol, "turnSpeed", 2000f);
        TestUtils.Set(patrol, "pauseSeconds", 0.1f);
        return patrol;
    }

    [UnityTest]
    public IEnumerator Guard_WalksToEachPoint_AndBack()
    {
        GuardPatrol patrol = MakeGuard(out Transform[] points);
        patrol.gameObject.SetActive(true);

        yield return TestUtils.WaitUntil(() => patrol.transform.position == points[0].position, 3f, "the guard to reach the first point");
        yield return TestUtils.WaitUntil(() => patrol.transform.position == points[1].position, 3f, "the guard to reach the second point");
        yield return TestUtils.WaitUntil(() => patrol.transform.position == points[0].position, 3f, "the guard to come back to the first");
    }

    [UnityTest]
    public IEnumerator Guard_LooksWhereHeWalks()
    {
        GuardPatrol patrol = MakeGuard(out Transform[] points);
        patrol.gameObject.SetActive(true);

        yield return TestUtils.WaitUntil(() => patrol.IsWalking, 3f, "the guard to walk");

        Vector3 toGoal = (points[0].position - patrol.transform.position).normalized;
        Assert.Greater(Vector3.Dot(patrol.transform.up, toGoal), 0.99f);
    }

    [UnityTest]
    public IEnumerator Guard_IsNotWalking_WhileHeWaits()
    {
        GuardPatrol patrol = MakeGuard(out Transform[] points);
        TestUtils.Set(patrol, "pauseSeconds", 1f);
        patrol.gameObject.SetActive(true);

        yield return TestUtils.WaitUntil(() => patrol.transform.position == points[0].position, 3f, "the guard to reach the first point");
        yield return TestUtils.WaitFrames(10); // Time to turn to the wall, less than the pause.

        Assert.IsFalse(patrol.IsWalking);
        Assert.Greater(Vector3.Dot(patrol.transform.up, Vector3.up), 0.99f, "He looks at the wall (pauseLook) while he waits");
    }

    private HeardSteps MakeHearing(GameObject guard, out HopeSenses hero, int heroHope)
    {
        GameObject player = MakeOff("Player");
        Hope hope = player.AddComponent<Hope>();
        TestUtils.Set(hope, "settings", Track(TestData.Settings(100, heroHope)));
        TestHero.Create(player);
        hero = player.AddComponent<HopeSenses>();
        TestUtils.Set(hero, "effects", Track(TestData.HopeEffects()));
        TestUtils.Set(hero, "eyes", Make("Eyes").AddComponent<Light2D>());
        TestUtils.Set(hero, "tunnelLight", Make("Tunnel Light").AddComponent<Light2D>());
        player.SetActive(true);

        HeardSteps steps = guard.AddComponent<HeardSteps>();
        GameObject ring = Make("Ring");
        TestUtils.Set(steps, "heroSenses", hero);
        TestUtils.Set(steps, "ring", ring.AddComponent<SpriteRenderer>());
        return steps;
    }

    private static void SetWalking(GuardPatrol patrol, bool walking)
    {
        TestUtils.Set(patrol, "<IsWalking>k__BackingField", walking);
    }

    [UnityTest]
    public IEnumerator HeardSteps_ShowTheRing_WhenTheGuardWalksNearby()
    {
        GuardPatrol patrol = MakeGuard(out _);
        patrol.enabled = false; // Held still, the test decides if he walks.
        HeardSteps steps = MakeHearing(patrol.gameObject, out HopeSenses hero, 50);
        yield return null; // The hero's senses start from the hope in Start.
        patrol.transform.position = new Vector3(hero.HearingRange - 0.5f, 0f, 0f);
        patrol.gameObject.SetActive(true);
        SetWalking(patrol, true);
        yield return null;
        yield return null;

        SpriteRenderer ring = TestUtils.Get<SpriteRenderer>(steps, "ring");
        Assert.IsTrue(ring.enabled);
        Assert.LessOrEqual(ring.color.a, TestUtils.Get<float>(steps, "maxAlpha"));
    }

    [UnityTest]
    public IEnumerator HeardSteps_AreSilent_BeyondTheHearingRange()
    {
        GuardPatrol patrol = MakeGuard(out _);
        patrol.enabled = false;
        HeardSteps steps = MakeHearing(patrol.gameObject, out HopeSenses hero, 50);
        yield return null; // The hero's senses start from the hope in Start.
        patrol.transform.position = new Vector3(hero.HearingRange + 0.5f, 0f, 0f);
        patrol.gameObject.SetActive(true);
        SetWalking(patrol, true);
        yield return null;
        yield return null;

        Assert.IsFalse(TestUtils.Get<SpriteRenderer>(steps, "ring").enabled);
    }

    [UnityTest]
    public IEnumerator HeardSteps_AreSilent_WhenTheGuardStands()
    {
        GuardPatrol patrol = MakeGuard(out _);
        patrol.enabled = false;
        HeardSteps steps = MakeHearing(patrol.gameObject, out _, 50);
        patrol.gameObject.SetActive(true);
        SetWalking(patrol, false);
        yield return null;

        Assert.IsFalse(TestUtils.Get<SpriteRenderer>(steps, "ring").enabled);
    }

    [UnityTest]
    public IEnumerator HeardSteps_MoreHope_HearsFurther()
    {
        GuardPatrol patrol = MakeGuard(out _);
        patrol.enabled = false;
        HeardSteps steps = MakeHearing(patrol.gameObject, out HopeSenses weakHero, 0);
        yield return null;
        float weakRange = weakHero.HearingRange;
        patrol.transform.position = new Vector3(weakRange + 2f, 0f, 0f);
        patrol.gameObject.SetActive(true);
        SetWalking(patrol, true);
        yield return null;
        Assert.IsFalse(TestUtils.Get<SpriteRenderer>(steps, "ring").enabled, "A hero with no hope doesn't hear him");

        weakHero.GetComponent<Hope>().Add(100);
        yield return TestUtils.WaitUntil(() => weakHero.HearingRange > weakRange + 2.5f, 5f, "the hearing to grow with hope");
        yield return null;

        Assert.IsTrue(TestUtils.Get<SpriteRenderer>(steps, "ring").enabled, "With hope he hears the same steps");
    }
}
