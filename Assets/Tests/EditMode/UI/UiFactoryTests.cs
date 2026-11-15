using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UiFactoryTests : EditModeTest
{
    private Canvas canvas;

    [SetUp]
    public void SetUp()
    {
        canvas = Track(UiFactory.CreateCanvas("Test Canvas", 7).gameObject).GetComponent<Canvas>();
    }

    [Test]
    public void CreateCanvas_IsAnOverlayThatScalesWithTheScreen()
    {
        Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode);
        Assert.AreEqual(7, canvas.sortingOrder);
        var scaler = canvas.GetComponent<CanvasScaler>();
        Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode);
        Assert.AreEqual(new Vector2(1920f, 1080f), scaler.referenceResolution);
        Assert.IsNotNull(canvas.GetComponent<GraphicRaycaster>());
    }

    [Test]
    public void CreateText_HasTheThoughtFont_AndDoesNotCatchClicks()
    {
        Text text = UiFactory.CreateText(canvas.transform, "Line", UiFactory.Center, new Vector2(100f, 20f), 30, TextAnchor.MiddleLeft);

        Assert.AreEqual(UiTheme.ThoughtFont, text.font);
        Assert.AreEqual(30, text.fontSize);
        Assert.AreEqual(TextAnchor.MiddleLeft, text.alignment);
        Assert.IsFalse(text.raycastTarget);
        Assert.IsNotNull(text.GetComponent<Shadow>());
        Assert.AreEqual(canvas.transform, text.transform.parent);
        Assert.AreEqual(new Vector2(100f, 20f), text.rectTransform.sizeDelta);
    }

    [Test]
    public void CreateTitle_HasTheTitleFontAndColor()
    {
        Text title = UiFactory.CreateTitle(canvas.transform, "Title", UiFactory.Center, Vector2.one, 50, Color.red);

        Assert.AreEqual(UiTheme.TitleFont, title.font);
        Assert.AreEqual(Color.red, title.color);
        Assert.AreEqual(TextAnchor.MiddleCenter, title.alignment);
    }

    [Test]
    public void CreateIcon_LoadsTheSprite_AndKeepsItsShape()
    {
        Image icon = UiFactory.CreateIcon(canvas.transform, "Hope", new Vector2(64f, 32f));

        Assert.AreEqual(UiTheme.Sprite("Hope"), icon.sprite);
        Assert.IsTrue(icon.preserveAspect);
        Assert.AreEqual(new Vector2(64f, 32f), icon.rectTransform.sizeDelta);
    }

    [Test]
    public void CreatePanel_IsSliced()
    {
        Image panel = UiFactory.CreatePanel(canvas.transform, "Panel", "BarBack");

        Assert.AreEqual(Image.Type.Sliced, panel.type);
    }

    [Test]
    public void CreateButton_CatchesClicks_AndHasItsLabelAndFeel()
    {
        Button button = UiFactory.CreateButton(canvas.transform, "Play again", UiFactory.Center, new Vector2(300f, 80f), 40, Color.white);

        Assert.AreEqual("Play again Button", button.name);
        Assert.IsTrue(button.GetComponent<Image>().raycastTarget);
        Assert.IsNotNull(button.GetComponent<ButtonFeel>());
        Assert.AreEqual("Play again", button.GetComponentInChildren<Text>().text);
        Assert.AreEqual(Selectable.Transition.None, button.transition);
    }

    [Test]
    public void CreateRow_LaysOutItsChildrenSideBySide()
    {
        RectTransform row = UiFactory.CreateRow(canvas.transform, "Row", 12f);

        var layout = row.GetComponent<HorizontalLayoutGroup>();
        Assert.AreEqual(12f, layout.spacing);
        Assert.IsFalse(layout.childForceExpandWidth);
        Assert.AreEqual(ContentSizeFitter.FitMode.PreferredSize, row.GetComponent<ContentSizeFitter>().horizontalFit);
    }

    [Test]
    public void Place_SetsAnchorPivotOffsetAndSize()
    {
        Image image = UiFactory.CreateImage(canvas.transform, "Image", null, Color.white);

        UiFactory.Place(image.rectTransform, UiFactory.TopLeft, new Vector2(10f, -20f), new Vector2(30f, 40f));

        Assert.AreEqual(UiFactory.TopLeft, image.rectTransform.anchorMin);
        Assert.AreEqual(UiFactory.TopLeft, image.rectTransform.anchorMax);
        Assert.AreEqual(UiFactory.TopLeft, image.rectTransform.pivot);
        Assert.AreEqual(new Vector2(10f, -20f), image.rectTransform.anchoredPosition);
        Assert.AreEqual(new Vector2(30f, 40f), image.rectTransform.sizeDelta);
    }

    [Test]
    public void Stretch_FillsTheParent()
    {
        Image image = UiFactory.CreateImage(canvas.transform, "Image", null, Color.white);

        UiFactory.Stretch(image.rectTransform);

        Assert.AreEqual(Vector2.zero, image.rectTransform.anchorMin);
        Assert.AreEqual(Vector2.one, image.rectTransform.anchorMax);
        Assert.AreEqual(Vector2.zero, image.rectTransform.offsetMin);
        Assert.AreEqual(Vector2.zero, image.rectTransform.offsetMax);
    }

    [Test]
    public void EnsureEventSystem_MakesOnlyOne()
    {
        foreach (EventSystem old in Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
        {
            Object.DestroyImmediate(old.gameObject);
        }

        UiFactory.EnsureEventSystem();
        UiFactory.EnsureEventSystem();

        EventSystem[] systems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        foreach (EventSystem system in systems)
        {
            Track(system.gameObject);
        }
        Assert.AreEqual(1, systems.Length);
    }
}
