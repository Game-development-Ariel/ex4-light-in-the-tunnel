using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>The minimap draws the map in a corner, with a dot where the player is. M shows and hides it.</summary>
public class MinimapTests : PlayModeTest
{
    private Minimap minimap;
    private Transform player;
    private ScreenSizeWatcher screenSize;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        GameObject grid = Make("TunnelGrid");
        var map = TestMap.Create(grid, 10, 5);
        player = Make("Player").transform;
        screenSize = Make("Watcher").AddComponent<ScreenSizeWatcher>();

        GameObject hud = MakeOff("Minimap");
        minimap = hud.AddComponent<Minimap>();
        TestUtils.Set(minimap, "mapBounds", map);
        TestUtils.Set(minimap, "player", player);
        TestUtils.Set(minimap, "screenSize", screenSize);
        hud.AddComponent<MinimapToggle>();
        TestUtils.Call(hud.GetComponent<MinimapToggle>(), "OnValidate");
        hud.SetActive(true);
        yield return null;
    }

    private RectTransform Dot => minimap.GetComponentsInChildren<RectTransform>(true).First(t => t.name == "Player Dot");

    private Camera MinimapCamera => minimap.GetComponentInChildren<Camera>(true);

    [Test]
    public void Minimap_IsVisible_AndHasItsOwnCamera()
    {
        Assert.IsTrue(minimap.IsVisible);
        Assert.IsTrue(MinimapCamera.enabled);
        Assert.AreEqual(2.5f, MinimapCamera.orthographicSize, 1e-3f, "Half the map's height");
        Assert.AreEqual(new Vector3(5f, 2.5f, -10f), MinimapCamera.transform.position);
        Assert.AreEqual(2f, (float)MinimapCamera.targetTexture.width / MinimapCamera.targetTexture.height, 0.01f, "The map's shape");
    }

    [Test]
    public void SetVisible_HidesAndShowsTheMapAndItsCamera()
    {
        minimap.SetVisible(false);
        Assert.IsFalse(minimap.IsVisible);
        Assert.IsFalse(MinimapCamera.enabled, "A hidden map doesn't render");

        minimap.SetVisible(true);
        Assert.IsTrue(minimap.IsVisible);
        Assert.IsTrue(MinimapCamera.enabled);
    }

    [UnityTest]
    public IEnumerator Dot_FollowsThePlayer()
    {
        player.position = new Vector3(5f, 2.5f, 0f);
        yield return null;
        Assert.AreEqual(new Vector2(0.5f, 0.5f), Dot.anchorMin);

        player.position = new Vector3(0f, 5f, 0f);
        yield return null;
        Assert.AreEqual(new Vector2(0f, 1f), Dot.anchorMin);

        player.position = new Vector3(99f, -99f, 0f);
        yield return null;
        Assert.AreEqual(new Vector2(1f, 0f), Dot.anchorMax, "Outside the map the dot stays on the edge");
    }

    [Test]
    public void ScreenSizeChange_RescalesTheMap()
    {
        var scaler = minimap.GetComponentsInChildren<CanvasScaler>(true).First(s => s.name == "Minimap Canvas");
        scaler.scaleFactor = 99f;

        TestUtils.RaiseEvent(screenSize, "Changed");

        Assert.AreEqual(Mathf.Min(Screen.width, Screen.height) / 1080f, scaler.scaleFactor, 1e-4f);
    }

    [UnityTest]
    public IEnumerator PressingM_TogglesTheMinimap()
    {
        var keyboard = new VirtualKeyboard();
        try
        {
            keyboard.Hold(Key.M);
            yield return null;
            yield return null;
            Assert.IsFalse(minimap.IsVisible);

            keyboard.ReleaseAll();
            yield return null;
            keyboard.Hold(Key.M);
            yield return null;
            yield return null;
            Assert.IsTrue(minimap.IsVisible);
        }
        finally
        {
            keyboard.Remove();
        }
    }

    [UnityTest]
    public IEnumerator HoldingM_TogglesOnlyOnce()
    {
        var keyboard = new VirtualKeyboard();
        try
        {
            keyboard.Hold(Key.M);
            yield return TestUtils.WaitFrames(10);
            Assert.IsFalse(minimap.IsVisible);
        }
        finally
        {
            keyboard.Remove();
        }
    }
}
