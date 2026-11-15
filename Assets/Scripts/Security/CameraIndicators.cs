using UnityEngine;

/// <summary>
/// The small signs that a security camera is on: a red light that blinks on the lens,
/// and a hum that gets louder when the player is closer. Both are created here in code.
/// </summary>
[RequireComponent(typeof(VisionSensor))]
public class CameraIndicators : MonoBehaviour
{
    // Where the light sits on the camera's picture, how big it is, and that it is drawn above the camera.
    private static readonly Vector3 LedPosition = new Vector3(0f, 0.32f, 0f);
    private const float LedScale = 0.35f;
    private const int LedSortingOrder = 7;

    [Tooltip("Blinks per second of the red light")]
    [SerializeField] private float blinkRate = 1.2f;

    [Tooltip("The part of each blink that the light is on. 0.3 = on for 30 percent of the time")]
    [Range(0f, 1f)]
    [SerializeField] private float blinkOnPart = 0.3f;

    [Tooltip("The color of the blinking light")]
    [SerializeField] private Color ledColor = new Color(1f, 0.15f, 0.1f);

    [Tooltip("The volume of the hum up close, from 0 to 1")]
    [SerializeField] private float humVolume = 0.35f;

    [Tooltip("The hum is full up to the first distance and silent from the second, in world units from the listener. "
        + "The listener (on the main camera) is 10 units in front of the map, so the distances start at 10")]
    [SerializeField] private Vector2 humDistance = new Vector2(10f, 20f);

    private VisionSensor sensor;
    private SpriteRenderer led;

    private void Awake()
    {
        sensor = GetComponent<VisionSensor>();
        CreateLed();
        CreateHum();
    }

    private void Update()
    {
        // During the alarm the light stays on. Repeat(t, 1) counts from 0 to 1 again and again.
        led.enabled = sensor.IsAlarmed || Mathf.Repeat(Time.time * blinkRate, 1f) < blinkOnPart;
    }

    private void CreateLed()
    {
        var child = new GameObject("Led");
        child.transform.SetParent(transform, false);
        child.transform.localPosition = LedPosition;
        child.transform.localScale = Vector3.one * LedScale;
        led = child.AddComponent<SpriteRenderer>();
        led.sprite = Resources.Load<Sprite>("UI/Glow");
        led.color = ledColor;
        led.sortingOrder = LedSortingOrder;
    }

    private void CreateHum()
    {
        // A 3D sound: it gets quieter with distance.
        AudioSource hum = gameObject.AddComponent<AudioSource>();
        hum.clip = Resources.Load<AudioClip>("Audio/CameraHum");
        hum.loop = true;
        hum.spatialBlend = 1f;
        hum.rolloffMode = AudioRolloffMode.Linear;
        hum.minDistance = humDistance.x;
        hum.maxDistance = humDistance.y;
        hum.volume = humVolume;
        hum.Play();
    }
}
