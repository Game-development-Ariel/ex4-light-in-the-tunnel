using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// A watcher at (0, 0) that looks up (+Y), 60 degrees wide and 5 tiles far. The rays stop at walls,
/// so the player is seen only inside the cone and with nothing between them.
/// </summary>
public class VisionSensorTests : PlayModeTest
{
    private VisionStats stats;
    private VisionSensor sensor;
    private Transform target;
    private EventSpy seen;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        stats = Track(TestData.Vision(60f, 5f, 0.5f));
        GameEvent targetSeen = Track(TestData.Event());
        seen = new EventSpy(targetSeen);

        GameObject player = Make("Player");
        player.layer = LayerMask.NameToLayer("Ignore Raycast");
        player.AddComponent<SphereCollider>().radius = 0.3f;
        target = player.transform;
        target.position = new Vector3(100f, 100f, 0f); // Out of sight until the test places it.

        GameObject watcher = MakeOff("Camera");
        sensor = watcher.AddComponent<VisionSensor>();
        TestUtils.Set(sensor, "stats", stats);
        TestUtils.Set(sensor, "target", target);
        TestUtils.Set(sensor, "targetSeen", targetSeen);
        watcher.SetActive(true);
        Time.captureFramerate = 60;
        yield return null;
    }

    private IEnumerator PlaceTarget(Vector3 position)
    {
        target.position = position;
        Physics.SyncTransforms();
        yield return null;
        yield return null;
    }

    private GameObject Wall(Vector3 position, bool trigger = false)
    {
        GameObject wall = TestHero.Wall(Make("Wall"), position, new Vector3(2f, 0.3f, 2f));
        wall.GetComponent<BoxCollider>().isTrigger = trigger;
        Physics.SyncTransforms();
        return wall;
    }

    [UnityTest]
    public IEnumerator SeesThePlayer_InTheCone_WithAClearLine()
    {
        yield return PlaceTarget(new Vector3(0f, 3f, 0f));

        Assert.AreEqual(1, seen.Count);
        Assert.IsTrue(sensor.IsAlarmed);
    }

    [UnityTest]
    public IEnumerator DoesNotSeeThePlayer_BehindAWall()
    {
        Wall(new Vector3(0f, 1.5f, 0f));

        yield return PlaceTarget(new Vector3(0f, 3f, 0f));

        Assert.AreEqual(0, seen.Count);
    }

    [UnityTest]
    public IEnumerator DoesNotSeeThePlayer_BehindACrate()
    {
        GameObject crate = Make("Crate");
        crate.transform.position = new Vector3(0f, 1.5f, 0f);
        crate.AddComponent<BoxCollider>();
        crate.AddComponent<Rigidbody>().isKinematic = true;
        Physics.SyncTransforms();

        yield return PlaceTarget(new Vector3(0f, 3f, 0f));

        Assert.AreEqual(0, seen.Count, "A pushed crate is a place to hide");
    }

    [UnityTest]
    public IEnumerator SeesThePlayer_InFrontOfAWall()
    {
        Wall(new Vector3(0f, 4f, 0f));

        yield return PlaceTarget(new Vector3(0f, 3f, 0f));

        Assert.AreEqual(1, seen.Count);
    }

    // The bottle and the hint zones are triggers. They must not hide the player.
    [UnityTest]
    public IEnumerator Triggers_DoNotBlockTheView()
    {
        Wall(new Vector3(0f, 1.5f, 0f), trigger: true);

        yield return PlaceTarget(new Vector3(0f, 3f, 0f));

        Assert.AreEqual(1, seen.Count);
    }

    [UnityTest]
    public IEnumerator EdgesOfTheCone()
    {
        // Half of 60 degrees is 30: 29 is inside, 31 is outside. 4.9 tiles is inside, 5.1 is outside.
        var places = new[]
        {
            (Quaternion.Euler(0f, 0f, 29f) * Vector3.up * 3f, true),
            (Quaternion.Euler(0f, 0f, -29f) * Vector3.up * 3f, true),
            (Quaternion.Euler(0f, 0f, 31f) * Vector3.up * 3f, false),
            (Quaternion.Euler(0f, 0f, -31f) * Vector3.up * 3f, false),
            (Vector3.up * 4.9f, true),
            (Vector3.up * 5.1f, false),
            (Vector3.down * 2f, false),
            (Vector3.right * 2f, false),
        };
        foreach ((Vector3 place, bool expected) in places)
        {
            sensor.enabled = false;
            TestUtils.Set(sensor, "alarmTimer", 0f);
            int before = seen.Count;
            sensor.enabled = true;
            yield return PlaceTarget(place);
            Assert.AreEqual(expected, seen.Count > before, $"Target at {place}");
        }
    }

    [UnityTest]
    public IEnumerator LooksWhereTheObjectFaces()
    {
        sensor.transform.rotation = Quaternion.Euler(0f, 0f, 180f); // Now it looks down.

        yield return PlaceTarget(new Vector3(0f, 3f, 0f));
        Assert.AreEqual(0, seen.Count);

        yield return PlaceTarget(new Vector3(0f, -3f, 0f));
        Assert.AreEqual(1, seen.Count);
    }

    // Regression (cleanup): a target exactly on the sensor divided by zero.
    [UnityTest]
    public IEnumerator TargetRightOnTheSensor_IsSeen_WithoutErrors()
    {
        yield return PlaceTarget(Vector3.zero);

        Assert.AreEqual(1, seen.Count);
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator AfterSeeing_ItWaitsAlarmSeconds_BeforeLookingAgain()
    {
        yield return PlaceTarget(new Vector3(0f, 3f, 0f));
        Assert.AreEqual(1, seen.Count);

        yield return TestUtils.WaitFrames(25); // 0.42 s, less than the 0.5 s alarm.
        Assert.AreEqual(1, seen.Count, "Still alarmed");

        yield return TestUtils.WaitFrames(10); // 0.58 s.
        Assert.AreEqual(2, seen.Count, "Looks again after the alarm");
    }

    [UnityTest]
    public IEnumerator IsAlarmed_EndsAfterAlarmSeconds()
    {
        yield return PlaceTarget(new Vector3(0f, 3f, 0f));
        target.position = new Vector3(100f, 100f, 0f);

        yield return TestUtils.WaitFrames(35);

        Assert.IsFalse(sensor.IsAlarmed);
    }

    [UnityTest]
    public IEnumerator NoTarget_SeesNothing_WithoutErrors()
    {
        TestUtils.Set(sensor, "target", null);

        yield return TestUtils.WaitFrames(5);

        Assert.AreEqual(0, seen.Count);
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator TurnedOff_SeesNothing()
    {
        sensor.enabled = false;

        yield return PlaceTarget(new Vector3(0f, 3f, 0f));

        Assert.AreEqual(0, seen.Count);
    }

    [Test]
    public void DistanceToWall_IsTheViewDistance_WhenNothingIsThere()
    {
        Assert.AreEqual(5f, sensor.DistanceToWall(Vector3.up), 1e-4f);
    }

    [Test]
    public void DistanceToWall_StopsAtTheWall()
    {
        Wall(new Vector3(0f, 2f, 0f));

        Assert.AreEqual(2f - 0.15f, sensor.DistanceToWall(Vector3.up), 0.01f);
    }

    [Test]
    public void DistanceToWall_IgnoresThePlayer()
    {
        target.position = new Vector3(0f, 2f, 0f);
        Physics.SyncTransforms();

        Assert.AreEqual(5f, sensor.DistanceToWall(Vector3.up), 1e-4f);
    }

    [Test]
    public void Stats_AreTheAssetGiven()
    {
        Assert.AreSame(stats, sensor.Stats);
    }
}
