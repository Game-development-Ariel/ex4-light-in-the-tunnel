using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Changes the post-processing (the filter over the whole picture) with the hero's feelings:
/// - the dark edges of the screen slowly grow and shrink, like breathing;
/// - near danger the edges close in and the colors fade;
/// - with less hope the picture is darker (from the design: "more hope, a bit more light");
/// - when caught, the colors drain away for a moment and the picture splits a little.
/// </summary>
[RequireComponent(typeof(Volume))]
public class MoodVolume : MonoBehaviour
{
    [Tooltip("Seconds for one slow breath of the screen's edges")]
    [SerializeField] private float breathSeconds = 5.2f;

    [Tooltip("How much the edges grow and shrink with each breath")]
    [SerializeField] private float breathVignette = 0.03f;

    [Tooltip("How dark the screen edges are when calm, as the vignette intensity from 0 to 1")]
    [SerializeField] private float baseVignette = 0.36f;
    [Tooltip("How much darker the edges get at full danger, as vignette intensity")]
    [SerializeField] private float dangerVignette = 0.18f;
    [Tooltip("The color saturation when calm. 0 = normal, -100 = black and white")]
    [SerializeField] private float baseSaturation = -22f;
    [Tooltip("How much the saturation changes at full danger, on the same -100 to 100 scale")]
    [SerializeField] private float dangerSaturation = -25f;

    [Tooltip("How much darker the picture is at the lowest hope, in exposure stops")]
    [SerializeField] private float lowHopeDarkness = 0.7f;

    [Header("The shock of being caught")]
    [Tooltip("Seconds the shock takes to fade")]
    [Min(0.01f)]
    [SerializeField] private float shockSeconds = 2.5f;

    [Tooltip("How much the edges close in at the moment of the shock")]
    [SerializeField] private float shockVignette = 0.2f;

    [Tooltip("How much color drains away at the moment of the shock")]
    [SerializeField] private float shockSaturation = 70f;

    [Tooltip("How much darker the picture is at the moment of the shock, in exposure stops")]
    [SerializeField] private float shockDarkness = 0.6f;

    [Tooltip("How much the picture splits into colors at the moment of the shock")]
    [SerializeField] private float shockSplit = 0.8f;

    [SerializeField] private Tension tension;
    [SerializeField] private Hope hope;
    [SerializeField] private GameEvent playerCaught;

    private VolumeProfile profile;
    private Vignette vignette;
    private ColorAdjustments colors;
    private ChromaticAberration split;

    // 1 right after being caught, then back to 0.
    private float shock;

    private void Awake()
    {
        // .profile (and not .sharedProfile) gives this scene its own copy, so the asset on disk doesn't change.
        profile = GetComponent<Volume>().profile;
        profile.TryGet(out vignette);
        profile.TryGet(out colors);
        profile.TryGet(out split);
    }

    // The copy was made in code, so nothing else frees it.
    private void OnDestroy()
    {
        Destroy(profile);
    }

    private void OnEnable()
    {
        playerCaught.Subscribe(OnCaptured);
    }

    private void OnDisable()
    {
        playerCaught.Unsubscribe(OnCaptured);
    }

    private void OnCaptured()
    {
        StopAllCoroutines(); // A second capture restarts the shock instead of running two at once.
        StartCoroutine(Shock());
    }

    private IEnumerator Shock()
    {
        for (shock = 1f; shock > 0f; shock -= Time.deltaTime / shockSeconds)
        {
            yield return null;
        }
        shock = 0f;
    }

    private void Update()
    {
        float danger = tension.Danger;
        float breath = Mathf.Sin(Time.time * 2f * Mathf.PI / breathSeconds) * breathVignette;

        if (vignette != null)
        {
            vignette.intensity.value = baseVignette + breath + danger * dangerVignette + shock * shockVignette;
        }

        if (colors != null)
        {
            float hopeLeft = (float)hope.Current / hope.Max;
            colors.saturation.value = baseSaturation + danger * dangerSaturation - shock * shockSaturation;
            colors.postExposure.value = -(1f - hopeLeft) * lowHopeDarkness - shock * shockDarkness;
        }

        if (split != null)
        {
            split.intensity.value = shock * shockSplit;
        }
    }
}
