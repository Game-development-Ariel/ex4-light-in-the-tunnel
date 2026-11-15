using UnityEditor;
using UnityEngine;

/// <summary>
/// The teaching of the tutorial: the opening, the hints, the memory captions and the objective arrow,
/// and on the map the hint zones, the memories and the quiet place.
/// </summary>
public static class TeachingSetup
{
    // Where each zone is on the tutorial map (its center), and its size, in world units (one unit is one tile).
    private static readonly Vector2 NoteMemoryPosition = new Vector2(-8.5f, 1.5f);
    private static readonly Vector2 NoteMemorySize = new Vector2(0.8f, 0.8f);
    private static readonly Vector2 WallMemoryPosition = new Vector2(-4f, 3.5f);
    private static readonly Vector2 WallMemorySize = new Vector2(2f, 1f);
    private static readonly Vector2 DoorHintPosition = new Vector2(-2f, 1f);
    private static readonly Vector2 DoorHintSize = new Vector2(1f, 4f);
    private static readonly Vector2 GuardHintPosition = new Vector2(1f, 0.5f);
    private static readonly Vector2 GuardHintSize = new Vector2(1f, 3f);
    private static readonly Vector2 RoomCameraHintPosition = new Vector2(21f, -2.5f);
    private static readonly Vector2 RoomCameraHintSize = new Vector2(2f, 1f);
    private static readonly Vector2 CupMemoryPosition = new Vector2(23.5f, 1.7f);
    private static readonly Vector2 CupMemorySize = new Vector2(1.6f, 1f);
    private static readonly Vector2 CameraHintPosition = new Vector2(10.5f, -3.5f);
    private static readonly Vector2 CameraHintSize = new Vector2(3f, 1f);
    private static readonly Vector2 WaitHintPosition = new Vector2(18.5f, -6.5f);
    private static readonly Vector2 WaitHintSize = new Vector2(1f, 3f);

    // How much hope each memory gives when it is found. Hints give none (0).
    private const int NoteMemoryHope = 15;
    private const int WallMemoryHope = 10;
    private const int CupMemoryHope = 15;

    // The quiet place in the corridor wall: where it is and its size, in world units, and its faint warm light.
    private static readonly Vector2 QuietPlacePosition = new Vector2(2.5f, -1.5f);
    private static readonly Vector3 QuietPlaceSize = new Vector3(0.9f, 0.9f, 1f);
    private const float QuietGlowRadius = 1.4f;
    private const float QuietGlowIntensity = 0.45f;
    private static readonly Color QuietGlowColor = new Color(1f, 0.85f, 0.6f);

    /// <summary>
    /// The opening, the hints and the objective arrow, then the hint zones and the memories on the map.
    /// The positions are world coordinates of the tutorial map (see the layout in TutorialLayout).
    /// </summary>
    public static void Setup()
    {
        GameObject ui = SetupTools.Recreate("Tutorial UI");
        HintToast hints = ui.AddComponent<HintToast>();
        SetupTools.SetField(hints, "hintRequested", TunnelAssets.HintRequested);
        SetupTools.SetField(ui.AddComponent<MemoryCaption>(), "memoryFound", TunnelAssets.MemoryFound);
        SetupTools.SetField(ui.AddComponent<TitleCard>(), "openingFinished", TunnelAssets.OpeningFinished);
        ui.AddComponent<MobileJoystick>();
        ObjectiveMarker marker = ui.AddComponent<ObjectiveMarker>();
        SetupTools.SetField(marker, "mattress", GameObject.Find("MattressGoal").transform);
        SetupTools.SetField(marker, "spawner", Object.FindAnyObjectByType<BottleSpawner>());
        SetupTools.SetField(marker, "bottlePicked", TunnelAssets.BottlePicked);
        SetupTools.SetField(marker, "bottleDropped", TunnelAssets.BottleDropped);

        GameObject zones = SetupTools.Recreate("Teaching Zones");
        foreach (string old in new[] { "Hint Door", "Hint Camera", "Hint Wait" })
        {
            Object.DestroyImmediate(GameObject.Find(old)); // Left from an older version of this tool.
        }

        AddZone<MemoryZone>(zones.transform, "Memory: Note", NoteMemoryPosition, NoteMemorySize, "memory",
            "A scrap of paper under the mattress.\nI wrote my family's names on it, so I won't forget their voices.", NoteMemoryHope);
        AddZone<MemoryZone>(zones.transform, "Memory: Wall", WallMemoryPosition, WallMemorySize, "memory",
            "Lines scratched into the wall. Someone counted days here before me.\nI'll count too. And I'll go home.", WallMemoryHope);
        AddZone<HintZone>(zones.transform, "Hint: Door", DoorHintPosition, DoorHintSize, "hint",
            "The door isn't locked all the way. I lean on it, and it gives.\nSteps outside. A guard. I wait until he walks away.", 0);
        AddZone<HintZone>(zones.transform, "Hint: Guard", GuardHintPosition, GuardHintSize, "hint",
            "His flashlight shows where he looks. I stay behind him,\nor wait in a dark corner of the wall until he passes.", 0);
        AddZone<HintZone>(zones.transform, "Hint: Room Camera", RoomCameraHintPosition, RoomCameraHintSize, "hint",
            "Another eye, in the ceiling of the storage room.\nI wait here until it looks away. Then straight to the water.", 0);
        AddZone<MemoryZone>(zones.transform, "Memory: Cup", CupMemoryPosition, CupMemorySize, "memory",
            "A tin cup on the table.\nAt home the kettle is always on, and someone is waiting to pour.", CupMemoryHope);
        AddQuietPlace(zones.transform, QuietPlacePosition);
        AddZone<HintZone>(zones.transform, "Hint: Camera", CameraHintPosition, CameraHintSize, "hint",
            "A camera. Its pale light is its eye.\nIf the light touches me, they'll know.", 0);
        AddZone<HintZone>(zones.transform, "Hint: Wait", WaitHintPosition, WaitHintSize, "hint",
            "I wait here, in the dark, and watch the light.\nWhen it turns away, I go up. Quietly.", 0);
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
        box.size = QuietPlaceSize;

        QuietPlace quiet = place.AddComponent<QuietPlace>();
        SetupTools.SetField(quiet, "memoryFound", TunnelAssets.MemoryFound);
        SerializedObject fields = new SerializedObject(quiet);
        fields.FindProperty("verse").stringValue = "\"Hope in the Lord; be strong, and let your heart take courage.\"\nI say it without a sound. Something in me stands up again.";
        fields.ApplyModifiedPropertiesWithoutUndo();

        LightSetup.AddBulb(place.transform, "Glow", position, QuietGlowRadius, QuietGlowIntensity, QuietGlowColor);
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
            fields.FindProperty("hopeGained").intValue = hope;
        }
        fields.ApplyModifiedPropertiesWithoutUndo();
    }
}
