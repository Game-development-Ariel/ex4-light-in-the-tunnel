using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>The end screen's Play again button loads the level again, through the ScreenFader.</summary>
public class PlayAgainTests : TutorialSceneTest
{
    // The end screen is a slow film. These shorter times keep the test short (they change only this scene's copy).
    private void MakeTheEndScreenFast()
    {
        foreach (string field in new[] { "beamSeconds", "afterBeam", "titleFade", "subtitleFade", "afterTitle", "betweenThoughts", "resultsFade" })
        {
            TestUtils.Set(End, field, 0.01f);
        }
        TestUtils.Set(End, "lettersPerSecond", 1000f);
        TestUtils.Set(End, "winThoughts", new[] { "a" });
    }

    [UnityTest]
    public IEnumerator PlayAgain_AfterTheWin_LoadsAFreshLevel()
    {
        yield return SkipTheOpening();
        MakeTheEndScreenFast();
        TunnelRound oldRound = Round;
        Hope.Add(40);
        Carrier.PickUp();
        BottleDelivered.Raise();

        Button playAgain = null;
        yield return TestUtils.WaitUntil(() => (playAgain = FindButton()) != null, 10f, "the Play again button");
        playAgain.onClick.Invoke();
        Assert.IsTrue(TestUtils.GetStatic<bool>(typeof(ScreenFader), "isFading"));

        yield return TestUtils.WaitUntil(() => Object.FindAnyObjectByType<TunnelRound>() != null && Object.FindAnyObjectByType<TunnelRound>() != oldRound, 10f, "the level to load again");
        yield return TestUtils.WaitUntil(() => GameObject.Find("Screen Fader") == null, 5f, "the fader to clear");
        FindTheParts();

        Assert.IsFalse(TestUtils.GetStatic<bool>(typeof(ScreenFader), "isFading"), "The fader can load again");
        Assert.AreEqual(Mathf.Min(Settings.startHope, Settings.maxHope), Hope.Current, "A fresh level starts with the starting hope");
        Assert.IsFalse(Player.enabled, "The new level starts with its opening");
        Assert.AreEqual(0, Stats.Captures);
        LogAssert.NoUnexpectedReceived();
    }

    private static Button FindButton()
    {
        GameObject found = GameObject.Find("Play again Button");
        return found != null ? found.GetComponent<Button>() : null;
    }
}
