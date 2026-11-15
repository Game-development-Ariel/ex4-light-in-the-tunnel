using UnityEngine;
using UnityEngine.Tilemaps;
using static TutorialLayout;

/// <summary>
/// Puts 3D box colliders on the walls of the tutorial map, for the player's CharacterController and the camera's rays.
/// </summary>
public static class WallColliderBuilder
{
    /// <summary>
    /// One box collider for every run of wall tiles in a row that touch floor.
    /// Walls deep inside the rock can't be reached, so they get no collider.
    /// </summary>
    public static void Build(Tilemap floor)
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
}
