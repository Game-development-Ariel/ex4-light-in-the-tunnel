using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Paints the tutorial level from the text layout below, using the same tile rules as the ex2 tunnel:
/// a wall with floor below it shows its front face, floor next to a wall gets a shadow,
/// and a wall next to floor gets a rim. It also puts a 3D box collider on every wall that touches floor,
/// for the player's CharacterController and the camera's rays. Run it from Tools > Tunnel > 1 Build Tutorial Map.
/// To change the map, edit LAYOUT and run the menu again.
/// </summary>
public static class TutorialMapBuilder
{
    // Every character is one tile. Row 0 is the top (north) of the map.
    //  #  wall                     .  floor
    //  m  wall with tally marks    p  wall with a pipe     (only on a wall that has floor below it)
    //  c  wall with a security camera. It looks at the floor next to it
    //  v  wall with the storage room's camera: shorter sight and a smaller sweep
    //  S  mattress, left half      s  mattress, right half
    //  N  journal (note)           @  player start
    //  K  sharp concrete (rubble)  k  small rubble
    //  D  door                     P  puddle
    //  B  barrel                   C  crate
    //  T  table                    W  where the water bottle goes (marked by the BottleSpawn object)
    //  X  crate the player can push   G  sandbag the player can push   H  the cell door, on a hinge
    private static readonly string[] LAYOUT =
    {
        "######mm#################################",
        "#.........####################v##########",
        "#.Ss......###ppp############CC....T..####",
        "#.N@......#............#####C......W.####",
        "#.........H............#####.........####",
        "#.........#.k......B...#####.........####",
        "#....X.P..###.###.##...#####.k.......####",
        "#K........##########..B#####........B####",
        "####################.P.########GG########",
        "####################...###pp###..########",
        "####################.........B.......C..c",
        "####################........P..........D#",
        "####################.....CC.X.....BB....#",
        "#########################################",
    };

    // How many wall tiles to add around the layout, so the camera never shows empty space.
    private const int MARGIN = 4;

    // The layout column and row that sit at world cell (0, 0). Here: the corridor's first floor tile, middle row.
    private const int ORIGIN_COLUMN = 11;
    private const int ORIGIN_ROW = 4;

    // Fixed seed, so the random floor and wall variations come out the same on every run.
    private const int SEED = 7;

    private const string TILES_FOLDER = "Assets/Tiles/Tunnel/";

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

    [MenuItem("Tools/Tunnel/1 Build Tutorial Map")]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Stop the game first (the Play button). Changes made while playing are lost when it stops.");
            return;
        }

        GameObject grid = GameObject.Find("TunnelGrid");
        if (grid == null)
        {
            Debug.LogError("No TunnelGrid in the open scene. Open the Tutorial scene first.");
            return;
        }

        Tilemap floor = FindLayer(grid, "Floor");
        Tilemap shadow = FindLayer(grid, "FloorShadow");
        Tilemap walls = FindLayer(grid, "Walls");
        Tilemap details = FindLayer(grid, "WallDetails");
        Tilemap decor = FindLayer(grid, "Decor");
        if (floor == null || shadow == null || walls == null || details == null || decor == null)
        {
            return;
        }

        for (int row = 1; row < LAYOUT.Length; row++)
        {
            if (LAYOUT[row].Length != LAYOUT[0].Length)
            {
                Debug.LogError($"Row {row} of LAYOUT has {LAYOUT[row].Length} characters instead of {LAYOUT[0].Length}.");
                return;
            }
        }

        // The ex2 tunnel lights don't belong in the tutorial.
        foreach (string leftover in new[] { "Lights", "ExitLightPool" })
        {
            GameObject obj = GameObject.Find(leftover);
            if (obj != null)
            {
                Undo.DestroyObjectImmediate(obj);
            }
        }

        Tilemap[] layers = { floor, shadow, walls, details, decor };
        Undo.RegisterCompleteObjectUndo(layers, "Build Tutorial Map");
        foreach (Tilemap layer in layers)
        {
            layer.ClearAllTiles();
        }

        TileBase[] floorTiles = LoadTiles("Floor0", "Floor1", "Floor2", "Floor3");
        TileBase[] wallTops = LoadTiles("WallTop0", "WallTop1");
        TileBase[] wallFaces = LoadTiles("WallFace0", "WallFace1", "WallFace2");
        System.Random random = new System.Random(SEED);

        int width = LAYOUT[0].Length;
        int height = LAYOUT.Length;
        Vector3Int playerCell = Vector3Int.zero;
        Vector3Int bottleCell = Vector3Int.zero;

        for (int row = -MARGIN; row < height + MARGIN; row++)
        {
            for (int column = -MARGIN; column < width + MARGIN; column++)
            {
                char symbol = SymbolAt(column, row);
                Vector3Int cell = ToCell(column, row);

                if (IsFloor(symbol))
                {
                    floor.SetTile(cell, floorTiles[random.Next(floorTiles.Length)]);
                    shadow.SetTile(cell, FloorShadow(column, row));
                    decor.SetTile(cell, DecorTile(symbol));
                    if (symbol == '@')
                    {
                        playerCell = cell;
                    }
                    else if (symbol == 'W')
                    {
                        bottleCell = cell;
                    }
                    continue;
                }

                // A wall. If there is floor right below it, the player sees its front face.
                bool floorBelow = IsFloor(SymbolAt(column, row + 1));
                if (floorBelow)
                {
                    walls.SetTile(cell, wallFaces[random.Next(wallFaces.Length)]);
                    details.SetTile(cell, FaceDetail(symbol));
                }
                else
                {
                    walls.SetTile(cell, wallTops[random.Next(wallTops.Length)]);
                    details.SetTile(cell, WallRim(column, row));
                }
            }
        }

        walls.CompressBounds();
        floor.CompressBounds();
        BuildWallColliders(floor);
        BuildLooseObjects(floor);
        PlaceMarker("PlayerStart", floor.GetCellCenterWorld(playerCell));
        GameObject player = MovePlayer(floor, playerCell);
        PlaceMarker("BottleSpawn", floor.GetCellCenterWorld(bottleCell));
        PlaceWatchers(floor, player);
        // Saves right away, so the result is never lost by forgetting Cmd+S.
        EditorSceneManager.MarkSceneDirty(grid.scene);
        EditorSceneManager.SaveScene(grid.scene);
        Debug.Log("Tutorial map built and saved.");
    }

    /// <summary>
    /// One box collider for every run of wall tiles in a row that touch floor.
    /// Walls deep inside the rock can't be reached, so they get no collider.
    /// </summary>
    private static void BuildWallColliders(Tilemap floor)
    {
        GameObject parent = SetupTools.Recreate("WallColliders");
        for (int row = -1; row <= LAYOUT.Length; row++)
        {
            int runStart = -1;
            for (int column = -1; column <= LAYOUT[0].Length + 1; column++)
            {
                bool solid = column <= LAYOUT[0].Length && !IsFloor(SymbolAt(column, row)) && TouchesFloor(column, row);
                if (solid && runStart < 0)
                {
                    runStart = column;
                }
                else if (!solid && runStart >= 0)
                {
                    Vector3 first = floor.GetCellCenterWorld(ToCell(runStart, row));
                    Vector3 last = floor.GetCellCenterWorld(ToCell(column - 1, row));
                    BoxCollider box = parent.AddComponent<BoxCollider>();
                    box.center = (first + last) / 2f;
                    box.size = new Vector3(column - runStart, 1f, 1f);
                    runStart = -1;
                }
            }
        }
    }

    // ---------- Things the player can push ----------

    private static readonly Color SandColor = new Color(0.62f, 0.56f, 0.42f);
    private static readonly Color DoorColor = new Color(0.34f, 0.36f, 0.4f);

    /// <summary>
    /// Crates and sandbags are rigidbodies that slide on the floor, and the cell door hangs on a hinge joint.
    /// They also stop the camera's rays, so a crate can be pushed ahead as cover.
    /// </summary>
    private static void BuildLooseObjects(Tilemap floor)
    {
        Transform parent = SetupTools.Recreate("LooseObjects").transform;
        Sprite crateSprite = AssetDatabase.LoadAssetAtPath<Tile>(TILES_FOLDER + "Crate.asset").sprite;

        for (int row = 0; row < LAYOUT.Length; row++)
        {
            for (int column = 0; column < LAYOUT[row].Length; column++)
            {
                Vector3 center = floor.GetCellCenterWorld(ToCell(column, row));
                switch (LAYOUT[row][column])
                {
                    case 'X':
                        CreatePushable("Crate", parent, center, new Vector2(0.9f, 0.9f), crateSprite, Color.white, 3f);
                        break;
                    case 'G':
                        CreatePushable("Sandbag", parent, center, new Vector2(0.8f, 0.55f), SceneParts.Square, SandColor, 5f);
                        break;
                    case 'H':
                        CreateHingedDoor(parent, center);
                        break;
                }
            }
        }
    }

    private static void CreatePushable(string name, Transform parent, Vector3 center, Vector2 size, Sprite sprite, Color color, float mass)
    {
        GameObject pushable = CreateSpriteBox(name, parent, center, size, sprite, color);
        Rigidbody body = AddFlatRigidbody(pushable, mass);
        body.linearDamping = 5f; // Like friction with the floor: it stops soon after the push ends.
        body.angularDamping = 5f;
        body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;
        SetupTools.SetField(pushable.AddComponent<ImpactNoise>(), "hintRequested", TunnelAssets.HintRequested);
    }

    /// <summary>The door fills the doorway and turns around its top end, where the hinge joint holds it to the wall.</summary>
    private static void CreateHingedDoor(Transform parent, Vector3 doorwayCenter)
    {
        const float Length = 0.8f;
        var door = new GameObject("Cell Door");
        door.transform.SetParent(parent, false);
        door.transform.position = doorwayCenter + new Vector3(0f, 0.4f, 0f);
        CreateSpriteBox("Panel", door.transform, door.transform.position + new Vector3(0f, -Length / 2f, 0f), new Vector2(0.18f, Length), SceneParts.Square, DoorColor);

        BoxCollider box = door.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, -Length / 2f, 0f);
        box.size = new Vector3(0.18f, Length, 1f);

        Rigidbody body = AddFlatRigidbody(door, 2f);
        body.angularDamping = 3f;

        HingeJoint hinge = door.AddComponent<HingeJoint>();
        hinge.anchor = Vector3.zero;
        hinge.axis = Vector3.forward; // Turns on the floor plane, like a door seen from above.
        hinge.useLimits = true;
        hinge.limits = new JointLimits { min = -100f, max = 100f };
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
        spriteRenderer.sortingOrder = 5;
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

    private static bool TouchesFloor(int column, int row)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (IsFloor(SymbolAt(column + dx, row + dy)))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private static Tilemap FindLayer(GameObject grid, string name)
    {
        Transform child = grid.transform.Find(name);
        Tilemap tilemap = child != null ? child.GetComponent<Tilemap>() : null;
        if (tilemap == null)
        {
            Debug.LogError($"TunnelGrid has no tilemap layer called {name}.");
        }
        return tilemap;
    }

    private static TileBase[] LoadTiles(params string[] names)
    {
        TileBase[] tiles = new TileBase[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            tiles[i] = LoadTile(names[i]);
        }
        return tiles;
    }

    private static TileBase LoadTile(string name)
    {
        TileBase tile = AssetDatabase.LoadAssetAtPath<TileBase>(TILES_FOLDER + name + ".asset");
        if (tile == null)
        {
            Debug.LogError($"Missing tile {TILES_FOLDER}{name}.asset");
        }
        return tile;
    }

    // Anything outside the layout counts as wall.
    private static char SymbolAt(int column, int row)
    {
        if (row < 0 || row >= LAYOUT.Length || column < 0 || column >= LAYOUT[row].Length)
        {
            return '#';
        }
        return LAYOUT[row][column];
    }

    private static bool IsFloor(char symbol)
    {
        return symbol != '#' && symbol != 'm' && symbol != 'p' && symbol != 'c' && symbol != 'v';
    }

    // Rows go down in the layout but up in the world, so the row is flipped.
    private static Vector3Int ToCell(int column, int row)
    {
        return new Vector3Int(column - ORIGIN_COLUMN, ORIGIN_ROW - row, 0);
    }

    // Same order as the ex2 map: a wall to the north wins, then east, then west, then south.
    private static TileBase FloorShadow(int column, int row)
    {
        if (!IsFloor(SymbolAt(column, row - 1)))
        {
            return LoadTile("ShadowN");
        }
        if (!IsFloor(SymbolAt(column + 1, row)))
        {
            return LoadTile("ShadowE");
        }
        if (!IsFloor(SymbolAt(column - 1, row)))
        {
            return LoadTile("ShadowW");
        }
        if (!IsFloor(SymbolAt(column, row + 1)))
        {
            return LoadTile("ShadowS");
        }
        return null;
    }

    // The rim is a thin edge drawn on the side of the wall that touches floor.
    private static TileBase WallRim(int column, int row)
    {
        if (IsFloor(SymbolAt(column, row - 1)))
        {
            return LoadTile("RimN");
        }
        if (IsFloor(SymbolAt(column + 1, row)))
        {
            return LoadTile("RimE");
        }
        if (IsFloor(SymbolAt(column - 1, row)))
        {
            return LoadTile("RimW");
        }
        return null;
    }

    private static TileBase FaceDetail(char symbol)
    {
        switch (symbol)
        {
            case 'm': return LoadTile("WallMarks");
            case 'p': return LoadTile("Pipe");
            default: return null;
        }
    }

    private static TileBase DecorTile(char symbol)
    {
        switch (symbol)
        {
            case 'S': return LoadTile("MattressL");
            case 's': return LoadTile("MattressR");
            case 'N': return LoadTile("Note");
            case 'K': return LoadTile("RubbleA");
            case 'k': return LoadTile("RubbleB");
            case 'D': return LoadTile("Door");
            case 'P': return LoadTile("Puddle");
            case 'B': return LoadTile("Barrel");
            case 'C': return LoadTile("Crate");
            case 'T': return LoadTile("Table");
            default: return null;
        }
    }

    // An empty object that marks a place: where the player starts, where the bottle will appear.
    private static Transform PlaceMarker(string name, Vector3 position)
    {
        GameObject marker = GameObject.Find(name);
        if (marker == null)
        {
            marker = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(marker, "Build Tutorial Map");
        }
        Undo.RecordObject(marker.transform, "Build Tutorial Map");
        marker.transform.position = position;
        return marker.transform;
    }

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
    private static void PlaceWatchers(Tilemap floor, GameObject player)
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
        cameraObject.transform.position = center + new Vector3(facing.x, facing.y, 0f) * 0.6f;

        // Unity angles: 0 looks up, 90 left, 180 down, 270 right.
        float angle = Vector2.SignedAngle(Vector2.up, facing);
        if (angle < 0f)
        {
            angle += 360f;
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
        ringRenderer.color = new Color(1f, 0.9f, 0.75f, 0f);
        ringRenderer.sortingOrder = 20;
        SetupTools.SetField(guard.AddComponent<HeardSteps>(), "ring", ringRenderer);
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
        Color casing = new Color(0.6f, 0.6f, 0.62f, 1f);
        Color lens = new Color(0.1f, 0.1f, 0.12f, 1f);
        Color glint = new Color(0.4f, 0.7f, 1f, 1f);
        Color mount = new Color(0.3f, 0.3f, 0.32f, 1f);
        return PlaceholderSprites.LoadOrCreate(CAMERA_SPRITE_PATH, (x, y) =>
        {
            if (PlaceholderSprites.InRect(x, y, 15, 24, 16, 25))
            {
                return glint;
            }
            if (PlaceholderSprites.InRect(x, y, 12, 21, 19, 27))
            {
                return lens;
            }
            if (PlaceholderSprites.InRect(x, y, 10, 8, 21, 20))
            {
                return casing;
            }
            if (PlaceholderSprites.InRect(x, y, 14, 3, 17, 7))
            {
                return mount;
            }
            return Color.clear;
        });
    }

    private static GameObject MovePlayer(Tilemap floor, Vector3Int cell)
    {
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogWarning("No Player in the scene, so the start position was not set.");
            return null;
        }
        Undo.RecordObject(player.transform, "Build Tutorial Map");
        Vector3 center = floor.GetCellCenterWorld(cell);
        player.transform.position = new Vector3(center.x, center.y, player.transform.position.z);
        return player;
    }
}
