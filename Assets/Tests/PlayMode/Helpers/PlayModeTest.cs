using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

/// <summary>
/// The base of the play mode tests that build what they need in code. Objects made with Make are destroyed
/// after each test, and time is set back to normal, so one test never leaks into the next.
/// </summary>
public abstract class PlayModeTest
{
    private readonly List<Object> created = new List<Object>();
    private float volumeBefore;

    /// <summary>
    /// Makes an object that is off. Components added to it don't run Awake until it is turned on,
    /// so the test can fill their fields first, like the Inspector does.
    /// </summary>
    protected GameObject MakeOff(string name)
    {
        GameObject made = Make(name);
        made.SetActive(false);
        return made;
    }

    protected GameObject Make(string name)
    {
        return Track(new GameObject(name));
    }

    protected T Track<T>(T thing) where T : Object
    {
        created.Add(thing);
        return thing;
    }

    /// <summary>
    /// Every frame is 1/60 of a second of game time, however fast the computer really runs it,
    /// so the same test gives the same numbers every time. A test can change it for itself.
    /// </summary>
    [UnitySetUp]
    public IEnumerator FixTheFrameRate()
    {
        Time.timeScale = 1f;
        Time.captureFramerate = 60;
        volumeBefore = AudioListener.volume;
        AudioListener.volume = 0f; // Silent: the tests check what plays, not the speakers.
        Make("Listener").AddComponent<AudioListener>(); // Unity logs a warning when sounds play with no listener.
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator DestroyCreated()
    {
        Time.timeScale = 1f;
        Time.captureFramerate = 0;
        AudioListener.volume = volumeBefore;
        foreach (Object thing in created)
        {
            if (thing != null)
            {
                Object.Destroy(thing);
            }
        }
        created.Clear();
        foreach (EventSystem system in Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
        {
            Object.Destroy(system.gameObject);
        }
        yield return null;
    }
}
