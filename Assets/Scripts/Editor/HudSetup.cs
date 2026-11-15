using UnityEngine;

/// <summary>
/// Connects the UI to the game: the HUD, the end screen and the minimap get the objects and events they show.
/// The UI only listens and shows. What the game does (freezing the player, stopping the level) is in TunnelLevelSetup.
/// </summary>
public static class HudSetup
{
    public static void Setup(PlayerMovement player)
    {
        SetupHud(player.GetComponent<Hope>());
        SetupEndScreen(player.GetComponent<Hope>());
        SetupMinimap();
    }

    /// <summary>The HUD is a few small parts on one object, each showing one thing.</summary>
    private static void SetupHud(Hope hope)
    {
        Object.DestroyImmediate(GameObject.Find("StatusHud")); // The old one-class HUD, before it was split.
        GameObject hud = SetupTools.Recreate("HUD");
        hud.AddComponent<ScreenVignette>();

        SetupTools.SetField(hud.AddComponent<HopeBar>(), "hope", hope);

        WaterIndicator water = hud.AddComponent<WaterIndicator>();
        SetupTools.SetField(water, "bottlePicked", TunnelAssets.BottlePicked);
        SetupTools.SetField(water, "bottleDropped", TunnelAssets.BottleDropped);

        ObjectiveThought thought = hud.AddComponent<ObjectiveThought>();
        SetupTools.SetField(thought, "settings", TunnelAssets.LevelSettings);
        SetupTools.SetField(thought, "bottlePicked", TunnelAssets.BottlePicked);
        SetupTools.SetField(thought, "bottleDropped", TunnelAssets.BottleDropped);
        SetupTools.SetField(thought, "hintRequested", TunnelAssets.HintRequested);

        CaptureBlackout blackout = hud.AddComponent<CaptureBlackout>();
        SetupTools.SetField(blackout, "settings", TunnelAssets.LevelSettings);
        SetupTools.SetField(blackout, "playerCaught", TunnelAssets.PlayerCaught);
        SetupTools.SetField(blackout, "bottleDropped", TunnelAssets.BottleDropped);
        SetupTools.SetField(blackout, "hintRequested", TunnelAssets.HintRequested);
    }

    private static void SetupEndScreen(Hope hope)
    {
        EndScreen end = SetupTools.Recreate("EndScreen").AddComponent<EndScreen>();
        SetupTools.SetField(end, "levelWon", TunnelAssets.LevelWon);
        SetupTools.SetField(end, "levelLost", TunnelAssets.LevelLost);
        SetupTools.SetField(end, "settings", TunnelAssets.LevelSettings);
        SetupTools.SetField(end, "stats", Object.FindAnyObjectByType<RoundStats>());
        SetupTools.SetField(end, "hope", hope);
        SetupTools.SetArray(end, "hideWhenLevelEnds", new Object[] { GameObject.Find("HUD"), GameObject.Find("Tutorial UI") });
    }

    /// <summary>The minimap and the camera size both follow one ScreenSizeWatcher on the main camera.</summary>
    private static void SetupMinimap()
    {
        ScreenSizeWatcher screenSize = SetupTools.GetOrAdd<ScreenSizeWatcher>(Camera.main.gameObject);
        Minimap minimap = Object.FindAnyObjectByType<Minimap>();
        SetupTools.SetField(minimap, "screenSize", screenSize);
        SetupTools.GetOrAdd<MinimapToggle>(minimap.gameObject);
    }
}
