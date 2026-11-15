using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>The HUD parts each show one thing, and only react to the game's events.</summary>
public class HudTests : PlayModeTest
{
    private Hope hope;
    private TunnelLevelSettings settings;
    private GameEvent bottlePicked;
    private GameEvent bottleDropped;
    private GameEvent playerCaught;
    private TextEvent hintRequested;
    private EventSpy hints;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        settings = Track(TestData.Settings(100, 30, caughtFreezeSeconds: 0.3f));
        GameObject player = MakeOff("Player");
        hope = player.AddComponent<Hope>();
        TestUtils.Set(hope, "settings", settings);
        player.SetActive(true);
        bottlePicked = Track(TestData.Event());
        bottleDropped = Track(TestData.Event());
        playerCaught = Track(TestData.Event());
        hintRequested = Track(TestData.TextEvent());
        hints = new EventSpy(hintRequested);
        yield return null;
    }

    private static T Find<T>(GameObject root, string name) where T : Component
    {
        foreach (T found in root.GetComponentsInChildren<T>(true))
        {
            if (found.name == name)
            {
                return found;
            }
        }
        Assert.Fail($"No {typeof(T).Name} called {name} under {root.name}");
        return null;
    }

    private HopeBar MakeHopeBar()
    {
        GameObject hud = MakeOff("HUD");
        HopeBar bar = hud.AddComponent<HopeBar>();
        TestUtils.Set(bar, "hope", hope);
        TestUtils.Set(bar, "gainTextSeconds", new Vector2(0.1f, 0.1f));
        hud.SetActive(true);
        return bar;
    }

    [UnityTest]
    public IEnumerator HopeBar_StartsFilledToTheHope()
    {
        HopeBar bar = MakeHopeBar();
        yield return null;

        Assert.AreEqual(0.3f, Find<Image>(bar.gameObject, "Fill").fillAmount, 0.01f);
    }

    [UnityTest]
    public IEnumerator HopeBar_GainedHope_ShowsPlusN_AndSlidesTheBarUp()
    {
        HopeBar bar = MakeHopeBar();
        yield return null;

        hope.Add(15);
        yield return null;

        Assert.AreEqual("+15 hope", Find<Text>(bar.gameObject, "Gain").text);
        Image fill = Find<Image>(bar.gameObject, "Fill");
        Assert.Less(fill.fillAmount, 0.45f, "The bar slides, it doesn't jump");
        yield return TestUtils.WaitUntil(() => Mathf.Approximately(fill.fillAmount, 0.45f), 2f, "the bar to reach 0.45");
    }

    [UnityTest]
    public IEnumerator HopeBar_GainText_FadesAway()
    {
        HopeBar bar = MakeHopeBar();
        yield return null;
        hope.Add(5);

        Text gain = Find<Text>(bar.gameObject, "Gain");
        yield return TestUtils.WaitUntil(() => gain.color.a == 0f, 2f, "the gain text to fade");
    }

    [UnityTest]
    public IEnumerator HopeBar_LostHope_ShowsNoGain_AndSlidesTheBarDown()
    {
        HopeBar bar = MakeHopeBar();
        yield return null;

        hope.Lose(20);

        Assert.AreEqual("", Find<Text>(bar.gameObject, "Gain").text);
        Image fill = Find<Image>(bar.gameObject, "Fill");
        yield return TestUtils.WaitUntil(() => Mathf.Approximately(fill.fillAmount, 0.1f), 2f, "the bar to reach 0.1");
    }

    [UnityTest]
    public IEnumerator HopeBar_FlameFlickers()
    {
        HopeBar bar = MakeHopeBar();
        Transform flame = Find<Image>(bar.gameObject, "Hope").transform;
        float smallest = float.MaxValue;
        float largest = float.MinValue;

        for (int i = 0; i < 60; i++)
        {
            yield return null;
            smallest = Mathf.Min(smallest, flame.localScale.y);
            largest = Mathf.Max(largest, flame.localScale.y);
        }

        Assert.Greater(largest, smallest);
        Assert.That(smallest, Is.InRange(0.9f, 1.1f));
    }

    private Image MakeWaterDrop(out WaterIndicator water)
    {
        GameObject hud = MakeOff("HUD");
        water = hud.AddComponent<WaterIndicator>();
        TestUtils.Set(water, "bottlePicked", bottlePicked);
        TestUtils.Set(water, "bottleDropped", bottleDropped);
        TestUtils.Set(water, "fadeSeconds", 0.05f);
        TestUtils.Set(water, "pickPunch", new Vector2(0.3f, 0.05f));
        hud.SetActive(true);
        return Find<Image>(hud, "Drop");
    }

    [UnityTest]
    public IEnumerator WaterDrop_IsEmpty_UntilTheBottleIsPicked()
    {
        Image drop = MakeWaterDrop(out WaterIndicator water);
        float empty = TestUtils.Get<float>(water, "emptyAlpha");
        yield return null;
        Assert.AreEqual(empty, drop.color.a, 1e-4f);

        bottlePicked.Raise();
        yield return TestUtils.WaitUntil(() => drop.color.a == 1f, 2f, "the drop to fill");

        bottleDropped.Raise();
        yield return TestUtils.WaitUntil(() => Mathf.Approximately(drop.color.a, empty), 2f, "the drop to empty");
    }

    [UnityTest]
    public IEnumerator WaterDrop_Punches_WhenTheBottleIsPicked()
    {
        Image drop = MakeWaterDrop(out _);
        yield return null;

        bottlePicked.Raise();
        yield return null;
        yield return null;
        Assert.Greater(drop.transform.localScale.x, 1f);

        yield return TestUtils.WaitUntil(() => drop.transform.localScale == Vector3.one, 2f, "the punch to end");
    }

    private ObjectiveThought MakeObjective()
    {
        GameObject hud = MakeOff("HUD");
        ObjectiveThought objective = hud.AddComponent<ObjectiveThought>();
        TestUtils.Set(objective, "settings", settings);
        TestUtils.Set(objective, "bottlePicked", bottlePicked);
        TestUtils.Set(objective, "bottleDropped", bottleDropped);
        TestUtils.Set(objective, "hintRequested", hintRequested);
        TestUtils.Set(objective, "firstThoughtDelay", 0.05f);
        TestUtils.Set(objective, "pickedThoughtDelay", 0.05f);
        TestUtils.Set(objective, "lettersPerSecond", 1000f);
        hud.SetActive(true);
        return objective;
    }

    [UnityTest]
    public IEnumerator Objective_FirstIsToFindWater()
    {
        ObjectiveThought objective = MakeObjective();
        Text thought = Find<Text>(objective.gameObject, "Thought");
        Assert.AreEqual("", thought.text, "Nothing before the delay");

        string findWater = TestUtils.Get<string>(objective, "findWater");
        yield return TestUtils.WaitUntil(() => thought.text == findWater, 3f, "the first goal");
    }

    [UnityTest]
    public IEnumerator Objective_AfterPickingTheBottle_IsToGoBack_AndTheHeroSaysSo()
    {
        ObjectiveThought objective = MakeObjective();
        Text thought = Find<Text>(objective.gameObject, "Thought");
        yield return null;

        bottlePicked.Raise();

        Assert.AreEqual(TestUtils.Get<string>(objective, "pickedThought"), hints.Last);
        string goBack = TestUtils.Get<string>(objective, "goBack");
        yield return TestUtils.WaitUntil(() => thought.text == goBack, 3f, "the new goal");
    }

    [UnityTest]
    public IEnumerator Objective_AfterLosingTheBottle_IsToFindWaterAgain_AfterTheFreeze()
    {
        ObjectiveThought objective = MakeObjective();
        Text thought = Find<Text>(objective.gameObject, "Thought");
        yield return null; // After Start, which writes the first goal.
        bottlePicked.Raise();
        string goBack = TestUtils.Get<string>(objective, "goBack");
        yield return TestUtils.WaitUntil(() => thought.text == goBack, 3f, "the go back goal");

        bottleDropped.Raise();
        yield return TestUtils.WaitGameSeconds(settings.caughtFreezeSeconds * 0.5f);
        Assert.AreEqual(goBack, thought.text, "The new goal waits until the hero can move");

        string findWater = TestUtils.Get<string>(objective, "findWater");
        yield return TestUtils.WaitUntil(() => thought.text == findWater, 3f, "find water again");
    }

    private CaptureBlackout MakeBlackout()
    {
        GameObject hud = MakeOff("HUD");
        CaptureBlackout blackout = hud.AddComponent<CaptureBlackout>();
        TestUtils.Set(blackout, "settings", settings);
        TestUtils.Set(blackout, "playerCaught", playerCaught);
        TestUtils.Set(blackout, "bottleDropped", bottleDropped);
        TestUtils.Set(blackout, "hintRequested", hintRequested);
        TestUtils.Set(blackout, "fadeInSeconds", 0.05f);
        TestUtils.Set(blackout, "fadeOutSeconds", 0.05f);
        hud.SetActive(true);
        return blackout;
    }

    [UnityTest]
    public IEnumerator Blackout_DarkensTheScreen_ForTheFreezeTime_ThenThinks()
    {
        Time.captureFramerate = 0; // The fades count real time and the dark counts game time, so here they must be the same.
        CaptureBlackout blackout = MakeBlackout();
        var group = blackout.GetComponentInChildren<CanvasGroup>();
        yield return null;
        Assert.AreEqual(0f, group.alpha);

        playerCaught.Raise();
        float caughtAt = Time.time;
        yield return TestUtils.WaitUntil(() => group.alpha == 1f, 2f, "the screen to go black");
        Assert.AreEqual(TestUtils.Get<string>(blackout, "firstLine"), Find<Text>(blackout.gameObject, "Line").text);

        yield return TestUtils.WaitUntil(() => group.alpha == 0f, 2f, "the screen to clear");
        Assert.AreEqual(settings.caughtFreezeSeconds, Time.time - caughtAt, 0.15f, "Dark as long as the player is held");
        yield return null;
        Assert.AreEqual(TestUtils.Get<string>(blackout, "firstThought"), hints.Last);
    }

    [UnityTest]
    public IEnumerator Blackout_SecondCapture_SaysAgain()
    {
        CaptureBlackout blackout = MakeBlackout();
        yield return null;
        playerCaught.Raise();
        yield return TestUtils.WaitUntil(() => hints.Count == 1, 3f, "the first capture to end");

        playerCaught.Raise();
        yield return null;
        Assert.AreEqual(TestUtils.Get<string>(blackout, "againLine"), Find<Text>(blackout.gameObject, "Line").text);
        yield return TestUtils.WaitUntil(() => hints.Count == 2, 3f, "the second capture to end");
        Assert.AreEqual(TestUtils.Get<string>(blackout, "againThought"), hints.Last);
    }

    [UnityTest]
    public IEnumerator Blackout_CaughtWithTheBottle_ThinksAboutTheBottle()
    {
        CaptureBlackout blackout = MakeBlackout();
        yield return null;

        playerCaught.Raise();
        bottleDropped.Raise();
        yield return TestUtils.WaitUntil(() => hints.Count == 1, 3f, "the capture to end");

        Assert.AreEqual(TestUtils.Get<string>(blackout, "lostBottleThought"), hints.Last);
    }

    [UnityTest]
    public IEnumerator Blackout_CaptureDuringTheDark_StartsItOver()
    {
        CaptureBlackout blackout = MakeBlackout();
        yield return null;
        playerCaught.Raise();
        yield return TestUtils.WaitGameSeconds(0.1f);

        playerCaught.Raise();
        yield return TestUtils.WaitUntil(() => hints.Count > 0, 3f, "the dark to end");
        yield return TestUtils.WaitGameSeconds(0.5f);

        Assert.AreEqual(1, hints.Count, "One thought for the two captures");
    }

    [UnityTest]
    public IEnumerator Vignette_CoversTheScreen_UnderEverything()
    {
        GameObject hud = MakeOff("HUD");
        hud.AddComponent<ScreenVignette>();
        hud.SetActive(true);
        yield return null;

        Canvas canvas = hud.GetComponentInChildren<Canvas>();
        Assert.Less(canvas.sortingOrder, 0);
        Image vignette = Find<Image>(hud, "Vignette");
        Assert.AreEqual(UiTheme.Sprite("Vignette"), vignette.sprite);
        Assert.AreEqual(Vector2.one, vignette.rectTransform.anchorMax);
    }
}
