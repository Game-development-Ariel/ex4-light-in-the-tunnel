using UnityEngine;

/// <summary>
/// The small signs that a security camera is on: a red light that blinks on the lens,
/// and a hum that gets louder when the player is closer. Both are created here in code.
/// </summary>
[RequireComponent(typeof(VisionSensor))]
public class CameraIndicators : MonoBehaviour
{
    [Tooltip("Blinks per second of the red light")]
    [SerializeField] private float blinkRate = 1.2f;

    [SerializeField] private float humVolume = 0.35f;

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
        led.enabled = sensor.IsAlarmed || Mathf.Repeat(Time.time * blinkRate, 1f) < 0.3f;
    }

    private void CreateLed()
    {
        var child = new GameObject("Led");
        child.transform.SetParent(transform, false);
        child.transform.localPosition = new Vector3(0f, 0.32f, 0f);
        child.transform.localScale = Vector3.one * 0.35f;
        led = child.AddComponent<SpriteRenderer>();
        led.sprite = Resources.Load<Sprite>("UI/Glow");
        led.color = new Color(1f, 0.15f, 0.1f);
        led.sortingOrder = 7;
    }

    private void CreateHum()
    {
        // A 3D sound: it gets quieter with distance. The listener (on the main camera) is 10 units
        // in front of the map, so the distances start at 10.
        AudioSource hum = gameObject.AddComponent<AudioSource>();
        hum.clip = Resources.Load<AudioClip>("Audio/CameraHum");
        hum.loop = true;
        hum.spatialBlend = 1f;
        hum.rolloffMode = AudioRolloffMode.Linear;
        hum.minDistance = 10f;
        hum.maxDistance = 20f;
        hum.volume = humVolume;
        hum.Play();
    }
}
