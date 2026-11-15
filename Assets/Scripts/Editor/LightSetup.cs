using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// The light of the tutorial: a dim tunnel, weak flickering bulbs, light under the cell door, and dust floating in the light.
/// </summary>
public static class LightSetup
{
    private const string DustMaterialPath = "Assets/Materials/Dust.mat";
    private const string LitSpriteShader = "Universal Render Pipeline/2D/Sprite-Lit-Default";
    private const string GlowTexturePath = "Assets/Resources/UI/Glow.png";

    // The light that is everywhere, even far from the bulbs: weak and a little cold.
    private const float AmbientIntensity = 0.24f;
    private static readonly Color AmbientColor = new Color(0.55f, 0.6f, 0.78f);

    // The light of a bulb is full up to this part of its radius, then fades out to the edge.
    private const float InnerRadiusPart = 0.15f;

    private static readonly Color BulbColor = new Color(1f, 0.74f, 0.48f);
    private static readonly Color StorageRoomColor = new Color(1f, 0.8f, 0.56f);

    // Each bulb: its name, where it is and how far its light reaches (in world units), how bright it is, and its color.
    private static readonly (string Name, Vector2 Position, float Radius, float Intensity, Color Color)[] Bulbs =
    {
        ("Cell", new Vector2(-5.5f, 2f), 4.5f, 0.55f, BulbColor),
        ("Under the Door", new Vector2(-0.5f, 0.5f), 1.6f, 0.5f, BulbColor),
        ("Corridor", new Vector2(6f, 0.5f), 4.5f, 0.7f, BulbColor),
        ("Way Down", new Vector2(10.5f, -3f), 4f, 0.6f, BulbColor),
        ("Waiting Spot", new Vector2(15.5f, -6.5f), 3.2f, 0.4f, BulbColor),
        ("Storage Room", new Vector2(22f, 0f), 6f, 0.95f, StorageRoomColor),
    };

    // Each place the dust floats in: its center and its size, in world units (the cell, the corridor, the storage room).
    private static readonly (Vector2 Position, Vector2 Area)[] DustAreas =
    {
        (new Vector2(-6f, 0.5f), new Vector2(8f, 6f)),
        (new Vector2(6f, 0.5f), new Vector2(10f, 2.5f)),
        (new Vector2(22f, 0.5f), new Vector2(8f, 6f)),
    };

    /// <summary>
    /// The tunnel is almost dark. A few weak bulbs show the way, each flickering on its own.
    /// The storage room is a little warmer and brighter: the place of water, the place of hope.
    /// Dust floats in the light of the rooms. Positions are world coordinates of the tutorial map.
    /// </summary>
    public static void Setup()
    {
        GameObject globalLight = GameObject.Find("Global Light 2D");
        if (globalLight != null)
        {
            Light2D ambient = globalLight.GetComponent<Light2D>();
            ambient.intensity = AmbientIntensity;
            ambient.color = AmbientColor;
        }

        GameObject bulbs = SetupTools.Recreate("Bulbs");
        foreach ((string name, Vector2 position, float radius, float intensity, Color color) in Bulbs)
        {
            AddBulb(bulbs.transform, name, position, radius, intensity, color);
        }

        Material dust = DustMaterial();
        // Older versions named the children "Dust" too, so Recreate found a child instead of the parent
        // and every rebuild left one more copy of the dust. All of them are removed first.
        GameObject oldDust = GameObject.Find("Dust");
        while (oldDust != null)
        {
            Object.DestroyImmediate(oldDust);
            oldDust = GameObject.Find("Dust");
        }
        GameObject dustParent = new GameObject("Dust");
        foreach ((Vector2 position, Vector2 area) in DustAreas)
        {
            AddDust(dustParent.transform, position, area, dust);
        }
    }

    /// <summary>A point light that flickers like a weak bulb. The quiet place uses it for its warm glow too.</summary>
    public static void AddBulb(Transform parent, string name, Vector2 position, float radius, float intensity, Color color)
    {
        var bulb = new GameObject(name);
        bulb.transform.SetParent(parent, false);
        bulb.transform.position = position;
        Light2D light = bulb.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.pointLightOuterRadius = radius;
        light.pointLightInnerRadius = radius * InnerRadiusPart;
        light.intensity = intensity;
        light.color = color;
        bulb.AddComponent<LightFlicker>();
    }

    private static void AddDust(Transform parent, Vector2 position, Vector2 area, Material material)
    {
        var dust = new GameObject("Dust Motes");
        dust.transform.SetParent(parent, false);
        dust.transform.position = position;
        DustMotes motes = dust.AddComponent<DustMotes>(); // Also adds the ParticleSystem it needs.
        SetupTools.SetField(motes, "material", material);
        SerializedObject fields = new SerializedObject(motes);
        fields.FindProperty("area").vector2Value = area;
        fields.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// A material that the 2D lights light up, with the soft glow picture.
    /// It is saved as an asset, so the build includes its shader.
    /// </summary>
    private static Material DustMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(DustMaterialPath);
        if (material == null)
        {
            material = new Material(Shader.Find(LitSpriteShader));
            System.IO.Directory.CreateDirectory("Assets/Materials");
            AssetDatabase.CreateAsset(material, DustMaterialPath);
        }
        material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(GlowTexturePath);
        EditorUtility.SetDirty(material);
        return material;
    }
}
