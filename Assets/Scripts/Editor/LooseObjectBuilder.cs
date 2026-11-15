using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using static TutorialLayout;

/// <summary>
/// Creates the things the player can push, from the X, G and H symbols of the layout:
/// crates and sandbags that make noise when they hit a wall, and the cell door on a hinge.
/// </summary>
public static class LooseObjectBuilder
{
    private static readonly Color SandColor = new Color(0.62f, 0.56f, 0.42f);
    private static readonly Color DoorColor = new Color(0.34f, 0.36f, 0.4f);

    // The size of a crate and a sandbag, in tiles, and their mass, in kilograms. The sandbag is heavier.
    private static readonly Vector2 CrateSize = new Vector2(0.9f, 0.9f);
    private const float CrateMass = 3f;
    private static readonly Vector2 SandbagSize = new Vector2(0.8f, 0.55f);
    private const float SandbagMass = 5f;

    // How fast a pushed object slows down when nothing pushes it, for both sliding and turning.
    private const float PushableDamping = 5f;

    // The cell door: its length and thickness in tiles, its mass in kilograms, how fast its swing slows down,
    // and how far it can turn to each side of closed, in degrees.
    private const float DoorLength = 0.8f;
    private const float DoorThickness = 0.18f;
    private const float DoorMass = 2f;
    private const float DoorDamping = 3f;
    private const float DoorMaxAngle = 100f;

    // The loose objects are drawn above the floor and the walls.
    private const int SortingOrder = 5;

    /// <summary>
    /// Crates and sandbags are rigidbodies that slide on the floor, and the cell door hangs on a hinge joint.
    /// They also stop the camera's rays, so a crate can be pushed ahead as cover.
    /// One NoiseWarning on the parent hears every wall hit, so the warning about noise is shown once in the level.
    /// </summary>
    public static void Build(Tilemap floor)
    {
        Transform parent = SetupTools.Recreate("LooseObjects").transform;
        Sprite crateSprite = AssetDatabase.LoadAssetAtPath<Tile>(TILES_FOLDER + "Crate.asset").sprite;
        NoiseWarning warning = parent.gameObject.AddComponent<NoiseWarning>();
        SetupTools.SetField(warning, "hintRequested", TunnelAssets.HintRequested);

        for (int row = 0; row < LAYOUT.Length; row++)
        {
            for (int column = 0; column < LAYOUT[row].Length; column++)
            {
                Vector3 center = floor.GetCellCenterWorld(ToCell(column, row));
                switch (LAYOUT[row][column])
                {
                    case 'X':
                        CreatePushable("Crate", parent, center, CrateSize, crateSprite, Color.white, CrateMass, TunnelAssets.CrateSounds, warning);
                        break;
                    case 'G':
                        CreatePushable("Sandbag", parent, center, SandbagSize, SceneParts.Square, SandColor, SandbagMass, TunnelAssets.SandbagSounds, warning);
                        break;
                    case 'H':
                        CreateHingedDoor(parent, center);
                        break;
                }
            }
        }
    }

    private static void CreatePushable(string name, Transform parent, Vector3 center, Vector2 size, Sprite sprite, Color color, float mass, ImpactSounds sounds, NoiseWarning warning)
    {
        GameObject pushable = CreateSpriteBox(name, parent, center, size, sprite, color);
        Rigidbody body = AddFlatRigidbody(pushable, mass);
        body.linearDamping = PushableDamping; // Like friction with the floor: it stops soon after the push ends.
        body.angularDamping = PushableDamping;
        body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;
        ImpactNoise noise = pushable.AddComponent<ImpactNoise>();
        SetupTools.SetField(noise, "warning", warning);
        SetupTools.SetField(noise, "sounds", sounds);
    }

    /// <summary>The door fills the doorway and turns around its top end, where the hinge joint holds it to the wall.</summary>
    private static void CreateHingedDoor(Transform parent, Vector3 doorwayCenter)
    {
        var door = new GameObject("Cell Door");
        door.transform.SetParent(parent, false);
        door.transform.position = doorwayCenter + new Vector3(0f, DoorLength / 2f, 0f);
        CreateSpriteBox("Panel", door.transform, door.transform.position + new Vector3(0f, -DoorLength / 2f, 0f), new Vector2(DoorThickness, DoorLength), SceneParts.Square, DoorColor);

        BoxCollider box = door.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, -DoorLength / 2f, 0f);
        box.size = new Vector3(DoorThickness, DoorLength, 1f);

        Rigidbody body = AddFlatRigidbody(door, DoorMass);
        body.angularDamping = DoorDamping;

        HingeJoint hinge = door.AddComponent<HingeJoint>();
        hinge.anchor = Vector3.zero;
        hinge.axis = Vector3.forward; // Turns on the floor plane, like a door seen from above.
        hinge.useLimits = true;
        hinge.limits = new JointLimits { min = -DoorMaxAngle, max = DoorMaxAngle };
    }

    private static GameObject CreateSpriteBox(string name, Transform parent, Vector3 center, Vector2 size, Sprite sprite, Color color)
    {
        var box = new GameObject(name);
        box.transform.SetParent(parent, true);
        box.transform.position = center;
        var picture = new GameObject("Picture");
        picture.transform.SetParent(box.transform, false);
        picture.transform.localScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1f);
        SpriteRenderer spriteRenderer = picture.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.color = color;
        spriteRenderer.sortingOrder = SortingOrder;
        if (name != "Panel")
        {
            box.AddComponent<BoxCollider>().size = new Vector3(size.x, size.y, 1f);
        }
        return box;
    }

    // The map is flat, so there is no gravity: the body only moves when something pushes it.
    private static Rigidbody AddFlatRigidbody(GameObject target, float mass)
    {
        Rigidbody body = target.AddComponent<Rigidbody>();
        body.mass = mass;
        body.useGravity = false;
        return body;
    }
}
