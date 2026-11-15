using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>The end screen covers the level after a win or a loss and shows the results of the day.</summary>
public class EndScreenTests : PlayModeTest
{
    private GameEvent levelWon;
    private GameEvent levelLost;
    private RoundStats stats;
    private Hope hope;
    private GameObject hud;
    private EndScreen screen;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        levelWon = Track(TestData.Event());
        levelLost = Track(TestData.Event());
        TunnelLevelSettings settings = Track(TestData.Settings(100, 30, endFadeSeconds: 0.05f));

        GameObject player = MakeOff("Player");
        hope = player.AddComponent<Hope>();
        TestUtils.Set(hope, "settings", settings);
        player.SetActive(true);

        GameObject round = MakeOff("Round");
        stats = round.AddComponent<RoundStats>();
        foreach (string field in new[] { "openingFinished", "playerCaught", "levelWon", "levelLost" })
        {
            TestUtils.Set(stats, field, Track(TestData.Event()));
        }
        round.SetActive(true);
        hud = Make("HUD");

        GameObject end = MakeOff("End Screen");
        screen = end.AddComponent<EndScreen>();
        TestUtils.Set(screen, "levelWon", levelWon);
        TestUtils.Set(screen, "levelLost", levelLost);
        TestUtils.Set(screen, "settings", settings);
        TestUtils.Set(screen, "stats", stats);
        TestUtils.Set(screen, "hope", hope);
        TestUtils.Set(screen, "hideWhenLevelEnds", new[] { hud });
        foreach (string field in new[] { "beamSeconds", "afterBeam", "titleFade", "subtitleFade", "afterTitle", "betweenThoughts", "resultsFade" })
        {
            TestUtils.Set(screen, field, 0.01f);
        }
        TestUtils.Set(screen, "lettersPerSecond", 1000f);
        end.SetActive(true);
        yield return null;
    }

    [TearDown]
    public void DestroyTheEndCanvas()
    {
        GameObject canvas = GameObject.Find("End Canvas");
        if (canvas != null)
        {
            Object.Destroy(canvas);
        }
    }

    private static Text Text(string name)
    {
        GameObject found = GameObject.Find(name);
        Assert.IsNotNull(found, $"No {name} on the end screen");
        return found.GetComponent<Text>();
    }

    private IEnumerator WaitForTheButton()
    {
        yield return TestUtils.WaitUntil(() => GameObject.Find("Play again Button") != null, 10f, "the Play again button");
    }

    [UnityTest]
    public IEnumerator NothingIsShown_BeforeTheLevelEnds()
    {
        yield return TestUtils.WaitFrames(5);

        Assert.IsNull(GameObject.Find("End Canvas"));
        Assert.IsTrue(hud.activeSelf);
    }

    [UnityTest]
    public IEnumerator Win_ShowsTheWinWords_AndThePlayAgainButton()
    {
        levelWon.Raise();
        yield return WaitForTheButton();

        Assert.AreEqual(TestUtils.Get<string>(screen, "title"), Text("Title").text);
        Assert.AreEqual(TestUtils.Get<string>(screen, "winSubtitle"), Text("Subtitle").text);
        string[] thoughts = TestUtils.Get<string[]>(screen, "winThoughts");
        string[] shown = Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Where(t => t.name == "Thought").Select(t => t.text).ToArray();
        CollectionAssert.AreEquivalent(thoughts, shown);
    }

    [UnityTest]
    public IEnumerator Loss_ShowsTheLossWords()
    {
        levelLost.Raise();
        yield return WaitForTheButton();

        Assert.AreEqual(TestUtils.Get<string>(screen, "loseSubtitle"), Text("Subtitle").text);
    }

    [UnityTest]
    public IEnumerator TheHud_IsHidden_OnceTheScreenIsBlack()
    {
        levelWon.Raise();
        yield return TestUtils.WaitUntil(() => !hud.activeSelf, 3f, "the HUD to hide");

        Image background = GameObject.Find("Background").GetComponent<Image>();
        Assert.AreEqual(1f, background.color.a, 1e-3f, "Hidden only after the fade to black");
    }

    [UnityTest]
    public IEnumerator Results_ShowTheHopeAndTheTime()
    {
        hope.Add(12);
        TestUtils.Set(stats, "<Seconds>k__BackingField", 83.4f);

        levelWon.Raise();
        yield return WaitForTheButton();

        Assert.AreEqual("hope 42", Text("Hope Value").text);
        Assert.AreEqual("83 seconds", Text("Clock Value").text);
    }

    [UnityTest]
    public IEnumerator Results_NeverSeen()
    {
        yield return ShowWithCaptures(0);
        Assert.AreEqual("never seen", Text("Eye Value").text);
    }

    [UnityTest]
    public IEnumerator Results_SeenOnce()
    {
        yield return ShowWithCaptures(1);
        Assert.AreEqual("seen once", Text("Eye Value").text);
    }

    [UnityTest]
    public IEnumerator Results_SeenManyTimes()
    {
        yield return ShowWithCaptures(3);
        Assert.AreEqual("seen 3 times", Text("Eye Value").text);
    }

    private IEnumerator ShowWithCaptures(int captures)
    {
        TestUtils.Set(stats, "<Captures>k__BackingField", captures);
        levelLost.Raise();
        yield return WaitForTheButton();
    }

    [UnityTest]
    public IEnumerator EndScreen_MakesAnEventSystem_SoTheButtonCanBeClicked()
    {
        levelWon.Raise();
        yield return WaitForTheButton();

        Assert.IsNotNull(Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>());
    }
}
