using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Tools > Tunnel > 3 Setup Game Feel: adds the mood, the sound and the teaching of the tutorial to the open Tutorial scene.
/// It does the UI sprites, the bottle and the player itself, and calls one setup for each of the other parts:
/// - LightSetup: a dim tunnel, weak flickering bulbs, light under the cell door, dust floating in the light.
/// - MoodSetup: the post-processing, which MoodVolume changes with the hero's feelings.
/// - SoundSetup: the tunnel, the music, the heartbeat and the breathing.
/// - TeachingSetup: the opening, the hero's thoughts, the hint zones, the memories and the quiet place.
/// - HudSetup: connects the UI to the game (the HUD, the end screen, the minimap).
/// The player gets no flashlight yet (from the design, it is found later), soft footsteps and a shadow.
/// Run it after Setup Level. Running it again is safe: it rebuilds its own objects.
/// </summary>
public static class GameFeelSetup
{
    private const string UiFolder = "Assets/Resources/UI/";

    [MenuItem("Tools/Tunnel/3 Setup Game Feel")]
    public static void Setup()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Stop the game first (the Play button). Changes made while playing are lost when it stops.");
            return;
        }

        Scene level = SceneManager.GetActiveScene();
        if (level.path != TunnelAssets.ScenePath)
        {
            Debug.LogError("Open the Tutorial scene first.");
            return;
        }

        ImportUiSprites();
        SetupBottlePrefab();
        PlayerMovement player = Object.FindAnyObjectByType<PlayerMovement>();
        SetupPlayer(player);
        LightSetup.Setup();
        Tension tension = SoundSetup.Setup(player);
        MoodSetup.Setup(tension, player.GetComponent<Hope>());
        TeachingSetup.Setup();
        HudSetup.Setup(player);
        TunnelLevelSetup.WireLevelEnd(); // The sound was just rebuilt, so the level's end gets the new one.

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
        GameObject bottle = PrefabUtility.LoadPrefabContents(TunnelAssets.BottlePrefabPath);
        Object.DestroyImmediate(bottle.GetComponent<Bob>());
        bottle.AddComponent<Bob>();

        SpriteRenderer glow = ChildSprite(bottle.transform, "Glow", "Glow", new Color(0.6f, 0.78f, 1f, 0.3f), 3);
        glow.transform.localScale = Vector3.one * 1.6f;
        SpriteRenderer shadow = ChildSprite(bottle.transform, "Shadow", "Shadow", new Color(1f, 1f, 1f, 0.55f), 4);
        shadow.transform.localPosition = new Vector3(0f, -0.38f, 0f);
        shadow.transform.localScale = new Vector3(0.9f, 0.7f, 1f);

        PrefabUtility.SaveAsPrefabAsset(bottle, TunnelAssets.BottlePrefabPath);
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
        SetupTools.SetField(bounce, "movement", player);

        SpriteRenderer shadow = ChildSprite(player.transform, "Shadow", "Shadow", new Color(1f, 1f, 1f, 0.6f), 9);
        shadow.transform.localPosition = new Vector3(0f, -0.18f, 0f);
        shadow.transform.localScale = new Vector3(1.2f, 0.9f, 1f);
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
