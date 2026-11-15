using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>The camera follows the player without showing outside the map, and keeps its size when the screen turns.</summary>
public class CameraTests : PlayModeTest
{
    private Transform target;

    private CameraFollow MakeFollow(int mapWidth, int mapHeight, Vector3 targetAt)
    {
        target = Make("Player").transform;
        target.position = targetAt;
        GameObject view = MakeOff("Main Camera");
        view.transform.position = new Vector3(0f, 0f, -10f);
        Camera cam = view.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 2f;
        cam.aspect = 2f; // The view is 8 x 4 tiles.
        CameraFollow follow = view.AddComponent<CameraFollow>();
        TestUtils.Set(follow, "target", target);
        TestUtils.Set(follow, "smoothTime", 0.01f);
        if (mapWidth > 0)
        {
            TestUtils.Set(follow, "mapBounds", TestMap.Create(Make("Grid"), mapWidth, mapHeight));
        }
        view.SetActive(true);
        return follow;
    }

    private static void AssertAt(CameraFollow follow, float x, float y, string message = "")
    {
        Assert.AreEqual(x, follow.transform.position.x, 0.01f, message);
        Assert.AreEqual(y, follow.transform.position.y, 0.01f, message);
    }

    [UnityTest]
    public IEnumerator Follow_StartsOnTheTarget()
    {
        CameraFollow follow = MakeFollow(20, 10, new Vector3(10f, 5f, 0f));
        yield return null;

        AssertAt(follow, 10f, 5f);
        Assert.AreEqual(-10f, follow.transform.position.z, "Keeps its distance from the map");
    }

    [UnityTest]
    public IEnumerator Follow_CatchesUpWithAMovingTarget()
    {
        CameraFollow follow = MakeFollow(20, 10, new Vector3(10f, 5f, 0f));
        yield return null;

        target.position = new Vector3(12f, 6f, 0f);
        yield return TestUtils.WaitGameSeconds(0.3f);

        AssertAt(follow, 12f, 6f);
    }

    [UnityTest]
    public IEnumerator Follow_NeverShowsOutsideTheMap()
    {
        CameraFollow follow = MakeFollow(20, 10, new Vector3(-50f, -50f, 0f));
        yield return TestUtils.WaitGameSeconds(0.2f);
        AssertAt(follow, 4f, 2f, "The bottom left corner, half a view in");

        target.position = new Vector3(100f, 100f, 0f);
        yield return TestUtils.WaitGameSeconds(0.3f);
        AssertAt(follow, 16f, 8f);
    }

    [UnityTest]
    public IEnumerator Follow_CentersAMapSmallerThanTheView()
    {
        CameraFollow follow = MakeFollow(4, 3, new Vector3(-50f, 50f, 0f));
        yield return null;

        AssertAt(follow, 2f, 1.5f);
    }

    [UnityTest]
    public IEnumerator Follow_WithoutAMap_GoesAnywhere()
    {
        CameraFollow follow = MakeFollow(0, 0, new Vector3(-50f, 50f, 0f));
        yield return null;

        AssertAt(follow, -50f, 50f);
    }

    [UnityTest]
    public IEnumerator Follow_WithoutATarget_StaysAndDoesNotFail()
    {
        CameraFollow follow = MakeFollow(20, 10, Vector3.zero);
        TestUtils.Set(follow, "target", null);
        yield return TestUtils.WaitFrames(3);

        AssertAt(follow, 0f, 0f);
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator OrientationSize_KeepsTheSizeOfThingsWhenTheScreenTurns()
    {
        GameObject view = MakeOff("Main Camera");
        Camera cam = view.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        ScreenSizeWatcher watcher = view.AddComponent<ScreenSizeWatcher>();
        view.AddComponent<OrientationCameraSize>();
        view.SetActive(true);
        yield return null;
        float expected = 5f * Screen.height / Mathf.Min(Screen.width, Screen.height);
        Assert.AreEqual(expected, cam.orthographicSize, 1e-4f);

        cam.orthographicSize = 1f;
        TestUtils.RaiseEvent(watcher, "Changed");

        Assert.AreEqual(expected, cam.orthographicSize, 1e-4f, "Recomputed from the size set in the Inspector, not from the last one");
    }

    [UnityTest]
    public IEnumerator ScreenSizeWatcher_TellsOnlyWhenTheSizeChanges()
    {
        GameObject view = MakeOff("Watcher");
        ScreenSizeWatcher watcher = view.AddComponent<ScreenSizeWatcher>();
        view.SetActive(true);
        int changes = 0;
        watcher.Changed += () => changes++;

        yield return TestUtils.WaitFrames(5);
        Assert.AreEqual(0, changes, "The same screen is not a change");

        TestUtils.Set(watcher, "lastWidth", -1); // As if the screen was a different size until now.
        yield return TestUtils.WaitFrames(5);
        Assert.AreEqual(1, changes);
    }
}
