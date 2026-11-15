using UnityEngine;

/// <summary>Creates data assets in memory, with values chosen by the test. Nothing is saved to disk.</summary>
public static class TestData
{
    public static GameEvent Event(string name = "Test Event")
    {
        var gameEvent = ScriptableObject.CreateInstance<GameEvent>();
        gameEvent.name = name;
        return gameEvent;
    }

    public static TextEvent TextEvent(string name = "Test Text Event")
    {
        var textEvent = ScriptableObject.CreateInstance<TextEvent>();
        textEvent.name = name;
        return textEvent;
    }

    public static TunnelLevelSettings Settings(int maxHope = 100, int startHope = 30, int hopeLostWhenCaught = 20,
        float caughtFreezeSeconds = 0.5f, float endFadeSeconds = 0.2f)
    {
        var settings = ScriptableObject.CreateInstance<TunnelLevelSettings>();
        settings.maxHope = maxHope;
        settings.startHope = startHope;
        settings.hopeLostWhenCaught = hopeLostWhenCaught;
        settings.caughtFreezeSeconds = caughtFreezeSeconds;
        settings.endFadeSeconds = endFadeSeconds;
        return settings;
    }

    public static VisionStats Vision(float viewAngle = 60f, float viewDistance = 5f, float alarmSeconds = 1f)
    {
        var stats = ScriptableObject.CreateInstance<VisionStats>();
        stats.viewAngle = viewAngle;
        stats.viewDistance = viewDistance;
        stats.alarmSeconds = alarmSeconds;
        return stats;
    }

    public static HopeEffects HopeEffects()
    {
        return ScriptableObject.CreateInstance<HopeEffects>();
    }

    public static ImpactSounds Sounds()
    {
        return ScriptableObject.CreateInstance<ImpactSounds>();
    }
}
