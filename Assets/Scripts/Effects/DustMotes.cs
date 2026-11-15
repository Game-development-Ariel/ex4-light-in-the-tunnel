using UnityEngine;

/// <summary>
/// Tiny specks of dust that float slowly in the air. The material is lit by the 2D lights,
/// so the dust shows only where there is light, like dust in a beam in a dark room.
/// The particle system is set up here in code, so all its settings are in one readable place.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class DustMotes : MonoBehaviour
{
    // In the air, above the player.
    private const int SortingOrder = 15;

    [Tooltip("The size of the area the dust floats in, in tiles")]
    [SerializeField] private Vector2 area = new Vector2(8f, 5f);

    [Tooltip("A material lit by the 2D lights")]
    [SerializeField] private Material material;

    [Tooltip("The most specks of dust in the air at once")]
    [SerializeField] private int maxSpecks = 70;

    [Tooltip("Seconds a speck lives: the shortest and the longest")]
    [SerializeField] private Vector2 lifetime = new Vector2(6f, 10f);

    [Tooltip("How fast a speck drifts, in tiles per second: the slowest and the fastest")]
    [SerializeField] private Vector2 startSpeed = new Vector2(0.02f, 0.08f);

    [Tooltip("The size of a speck, in tiles: the smallest and the largest")]
    [SerializeField] private Vector2 startSize = new Vector2(0.04f, 0.1f);

    [Tooltip("The color of the dust. Its alpha is how strong the specks look at most")]
    [SerializeField] private Color speckColor = new Color(1f, 0.95f, 0.85f, 0.55f);

    [Tooltip("How thick the dust area is, in tiles. The map is flat, so it is thin")]
    [SerializeField] private float areaDepth = 0.1f;

    [Tooltip("How much each speck drifts and turns, like in still air")]
    [SerializeField] private float driftStrength = 0.12f;

    [Tooltip("How fast the drift changes")]
    [SerializeField] private float driftFrequency = 0.25f;

    [Tooltip("The parts of a speck's life when it has finished fading in, and when it starts to fade out")]
    [SerializeField] private Vector2 fadeInOut = new Vector2(0.3f, 0.7f);

    private void Awake()
    {
        var particles = GetComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime.x, lifetime.y);
        main.startSpeed = new ParticleSystem.MinMaxCurve(startSpeed.x, startSpeed.y);
        main.startSize = new ParticleSystem.MinMaxCurve(startSize.x, startSize.y);
        main.startColor = speckColor;
        main.maxParticles = maxSpecks;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.prewarm = true; // The room is already dusty when the scene starts.

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = maxSpecks / ((lifetime.x + lifetime.y) / 2f); // About maxSpecks alive at a time.

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(area.x, area.y, areaDepth);

        // Noise makes each speck drift and turn a little, like in still air.
        ParticleSystem.NoiseModule noise = particles.noise;
        noise.enabled = true;
        noise.strength = driftStrength;
        noise.frequency = driftFrequency;

        // Each speck fades in and out, so none of them pop.
        ParticleSystem.ColorOverLifetimeModule fade = particles.colorOverLifetime;
        fade.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, fadeInOut.x), new GradientAlphaKey(1f, fadeInOut.y), new GradientAlphaKey(0f, 1f) });
        fade.color = gradient;

        var particleRenderer = GetComponent<ParticleSystemRenderer>();
        particleRenderer.material = material;
        particleRenderer.sortingOrder = SortingOrder;

        particles.Play();
    }
}
