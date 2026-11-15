using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using static TutorialLayout;

/// <summary>
/// Places everything that can see the player: a security camera for every c and v of the layout, and the guard.
/// </summary>
public static class WatcherBuilder
{
    // The guard walks along this row of the first corridor, between these two columns.
    private const int GUARD_ROW = 3;
    private const int GUARD_FIRST_COLUMN = 13;
    private const int GUARD_LAST_COLUMN = 18;
    private const string GUARD_SPRITE_PATH = "Assets/Sprites/Player/Player.png";

    // A flashlight: warmer and stronger than the camera's pale light.
    private static readonly Color GuardLightColor = new Color(1f, 0.82f, 0.5f, 0.2f);
    private static readonly Color GuardTint = new Color(0.5f, 0.46f, 0.42f);

    // The storage room's camera turns less to each side than the corridor's camera.
    private const float ROOM_CAMERA_SWEEP = 35f;

    // The camera's picture. Drawn with the lens pointing up, because "up" is where the camera looks.
    private const string CAMERA_SPRITE_PATH = "Assets/Sprites/Items/SecurityCamera.png";

    // Above all the tilemaps (the Decor layer is 4), so the camera is drawn on top of the wall.
    private const int CAMERA_SORTING_ORDER = 6;

    // The vision cone is drawn on the floor but under the walls (the Walls layer is 2),
    // so a wall always covers it. Same order as FloorShadow, and a bit closer to the camera so it draws above it.
    private const int CONE_SORTING_ORDER = 1;
    private const float CONE_Z = -0.1f;

    // An unlit material: the cone keeps its own color in the dark tunnel, like a beam of light.
    private const string CONE_MATERIAL_PATH = "Assets/Materials/VisionCone.mat";
    private const string CONE_SHADER = "Universal Render Pipeline/2D/Sprite-Unlit-Default";

    // How far the camera sits from the center of its wall tile toward the floor, in tiles: just past the tile's edge.
    private const float CAMERA_EDGE_OFFSET = 0.6f;

    // Degrees in a full turn.
    private const float FULL_TURN = 360f;

    // The ring of the guard's footsteps: a warm white that starts invisible (HeardSteps sets its alpha),
    // drawn above the walls.
    private static readonly Color StepsRingColor = new Color(1f, 0.9f, 0.75f, 0f);
    private const int RING_SORTING_ORDER = 20;

    // The stand-in camera picture, in pixels of its 32 x 32 texture: (left, bottom, right, top), edges included.
    private static readonly (int Left, int Bottom, int Right, int Top) GlintRect = (15, 24, 16, 25);
    private static readonly (int Left, int Bottom, int Right, int Top) LensRect = (12, 21, 19, 27);
    private static readonly (int Left, int Bottom, int Right, int Top) CasingRect = (10, 8, 21, 20);
    private static readonly (int Left, int Bottom, int Right, int Top) MountRect = (14, 3, 17, 7);
    private static readonly Color GlintColor = new Color(0.4f, 0.7f, 1f, 1f);
    private static readonly Color LensColor = new Color(0.1f, 0.1f, 0.12f, 1f);
    private static readonly Color CasingColor = new Color(0.6f, 0.6f, 0.62f, 1f);
    private static readonly Color MountColor = new Color(0.3f, 0.3f, 0.32f, 1f);

    // The direction from the camera's wall to the floor next to it.
    private static Vector2Int CameraFacing(int column, int row)
    {
        if (IsFloor(SymbolAt(column, row + 1)))
        {
            return Vector2Int.down;
        }
        if (IsFloor(SymbolAt(column - 1, row)))
        {
            return Vector2Int.left;
        }
        if (IsFloor(SymbolAt(column + 1, row)))
        {
            return Vector2Int.right;
        }
        return Vector2Int.up;
    }

    /// <summary>
    /// Everything that can see the player: the cameras from the layout, and the guard.
    /// They all sit under one "Watchers" object, and they all raise the same PlayerCaught event.
    /// </summary>
    public static void Build(Tilemap floor, GameObject player)
    {
        Object.DestroyImmediate(GameObject.Find("SecurityCamera")); // Left from the older, single-camera scene.
        Transform parent = SetupTools.Recreate("Watchers").transform;

        for (int row = 0; row < LAYOUT.Length; row++)
        {
            for (int column = 0; column < LAYOUT[row].Length; column++)
            {
                char symbol = LAYOUT[row][column];
                if (symbol == 'c')
                {
                    PlaceSecurityCamera(parent, floor, column, row, null, TunnelAssets.CameraVision, player);
                }
                else if (symbol == 'v')
                {
                    PlaceSecurityCamera(parent, floor, column, row, ROOM_CAMERA_SWEEP, TunnelAssets.RoomCameraVision, player);
                }
            }
        }
        PlaceGuard(parent, floor, player);
    }

    // The camera hangs on the edge of its wall tile, so its view starts at the floor and not inside the wall.
    // It is built from small parts: the sweep turns it, the sensor looks, the cone mesh draws what the sensor sees.
    // A null sweepAngle keeps the default from CameraSweep.cs.
    private static void PlaceSecurityCamera(Transform parent, Tilemap floor, int column, int row, float? sweepAngle, VisionStats stats, GameObject player)
    {
        Vector2Int facing = CameraFacing(column, row);
        GameObject cameraObject = new GameObject("SecurityCamera");
        cameraObject.transform.SetParent(parent, false);
        CameraSweep sweep = cameraObject.AddComponent<CameraSweep>();
        VisionSensor sensor = AddSensor(cameraObject, stats, player);
        cameraObject.AddComponent<CameraIndicators>();
        AddCone(cameraObject.transform, sensor);

        // The picture is a child, so it turns together with the camera and its cone.
        var body = new GameObject("Body");
        body.transform.SetParent(cameraObject.transform, false);
        SpriteRenderer bodyRenderer = body.AddComponent<SpriteRenderer>();
        bodyRenderer.sprite = CameraSprite();
        bodyRenderer.sortingOrder = CAMERA_SORTING_ORDER;

        Vector3 center = floor.GetCellCenterWorld(ToCell(column, row));
        cameraObject.transform.position = center + new Vector3(facing.x, facing.y, 0f) * CAMERA_EDGE_OFFSET;

        // Unity angles: 0 looks up, 90 left, 180 down, 270 right.
        float angle = Vector2.SignedAngle(Vector2.up, facing);
        if (angle < 0f)
        {
            angle += FULL_TURN;
        }
        cameraObject.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // Fills the fields, like typing them in the Inspector.
        SerializedObject fields = new SerializedObject(sweep);
        fields.FindProperty("centerAngle").floatValue = angle;
        if (sweepAngle.HasValue)
        {
            fields.FindProperty("sweepAngle").floatValue = sweepAngle.Value;
        }
        fields.ApplyModifiedProperties();
    }

    /// <summary>
    /// The guard is made of the same parts as a camera: a VisionSensor and its cone.
    /// Only the thing that moves him is new: GuardPatrol instead of CameraSweep.
    /// </summary>
    private static void PlaceGuard(Transform parent, Tilemap floor, GameObject player)
    {
        Transform route = new GameObject("Guard Route").transform;
        route.SetParent(parent, false);
        Transform west = AddRoutePoint(route, floor, GUARD_FIRST_COLUMN);
        Transform east = AddRoutePoint(route, floor, GUARD_LAST_COLUMN);

        var guard = new GameObject("Guard");
        guard.transform.SetParent(parent, false);
        guard.transform.position = west.position;
        GuardPatrol patrol = guard.AddComponent<GuardPatrol>();
        SetupTools.SetArray(patrol, "points", new Object[] { east, west });

        VisionSensor sensor = AddSensor(guard, TunnelAssets.GuardVision, player);
        VisionConeMesh cone = AddCone(guard.transform, sensor);
        SerializedObject coneFields = new SerializedObject(cone);
        coneFields.FindProperty("normalColor").colorValue = GuardLightColor;
        coneFields.ApplyModifiedProperties();

        var body = new GameObject("Body");
        body.transform.SetParent(guard.transform, false);
        SpriteRenderer bodyRenderer = body.AddComponent<SpriteRenderer>();
        bodyRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GUARD_SPRITE_PATH);
        bodyRenderer.color = GuardTint;
        bodyRenderer.sortingOrder = CAMERA_SORTING_ORDER;

        // The ring of the footsteps is drawn above the walls and without light, so it is "heard" through them.
        var ring = new GameObject("Steps Ring");
        ring.transform.SetParent(guard.transform, false);
        SpriteRenderer ringRenderer = ring.AddComponent<SpriteRenderer>();
        ringRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/UI/JoystickRing.png");
        ringRenderer.sharedMaterial = ConeMaterial();
        ringRenderer.color = StepsRingColor;
        ringRenderer.sortingOrder = RING_SORTING_ORDER;
        HeardSteps steps = guard.AddComponent<HeardSteps>();
        SetupTools.SetField(steps, "ring", ringRenderer);
        SetupTools.SetField(steps, "heroSenses", player != null ? player.GetComponent<HopeSenses>() : null);
    }

    private static Transform AddRoutePoint(Transform route, Tilemap floor, int column)
    {
        Transform point = new GameObject("Point").transform;
        point.SetParent(route, false);
        point.position = floor.GetCellCenterWorld(ToCell(column, GUARD_ROW));
        return point;
    }

    private static VisionSensor AddSensor(GameObject watcher, VisionStats stats, GameObject player)
    {
        VisionSensor sensor = watcher.AddComponent<VisionSensor>();
        SetupTools.SetField(sensor, "stats", stats);
        SetupTools.SetField(sensor, "target", player != null ? player.transform : null);
        SetupTools.SetField(sensor, "targetSeen", TunnelAssets.PlayerCaught);
        return sensor;
    }

    // The cone is a child, so it turns with its watcher. VisionConeMesh rebuilds its mesh every frame.
    private static VisionConeMesh AddCone(Transform watcher, VisionSensor sensor)
    {
        var cone = new GameObject("VisionCone");
        cone.transform.SetParent(watcher, false);
        cone.transform.localPosition = new Vector3(0f, 0f, CONE_Z);
        cone.AddComponent<MeshFilter>();
        VisionConeMesh mesh = cone.AddComponent<VisionConeMesh>();
        SetupTools.SetField(mesh, "sensor", sensor);
        MeshRenderer coneRenderer = cone.AddComponent<MeshRenderer>();
        coneRenderer.sharedMaterial = ConeMaterial();
        coneRenderer.sortingOrder = CONE_SORTING_ORDER;
        return mesh;
    }

    /// <summary>
    /// The material is saved as an asset, so it goes into the build. A material made only in code at runtime
    /// could lose its shader in the build, because Unity leaves out shaders that nothing references.
    /// </summary>
    private static Material ConeMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(CONE_MATERIAL_PATH);
        if (material == null)
        {
            material = new Material(Shader.Find(CONE_SHADER));
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(CONE_MATERIAL_PATH));
            AssetDatabase.CreateAsset(material, CONE_MATERIAL_PATH);
        }
        return material;
    }

    /// <summary>A gray box with a dark lens on top. Only a stand-in until the AI-made sprite is ready.</summary>
    private static Sprite CameraSprite()
    {
        return PlaceholderSprites.LoadOrCreate(CAMERA_SPRITE_PATH, (x, y) =>
        {
            if (PlaceholderSprites.InRect(x, y, GlintRect))
            {
                return GlintColor;
            }
            if (PlaceholderSprites.InRect(x, y, LensRect))
            {
                return LensColor;
            }
            if (PlaceholderSprites.InRect(x, y, CasingRect))
            {
                return CasingColor;
            }
            if (PlaceholderSprites.InRect(x, y, MountRect))
            {
                return MountColor;
            }
            return Color.clear;
        });
    }
}
