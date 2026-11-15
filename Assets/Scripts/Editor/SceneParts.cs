using UnityEditor;
using UnityEngine;

/// <summary>
/// Plain shapes for the scene builders. The loose objects use them on purpose, there is no final art yet.
/// </summary>
public static class SceneParts
{
    private const string SquarePath = "Assets/Sprites/Shapes/Square.png";

    /// <summary>A white square, one world unit wide. Tinting and scaling it gives any colored box.</summary>
    public static Sprite Square
    {
        get
        {
            // The file is drawn and imported only the first time. After that it is just loaded.
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SquarePath);
            return sprite != null ? sprite : PlaceholderSprites.LoadOrCreate(SquarePath, (x, y) => Color.white);
        }
    }
}
