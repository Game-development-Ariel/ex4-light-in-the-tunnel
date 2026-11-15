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
            bloom.threshold.Override(0.8f);
            bloom.intensity.Override(0.7f);
            bloom.scatter.Override(0.7f);

            Vignette vignette = AddEffect<Vignette>(profile);
            vignette.intensity.Override(0.36f);
            vignette.smoothness.Override(0.5f);

            FilmGrain grain = AddEffect<FilmGrain>(profile);
            grain.type.Override(FilmGrainLookup.Medium3);
            grain.intensity.Override(0.35f);

            ColorAdjustments colors = AddEffect<ColorAdjustments>(profile);
            colors.contrast.Override(14f);
            colors.saturation.Override(-22f);
            colors.postExposure.Override(0f);
            colors.colorFilter.Override(new Color(1f, 0.97f, 0.92f));

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
