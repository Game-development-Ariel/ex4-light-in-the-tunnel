using UnityEngine;

/// <summary>
/// The noise a loose object makes: a bang when it hits a wall, and a softer sound when the player pushes it.
/// Both get louder with the speed. Every wall hit is also reported to the level's NoiseWarning.
/// Needs a Rigidbody.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ImpactNoise : MonoBehaviour, IPushable
{
    [Tooltip("Which sounds this object makes, and how loud")]
    [SerializeField] private ImpactSounds sounds;

    [Tooltip("Seconds before the same sound can play again, so one bump doesn't play it many times")]
    [SerializeField] private float quietSeconds = 0.3f;

    [Tooltip("The level's warning about loud noise. One for all the loose objects, so it is shown once")]
    [SerializeField] private NoiseWarning warning;

    private float nextHitTime;
    private float nextTouchTime;

    private void OnCollisionEnter(Collision collision)
    {
        // Only a hit against something that doesn't move (a wall) is loud. Another crate or the player is soft.
        bool hitWall = collision.rigidbody == null && collision.collider.GetComponent<CharacterController>() == null;
        float speed = collision.relativeVelocity.magnitude;
        if (!hitWall || speed < sounds.silentBelowSpeed)
        {
            return;
        }

        Play(sounds.wallHitSounds, sounds.HitVolume(speed), ref nextHitTime);
        warning.WallHit(speed);
    }

    public void PushStarted(float playerSpeed)
    {
        Play(sounds.touchSounds, sounds.TouchVolume(playerSpeed), ref nextTouchTime);
    }

    // Each sound has its own timer, so touching a crate doesn't silence the bang right after it.
    // A random one of a few recordings, so the same bump doesn't sound exactly the same every time.
    private void Play(string[] names, float volume, ref float nextTime)
    {
        if (names.Length == 0 || Time.time < nextTime)
        {
            return;
        }
        nextTime = Time.time + quietSeconds;
        Sfx.Play(names[Random.Range(0, names.Length)], volume);
    }
}
