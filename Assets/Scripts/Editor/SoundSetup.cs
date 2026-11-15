using UnityEngine;

/// <summary>
/// The sound of the tutorial: the tunnel, the music, and the heartbeat and breathing that Tension makes louder near danger.
/// </summary>
public static class SoundSetup
{
    // Volumes from 0 to 1. The music and the heartbeat start silent: MusicPlayer and Tension raise them.
    private const float AmbienceVolume = 0.35f;
    private const float BreathingVolume = 0.2f;
    private const float SilentVolume = 0f;

    public static Tension Setup(PlayerMovement player)
    {
        Object.DestroyImmediate(GameObject.Find("Ambience"));
        GameObject sound = SetupTools.Recreate("Sound");

        AudioSource ambience = LoopingSource(sound.transform, "Ambience", "Ambience", AmbienceVolume);
        ambience.playOnAwake = true;

        AudioSource music = LoopingSource(sound.transform, "Music", "Music", SilentVolume);
        music.playOnAwake = false;
        music.gameObject.AddComponent<MusicPlayer>();

        // The heartbeat and the breathing play all the time. Tension sets how loud they are.
        AudioSource heartbeat = LoopingSource(sound.transform, "Heartbeat", "Heartbeat", SilentVolume);
        AudioSource breathing = LoopingSource(sound.transform, "Breathing", "Breathing", BreathingVolume);
        Tension tension = sound.AddComponent<Tension>();
        SetupTools.SetField(tension, "heartbeat", heartbeat);
        SetupTools.SetField(tension, "breathing", breathing);
        SetupTools.SetField(tension, "player", player.transform);
        return tension;
    }

    private static AudioSource LoopingSource(Transform parent, string name, string clip, float volume)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent, false);
        AudioSource source = child.AddComponent<AudioSource>();
        source.clip = Resources.Load<AudioClip>("Audio/" + clip);
        source.loop = true;
        source.playOnAwake = true;
        source.volume = volume;
        return source;
    }
}
