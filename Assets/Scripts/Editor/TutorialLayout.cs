using UnityEngine;

/// <summary>
/// The text layout of the tutorial map, and how to read it. The map builders all read the map from here.
/// To change the map, edit LAYOUT and run Tools > Tunnel > 1 Build Tutorial Map.
/// </summary>
public static class TutorialLayout
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
    public static readonly string[] LAYOUT =
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

    // The layout column and row that sit at world cell (0, 0). Here: the corridor's first floor tile, middle row.
    private const int ORIGIN_COLUMN = 11;
    private const int ORIGIN_ROW = 4;

    // The tiles that paint the map, and the tiles that loose objects take their pictures from.
    public const string TILES_FOLDER = "Assets/Tiles/Tunnel/";

    /// <summary>The symbol at a column and row. Anything outside the layout counts as wall.</summary>
    public static char SymbolAt(int column, int row)
    {
        if (row < 0 || row >= LAYOUT.Length || column < 0 || column >= LAYOUT[row].Length)
        {
            return '#';
        }
        return LAYOUT[row][column];
    }

    public static bool IsFloor(char symbol)
    {
        return symbol != '#' && symbol != 'm' && symbol != 'p' && symbol != 'c' && symbol != 'v';
    }

    /// <summary>The world cell of a layout place. Rows go down in the layout but up in the world, so the row is flipped.</summary>
    public static Vector3Int ToCell(int column, int row)
    {
        return new Vector3Int(column - ORIGIN_COLUMN, ORIGIN_ROW - row, 0);
    }

    /// <summary>True if one of the 8 tiles around this place is floor.</summary>
    public static bool TouchesFloor(int column, int row)
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
}
