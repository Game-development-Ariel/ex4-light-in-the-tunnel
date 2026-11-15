using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The data assets in Assets/Data: they load, they hold sensible numbers, and the sounds they name exist.
/// </summary>
public class DataAssetTests
{
    private const string DataFolder = "Assets/Data";

    private static readonly string[] EventNames =
    {
        "PlayerCaught", "BottlePicked", "BottleDropped", "BottleDelivered", "OpeningFinished", "LevelWon", "LevelLost",
    };

    private static readonly string[] TextEventNames = { "HintRequested", "MemoryFound" };

    private static readonly string[] VisionAssets = { "SecurityCameraVision", "RoomCameraVision", "GuardVision" };

    private static readonly string[] SoundAssets = { "CrateSounds", "SandbagSounds" };

    private static T Load<T>(string name) where T : Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>($"{DataFolder}/{name}.asset");
        Assert.IsNotNull(asset, $"{DataFolder}/{name}.asset is missing or is not a {typeof(T).Name}");
        return asset;
    }

    private static IEnumerable<string> AllDataAssetPaths()
    {
        return AssetDatabase.FindAssets("t:Object", new[] { DataFolder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => path.EndsWith(".asset"));
    }

    [Test]
    public void EveryDataAsset_LoadsAsAGameScriptableObject()
    {
        List<string> paths = AllDataAssetPaths().ToList();
        Assert.IsNotEmpty(paths);
        foreach (string path in paths)
        {
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            Assert.IsNotNull(asset, $"{path} does not load (missing script?)");
            Assert.AreEqual(typeof(GameEvent).Assembly, asset.GetType().Assembly, $"{path} is not one of the game's data types");
        }
    }

    [TestCaseSource(nameof(EventNames))]
    public void GameEventAsset_Exists(string name)
    {
        Load<GameEvent>("Events/" + name);
    }

    [TestCaseSource(nameof(TextEventNames))]
    public void TextEventAsset_Exists(string name)
    {
        Load<TextEvent>("Events/" + name);
    }

    [Test]
    public void EventsFolder_HasNoUnknownEvents()
    {
        var known = new HashSet<string>(EventNames.Concat(TextEventNames));
        foreach (string path in AssetDatabase.FindAssets("t:Object", new[] { DataFolder + "/Events" }).Select(AssetDatabase.GUIDToAssetPath))
        {
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            Assert.IsTrue(known.Contains(name), $"{path} is not listed in the tests. Add it to EventNames and to the README");
        }
    }

    [Test]
    public void LevelSettings_HaveSensibleNumbers()
    {
        var settings = Load<TunnelLevelSettings>("TutorialLevelSettings");
        Assert.Greater(settings.maxHope, 0);
        Assert.That(settings.startHope, Is.InRange(1, settings.maxHope));
        Assert.Greater(settings.hopeLostWhenCaught, 0, "Being caught has to cost hope, or the level can't be lost");
        Assert.LessOrEqual(settings.hopeLostWhenCaught, settings.maxHope);
        Assert.Greater(settings.caughtFreezeSeconds, 0f);
        Assert.Less(settings.caughtFreezeSeconds, 15f);
        Assert.That(settings.endFadeSeconds, Is.InRange(0f, 5f));
    }

    [Test]
    public void LevelSettings_MatchTheReadme()
    {
        var settings = Load<TunnelLevelSettings>("TutorialLevelSettings");
        Assert.AreEqual(30, settings.startHope, "The README says the hero starts with 30 hope");
        Assert.AreEqual(20, settings.hopeLostWhenCaught, "The README says every capture costs 20 hope");
    }

    [TestCaseSource(nameof(VisionAssets))]
    public void VisionStats_HaveSensibleNumbers(string name)
    {
        var stats = Load<VisionStats>(name);
        Assert.That(stats.viewAngle, Is.InRange(1f, 359f));
        Assert.That(stats.viewDistance, Is.InRange(0.1f, 50f));
        Assert.GreaterOrEqual(stats.alarmSeconds, 0f);
    }

    [Test]
    public void GuardVision_IsWiderAndShorterThanTheCamera()
    {
        var guard = Load<VisionStats>("GuardVision");
        var camera = Load<VisionStats>("SecurityCameraVision");
        Assert.Greater(guard.viewAngle, camera.viewAngle);
        Assert.Less(guard.viewDistance, camera.viewDistance);
    }

    [Test]
    public void HopeEffects_GrowWithHope()
    {
        var effects = Load<HopeEffects>("HopeEffects");
        var settings = Load<TunnelLevelSettings>("TutorialLevelSettings");
        foreach (Vector2 range in new[] { effects.speedFactor, effects.sightRadius, effects.sightBrightness, effects.tunnelBrightness, effects.hearingRange })
        {
            Assert.Less(range.x, range.y, "Each effect should be stronger with full hope");
            Assert.GreaterOrEqual(range.x, 0f);
        }
        Assert.Greater(effects.speedFactor.x, 0f, "With no hope the hero must still be able to move");
        Assert.Greater(effects.surgeSeconds, 0f);
        Assert.Less(effects.steadyHope, effects.faithHope);
        Assert.That(effects.steadyHope, Is.InRange(1, settings.maxHope));
        Assert.That(effects.faithHope, Is.InRange(1, settings.maxHope));
    }

    [TestCaseSource(nameof(SoundAssets))]
    public void ImpactSounds_NameSoundsThatExist(string name)
    {
        var sounds = Load<ImpactSounds>(name);
        Assert.IsNotEmpty(sounds.wallHitSounds);
        Assert.IsNotEmpty(sounds.touchSounds);
        foreach (string sound in sounds.wallHitSounds.Concat(sounds.touchSounds))
        {
            Assert.IsNotNull(Resources.Load<AudioClip>("Audio/" + sound), $"{name} names a missing sound: Resources/Audio/{sound}");
        }
        Assert.That(sounds.minVolume, Is.InRange(0f, 1f));
        Assert.Greater(sounds.fullVolumeHitSpeed, 0f);
        Assert.Greater(sounds.fullVolumeTouchSpeed, 0f);
        Assert.GreaterOrEqual(sounds.silentBelowSpeed, 0f);
        Assert.Less(sounds.silentBelowSpeed, sounds.fullVolumeHitSpeed);
    }
}
