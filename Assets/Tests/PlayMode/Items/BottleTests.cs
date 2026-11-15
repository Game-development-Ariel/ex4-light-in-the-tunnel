using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>The water bottle, the spawner that makes it, and the mattress where it is brought.</summary>
public class BottleTests : PlayModeTest
{
    private GameObject bottlePrefab;
    private BottleSpawner spawner;
    private BottleCarrier carrier;
    private GameEvent bottleDropped;
    private GameEvent playerCaught;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        // A stand-in for the prefab: a turned-off object that Instantiate copies.
        bottlePrefab = MakeOff("Bottle Prefab");
        bottlePrefab.AddComponent<SphereCollider>().isTrigger = true;
        bottlePrefab.AddComponent<WaterBottle>();

        bottleDropped = Track(TestData.Event());
        playerCaught = Track(TestData.Event());
        GameObject spawnPoint = MakeOff("BottleSpawn");
        spawnPoint.transform.position = new Vector3(3f, 4f, 0f);
        spawner = spawnPoint.AddComponent<BottleSpawner>();
        TestUtils.Set(spawner, "bottlePrefab", bottlePrefab);
        TestUtils.Set(spawner, "bottleDropped", bottleDropped);

        GameObject hero = MakeOff("Player");
        TestHero.Create(hero);
        carrier = hero.AddComponent<BottleCarrier>();
        TestUtils.Set(carrier, "bottlePicked", Track(TestData.Event()));
        TestUtils.Set(carrier, "bottleDropped", bottleDropped);
        TestUtils.Set(carrier, "playerCaught", playerCaught);

        hero.SetActive(true);
        spawnPoint.SetActive(true);
        yield return null;
    }

    private WaterBottle Bottle()
    {
        Assert.IsNotNull(spawner.Bottle, "No bottle on the floor");
        WaterBottle bottle = spawner.Bottle.GetComponent<WaterBottle>();
        bottle.gameObject.SetActive(true); // The stand-in prefab is off, so its copy is too.
        Track(bottle.gameObject);
        return bottle;
    }

    [Test]
    public void Spawner_PutsABottleAtItsPlace_WhenTheLevelStarts()
    {
        Assert.IsNotNull(spawner.Bottle);
        Assert.AreEqual(new Vector3(3f, 4f, 0f), spawner.Bottle.position);
    }

    [UnityTest]
    public IEnumerator TouchingTheBottle_PicksItUp_AndTakesItOffTheFloor()
    {
        WaterBottle bottle = Bottle();

        TestUtils.Call(bottle, "OnTriggerEnter", carrier.GetComponent<Collider>());
        yield return null;

        Assert.IsTrue(carrier.HasBottle);
        Assert.IsTrue(spawner.Bottle == null, "The bottle destroyed itself");
    }

    [UnityTest]
    public IEnumerator SomethingElse_TouchingTheBottle_DoesNothing()
    {
        WaterBottle bottle = Bottle();
        var crate = Make("Crate").AddComponent<BoxCollider>();

        TestUtils.Call(bottle, "OnTriggerEnter", crate);
        yield return null;

        Assert.IsFalse(carrier.HasBottle);
        Assert.IsNotNull(spawner.Bottle);
    }

    [UnityTest]
    public IEnumerator WalkingIntoTheBottle_PicksItUp()
    {
        WaterBottle bottle = Bottle();
        bottle.transform.position = new Vector3(0.5f, 0f, 0f);
        Physics.SyncTransforms();

        var controller = carrier.GetComponent<CharacterController>();
        for (int i = 0; i < 20 && !carrier.HasBottle; i++)
        {
            controller.Move(Vector3.right * 0.05f);
            yield return new WaitForFixedUpdate();
        }

        Assert.IsTrue(carrier.HasBottle);
    }

    [UnityTest]
    public IEnumerator LosingTheBottle_PutsANewOneAtThePlace()
    {
        TestUtils.Call(Bottle(), "OnTriggerEnter", carrier.GetComponent<Collider>());
        yield return null;

        playerCaught.Raise();

        Assert.IsNotNull(spawner.Bottle);
        Assert.AreEqual(new Vector3(3f, 4f, 0f), spawner.Bottle.position);
        Track(spawner.Bottle.gameObject);
    }

    [UnityTest]
    public IEnumerator CaughtWithoutTheBottle_DoesNotMakeASecondBottle()
    {
        Transform first = Bottle().transform;

        playerCaught.Raise();
        yield return null;

        Assert.AreSame(first, spawner.Bottle);
        Assert.AreEqual(1, Object.FindObjectsByType<WaterBottle>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length - 1, "One bottle and the stand-in prefab");
    }

    [UnityTest]
    public IEnumerator Mattress_WithTheBottle_RaisesBottleDelivered()
    {
        MattressGoal goal = MakeGoal(out EventSpy delivered);
        carrier.PickUp();

        TestUtils.Call(goal, "OnTriggerStay", carrier.GetComponent<Collider>());
        yield return null;

        Assert.AreEqual(1, delivered.Count);
        Assert.IsFalse(carrier.HasBottle);
    }

    [UnityTest]
    public IEnumerator Mattress_WithoutTheBottle_DoesNothing()
    {
        MattressGoal goal = MakeGoal(out EventSpy delivered);

        TestUtils.Call(goal, "OnTriggerStay", carrier.GetComponent<Collider>());
        yield return null;

        Assert.AreEqual(0, delivered.Count);
    }

    [UnityTest]
    public IEnumerator Mattress_DeliversOnlyOnce_WhileThePlayerStaysOnIt()
    {
        MattressGoal goal = MakeGoal(out EventSpy delivered);
        carrier.PickUp();

        for (int i = 0; i < 5; i++)
        {
            TestUtils.Call(goal, "OnTriggerStay", carrier.GetComponent<Collider>());
        }
        yield return null;

        Assert.AreEqual(1, delivered.Count);
    }

    [UnityTest]
    public IEnumerator Mattress_IgnoresOtherObjects()
    {
        MattressGoal goal = MakeGoal(out EventSpy delivered);
        var crate = Make("Crate").AddComponent<BoxCollider>();

        TestUtils.Call(goal, "OnTriggerStay", crate);
        yield return null;

        Assert.AreEqual(0, delivered.Count);
    }

    private MattressGoal MakeGoal(out EventSpy delivered)
    {
        GameEvent bottleDelivered = Track(TestData.Event());
        delivered = new EventSpy(bottleDelivered);
        GameObject mattress = MakeOff("MattressGoal");
        mattress.AddComponent<BoxCollider>().isTrigger = true;
        MattressGoal goal = mattress.AddComponent<MattressGoal>();
        TestUtils.Set(goal, "bottleDelivered", bottleDelivered);
        mattress.SetActive(true);
        return goal;
    }
}
