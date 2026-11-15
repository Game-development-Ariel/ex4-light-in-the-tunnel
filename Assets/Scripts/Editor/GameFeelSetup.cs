using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Tools > Tunnel > 3 Setup Game Feel: adds the mood, the sound and the teaching of the tutorial to the open Tutorial scene.
/// - Light: a dim tunnel, weak flickering bulbs, light under the cell door, dust floating in the light.
/// - Post-processing: soft glow, film grain, dark edges and faded colors, which MoodVolume changes with the hero's feelings.
/// - The player: no flashlight yet (from the design, it is found later), soft footsteps, a shadow.
/// - Sound: the tunnel, the music, the heartbeat and the breathing.
/// - Teaching: the opening, the hero's thoughts, the hint zones, and two memories in the cell.
/// The last step also connects the UI to the game: the HUD and the end screen get the objects and events they listen to.
/// Run it after Setup Level. Running it again is safe: it rebuilds its own objects.
/// </summary>
public static class GameFeelSetup
{
    private const string BottlePrefabPath = "Assets/Prefabs/WaterBottle.prefab";
    private const string UiFolder = "Assets/Resources/UI/";
    private const string DustMaterialPath = "Assets/Materials/Dust.mat";
    private const string MoodProfilePath = "Assets/Settings/TunnelMood.asset";
    private const string LitSpriteShader = "Universal Render Pipeline/2D/Sprite-Lit-Default";

    private static readonly Color BulbColor = new Color(1f, 0.74f, 0.48f);

    [MenuItem("Tools/Tunnel/3 Setup Game Feel")]
    public static void Setup()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Stop the game first (the Play button). Changes made while playing are lost when it stops.");
            return;
        }

        Scene level = SceneManager.GetActiveScene();
        if (level.path != TunnelLevelSetup.LevelScenePath)
        {
            Debug.LogError("Open the Tutorial scene first.");
            return;
        }

        ImportUiSprites();
        SetupBottlePrefab();
        PlayerMovement player = Object.FindAnyObjectByType<PlayerMovement>();
        SetupPlayer(player);
        SetupLights();
        Tension tension = SetupSound(player);
        SetupPostProcessing(tension, player.GetComponent<Hope>());
        HintToast hints = SetupTeaching(player);
        SetupHud(player, hints);
        SetupEndScreen(player);

        // Saves right away, so the result is never lost by forgetting Cmd+S.
        EditorSceneManager.MarkSceneDirty(level);
        EditorSceneManager.SaveScene(level);
        Debug.Log("Game feel setup done and saved.");
    }

    // ---------- Sprites ----------

    /// <summary>
    /// The UI sprites are smooth (Bilinear). The frames get a border, so a sliced Image stretches
    /// only their middle and keeps the rounded corners.
    /// </summary>
    private static void ImportUiSprites()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { UiFolder.TrimEnd('/') }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);

            float border = 0f;
            if (name == "Panel" || name == "Button")
            {
                border = 16f;
            }
            else if (name == "Key")
            {
                border = 12f;
            }
            else if (name.StartsWith("Bar"))
            {
                border = 7f;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.spriteBorder = Vector4.one * border;
            importer.SaveAndReimport();
        }
    }

    // ---------- Objects ----------

    /// <summary>The bottle floats a little, has a faint cold glow, and a soft shadow on the floor.</summary>
    private static void SetupBottlePrefab()
    {
        GameObject bottle = PrefabUtility.LoadPrefabContents(BottlePrefabPath);
        Object.DestroyImmediate(bottle.GetComponent<Bob>());
        bottle.AddComponent<Bob>();

        SpriteRenderer glow = ChildSprite(bottle.transform, "Glow", "Glow", new Color(0.6f, 0.78f, 1f, 0.3f), 3);
        glow.transform.localScale = Vector3.one * 1.6f;
        SpriteRenderer shadow = ChildSprite(bottle.transform, "Shadow", "Shadow", new Color(1f, 1f, 1f, 0.55f), 4);
        shadow.transform.localPosition = new Vector3(0f, -0.38f, 0f);
        shadow.transform.localScale = new Vector3(0.9f, 0.7f, 1f);

        PrefabUtility.SaveAsPrefabAsset(bottle, BottlePrefabPath);
        PrefabUtility.UnloadPrefabContents(bottle);
    }

    /// <summary>
    /// The hero has no flashlight yet, only eyes that got used to the dark: a soft, small, warm halo.
    /// Soft footsteps, a gentle walking bounce, and a shadow that grounds the character on the floor.
    /// </summary>
    private static void SetupPlayer(PlayerMovement player)
    {
        Transform body = player.transform.Find("Body");

        Transform flashlight = body.Find("Flashlight");
        if (flashlight != null)
        {
            flashlight.gameObject.SetActive(false);
        }
        Light2D halo = body.GetComponent<Light2D>();
        if (halo != null)
        {
            halo.pointLightOuterRadius = 3.4f;
            halo.pointLightInnerRadius = 0.4f;
            halo.intensity = 0.75f;
            halo.color = new Color(1f, 0.88f, 0.74f);
        }

        Object.DestroyImmediate(player.GetComponent<Footsteps>());
        Footsteps footsteps = player.gameObject.AddComponent<Footsteps>(); // Also adds the AudioSource it needs.
        footsteps.GetComponent<AudioSource>().playOnAwake = false;

        Object.DestroyImmediate(body.GetComponent<WalkBounce>());
        WalkBounce bounce = body.gameObject.AddComponent<WalkBounce>();
        SetupTools.SetField(bounce, "body", player.GetComponent<CharacterController>());

        SpriteRenderer shadow = ChildSprite(player.transform, "Shadow", "Shadow", new Color(1f, 1f, 1f, 0.6f), 9);
        shadow.transform.localPosition = new Vector3(0f, -0.18f, 0f);
        shadow.transform.localScale = new Vector3(1.2f, 0.9f, 1f);
    }

    // ---------- Light ----------

    /// <summary>
    /// The tunnel is almost dark. A few weak bulbs show the way, each flickering on its own.
    /// The storage room is a little warmer and brighter: the place of water, the place of hope.
    /// Dust floats in the light of the rooms. Positions are world coordinates of the tutorial map.
    /// </summary>
    private static void SetupLights()
    {
        GameObject globalLight = GameObject.Find("Global Light 2D");
        if (globalLight != null)
        {
            Light2D ambient = globalLight.GetComponent<Light2D>();
            ambient.intensity = 0.24f;
            ambient.color = new Color(0.55f, 0.6f, 0.78f);
        }

        GameObject bulbs = SetupTools.Recreate("Bulbs");
        AddBulb(bulbs.transform, "Cell", new Vector2(-5.5f, 2f), 4.5f, 0.55f, BulbColor);
        AddBulb(bulbs.transform, "Under the Door", new Vector2(-0.5f, 0.5f), 1.6f, 0.5f, BulbColor);
        AddBulb(bulbs.transform, "Corridor", new Vector2(6f, 0.5f), 4.5f, 0.7f, BulbColor);
        AddBulb(bulbs.transform, "Way Down", new Vector2(10.5f, -3f), 4f, 0.6f, BulbColor);
        AddBulb(bulbs.transform, "Waiting Spot", new Vector2(15.5f, -6.5f), 3.2f, 0.4f, BulbColor);
        AddBulb(bulbs.transform, "Storage Room", new Vector2(22f, 0f), 6f, 0.95f, new Color(1f, 0.8f, 0.56f));

        Material dust = DustMaterial();
        GameObject dustParent = SetupTools.Recreate("Dust");
        AddDust(dustParent.transform, new Vector2(-6f, 0.5f), new Vector2(8f, 6f), dust);
        AddDust(dustParent.transform, new Vector2(6f, 0.5f), new Vector2(10f, 2.5f), dust);
        AddDust(dustParent.transform, new Vector2(22f, 0.5f), new Vector2(8f, 6f), dust);
    }

    private static void AddBulb(Transform parent, string name, Vector2 position, float radius, float intensity, Color color)
    {
        var bulb = new GameObject(name);
        bulb.transform.SetParent(parent, false);
        bulb.transform.position = position;
        Light2D light = bulb.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.pointLightOuterRadius = radius;
        light.pointLightInnerRadius = radius * 0.15f;
        light.intensity = intensity;
        light.color = color;
        bulb.AddComponent<LightFlicker>();
    }

    private static void AddDust(Transform parent, Vector2 position, Vector2 area, Material material)
    {
        var dust = new GameObject("Dust");
        dust.transform.SetParent(parent, false);
        dust.transform.position = position;
        DustMotes motes = dust.AddComponent<DustMotes>(); // Also adds the ParticleSystem it needs.
        SetupTools.SetField(motes, "material", material);
        SerializedObject fields = new SerializedObject(motes);
        fields.FindProperty("area").vector2Value = area;
        fields.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// A material that the 2D lights light up, with the soft glow picture.
    /// It is saved as an asset, so the build includes its shader.
    /// </summary>
    private static Material DustMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(DustMaterialPath);
        if (material == null)
        {
            material = new Material(Shader.Find(LitSpriteShader));
            System.IO.Directory.CreateDirectory("Assets/Materials");
            AssetDatabase.CreateAsset(material, DustMaterialPath);
        }
        material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(UiFolder + "Glow.png");
        EditorUtility.SetDirty(material);
        return material;
    }

    // ---------- Post-processing ----------

    /// <summary>
    /// A global Volume with the tunnel's look. The camera must have post-processing turned on to show it.
    /// MoodVolume changes the values while the game runs.
    /// </summary>
    private static void SetupPostProcessing(Tension tension, Hope hope)
    {
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(MoodProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            System.IO.Directory.CreateDirectory("Assets/Settings");
            AssetDatabase.CreateAsset(profile, MoodProfilePath);

            Bloom bloom = AddEffect<Bloom>(profile);
            bloom.threshold.Override(0.8f);
            bloom.intensity.Override(0.7f);
            bloom.scatter.Override(0.7f);

            Vignette vignette = AddEffect<Vignette>(profile);
            vignette.intensity.Override(0.36f);
            vignette.smoothness.Override(0.5f);

            FilmGrain grain = AddEffect<FilmGrain>(profile);
            grain.type.Override(FilmGrainLookup.Medium3);
            grain.intensity.Override(0.35f);

            ColorAdjustments colors = AddEffect<ColorAdjustments>(profile);
            colors.contrast.Override(14f);
            colors.saturation.Override(-22f);
            colors.postExposure.Override(0f);
            colors.colorFilter.Override(new Color(1f, 0.97f, 0.92f));

            ChromaticAberration split = AddEffect<ChromaticAberration>(profile);
            split.intensity.Override(0f);
            AssetDatabase.SaveAssets();
        }

        GameObject volumeObject = SetupTools.Recreate("Mood");
        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = profile;
        MoodVolume mood = volumeObject.AddComponent<MoodVolume>();
        SetupTools.SetField(mood, "tension", tension);
        SetupTools.SetField(mood, "hope", hope);
        SetupTools.SetField(mood, "playerCaught", TunnelAssets.PlayerCaught);

        Camera.main.GetUniversalAdditionalCameraData().renderPostProcessing = true;
    }

    /// <summary>Each effect is saved inside the profile asset, so it isn't lost when Unity closes.</summary>
    private static T AddEffect<T>(VolumeProfile profile) where T : VolumeComponent
    {
        T effect = profile.Add<T>(true);
        effect.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
        AssetDatabase.AddObjectToAsset(effect, profile);
        return effect;
    }

    // ---------- Sound ----------

    private static Tension SetupSound(PlayerMovement player)
    {
        Object.DestroyImmediate(GameObject.Find("Ambience"));
        GameObject sound = SetupTools.Recreate("Sound");

        AudioSource ambience = LoopingSource(sound.transform, "Ambience", "Ambience", 0.35f);
        ambience.playOnAwake = true;

        AudioSource music = LoopingSource(sound.transform, "Music", "Music", 0f);
        music.playOnAwake = false;
        music.gameObject.AddComponent<MusicPlayer>();

        // The heartbeat and the breathing play all the time. Tension sets how loud they are.
        AudioSource heartbeat = LoopingSource(sound.transform, "Heartbeat", "Heartbeat", 0f);
        AudioSource breathing = LoopingSource(sound.transform, "Breathing", "Breathing", 0.2f);
        Tension tension = sound.AddComponent<Tension>();
        SetupTools.SetField(tension, "heartbeat", heartbeat);
        SetupTools.SetField(tension, "breathing", breathing);
        SetupTools.SetField(tension, "player", player.transform);
        return tension;
    }

    private static AudioSource LoopingSource(Transform parent, string name, string clip, float volume)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent, false);
        AudioSource source = child.AddComponent<AudioSource>();
        source.clip = Resources.Load<AudioClip>("Audio/" + clip);
        source.loop = true;
        source.playOnAwake = true;
        source.volume = volume;
        return source;
    }

    // ---------- Teaching ----------

    /// <summary>
    /// The opening, the hints and the objective arrow, then the hint zones and the memories on the map.
    /// The positions are world coordinates of the tutorial map (see the layout in TutorialMapBuilder).
    /// </summary>
    private static HintToast SetupTeaching(PlayerMovement player)
    {
        GameObject ui = SetupTools.Recreate("Tutorial UI");
        HintToast hints = ui.AddComponent<HintToast>();
        SetupTools.SetField(hints, "hintRequested", TunnelAssets.HintRequested);
        SetupTools.SetField(ui.AddComponent<MemoryCaption>(), "memoryFound", TunnelAssets.MemoryFound);
        ui.AddComponent<TitleCard>();
        ui.AddComponent<MobileJoystick>();
        ObjectiveMarker marker = ui.AddComponent<ObjectiveMarker>();
        SetupTools.SetField(marker, "mattress", GameObject.Find("MattressGoal").transform);
        SetupTools.SetField(marker, "carrier", player.GetComponent<BottleCarrier>());

        GameObject zones = SetupTools.Recreate("Teaching Zones");
        foreach (string old in new[] { "Hint Door", "Hint Camera", "Hint Wait" })
        {
            Object.DestroyImmediate(GameObject.Find(old)); // Left from an older version of this tool.
        }

        AddZone<MemoryZone>(zones.transform, "Memory: Note", new Vector2(-8.5f, 1.5f), new Vector2(0.8f, 0.8f), "memory",
            "A scrap of paper under the mattress.\nI wrote my family's names on it, so I won't forget their voices.", 15);
        AddZone<MemoryZone>(zones.transform, "Memory: Wall", new Vector2(-4f, 3.5f), new Vector2(2f, 1f), "memory",
            "Lines scratched into the wall. Someone counted days here before me.\nI'll count too. And I'll go home.", 10);
        AddZone<HintZone>(zones.transform, "Hint: Door", new Vector2(-2f, 1f), new Vector2(1f, 4f), "hint",
            "The door isn't locked all the way. I lean on it, and it gives.\nSteps outside. A guard. I wait until he walks away.", 0);
        AddZone<HintZone>(zones.transform, "Hint: Guard", new Vector2(1f, 0.5f), new Vector2(1f, 3f), "hint",
            "His flashlight shows where he looks. I stay behind him,\nor wait in a dark corner of the wall until he passes.", 0);
        AddZone<HintZone>(zones.transform, "Hint: Room Camera", new Vector2(21f, -2.5f), new Vector2(2f, 1f), "hint",
            "Another eye, in the ceiling of the storage room.\nI wait here until it looks away. Then straight to the water.", 0);
        AddZone<MemoryZone>(zones.transform, "Memory: Cup", new Vector2(23.5f, 1.7f), new Vector2(1.6f, 1f), "memory",
            "A tin cup on the table.\nAt home the kettle is always on, and someone is waiting to pour.", 15);
        AddQuietPlace(zones.transform, new Vector2(2.5f, -1.5f));
        AddZone<HintZone>(zones.transform, "Hint: Camera", new Vector2(10.5f, -3.5f), new Vector2(3f, 1f), "hint",
            "A camera. Its pale light is its eye.\nIf the light touches me, they'll know.", 0);
        AddZone<HintZone>(zones.transform, "Hint: Wait", new Vector2(18.5f, -6.5f), new Vector2(1f, 3f), "hint",
            "I wait here, in the dark, and watch the light.\nWhen it turns away, I go up. Quietly.", 0);
        return hints;
    }

    // ---------- UI wiring ----------

    /// <summary>The HUD gets the game objects it shows and the events it listens to.</summary>
    private static void SetupHud(PlayerMovement player, HintToast hints)
    {
        StatusHud hud = SetupTools.Recreate("StatusHud").AddComponent<StatusHud>();
        SetupTools.SetField(hud, "hope", player.GetComponent<Hope>());
        SetupTools.SetField(hud, "carrier", player.GetComponent<BottleCarrier>());
        SetupTools.SetField(hud, "player", player);
        SetupTools.SetField(hud, "hints", hints);
        SetupTools.SetField(hud, "playerCaught", TunnelAssets.PlayerCaught);
        SetupTools.SetField(hud, "bottlePicked", TunnelAssets.BottlePicked);
    }

    private static void SetupEndScreen(PlayerMovement player)
    {
        EndScreen end = SetupTools.Recreate("EndScreen").AddComponent<EndScreen>();
        SetupTools.SetField(end, "levelWon", TunnelAssets.LevelWon);
        SetupTools.SetField(end, "levelLost", TunnelAssets.LevelLost);
        SetupTools.SetField(end, "round", Object.FindAnyObjectByType<TunnelRound>());
        SetupTools.SetField(end, "hope", player.GetComponent<Hope>());
        SetupTools.SetArray(end, "hideWhenLevelEnds", new Object[]
        {
            player.gameObject, GameObject.Find("Sound"), GameObject.Find("StatusHud"),
            GameObject.Find("Tutorial UI"), GameObject.Find("Watchers"),
        });
    }

    /// <summary>
    /// The dark corner in the corridor wall is also a quiet place: standing still in it gives hope.
    /// A faint warm light marks it, so the player is drawn to step in.
    /// </summary>
    private static void AddQuietPlace(Transform parent, Vector2 position)
    {
        var place = new GameObject("Quiet Place");
        place.transform.SetParent(parent, false);
        place.transform.position = position;
        BoxCollider box = place.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(0.9f, 0.9f, 1f);

        QuietPlace quiet = place.AddComponent<QuietPlace>();
        SetupTools.SetField(quiet, "memoryFound", TunnelAssets.MemoryFound);
        SerializedObject fields = new SerializedObject(quiet);
        fields.FindProperty("verse").stringValue = "\"Hope in the Lord; be strong, and let your heart take courage.\"\nI say it without a sound. Something in me stands up again.";
        fields.ApplyModifiedPropertiesWithoutUndo();

        AddBulb(place.transform, "Glow", position, 1.4f, 0.45f, new Color(1f, 0.85f, 0.6f));
    }

    private static void AddZone<T>(Transform parent, string name, Vector2 position, Vector2 size, string textField, string text, int hope) where T : Component
    {
        var zone = new GameObject(name);
        zone.transform.SetParent(parent, false);
        zone.transform.position = position;
        BoxCollider box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(size.x, size.y, 1f);

        T component = zone.AddComponent<T>();
        if (component is MemoryZone)
        {
            SetupTools.SetField(component, "memoryFound", TunnelAssets.MemoryFound);
        }
        else
        {
            SetupTools.SetField(component, "hintRequested", TunnelAssets.HintRequested);
        }

        SerializedObject fields = new SerializedObject(component);
        fields.FindProperty(textField).stringValue = text;
        if (hope > 0)
        {
            fields.FindProperty("hope").intValue = hope;
        }
        fields.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------- Helpers ----------

    /// <summary>Finds or makes a child with a sprite from the UI folder.</summary>
    private static SpriteRenderer ChildSprite(Transform parent, string name, string sprite, Color color, int sortingOrder)
    {
        Transform child = parent.Find(name);
        if (child == null)
        {
            child = new GameObject(name).transform;
            child.SetParent(parent, false);
        }
        SpriteRenderer spriteRenderer = child.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = child.gameObject.AddComponent<SpriteRenderer>();
        }
        spriteRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(UiFolder + sprite + ".png");
        spriteRenderer.color = color;
        spriteRenderer.sortingOrder = sortingOrder;
        return spriteRenderer;
    }
}
