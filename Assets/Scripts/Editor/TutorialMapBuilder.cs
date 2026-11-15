using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using static TutorialLayout;

/// <summary>
/// Paints the tutorial level from the text layout in TutorialLayout, using the same tile rules as the ex2 tunnel:
/// a wall with floor below it shows its front face, floor next to a wall gets a shadow,
/// and a wall next to floor gets a rim. Then it places the player, the bottle's spot, and calls the other map builders:
/// the wall colliders, the loose objects and the watchers. Run it from Tools > Tunnel > 1 Build Tutorial Map.
/// </summary>
public static class TutorialMapBuilder
{
    // How many wall tiles to add around the layout, so the camera never shows empty space.
    private const int MARGIN = 4;

    // Fixed seed, so the random floor and wall variations come out the same on every run.
    private const int SEED = 7;

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
        WallColliderBuilder.Build(floor);
        LooseObjectBuilder.Build(floor);
        PlaceMarker("PlayerStart", floor.GetCellCenterWorld(playerCell));
        GameObject player = MovePlayer(floor, playerCell);
        PlaceMarker("BottleSpawn", floor.GetCellCenterWorld(bottleCell));
        WatcherBuilder.Build(floor, player);
        // Saves right away, so the result is never lost by forgetting Cmd+S.
        EditorSceneManager.MarkSceneDirty(grid.scene);
        EditorSceneManager.SaveScene(grid.scene);
        Debug.Log("Tutorial map built and saved.");
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
