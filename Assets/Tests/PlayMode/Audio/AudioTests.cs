using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

public class AudioTests : PlayModeTest
{
    [UnityTest]
    public IEnumerator Footsteps_PlayOnlyWhileThePlayerWalks()
    {
        var keyboard = new VirtualKeyboard();
        try
        {
            GameObject hero = MakeOff("Player");
            TestHero.Create(hero);
            hero.AddComponent<AudioSource>();
            Footsteps steps = hero.AddComponent<Footsteps>();
            hero.SetActive(true);
            yield return TestUtils.WaitFrames(10);
            Assert.AreEqual(0f, TestUtils.Get<float>(steps, "nextStepTime"), "No step while standing");

            keyboard.Hold(Key.D);
            yield return TestUtils.WaitFrames(5);

            Assert.Greater(TestUtils.Get<float>(steps, "nextStepTime"), 0f, "A step while walking");
            Vector2 pitch = TestUtils.Get<Vector2>(steps, "pitchRange");
            Assert.That(hero.GetComponent<AudioSource>().pitch, Is.InRange(pitch.x, pitch.y));
        }
        finally
        {
            keyboard.Remove();
        }
    }

    [UnityTest]
    public IEnumerator Footsteps_LoadEveryStepSound()
    {
        GameObject hero = MakeOff("Player");
        TestHero.Create(hero);
        hero.AddComponent<AudioSource>();
        Footsteps steps = hero.AddComponent<Footsteps>();
        hero.SetActive(true);
        yield return null;

        AudioClip[] clips = TestUtils.Get<AudioClip[]>(steps, "steps");
        Assert.GreaterOrEqual(clips.Length, 1);
        CollectionAssert.AllItemsAreNotNull(clips);
        Assert.IsNull(Resources.Load<AudioClip>("Audio/Step" + (clips.Length + 1)), "Stops at the first missing number");
    }

    [UnityTest]
    public IEnumerator MusicPlayer_WaitsThenFadesIn()
    {
        GameObject music = MakeOff("Music");
        AudioSource source = music.AddComponent<AudioSource>();
        source.clip = Resources.Load<AudioClip>("Audio/Music");
        MusicPlayer player = music.AddComponent<MusicPlayer>();
        TestUtils.Set(player, "startDelay", 0.1f);
        TestUtils.Set(player, "fadeSeconds", 0.1f);
        TestUtils.Set(player, "volume", 0.3f);
        music.SetActive(true);
        yield return null;

        Assert.AreEqual(0f, source.volume, "Silent while the hero wakes up");
        Assert.IsTrue(source.loop);

        yield return TestUtils.WaitGameSeconds(0.4f);
        Assert.AreEqual(0.3f, source.volume, 1e-4f);
    }

    [UnityTest]
    public IEnumerator Sfx_UsesOneSourceThatSurvivesSceneChanges()
    {
        Sfx.Play("Click", 0.1f);
        Sfx.Play("Hint", 0.1f);
        yield return null;

        GameObject[] players = Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Select(s => s.gameObject).Where(g => g.name == "Sound Effects").ToArray();
        Assert.AreEqual(1, players.Length);
        Assert.AreEqual("DontDestroyOnLoad", players[0].scene.name);
    }

    [UnityTest]
    public IEnumerator Sfx_UnknownSound_IsSilentAndSafe()
    {
        Sfx.Play("No Such Sound");
        yield return null;

        LogAssert.NoUnexpectedReceived();
    }
}
