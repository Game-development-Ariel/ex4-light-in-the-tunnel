using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>The words on the screen: hints, memories, the title opening and the end screen.</summary>
public class MessageTests : PlayModeTest
{
    private TextEvent textEvent;

    [SetUp]
    public void SetUp()
    {
        textEvent = Track(TestData.TextEvent());
    }

    private static Text FindText(GameObject root, string name)
    {
        Text found = root.GetComponentsInChildren<Text>(true).FirstOrDefault(t => t.name == name);
        Assert.IsNotNull(found, $"No text called {name}");
        return found;
    }

    private HintToast MakeHints(GameObject off)
    {
        HintToast toast = off.AddComponent<HintToast>();
        TestUtils.Set(toast, "hintRequested", textEvent);
        TestUtils.Set(toast, "showSeconds", 0.1f);
        TestUtils.Set(toast, "lettersPerSecond", 1000f);
        return toast;
    }

    [UnityTest]
    public IEnumerator HintToast_WritesTheHint_WhenItIsRequested()
    {
        GameObject hud = MakeOff("HUD");
        MakeHints(hud);
        hud.SetActive(true);
        yield return null;
        var group = hud.GetComponentInChildren<CanvasGroup>();
        Assert.AreEqual(0f, group.alpha, "Hidden until a hint comes");

        textEvent.Raise("Watch the light.");

        Text message = FindText(hud, "Message");
        yield return TestUtils.WaitUntil(() => message.text == "Watch the light.", 3f, "the hint to be written");
        Assert.AreEqual(1f, group.alpha, 0.01f);
        yield return TestUtils.WaitUntil(() => group.alpha == 0f, 4f, "the hint to fade away");
    }

    [UnityTest]
    public IEnumerator HintToast_ShowsTheKeys_AndANewHintReplacesThem()
    {
        GameObject hud = MakeOff("HUD");
        HintToast toast = MakeHints(hud);
        hud.SetActive(true);
        yield return null;
        Transform keys = hud.GetComponentsInChildren<RectTransform>(true).First(t => t.name == "Keys");

        toast.Show("Move.", "W", "A", "S", "D");
        yield return null;
        CollectionAssert.AreEqual(new[] { "Key W", "Key A", "Key S", "Key D" }, keys.Cast<Transform>().Select(t => t.name).ToArray());
        Assert.AreEqual("A", keys.GetChild(1).GetComponentInChildren<Text>().text);

        toast.Show("No keys now.");
        yield return null;
        Assert.AreEqual(0, keys.childCount);
        yield return TestUtils.WaitUntil(() => FindText(hud, "Message").text == "No keys now.", 3f, "the new hint");
    }

    private MemoryCaption MakeCaption()
    {
        GameObject hud = MakeOff("HUD");
        MemoryCaption caption = hud.AddComponent<MemoryCaption>();
        TestUtils.Set(caption, "memoryFound", textEvent);
        TestUtils.Set(caption, "showSeconds", 0.05f);
        hud.SetActive(true);
        return caption;
    }

    [UnityTest]
    public IEnumerator MemoryCaption_ShowsTheMemory()
    {
        MemoryCaption caption = MakeCaption();
        yield return null;

        textEvent.Raise("My mother's voice.");

        Assert.AreEqual("My mother's voice.", FindText(caption.gameObject, "Memory").text);
        var group = caption.GetComponentInChildren<CanvasGroup>();
        yield return TestUtils.WaitUntil(() => group.alpha > 0.5f, 3f, "the memory to fade in");
    }

    [UnityTest]
    public IEnumerator MemoryCaption_TwoMemories_AreShownOneAfterTheOther()
    {
        MemoryCaption caption = MakeCaption();
        yield return null;
        Text text = FindText(caption.gameObject, "Memory");

        textEvent.Raise("First.");
        textEvent.Raise("Second.");
        yield return TestUtils.WaitGameSeconds(1f);
        Assert.AreEqual("First.", text.text, "The second waits for its turn");

        yield return TestUtils.WaitUntil(() => text.text == "Second.", 6f, "the second memory");
    }

    // Regression (cleanup): turned off in the middle of a memory, the caption never showed a memory again.
    [UnityTest]
    public IEnumerator MemoryCaption_TurnedOffAndOnAgain_StillShowsMemories()
    {
        MemoryCaption caption = MakeCaption();
        yield return null;
        textEvent.Raise("First.");
        yield return null;

        caption.gameObject.SetActive(false);
        caption.gameObject.SetActive(true);
        textEvent.Raise("After.");

        Assert.AreEqual("After.", FindText(caption.gameObject, "Memory").text);
    }

    private TitleCard MakeFastTitleCard(GameEvent openingFinished)
    {
        GameObject title = MakeOff("Title");
        MakeHints(title);
        TitleCard card = title.AddComponent<TitleCard>();
        TestUtils.Set(card, "openingFinished", openingFinished);
        TestUtils.Set(card, "thoughts", new[] { "I woke up.", "Dark." });
        TestUtils.Set(card, "lettersPerSecond", 1000f);
        foreach (string field in new[] { "darkBeforeThoughts", "betweenThoughts", "afterThoughts", "textFadeOut", "afterThoughtsFade", "titleFadeIn", "titleStays" })
        {
            TestUtils.Set(card, field, 0.02f);
        }
        TestUtils.Set(card, "eyelidMoves", new Vector3(0.05f, 0.02f, 0.05f));
        TestUtils.Set(card, "eyelidPauses", new Vector2(0.02f, 0.02f));
        title.SetActive(true);
        return card;
    }

    [UnityTest]
    public IEnumerator TitleCard_StartsBlack_ThenOpensTheEyes_AndRaisesOpeningFinished()
    {
        GameEvent openingFinished = Track(TestData.Event());
        var opened = new EventSpy(openingFinished);
        TitleCard card = MakeFastTitleCard(openingFinished);
        yield return null;
        Assert.IsNotNull(card.GetComponentsInChildren<Image>().FirstOrDefault(i => i.name == "Black"), "The opening starts in the dark");
        Assert.AreEqual(0, opened.Count);

        yield return TestUtils.WaitUntil(() => opened.Count > 0, 10f, "the eyes to open");

        Assert.AreEqual(1, opened.Count);
        yield return null;
        Assert.IsFalse(card.GetComponentsInChildren<Canvas>().Any(c => c.name == "Title Card"), "The opening's canvas is gone");
    }

    [UnityTest]
    public IEnumerator TitleCard_WritesTheThoughtsAndTheTitle()
    {
        TitleCard card = MakeFastTitleCard(Track(TestData.Event()));

        yield return TestUtils.WaitUntil(() => card.GetComponentsInChildren<Text>().Any(t => t.text == "Dark."), 5f, "the second thought");
        yield return TestUtils.WaitUntil(() => card.GetComponentsInChildren<Text>().Any(t => t.text == "Day 1"), 5f, "the title");
    }

    [UnityTest]
    public IEnumerator TitleCard_AfterTheOpening_ShowsHowToMove()
    {
        GameEvent openingFinished = Track(TestData.Event());
        var opened = new EventSpy(openingFinished);
        TitleCard card = MakeFastTitleCard(openingFinished);
        yield return TestUtils.WaitUntil(() => opened.Count > 0, 10f, "the eyes to open");
        yield return null;

        Transform keys = card.GetComponentsInChildren<RectTransform>(true).First(t => t.name == "Keys");
        Assert.IsFalse(MobileJoystick.UsesTouch);
        Assert.AreEqual(4, keys.childCount, "W A S D");
    }
}
