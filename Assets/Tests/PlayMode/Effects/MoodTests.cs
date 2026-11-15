using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

/// <summary>Tension rises near the watchers, and the picture (MoodVolume) darkens with danger, low hope and capture.</summary>
public class MoodTests : PlayModeTest
{
    private Transform player;
    private Tension tension;
    private AudioSource heartbeat;
    private AudioSource breathing;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        player = Make("Player").transform;
        player.position = new Vector3(100f, 0f, 0f); // Far away until the test moves it.
        GameObject watcher = MakeOff("Camera");
        watcher.AddComponent<VisionSensor>(); // No target: it never sees, it is only something to fear.
        watcher.SetActive(true);

        GameObject sound = MakeOff("Sound");
        heartbeat = sound.AddComponent<AudioSource>();
        breathing = sound.AddComponent<AudioSource>();
        tension = sound.AddComponent<Tension>();
        TestUtils.Set(tension, "player", player);
        TestUtils.Set(tension, "heartbeat", heartbeat);
        TestUtils.Set(tension, "breathing", breathing);
        TestUtils.Set(tension, "smoothing", 1000f); // Follows at once, so the test reads the goal.
        sound.SetActive(true);
        yield return null;
    }

    private IEnumerator StandAt(float distance)
    {
        player.position = new Vector3(distance, 0f, 0f);
        yield return null;
        yield return null;
    }

    [UnityTest]
    public IEnumerator Tension_IsFull_CloseToAWatcher()
    {
        yield return StandAt(3f);

        Assert.AreEqual(1f, tension.Danger, 1e-4f);
        Assert.AreEqual(TestUtils.Get<float>(tension, "maxHeartbeatVolume"), heartbeat.volume, 1e-4f);
        Assert.AreEqual(TestUtils.Get<Vector2>(tension, "heartbeatPitch").y, heartbeat.pitch, 1e-4f);
        Assert.AreEqual(TestUtils.Get<Vector2>(tension, "breathingVolume").y, breathing.volume, 1e-4f);
    }

    [UnityTest]
    public IEnumerator Tension_IsNone_FarFromEveryWatcher()
    {
        yield return StandAt(20f);

        Assert.AreEqual(0f, tension.Danger, 1e-4f);
        Assert.AreEqual(0f, heartbeat.volume, 1e-4f);
        Assert.AreEqual(TestUtils.Get<Vector2>(tension, "breathingVolume").x, breathing.volume, 1e-4f);
    }

    [UnityTest]
    public IEnumerator Tension_IsHalf_HalfWay()
    {
        yield return StandAt(10f); // Halfway between 6 (full) and 14 (none).

        Assert.AreEqual(0.5f, tension.Danger, 1e-3f);
    }

    [UnityTest]
    public IEnumerator Tension_RisesSmoothly()
    {
        TestUtils.Set(tension, "smoothing", 1.5f);
        Time.captureFramerate = 60;

        yield return StandAt(1f);

        Assert.Greater(tension.Danger, 0f);
        Assert.Less(tension.Danger, 0.1f);
    }

    private MoodVolume MakeMood(out GameEvent caught, out Hope hope)
    {
        caught = Track(TestData.Event());
        GameObject hero = MakeOff("Hero");
        hope = hero.AddComponent<Hope>();
        TestUtils.Set(hope, "settings", Track(TestData.Settings(100, 30)));
        hero.SetActive(true);

        VolumeProfile profile = Track(ScriptableObject.CreateInstance<VolumeProfile>());
        profile.Add<Vignette>(true);
        profile.Add<ColorAdjustments>(true);
        profile.Add<ChromaticAberration>(true);
        GameObject volumeObject = MakeOff("Mood");
        volumeObject.AddComponent<Volume>().sharedProfile = profile;
        MoodVolume mood = volumeObject.AddComponent<MoodVolume>();
        TestUtils.Set(mood, "tension", tension);
        TestUtils.Set(mood, "hope", hope);
        TestUtils.Set(mood, "playerCaught", caught);
        TestUtils.Set(mood, "shockSeconds", 0.2f);
        volumeObject.SetActive(true);
        return mood;
    }

    [UnityTest]
    public IEnumerator Mood_Capture_DrainsTheColor_ThenItComesBack()
    {
        Time.captureFramerate = 60;
        yield return StandAt(20f);
        MoodVolume mood = MakeMood(out GameEvent caught, out _);
        yield return null;
        var colors = TestUtils.Get<ColorAdjustments>(mood, "colors");
        var split = TestUtils.Get<ChromaticAberration>(mood, "split");
        float calm = colors.saturation.value;

        caught.Raise();
        yield return null;
        yield return null;
        Assert.Less(colors.saturation.value, calm - 30f, "The shock drains the color");
        Assert.Greater(split.intensity.value, 0.3f);

        yield return TestUtils.WaitGameSeconds(0.4f);
        Assert.AreEqual(calm, colors.saturation.value, 0.01f);
        Assert.AreEqual(0f, split.intensity.value, 1e-4f);
    }

    [UnityTest]
    public IEnumerator Mood_Danger_ClosesTheEdges()
    {
        yield return StandAt(20f);
        MoodVolume mood = MakeMood(out _, out _);
        yield return null;
        var vignette = TestUtils.Get<Vignette>(mood, "vignette");
        float calm = vignette.intensity.value;

        yield return StandAt(1f);

        Assert.Greater(vignette.intensity.value, calm + 0.1f);
    }

    [UnityTest]
    public IEnumerator Mood_LowHope_DarkensThePicture()
    {
        yield return StandAt(20f);
        MoodVolume mood = MakeMood(out _, out Hope hope);
        yield return null;
        var colors = TestUtils.Get<ColorAdjustments>(mood, "colors");
        float lowHopeDarkness = TestUtils.Get<float>(mood, "lowHopeDarkness");
        Assert.AreEqual(-0.7f * lowHopeDarkness, colors.postExposure.value, 1e-3f, "30 hope of 100");

        hope.Add(70);
        yield return null;

        Assert.AreEqual(0f, colors.postExposure.value, 1e-3f, "Full hope is not darker");
    }

    [UnityTest]
    public IEnumerator Mood_UsesItsOwnCopyOfTheProfile()
    {
        MoodVolume mood = MakeMood(out _, out _);
        yield return null;

        Volume volume = mood.GetComponent<Volume>();
        Assert.AreNotSame(volume.sharedProfile, TestUtils.Get<VolumeProfile>(mood, "profile"), "The asset on disk must not change");
    }
}
