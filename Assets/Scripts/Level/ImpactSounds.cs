using UnityEngine;

/// <summary>
/// The sounds of one kind of loose object (a crate, a sandbag), and how loud they get.
/// A new kind of loose object with its own sounds is a new asset of this type, and not new code.
/// </summary>
[CreateAssetMenu(menuName = "Tunnel/Impact Sounds")]
public class ImpactSounds : ScriptableObject
{
    [Tooltip("Played when the object bangs into a wall, one of them at random. Names of sounds in Resources/Audio")]
    public string[] wallHitSounds = { "CrateHit1" };

    [Tooltip("Played when the player walks into the object, one of them at random. Names of sounds in Resources/Audio")]
    public string[] touchSounds = { "CrateTouch" };

    [Tooltip("Wall hits slower than this, in units per second, make no sound")]
    public float silentBelowSpeed = 0.3f;

    [Tooltip("A wall hit at this speed or faster plays at full volume")]
    [Min(0.01f)]
    public float fullVolumeHitSpeed = 2f;

    [Tooltip("The player's walking speed that plays the touch at full volume")]
    [Min(0.01f)]
    public float fullVolumeTouchSpeed = 3.2f;

    [Tooltip("The quietest a sound plays, so even a soft one is heard")]
    [Range(0f, 1f)]
    public float minVolume = 0.25f;

    /// <summary>How loud a wall hit at this speed is.</summary>
    public float HitVolume(float speed)
    {
        return VolumeFor(speed, fullVolumeHitSpeed);
    }

    /// <summary>How loud the touch is when the player walks into the object at this speed.</summary>
    public float TouchVolume(float playerSpeed)
    {
        return VolumeFor(playerSpeed, fullVolumeTouchSpeed);
    }

    // A soft hit plays at minVolume, and the volume grows with the speed up to 1 at fullVolumeSpeed.
    private float VolumeFor(float speed, float fullVolumeSpeed)
    {
        return Mathf.Lerp(minVolume, 1f, Mathf.Clamp01(speed / fullVolumeSpeed));
    }
}
