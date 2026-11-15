/// <summary>
/// The data assets of the tunnel game (events and settings), in one place for the setup tools.
/// Each one is created the first time it is asked for.
/// </summary>
public static class TunnelAssets
{
    private const string Folder = "Assets/Data/";

    public static GameEvent PlayerCaught => SetupTools.Asset<GameEvent>(Folder + "Events/PlayerCaught.asset");
    public static GameEvent BottlePicked => SetupTools.Asset<GameEvent>(Folder + "Events/BottlePicked.asset");
    public static GameEvent BottleDelivered => SetupTools.Asset<GameEvent>(Folder + "Events/BottleDelivered.asset");
    public static GameEvent LevelWon => SetupTools.Asset<GameEvent>(Folder + "Events/LevelWon.asset");
    public static GameEvent LevelLost => SetupTools.Asset<GameEvent>(Folder + "Events/LevelLost.asset");
    public static TextEvent HintRequested => SetupTools.Asset<TextEvent>(Folder + "Events/HintRequested.asset");
    public static TextEvent MemoryFound => SetupTools.Asset<TextEvent>(Folder + "Events/MemoryFound.asset");

    public static TunnelLevelSettings LevelSettings => SetupTools.Asset<TunnelLevelSettings>(Folder + "TutorialLevelSettings.asset");
    public static VisionStats CameraVision => SetupTools.Asset<VisionStats>(Folder + "SecurityCameraVision.asset");
    public static HopeEffects HopeEffects => SetupTools.Asset<HopeEffects>(Folder + "HopeEffects.asset");

    /// <summary>A crate is light, so it gets fast and needs a faster hit to be at full volume.</summary>
    public static ImpactSounds CrateSounds => Sounds("CrateSounds", new[] { "CrateHit1", "CrateHit2" }, new[] { "CrateTouch" }, 2f);

    /// <summary>A sandbag is heavy and slow, so a slower hit is already a loud one.</summary>
    public static ImpactSounds SandbagSounds => Sounds("SandbagSounds", new[] { "SandbagHit" }, new[] { "SandbagTouch" }, 1.4f);

    // The values are written only when the asset is new, so tuning it in the Inspector is kept.
    private static ImpactSounds Sounds(string name, string[] wallHitSounds, string[] touchSounds, float fullVolumeHitSpeed)
    {
        string path = Folder + name + ".asset";
        bool isNew = !System.IO.File.Exists(path);
        ImpactSounds sounds = SetupTools.Asset<ImpactSounds>(path);
        if (isNew)
        {
            sounds.wallHitSounds = wallHitSounds;
            sounds.touchSounds = touchSounds;
            sounds.fullVolumeHitSpeed = fullVolumeHitSpeed;
            UnityEditor.EditorUtility.SetDirty(sounds);
        }
        return sounds;
    }

    /// <summary>The camera in the storage room sees a shorter distance, so the room's entrance is safe.</summary>
    public static VisionStats RoomCameraVision => Vision("RoomCameraVision", 40f, 5f);

    /// <summary>A guard sees wider than a camera, but only as far as his flashlight.</summary>
    public static VisionStats GuardVision => Vision("GuardVision", 60f, 4.5f);

    // The values are written only when the asset is new, so tuning it in the Inspector is kept.
    private static VisionStats Vision(string name, float viewAngle, float viewDistance)
    {
        string path = Folder + name + ".asset";
        bool isNew = !System.IO.File.Exists(path);
        VisionStats stats = SetupTools.Asset<VisionStats>(path);
        if (isNew)
        {
            stats.viewAngle = viewAngle;
            stats.viewDistance = viewDistance;
            UnityEditor.EditorUtility.SetDirty(stats);
        }
        return stats;
    }
}
