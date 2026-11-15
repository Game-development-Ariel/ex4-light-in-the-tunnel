using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

/// <summary>The player walks with the keys, stops at walls, and turns the body toward where he walks.</summary>
public class PlayerMovementTests : PlayModeTest
{
    private VirtualKeyboard keyboard;
    private PlayerMovement player;
    private Transform body;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        keyboard = new VirtualKeyboard();
        GameObject hero = MakeOff("Player");
        player = TestHero.Create(hero, 2f);
        body = new GameObject("Body").transform;
        body.SetParent(hero.transform, false);
        TestUtils.Set(player, "body", body);
        hero.SetActive(true);
        Time.captureFramerate = 60; // The same frame length every run, so distances are exact.
        yield return null;
    }

    [TearDown]
    public void RemoveKeyboard()
    {
        keyboard.Remove();
    }

    [UnityTest]
    public IEnumerator HoldingD_WalksRight()
    {
        keyboard.Hold(Key.D);
        yield return TestUtils.WaitFrames(30);

        Assert.Greater(player.transform.position.x, 0.5f);
        Assert.AreEqual(0f, player.transform.position.y, 0.01f);
    }

    [UnityTest]
    public IEnumerator EveryDirectionKey_WalksItsWay()
    {
        var keys = new[] { Key.W, Key.S, Key.A, Key.D, Key.UpArrow, Key.DownArrow, Key.LeftArrow, Key.RightArrow };
        var ways = new[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right, Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        for (int i = 0; i < keys.Length; i++)
        {
            Vector2 before = player.transform.position;
            keyboard.Hold(keys[i]);
            yield return TestUtils.WaitFrames(10);
            keyboard.ReleaseAll();
            yield return TestUtils.WaitFrames(2);
            Vector2 moved = (Vector2)player.transform.position - before;
            Assert.Greater(Vector2.Dot(moved.normalized, ways[i]), 0.99f, $"{keys[i]} moved the player {moved}");
        }
    }

    [UnityTest]
    public IEnumerator Speed_IsMoveSpeedTimesTheSpeedFactor()
    {
        player.SpeedFactor = 1.5f;
        keyboard.Hold(Key.D);
        yield return null; // The key is read from the next frame.
        float startX = player.transform.position.x;

        yield return TestUtils.WaitFrames(60);

        Assert.AreEqual(3f, player.CurrentSpeed, 1e-4f);
        Assert.AreEqual(3f, player.transform.position.x - startX, 0.1f, "One second at 2 * 1.5 tiles per second");
        Assert.AreEqual(3f, player.WalkingSpeed, 1e-4f);
    }

    [UnityTest]
    public IEnumerator Diagonal_IsNotFasterThanStraight()
    {
        keyboard.Hold(Key.D, Key.W);
        yield return null;
        Vector2 start = player.transform.position;

        yield return TestUtils.WaitFrames(60);

        Assert.AreEqual(2f, Vector2.Distance(start, player.transform.position), 0.1f);
    }

    [UnityTest]
    public IEnumerator IsWalking_WhileMoving_AndNotWhileStanding()
    {
        yield return null;
        Assert.IsFalse(player.IsWalking);
        Assert.AreEqual(0f, player.WalkingSpeed);

        keyboard.Hold(Key.D);
        yield return TestUtils.WaitFrames(5);
        Assert.IsTrue(player.IsWalking);

        keyboard.ReleaseAll();
        yield return TestUtils.WaitFrames(3);
        Assert.IsFalse(player.IsWalking);
    }

    // Regression (cleanup): steps and the walking bounce played while the player pushed against a wall.
    [UnityTest]
    public IEnumerator IsWalking_IsFalse_WhenWalkingIntoAWall()
    {
        TestHero.Wall(Make("Wall"), new Vector3(1f, 0f, 0f), new Vector3(0.2f, 4f, 4f));
        Physics.SyncTransforms();
        keyboard.Hold(Key.D);

        yield return TestUtils.WaitFrames(60);

        Assert.Less(player.transform.position.x, 0.9f, "The wall stops the player");
        Assert.IsFalse(player.IsWalking, "Pushing into a wall is not walking");
        Assert.Greater(player.WalkingSpeed, 0f, "The key is still held");
    }

    [UnityTest]
    public IEnumerator Wall_StopsThePlayer_ButHeSlidesAlongIt()
    {
        TestHero.Wall(Make("Wall"), new Vector3(1f, 0f, 0f), new Vector3(0.2f, 10f, 4f));
        Physics.SyncTransforms();
        keyboard.Hold(Key.D, Key.W);

        yield return TestUtils.WaitFrames(60);

        Assert.Less(player.transform.position.x, 0.9f);
        Assert.Greater(player.transform.position.y, 0.5f, "Slides up along the wall instead of getting stuck");
    }

    // Regression (cleanup): a frozen player kept the controller's last speed, so he still "walked".
    [UnityTest]
    public IEnumerator IsWalking_IsFalse_WhileFrozen()
    {
        keyboard.Hold(Key.D);
        yield return TestUtils.WaitFrames(5);
        Assert.IsTrue(player.IsWalking);

        player.enabled = false;

        Assert.IsFalse(player.IsWalking);
        Assert.AreEqual(0f, player.WalkingSpeed, "The input is cleared when the movement is turned off");
        float x = player.transform.position.x;
        yield return TestUtils.WaitFrames(10);
        Assert.AreEqual(x, player.transform.position.x, "A frozen player doesn't move");
    }

    [UnityTest]
    public IEnumerator Body_TurnsTowardTheWalkingDirection()
    {
        keyboard.Hold(Key.D);
        yield return TestUtils.WaitFrames(30);

        // The sprite faces up, so walking right is a turn of -90 degrees.
        Assert.Less(Quaternion.Angle(body.rotation, Quaternion.Euler(0f, 0f, -90f)), 1f);
    }

    [UnityTest]
    public IEnumerator Body_TurnsSmoothly_NotAtOnce()
    {
        keyboard.Hold(Key.S);
        yield return null;
        yield return null;

        float angle = Quaternion.Angle(body.rotation, Quaternion.identity);
        Assert.Greater(angle, 0f);
        Assert.Less(angle, 180f);
    }

    [UnityTest]
    public IEnumerator Body_KeepsFacingTheLastWay_WhenThePlayerStops()
    {
        keyboard.Hold(Key.A);
        yield return TestUtils.WaitFrames(30);
        Quaternion facing = body.rotation;

        keyboard.ReleaseAll();
        yield return TestUtils.WaitFrames(10);

        Assert.Less(Quaternion.Angle(facing, body.rotation), 0.01f);
    }

    [Test]
    public void NoKeys_AddsTheDefaultBindings()
    {
        var action = TestUtils.Get<InputAction>(player, "moveAction");
        Assert.GreaterOrEqual(action.bindings.Count, 3);
    }
}
