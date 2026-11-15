using UnityEditor;
using UnityEngine;

/// <summary>
/// The teaching of the tutorial: the opening, the hints, the memory captions and the objective arrow,
/// and on the map the hint zones, the memories and the quiet place.
/// </summary>
public static class TeachingSetup
{
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

        LightSetup.AddBulb(place.transform, "Glow", position, 1.4f, 0.45f, new Color(1f, 0.85f, 0.6f));
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
