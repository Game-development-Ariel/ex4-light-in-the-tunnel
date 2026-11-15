using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Tools > Tunnel > 2 Setup Level: adds the game logic to the open Tutorial scene and connects it with events.
/// - The player: a CharacterController, hope, the bottle carrier and the respawn.
/// - The round rules, the bottle spawner, the mattress goal, the HUD and the end screen.
/// Run it after Build Tutorial Map. Running it again is safe: things that already exist are kept.
/// </summary>
public static class TunnelLevelSetup
{
    public const string LevelScenePath = "Assets/Scenes/Tutorial.unity";
    private const string BottlePrefabPath = "Assets/Prefabs/WaterBottle.prefab";

    // The trigger is a bit smaller than a tile, so the player has to really reach the bottle.
    private const float BottleTriggerRadius = 0.35f;

    // Extra size around the mattress tiles, so touching its edge counts.
    private const float GoalPadding = 0.4f;

    private const float PlayerRadius = 0.3f;

    // From the design document: in the tutorial the hero is hurt, so he walks 2 tiles per second instead of 3.
    private const float TutorialPlayerSpeed = 2f;

    // Unity's built-in layer that raycasts skip. The player is on it, so the camera's rays stop only at walls.
    private const int IgnoreRaycastLayer = 2;

    [MenuItem("Tools/Tunnel/2 Setup Level")]
    public static void Setup()
    {
        Scene level = SceneManager.GetActiveScene();
        if (level.path != LevelScenePath)
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

        EditorSceneManager.MarkSceneDirty(level);
        EditorSceneManager.SaveScene(level);
        Debug.Log("Tunnel level setup done and saved.");
    }

    /// <summary>The bottle's trigger becomes a 3D sphere, because the player is a CharacterController now.</summary>
    private static void SetupBottlePrefab()
    {
        GameObject bottle = PrefabUtility.LoadPrefabContents(BottlePrefabPath);
        Object.DestroyImmediate(bottle.GetComponent<Collider2D>());
        SphereCollider trigger = SetupTools.GetOrAdd<SphereCollider>(bottle);
        trigger.isTrigger = true;
        trigger.radius = BottleTriggerRadius;
        PrefabUtility.SaveAsPrefabAsset(bottle, BottlePrefabPath);
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
        controller.skinWidth = 0.02f;
        controller.minMoveDistance = 0f;

        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        SerializedObject fields = new SerializedObject(movement);
        fields.FindProperty("moveSpeed").floatValue = TutorialPlayerSpeed;
        fields.ApplyModifiedPropertiesWithoutUndo();

        SetupTools.GetOrAdd<PlayerPush>(player);

        // Hope changes the body: speed, sight (the soft light on the Body child) and hearing.
        HopeSenses senses = SetupTools.GetOrAdd<HopeSenses>(player);
        SetupTools.SetField(senses, "effects", TunnelAssets.HopeEffects);
        SetupTools.SetField(senses, "eyes", player.transform.Find("Body").GetComponent<UnityEngine.Rendering.Universal.Light2D>());
        SetupTools.SetField(senses, "tunnelLight", GameObject.Find("Global Light 2D").GetComponent<UnityEngine.Rendering.Universal.Light2D>());
        SetupTools.SetField(senses, "hintRequested", TunnelAssets.HintRequested);
        foreach (HeardSteps steps in Object.FindObjectsByType<HeardSteps>(FindObjectsSortMode.None))
        {
            SetupTools.SetField(steps, "listener", senses);
        }

        Hope hope = SetupTools.GetOrAdd<Hope>(player);
        SetupTools.SetField(hope, "settings", TunnelAssets.LevelSettings);

        BottleCarrier carrier = SetupTools.GetOrAdd<BottleCarrier>(player);
        SetupTools.SetField(carrier, "bottlePicked", TunnelAssets.BottlePicked);
        SetupTools.SetField(carrier, "playerCaught", TunnelAssets.PlayerCaught);

        PlayerRespawn respawn = SetupTools.GetOrAdd<PlayerRespawn>(player);
        SetupTools.SetField(respawn, "startPoint", GameObject.Find("PlayerStart").transform);
        SetupTools.SetField(respawn, "playerCaught", TunnelAssets.PlayerCaught);
        return player;
    }

    private static void SetupRound(GameObject player)
    {
        Object.DestroyImmediate(GameObject.Find("GameManager")); // Left from assignment 3.

        TunnelRound round = SetupTools.Recreate("Round").AddComponent<TunnelRound>();
        SetupTools.SetField(round, "settings", TunnelAssets.LevelSettings);
        SetupTools.SetField(round, "hope", player.GetComponent<Hope>());
        SetupTools.SetField(round, "playerCaught", TunnelAssets.PlayerCaught);
        SetupTools.SetField(round, "bottleDelivered", TunnelAssets.BottleDelivered);
        SetupTools.SetField(round, "levelWon", TunnelAssets.LevelWon);
        SetupTools.SetField(round, "levelLost", TunnelAssets.LevelLost);
    }

    private static void SetupSpawner()
    {
        BottleSpawner spawner = SetupTools.GetOrAdd<BottleSpawner>(GameObject.Find("BottleSpawn"));
        SetupTools.SetField(spawner, "bottlePrefab", AssetDatabase.LoadAssetAtPath<GameObject>(BottlePrefabPath));
        SetupTools.SetField(spawner, "playerCaught", TunnelAssets.PlayerCaught);
        SetupTools.SetField(spawner, "carrier", GameObject.Find("Player").GetComponent<BottleCarrier>());
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
