using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// The post-processing of the tutorial: soft glow, film grain, dark edges and faded colors,
/// which MoodVolume changes with the hero's feelings.
/// </summary>
public static class MoodSetup
{
    private const string MoodProfilePath = "Assets/Settings/TunnelMood.asset";

    // A soft glow around the bulbs: pixels brighter than the threshold glow, and the scatter is how wide.
    private const float BloomThreshold = 0.8f;
    private const float BloomIntensity = 0.7f;
    private const float BloomScatter = 0.7f;

    // Dark edges of the screen, from 0 to 1. MoodVolume changes the intensity while the game runs,
    // starting from its own baseVignette (the same 0.36).
    private const float VignetteIntensity = 0.36f;
    private const float VignetteSmoothness = 0.5f;

    // Film grain, from 0 to 1.
    private const float GrainIntensity = 0.35f;

    // Faded, a little warm colors. Contrast and saturation are from -100 to 100, and 0 is normal.
    // MoodVolume sets the saturation while the game runs, from its own baseSaturation (the same -22).
    private const float Contrast = 14f;
    private const float Saturation = -22f;
    private static readonly Color ColorFilter = new Color(1f, 0.97f, 0.92f);

    /// <summary>
    /// A global Volume with the tunnel's look. The camera must have post-processing turned on to show it.
    /// MoodVolume changes the values while the game runs.
    /// </summary>
    public static void Setup(Tension tension, Hope hope)
    {
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(MoodProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            System.IO.Directory.CreateDirectory("Assets/Settings");
            AssetDatabase.CreateAsset(profile, MoodProfilePath);

            Bloom bloom = AddEffect<Bloom>(profile);
            bloom.threshold.Override(BloomThreshold);
            bloom.intensity.Override(BloomIntensity);
            bloom.scatter.Override(BloomScatter);

            Vignette vignette = AddEffect<Vignette>(profile);
            vignette.intensity.Override(VignetteIntensity);
            vignette.smoothness.Override(VignetteSmoothness);

            FilmGrain grain = AddEffect<FilmGrain>(profile);
            grain.type.Override(FilmGrainLookup.Medium3);
            grain.intensity.Override(GrainIntensity);

            ColorAdjustments colors = AddEffect<ColorAdjustments>(profile);
            colors.contrast.Override(Contrast);
            colors.saturation.Override(Saturation);
            colors.postExposure.Override(0f);
            colors.colorFilter.Override(ColorFilter);

            ChromaticAberration split = AddEffect<ChromaticAberration>(profile);
            split.intensity.Override(0f);
            AssetDatabase.SaveAssets();
        }

        GameObject volumeObject = SetupTools.Recreate("Mood");
        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = profile;
        MoodVolume mood = volumeObject.AddComponent<MoodVolume>();
        SetupTools.SetField(mood, "tension", tension);
        SetupTools.SetField(mood, "hope", hope);
        SetupTools.SetField(mood, "playerCaught", TunnelAssets.PlayerCaught);

        Camera.main.GetUniversalAdditionalCameraData().renderPostProcessing = true;
    }

    /// <summary>Each effect is saved inside the profile asset, so it isn't lost when Unity closes.</summary>
    private static T AddEffect<T>(VolumeProfile profile) where T : VolumeComponent
    {
        T effect = profile.Add<T>(true);
        effect.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
        AssetDatabase.AddObjectToAsset(effect, profile);
        return effect;
    }
}
