using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>The arrow shows where to go: the bottle while it is on the floor, the mattress while it is carried.</summary>
public class ObjectiveMarkerTests : PlayModeTest
{
    private Transform mattress;
    private BottleSpawner spawner;
    private GameEvent bottlePicked;
    private GameEvent bottleDropped;
    private ObjectiveMarker marker;
    private Transform worldArrow;
    private GameObject edgeArrow;

    private IEnumerator Build(Vector3 bottlePlace)
    {
        GameObject view = Make("Main Camera");
        view.tag = "MainCamera";
        Camera cam = view.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        view.transform.position = new Vector3(0f, 0f, -10f);

        mattress = Make("Mattress").transform;
        mattress.position = new Vector3(-2f, -1f, 0f);
        GameObject prefab = MakeOff("Bottle Prefab");
        bottlePicked = Track(TestData.Event());
        bottleDropped = Track(TestData.Event());
        GameObject spawn = MakeOff("BottleSpawn");
        spawn.transform.position = bottlePlace;
        spawner = spawn.AddComponent<BottleSpawner>();
        TestUtils.Set(spawner, "bottlePrefab", prefab);
        TestUtils.Set(spawner, "bottleDropped", bottleDropped);
        spawn.SetActive(true);

        GameObject hud = MakeOff("Objective");
        marker = hud.AddComponent<ObjectiveMarker>();
        TestUtils.Set(marker, "mattress", mattress);
        TestUtils.Set(marker, "spawner", spawner);
        TestUtils.Set(marker, "bottlePicked", bottlePicked);
        TestUtils.Set(marker, "bottleDropped", bottleDropped);
        hud.SetActive(true);
        yield return null;
        yield return null;
        Track(spawner.Bottle.gameObject);
        worldArrow = hud.transform.Find("Objective Arrow");
        edgeArrow = hud.GetComponentsInChildren<RectTransform>(true).First(t => t.name == "Chevron").gameObject;
    }

    private float HeightAbove => TestUtils.Get<float>(marker, "heightAbove");

    [UnityTest]
    public IEnumerator Arrow_FloatsOverTheBottle_AtTheStart()
    {
        yield return Build(new Vector3(1f, 1f, 0f));

        Assert.IsTrue(worldArrow.gameObject.activeSelf);
        Assert.AreEqual(new Vector3(1f, 1f + HeightAbove, 0f), worldArrow.position);
        Assert.IsFalse(edgeArrow.activeSelf, "The bottle is on the screen");
    }

    [UnityTest]
    public IEnumerator Arrow_PointsToTheMattress_WhileTheBottleIsCarried()
    {
        yield return Build(new Vector3(1f, 1f, 0f));

        bottlePicked.Raise();
        yield return null;

        Assert.AreEqual(mattress.position + Vector3.up * HeightAbove, worldArrow.position);
    }

    [UnityTest]
    public IEnumerator Arrow_GoesBackToTheBottle_WhenItIsLost()
    {
        yield return Build(new Vector3(1f, 1f, 0f));
        bottlePicked.Raise();
        yield return null;

        bottleDropped.Raise();
        yield return null;
        foreach (WaterBottle extra in Object.FindObjectsByType<WaterBottle>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Track(extra.gameObject);
        }

        Assert.AreEqual(spawner.Bottle.position + Vector3.up * HeightAbove, worldArrow.position);
    }

    [UnityTest]
    public IEnumerator Arrow_IsHidden_WhenThereIsNoGoal()
    {
        yield return Build(new Vector3(1f, 1f, 0f));

        Object.Destroy(spawner.Bottle.gameObject); // Picked up, but the carry event didn't come yet.
        yield return null;
        yield return null;

        Assert.IsFalse(worldArrow.gameObject.activeSelf);
        Assert.IsFalse(edgeArrow.activeSelf);
    }

    [UnityTest]
    public IEnumerator EdgeArrow_PointsToAGoalOffTheScreen()
    {
        yield return Build(new Vector3(50f, 0f, 0f));

        Assert.IsTrue(edgeArrow.activeSelf);
        var rect = (RectTransform)edgeArrow.transform;
        Assert.Greater(rect.anchoredPosition.x, 0f, "On the right edge, toward the goal");
        Assert.AreEqual(0f, rect.anchoredPosition.y, 1f);
    }
}
