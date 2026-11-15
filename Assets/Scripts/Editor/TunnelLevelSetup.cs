using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Tools > Tunnel > 2 Setup Level: adds the game logic to the open Tutorial scene and connects it with events.
/// - The player: a CharacterController, hope and what it does to the body, the bottle carrier and the respawn.
/// - The round: its rules, its stats and the freeze, the bottle spawner and the mattress goal.
/// Run it after Build Tutorial Map. Running it again is safe: things that already exist are kept.
/// </summary>
public static class TunnelLevelSetup
{
    // The trigger is a bit smaller than a tile, so the player has to really reach the bottle.
    private const float BottleTriggerRadius = 0.35f;

    // Extra size around the mattress tiles, so touching its edge counts.
    private const float GoalPadding = 0.4f;

    // The player's ball, in tiles, and the thin space it keeps from walls so it doesn't get stuck in them.
    private const float PlayerRadius = 0.3f;
    private const float PlayerSkinWidth = 0.02f;

    // From the design document: in the tutorial the hero is hurt, so his base speed is 2 tiles per second
    // instead of 3. HopeSenses multiplies it by HopeEffects.speedFactor, so the real speed changes with hope.
    private const float TutorialPlayerSpeed = 2f;

    // PlayerMovement's own default. If the scene still has it, the speed was never set for the tutorial.
    private const float UntunedPlayerSpeed = 5f;

    // Unity's built-in layer that raycasts skip. The player is on it, so the camera's rays stop only at walls.
    private const int IgnoreRaycastLayer = 2;

    [MenuItem("Tools/Tunnel/2 Setup Level")]
    public static void Setup()
    {
        Scene level = SceneManager.GetActiveScene();
        if (level.path != TunnelAssets.ScenePath)
        {
            Debug.LogError("Open the Tutorial scene first.");
            return;
        }

        SetupBottlePrefab();
        GameObject player = SetupPlayer();
        SetupRound(player);
        SetupSpawner();
        SetupMattressGoal();
        SetupCameraShake();
        WireLevelEnd();

        EditorSceneManager.MarkSceneDirty(level);
        EditorSceneManager.SaveScene(level);
        Debug.Log("Tunnel level setup done and saved.");
    }

    /// <summary>
    /// What LevelFreeze turns off when the level ends. The sound is made by Setup Game Feel,
    /// so that tool calls this again after it rebuilds the sound.
    /// </summary>
    public static void WireLevelEnd()
    {
        LevelFreeze freeze = Object.FindAnyObjectByType<LevelFreeze>();
        var stop = new System.Collections.Generic.List<Object> { GameObject.Find("Player"), GameObject.Find("Watchers") };
        GameObject sound = GameObject.Find("Sound");
        if (sound != null)
        {
            stop.Add(sound);
        }
        SetupTools.SetArray(freeze, "stopWhenLevelEnds", stop.ToArray());
    }

    /// <summary>The bottle's trigger becomes a 3D sphere, because the player is a CharacterController now.</summary>
    private static void SetupBottlePrefab()
    {
        GameObject bottle = PrefabUtility.LoadPrefabContents(TunnelAssets.BottlePrefabPath);
        Object.DestroyImmediate(bottle.GetComponent<Collider2D>());
        SphereCollider trigger = SetupTools.GetOrAdd<SphereCollider>(bottle);
        trigger.isTrigger = true;
        trigger.radius = BottleTriggerRadius;
        PrefabUtility.SaveAsPrefabAsset(bottle, TunnelAssets.BottlePrefabPath);
        PrefabUtility.UnloadPrefabContents(bottle);
    }

    private static GameObject SetupPlayer()
    {
        GameObject player = GameObject.Find("Player");
        player.layer = IgnoreRaycastLayer;

        // The old 2D physics of the player is replaced by the CharacterController.
        Object.DestroyImmediate(player.GetComponent<Collider2D>());
        Object.DestroyImmediate(player.GetComponent<Rigidbody2D>());

        // Height = 2 * radius makes the capsule a ball, which fits a flat top-down map.
        CharacterController controller = SetupTools.GetOrAdd<CharacterController>(player);
        controller.radius = PlayerRadius;
        controller.height = PlayerRadius * 2f;
        controller.stepOffset = 0f; // Nothing to climb on a flat map.
        controller.skinWidth = PlayerSkinWidth;
        controller.minMoveDistance = 0f;

        // The speed is written only if it was never set, so a speed tuned in the Inspector is kept.
        SerializedObject fields = new SerializedObject(player.GetComponent<PlayerMovement>());
        SerializedProperty speed = fields.FindProperty("moveSpeed");
        if (Mathf.Approximately(speed.floatValue, UntunedPlayerSpeed))
        {
            speed.floatValue = TutorialPlayerSpeed;
            fields.ApplyModifiedPropertiesWithoutUndo();
        }

        SetupTools.GetOrAdd<PlayerPush>(player);

        Hope hope = SetupTools.GetOrAdd<Hope>(player);
        SetupTools.SetField(hope, "settings", TunnelAssets.LevelSettings);

        // Hope changes the body: speed, sight (the soft light on the Body child) and hearing.
        HopeSenses senses = SetupTools.GetOrAdd<HopeSenses>(player);
        SetupTools.SetField(senses, "effects", TunnelAssets.HopeEffects);
        SetupTools.SetField(senses, "eyes", player.transform.Find("Body").GetComponent<Light2D>());
        SetupTools.SetField(senses, "tunnelLight", GameObject.Find("Global Light 2D").GetComponent<Light2D>());
        foreach (HeardSteps steps in Object.FindObjectsByType<HeardSteps>(FindObjectsSortMode.None))
        {
            SetupTools.SetField(steps, "heroSenses", senses);
        }

        HopeThoughts thoughts = SetupTools.GetOrAdd<HopeThoughts>(player);
        SetupTools.SetField(thoughts, "effects", TunnelAssets.HopeEffects);
        SetupTools.SetField(thoughts, "hintRequested", TunnelAssets.HintRequested);

        BottleCarrier carrier = SetupTools.GetOrAdd<BottleCarrier>(player);
        SetupTools.SetField(carrier, "bottlePicked", TunnelAssets.BottlePicked);
        SetupTools.SetField(carrier, "bottleDropped", TunnelAssets.BottleDropped);
        SetupTools.SetField(carrier, "playerCaught", TunnelAssets.PlayerCaught);

        PlayerRespawn respawn = SetupTools.GetOrAdd<PlayerRespawn>(player);
        SetupTools.SetField(respawn, "startPoint", GameObject.Find("PlayerStart").transform);
        SetupTools.SetField(respawn, "playerCaught", TunnelAssets.PlayerCaught);
        return player;
    }

    /// <summary>The Round object holds the level's rules, its stats, and the freeze that decides when it is played.</summary>
    private static void SetupRound(GameObject player)
    {
        Object.DestroyImmediate(GameObject.Find("GameManager")); // Left from assignment 3.
        GameObject roundObject = SetupTools.Recreate("Round");

        TunnelRound round = roundObject.AddComponent<TunnelRound>();
        SetupTools.SetField(round, "settings", TunnelAssets.LevelSettings);
        SetupTools.SetField(round, "hope", player.GetComponent<Hope>());
        SetupTools.SetField(round, "playerCaught", TunnelAssets.PlayerCaught);
        SetupTools.SetField(round, "bottleDelivered", TunnelAssets.BottleDelivered);
        SetupTools.SetField(round, "levelWon", TunnelAssets.LevelWon);
        SetupTools.SetField(round, "levelLost", TunnelAssets.LevelLost);

        RoundStats stats = roundObject.AddComponent<RoundStats>();
        SetupTools.SetField(stats, "openingFinished", TunnelAssets.OpeningFinished);
        SetupTools.SetField(stats, "playerCaught", TunnelAssets.PlayerCaught);
        SetupTools.SetField(stats, "levelWon", TunnelAssets.LevelWon);
        SetupTools.SetField(stats, "levelLost", TunnelAssets.LevelLost);

        LevelFreeze freeze = roundObject.AddComponent<LevelFreeze>();
        SetupTools.SetField(freeze, "settings", TunnelAssets.LevelSettings);
        SetupTools.SetField(freeze, "player", player.GetComponent<PlayerMovement>());
        SetupTools.SetField(freeze, "watchers", GameObject.Find("Watchers").transform);
        SetupTools.SetField(freeze, "openingFinished", TunnelAssets.OpeningFinished);
        SetupTools.SetField(freeze, "playerCaught", TunnelAssets.PlayerCaught);
        SetupTools.SetField(freeze, "levelWon", TunnelAssets.LevelWon);
        SetupTools.SetField(freeze, "levelLost", TunnelAssets.LevelLost);
    }

    private static void SetupSpawner()
    {
        BottleSpawner spawner = SetupTools.GetOrAdd<BottleSpawner>(GameObject.Find("BottleSpawn"));
        SetupTools.SetField(spawner, "bottlePrefab", TunnelAssets.BottlePrefab);
        SetupTools.SetField(spawner, "bottleDropped", TunnelAssets.BottleDropped);
    }

    /// <summary>Puts a trigger box over the mattress tiles of the Decor tilemap.</summary>
    private static void SetupMattressGoal()
    {
        Tilemap decor = GameObject.Find("TunnelGrid/Decor").GetComponent<Tilemap>();
        bool found = false;
        Bounds mattress = new Bounds();
        foreach (Vector3Int cell in decor.cellBounds.allPositionsWithin)
        {
            TileBase tile = decor.GetTile(cell);
            if (tile == null || !tile.name.StartsWith("Mattress"))
            {
                continue;
            }
            Bounds tileBounds = new Bounds(decor.GetCellCenterWorld(cell), decor.cellSize);
            if (found)
            {
                mattress.Encapsulate(tileBounds);
            }
            else
            {
                mattress = tileBounds;
                found = true;
            }
        }

        GameObject goal = SetupTools.Recreate("MattressGoal");
        goal.transform.position = mattress.center;
        BoxCollider box = goal.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(mattress.size.x + GoalPadding, mattress.size.y + GoalPadding, 1f);
        SetupTools.SetField(goal.AddComponent<MattressGoal>(), "bottleDelivered", TunnelAssets.BottleDelivered);
    }

    private static void SetupCameraShake()
    {
        ScreenShake shake = SetupTools.GetOrAdd<ScreenShake>(Camera.main.gameObject);
        SetupTools.SetField(shake, "shakeOn", TunnelAssets.PlayerCaught);
    }
}
