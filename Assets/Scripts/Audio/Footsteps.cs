using UnityEngine;

/// <summary>
/// Plays a soft footstep (bare feet on sandy concrete) every few steps while the player walks.
/// It reads the speed from the CharacterController, so it stops by itself when the player stops or hits a wall.
/// </summary>
[RequireComponent(typeof(CharacterController), typeof(AudioSource))]
public class Footsteps : MonoBehaviour
{
    // Slower than this counts as standing still.
    private const float MinWalkSpeed = 0.3f;

    [Tooltip("Seconds between two steps")]
    [SerializeField] private float stepInterval = 0.5f;

    [Tooltip("Each step gets a random pitch in this range, so walking doesn't sound like a loop")]
    [SerializeField] private Vector2 pitchRange = new Vector2(0.85f, 1.1f);

    [SerializeField] private float volume = 0.35f;

    private CharacterController controller;
    private AudioSource audioSource;
    private AudioClip[] steps;
    private float nextStepTime;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();
        steps = new AudioClip[4];
        for (int i = 0; i < steps.Length; i++)
        {
            steps[i] = Resources.Load<AudioClip>("Audio/Step" + (i + 1));
        }
    }

    private void Update()
    {
        bool isWalking = controller.velocity.magnitude > MinWalkSpeed;
        if (!isWalking || Time.time < nextStepTime)
        {
            return;
        }

        audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        audioSource.PlayOneShot(steps[Random.Range(0, steps.Length)], volume);
        nextStepTime = Time.time + stepInterval;
    }
}
