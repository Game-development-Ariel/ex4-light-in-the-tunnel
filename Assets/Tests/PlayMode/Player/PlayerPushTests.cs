using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

/// <summary>
/// The player pushes loose things by walking into them. The push has to be the same at any frame rate:
/// before the cleanup a faster computer pushed harder, because the force was added once per frame.
/// </summary>
public class PlayerPushTests : PlayModeTest
{
    private PlayerPush push;
    private Rigidbody crate;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        GameObject hero = MakeOff("Player");
        TestHero.Create(hero);
        push = hero.AddComponent<PlayerPush>();
        hero.SetActive(true);

        GameObject box = Make("Crate");
        box.transform.position = new Vector3(5f, 0f, 0f);
        box.AddComponent<BoxCollider>();
        crate = box.AddComponent<Rigidbody>();
        crate.useGravity = false;
        crate.mass = 1f;
        crate.linearDamping = 0f;
        crate.angularDamping = 0f;
        crate.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
        yield return null;
    }

    // Unity makes this object when the controller touches a collider. The test makes the same one.
    private static ControllerColliderHit Hit(Collider collider, Vector3 direction)
    {
        var hit = (ControllerColliderHit)Activator.CreateInstance(typeof(ControllerColliderHit), true);
        TestUtils.Set(hit, "m_Collider", collider);
        TestUtils.Set(hit, "m_Point", collider.transform.position);
        TestUtils.Set(hit, "m_MoveDirection", direction);
        return hit;
    }

    private IEnumerator PushFor(float seconds, int framesPerSecond, Collider collider)
    {
        Time.captureFramerate = framesPerSecond;
        yield return null; // The first frame at the new rate.
        ControllerColliderHit hit = Hit(collider, Vector3.right);
        int frames = Mathf.RoundToInt(seconds * framesPerSecond);
        for (int i = 0; i < frames; i++)
        {
            TestUtils.Call(push, "OnControllerColliderHit", hit);
            yield return null;
        }
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
    }

    [UnityTest]
    public IEnumerator Push_MovesTheBody_InTheWalkingDirection()
    {
        yield return PushFor(0.5f, 60, crate.GetComponent<Collider>());

        Assert.Greater(crate.linearVelocity.x, 0f);
        Assert.AreEqual(0f, crate.linearVelocity.y, 1e-3f);
    }

    [UnityTest]
    public IEnumerator Push_GivesForceTimesTime()
    {
        float strength = TestUtils.Get<float>(push, "pushStrength");

        yield return PushFor(0.5f, 60, crate.GetComponent<Collider>());

        // An impulse of force * seconds on a body of 1 kg is that much speed.
        Assert.AreEqual(strength * 0.5f, crate.linearVelocity.x, strength * 0.5f * 0.03f);
    }

    // Regression (cleanup): the push used to be 4 times stronger at 120 fps than at 30 fps.
    [UnityTest]
    public IEnumerator Push_IsTheSame_At30And120FramesPerSecond()
    {
        yield return PushFor(1f, 30, crate.GetComponent<Collider>());
        float at30 = crate.linearVelocity.x;

        crate.linearVelocity = Vector3.zero;
        yield return new WaitForFixedUpdate();
        yield return PushFor(1f, 120, crate.GetComponent<Collider>());
        float at120 = crate.linearVelocity.x;

        Assert.Greater(at30, 0f);
        Assert.AreEqual(1f, at120 / at30, 0.03f, $"30 fps gave {at30:0.00}, 120 fps gave {at120:0.00}");
    }

    [UnityTest]
    public IEnumerator KinematicBody_IsNotPushed()
    {
        crate.isKinematic = true;

        yield return PushFor(0.2f, 60, crate.GetComponent<Collider>());

        Assert.AreEqual(new Vector3(5f, 0f, 0f), crate.position);
    }

    [UnityTest]
    public IEnumerator Wall_WithoutABody_IsIgnored()
    {
        GameObject wall = TestHero.Wall(Make("Wall"), new Vector3(-5f, 0f, 0f), Vector3.one);

        yield return PushFor(0.2f, 60, wall.GetComponent<Collider>());

        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator PushStarted_IsToldOnce_ForOneLongPush()
    {
        PushableSpy spy = crate.gameObject.AddComponent<PushableSpy>();

        yield return PushFor(0.5f, 60, crate.GetComponent<Collider>());

        Assert.AreEqual(1, spy.Pushes);
    }

    [UnityTest]
    public IEnumerator PushStarted_IsToldAgain_AfterAPause()
    {
        PushableSpy spy = crate.gameObject.AddComponent<PushableSpy>();
        float pause = TestUtils.Get<float>(push, "newPushAfterSeconds");

        yield return PushFor(0.1f, 60, crate.GetComponent<Collider>());
        yield return TestUtils.WaitGameSeconds(pause + 0.1f);
        yield return PushFor(0.1f, 60, crate.GetComponent<Collider>());

        Assert.AreEqual(2, spy.Pushes);
    }

    [UnityTest]
    public IEnumerator PushStarted_IsTold_ForEachNewObject()
    {
        PushableSpy first = crate.gameObject.AddComponent<PushableSpy>();
        GameObject other = Make("Sandbag");
        other.transform.position = new Vector3(0f, 5f, 0f);
        other.AddComponent<BoxCollider>();
        other.AddComponent<Rigidbody>().useGravity = false;
        PushableSpy second = other.AddComponent<PushableSpy>();

        Time.captureFramerate = 60;
        TestUtils.Call(push, "OnControllerColliderHit", Hit(crate.GetComponent<Collider>(), Vector3.right));
        TestUtils.Call(push, "OnControllerColliderHit", Hit(other.GetComponent<Collider>(), Vector3.up));
        TestUtils.Call(push, "OnControllerColliderHit", Hit(crate.GetComponent<Collider>(), Vector3.right));
        yield return null;

        Assert.AreEqual(2, first.Pushes);
        Assert.AreEqual(1, second.Pushes);
    }

    [UnityTest]
    public IEnumerator WalkingIntoACrate_PushesIt()
    {
        var keyboard = new VirtualKeyboard();
        try
        {
            crate.position = new Vector3(1f, 0f, 0f);
            crate.transform.position = crate.position;
            Physics.SyncTransforms();
            Time.captureFramerate = 60;
            keyboard.Hold(Key.D);

            yield return TestUtils.WaitFrames(90);

            Assert.Greater(crate.position.x, 1.2f, "The crate moved away from the player");
        }
        finally
        {
            keyboard.Remove();
        }
    }
}
