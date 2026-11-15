using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plays a soft footstep (bare feet on sandy concrete) every few steps while the player walks.
/// The steps are the sounds Step1, Step2, ... in Resources/Audio, as many as there are.
/// </summary>
[RequireComponent(typeof(PlayerMovement), typeof(AudioSource))]
public class Footsteps : MonoBehaviour
{
    [Tooltip("Seconds between two steps")]
    [SerializeField] private float stepInterval = 0.5f;

    [Tooltip("Each step gets a random pitch in this range, so walking doesn't sound like a loop")]
    [SerializeField] private Vector2 pitchRange = new Vector2(0.85f, 1.1f);

    [Tooltip("The volume of each step, from 0 to 1")]
    [SerializeField] private float volume = 0.35f;

    private PlayerMovement movement;
    private AudioSource audioSource;
    private AudioClip[] steps;
    private float nextStepTime;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        audioSource = GetComponent<AudioSource>();

        // Loads Step1, Step2, ... until there is no next one, so adding a step sound needs no code.
        var found = new List<AudioClip>();
        AudioClip step = Resources.Load<AudioClip>("Audio/Step1");
        while (step != null)
        {
            found.Add(step);
            step = Resources.Load<AudioClip>("Audio/Step" + (found.Count + 1));
        }
        steps = found.ToArray();
    }

    private void Update()
    {
        if (!movement.IsWalking || steps.Length == 0 || Time.time < nextStepTime)
        {
            return;
        }

        audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        audioSource.PlayOneShot(steps[Random.Range(0, steps.Length)], volume);
        nextStepTime = Time.time + stepInterval;
    }
}
