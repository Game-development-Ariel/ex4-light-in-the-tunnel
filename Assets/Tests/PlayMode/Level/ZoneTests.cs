using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>The teaching places on the map: hints, memories and the quiet corner for the verse.</summary>
public class ZoneTests : PlayModeTest
{
    private PlayerMovement player;
    private Hope hope;
    private TextEvent textEvent;
    private EventSpy texts;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        GameObject hero = MakeOff("Player");
        hope = hero.AddComponent<Hope>();
        TestUtils.Set(hope, "settings", Track(TestData.Settings(100, 30)));
        player = TestHero.Create(hero);
        hero.SetActive(true);
        textEvent = Track(TestData.TextEvent());
        texts = new EventSpy(textEvent);
        yield return null;
    }

    private Collider PlayerCollider => player.GetComponent<Collider>();

    private T MakeZone<T>(string text, string textField) where T : MonoBehaviour
    {
        GameObject zone = MakeOff(typeof(T).Name);
        zone.transform.position = new Vector3(50f, 0f, 0f); // Away from the player, so only the test touches it.
        zone.AddComponent<BoxCollider>().isTrigger = true;
        T script = zone.AddComponent<T>();
        TestUtils.Set(script, textField, text);
        zone.SetActive(true);
        return script;
    }

    [UnityTest]
    public IEnumerator HintZone_ShowsItsHintOnce_ThenDisappears()
    {
        HintZone zone = MakeZone<HintZone>("Watch the light.", "hint");
        TestUtils.Set(zone, "hintRequested", textEvent);

        TestUtils.Call(zone, "OnTriggerEnter", PlayerCollider);
        yield return null;

        Assert.AreEqual("Watch the light.", texts.Last);
        Assert.IsTrue(zone == null, "Shown once: the zone is gone");
    }

    [UnityTest]
    public IEnumerator HintZone_IgnoresEverythingButThePlayer()
    {
        HintZone zone = MakeZone<HintZone>("Watch the light.", "hint");
        TestUtils.Set(zone, "hintRequested", textEvent);

        TestUtils.Call(zone, "OnTriggerEnter", Make("Crate").AddComponent<BoxCollider>());
        yield return null;

        Assert.AreEqual(0, texts.Count);
        Assert.IsTrue(zone != null);
    }

    [UnityTest]
    public IEnumerator MemoryZone_GivesHopeAndItsMemory_Once()
    {
        MemoryZone zone = MakeZone<MemoryZone>("My mother's voice.", "memory");
        TestUtils.Set(zone, "memoryFound", textEvent);
        TestUtils.Set(zone, "hopeGained", 15);

        TestUtils.Call(zone, "OnTriggerEnter", PlayerCollider);
        yield return null;

        Assert.AreEqual(45, hope.Current);
        Assert.AreEqual("My mother's voice.", texts.Last);
        Assert.IsTrue(zone == null);
    }

    [UnityTest]
    public IEnumerator MemoryZone_IgnoresEverythingButThePlayer()
    {
        MemoryZone zone = MakeZone<MemoryZone>("memory", "memory");
        TestUtils.Set(zone, "memoryFound", textEvent);

        TestUtils.Call(zone, "OnTriggerEnter", Make("Crate").AddComponent<BoxCollider>());
        yield return null;

        Assert.AreEqual(30, hope.Current);
        Assert.AreEqual(0, texts.Count);
    }

    private QuietPlace MakeQuietPlace(float stillSeconds)
    {
        QuietPlace place = MakeZone<QuietPlace>("The verse.", "verse");
        TestUtils.Set(place, "memoryFound", textEvent);
        TestUtils.Set(place, "hopeGained", 20);
        TestUtils.Set(place, "stillSeconds", stillSeconds);
        return place;
    }

    // OnTriggerStay comes once every physics step, so standing N seconds is N / fixedDeltaTime calls.
    private static int Steps(float seconds)
    {
        return Mathf.CeilToInt(seconds / Time.fixedDeltaTime);
    }

    [UnityTest]
    public IEnumerator QuietPlace_StandingStillLongEnough_GivesHopeAndTheVerse()
    {
        QuietPlace place = MakeQuietPlace(1f);

        for (int i = 0; i < Steps(1f) + 1; i++)
        {
            TestUtils.Call(place, "OnTriggerStay", PlayerCollider);
        }
        yield return null;

        Assert.AreEqual(50, hope.Current);
        Assert.AreEqual("The verse.", texts.Last);
        Assert.IsTrue(place == null);
    }

    // Bug found by the tests: Destroy waits for the end of the frame, and a slow frame runs several physics
    // steps, so the place gave its hope (and its verse) once for every extra step.
    [UnityTest]
    public IEnumerator QuietPlace_GivesItsHopeOnlyOnce_EvenInASlowFrame()
    {
        QuietPlace place = MakeQuietPlace(1f);

        for (int i = 0; i < Steps(1f) + 5; i++)
        {
            TestUtils.Call(place, "OnTriggerStay", PlayerCollider);
        }
        yield return null;

        Assert.AreEqual(50, hope.Current);
        Assert.AreEqual(1, texts.Count);
    }

    [UnityTest]
    public IEnumerator QuietPlace_StandingNotLongEnough_GivesNothing()
    {
        QuietPlace place = MakeQuietPlace(1f);

        for (int i = 0; i < Steps(1f) - 2; i++)
        {
            TestUtils.Call(place, "OnTriggerStay", PlayerCollider);
        }
        yield return null;

        Assert.AreEqual(30, hope.Current);
        Assert.IsTrue(place != null);
    }

    [UnityTest]
    public IEnumerator QuietPlace_Walking_StartsTheCountOver()
    {
        QuietPlace place = MakeQuietPlace(1f);
        var controller = player.GetComponent<CharacterController>();

        for (int i = 0; i < Steps(0.8f); i++)
        {
            TestUtils.Call(place, "OnTriggerStay", PlayerCollider);
        }
        controller.Move(Vector3.right * 0.1f); // One step: the controller has speed now, so the player is walking.
        Assert.IsTrue(player.IsWalking);
        TestUtils.Call(place, "OnTriggerStay", PlayerCollider);
        yield return null;
        controller.Move(Vector3.zero);
        for (int i = 0; i < Steps(0.8f); i++)
        {
            TestUtils.Call(place, "OnTriggerStay", PlayerCollider);
        }

        Assert.AreEqual(30, hope.Current, "0.8 + 0.8 seconds with a step between is not 1 second still");
    }

    // Regression (cleanup): leaving the corner kept the count, so coming back finished the verse at once.
    [UnityTest]
    public IEnumerator QuietPlace_Leaving_StartsTheCountOver()
    {
        QuietPlace place = MakeQuietPlace(1f);

        for (int i = 0; i < Steps(0.8f); i++)
        {
            TestUtils.Call(place, "OnTriggerStay", PlayerCollider);
        }
        TestUtils.Call(place, "OnTriggerExit", PlayerCollider);
        for (int i = 0; i < Steps(0.8f); i++)
        {
            TestUtils.Call(place, "OnTriggerStay", PlayerCollider);
        }
        yield return null;

        Assert.AreEqual(30, hope.Current);
    }

    [UnityTest]
    public IEnumerator QuietPlace_SomethingElseLeaving_DoesNotBreakTheMoment()
    {
        QuietPlace place = MakeQuietPlace(1f);
        var crate = Make("Crate").AddComponent<BoxCollider>();

        for (int i = 0; i < Steps(0.8f); i++)
        {
            TestUtils.Call(place, "OnTriggerStay", PlayerCollider);
        }
        TestUtils.Call(place, "OnTriggerExit", crate);
        TestUtils.Call(place, "OnTriggerStay", crate);
        for (int i = 0; i < Steps(0.3f); i++)
        {
            TestUtils.Call(place, "OnTriggerStay", PlayerCollider);
        }
        yield return null;

        Assert.AreEqual(50, hope.Current);
    }
}
