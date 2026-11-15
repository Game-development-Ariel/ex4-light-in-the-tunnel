using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>A pushed crate that bangs into a wall makes a noise, and a loud bang warns the player once.</summary>
public class ImpactNoiseTests : PlayModeTest
{
    private ImpactNoise noise;
    private Rigidbody crate;
    private EventSpy hints;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        TextEvent hintRequested = Track(TestData.TextEvent());
        hints = new EventSpy(hintRequested);
        GameObject warningObject = MakeOff("Noise Warning");
        NoiseWarning warning = warningObject.AddComponent<NoiseWarning>();
        TestUtils.Set(warning, "hintRequested", hintRequested);
        warningObject.SetActive(true);

        TestHero.Wall(Make("Wall"), new Vector3(2f, 0f, 0f), new Vector3(0.5f, 4f, 4f));

        GameObject box = MakeOff("Crate");
        box.transform.position = new Vector3(1.1f, 0f, 0f); // 0.15 from the wall.
        box.AddComponent<BoxCollider>();
        crate = box.AddComponent<Rigidbody>();
        crate.useGravity = false;
        crate.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        crate.collisionDetectionMode = CollisionDetectionMode.Continuous;
        noise = box.AddComponent<ImpactNoise>();
        TestUtils.Set(noise, "sounds", Track(TestData.Sounds()));
        TestUtils.Set(noise, "warning", warning);
        box.SetActive(true);
        Physics.SyncTransforms();
        yield return new WaitForFixedUpdate();
    }

    private float NextHitTime => TestUtils.Get<float>(noise, "nextHitTime");

    private IEnumerator ThrowAtTheWall(float speed)
    {
        crate.linearVelocity = new Vector3(speed, 0f, 0f);
        float until = Time.time + 3f;
        while (crate.linearVelocity.x > 0f && Time.time < until)
        {
            yield return new WaitForFixedUpdate();
        }
        yield return new WaitForFixedUpdate();
    }

    [UnityTest]
    public IEnumerator LoudBang_PlaysAHit_AndWarnsThePlayer()
    {
        yield return ThrowAtTheWall(6f);

        Assert.Greater(NextHitTime, 0f, "The hit sound played");
        Assert.AreEqual(1, hints.Count);
    }

    [UnityTest]
    public IEnumerator SoftBump_IsSilent()
    {
        yield return ThrowAtTheWall(0.2f); // Below silentBelowSpeed.

        Assert.AreEqual(0f, NextHitTime);
        Assert.AreEqual(0, hints.Count);
    }

    [UnityTest]
    public IEnumerator MediumBang_PlaysAHit_ButDoesNotWarn()
    {
        yield return ThrowAtTheWall(0.8f);

        Assert.Greater(NextHitTime, 0f);
        Assert.AreEqual(0, hints.Count);
    }

    [UnityTest]
    public IEnumerator BumpingAnotherCrate_IsSoft()
    {
        GameObject other = Make("Other Crate");
        other.transform.position = new Vector3(-0.3f, 0f, 0f);
        other.AddComponent<BoxCollider>();
        other.AddComponent<Rigidbody>().useGravity = false;
        Physics.SyncTransforms();

        crate.linearVelocity = new Vector3(-5f, 0f, 0f);
        yield return TestUtils.WaitGameSeconds(0.3f);

        Assert.AreEqual(0, hints.Count, "Only a wall makes the loud bang");
    }

    [UnityTest]
    public IEnumerator PushStarted_PlaysTheTouch_OncePerQuietTime()
    {
        noise.PushStarted(2f);
        float first = TestUtils.Get<float>(noise, "nextTouchTime");
        noise.PushStarted(2f);

        Assert.Greater(first, 0f);
        Assert.AreEqual(first, TestUtils.Get<float>(noise, "nextTouchTime"), "Too soon for the same sound again");
        yield return TestUtils.WaitGameSeconds(TestUtils.Get<float>(noise, "quietSeconds") + 0.05f);

        noise.PushStarted(2f);
        Assert.Greater(TestUtils.Get<float>(noise, "nextTouchTime"), first);
    }

    [UnityTest]
    public IEnumerator EmptySoundList_IsSafe()
    {
        ImpactSounds sounds = TestUtils.Get<ImpactSounds>(noise, "sounds");
        sounds.touchSounds = new string[0];
        sounds.wallHitSounds = new string[0];

        noise.PushStarted(2f);
        yield return ThrowAtTheWall(6f);

        Assert.AreEqual(0f, TestUtils.Get<float>(noise, "nextTouchTime"));
        LogAssert.NoUnexpectedReceived();
    }
}
