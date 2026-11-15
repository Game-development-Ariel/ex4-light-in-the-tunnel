using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>A small tilemap made in code, for the scripts that read the size of the map.</summary>
public static class TestMap
{
    /// <summary>A grid with one tilemap that has a tile in every cell from (0,0) to (width-1, height-1).</summary>
    public static Tilemap Create(GameObject grid, int width, int height)
    {
        grid.AddComponent<Grid>();
        var layer = new GameObject("Floor");
        layer.transform.SetParent(grid.transform, false);
        Tilemap map = layer.AddComponent<Tilemap>();
        layer.AddComponent<TilemapRenderer>();
        Tile tile = ScriptableObject.CreateInstance<Tile>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                map.SetTile(new Vector3Int(x, y, 0), tile);
            }
        }
        return map;
    }
}
