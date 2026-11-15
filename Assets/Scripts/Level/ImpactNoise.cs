using UnityEngine;

/// <summary>
/// The noise a loose object makes: a bang when it hits a wall, and a softer sound when the player pushes it.
/// Both get louder with the speed. The first loud bang also raises a thought, to teach the player to push slowly.
/// Needs a Rigidbody.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ImpactNoise : MonoBehaviour, IPushable
{
    [Tooltip("Which sounds this object makes, and how loud")]
    [SerializeField] private ImpactSounds sounds;

    [Tooltip("Seconds before the same sound can play again, so one bump doesn't play it many times")]
    [SerializeField] private float quietSeconds = 0.3f;

    [Tooltip("A wall hit at this speed or faster raises the warning thought (the first time only)")]
    [SerializeField] private float warnAboveSpeed = 1.2f;

    [TextArea]
    [SerializeField] private string warning = "Too loud. I push slowly, so nobody hears.";

    [Tooltip("Raised with the warning, the first time only")]
    [SerializeField] private TextEvent hintRequested;

    private float nextHitTime;
    private float nextTouchTime;
    private bool warned;

    private void OnCollisionEnter(Collision collision)
    {
        // Only a hit against something that doesn't move (a wall) is loud. Another crate or the player is soft.
        bool hitWall = collision.rigidbody == null && collision.collider.GetComponent<CharacterController>() == null;
        float speed = collision.relativeVelocity.magnitude;
        if (!hitWall || speed < sounds.silentBelowSpeed)
        {
            return;
        }

        Play(PickOne(sounds.wallHitSounds), sounds.VolumeFor(speed, sounds.fullVolumeHitSpeed), ref nextHitTime);
        if (!warned && speed >= warnAboveSpeed)
        {
            warned = true;
            hintRequested.Raise(warning);
        }
    }

    public void PushStarted(float playerSpeed)
    {
        Play(PickOne(sounds.touchSounds), sounds.VolumeFor(playerSpeed, sounds.fullVolumeTouchSpeed), ref nextTouchTime);
    }

    // A random one of a few recordings, so the same bump doesn't sound exactly the same every time.
    private static string PickOne(string[] names)
    {
        return names[Random.Range(0, names.Length)];
    }

    // Each sound has its own timer, so touching a crate doesn't silence the bang right after it.
    private void Play(string sound, float volume, ref float nextTime)
    {
        if (Time.time < nextTime)
        {
            return;
        }
        nextTime = Time.time + quietSeconds;
        Sfx.Play(sound, volume);
    }
}
