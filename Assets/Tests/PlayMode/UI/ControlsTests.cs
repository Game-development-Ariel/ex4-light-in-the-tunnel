using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>The buttons' feel, the phone joystick, and the small UI animations.</summary>
public class ControlsTests : PlayModeTest
{
    [UnityTest]
    public IEnumerator ButtonFeel_GrowsOnHover_ShrinksOnPress_AndSettles()
    {
        GameObject button = Make("Button");
        ButtonFeel feel = button.AddComponent<ButtonFeel>();

        feel.OnPointerEnter(null);
        yield return ScaleReaches(button.transform, 1.04f, "hover");

        feel.OnPointerDown(null);
        yield return ScaleReaches(button.transform, 0.97f, "press");

        feel.OnPointerUp(null);
        yield return ScaleReaches(button.transform, 1.04f, "release");

        feel.OnPointerExit(null);
        yield return ScaleReaches(button.transform, 1f, "leave");
    }

    // ButtonFeel counts real (unscaled) time, so it is waited for instead of counted in frames.
    private static IEnumerator ScaleReaches(Transform target, float scale, string what)
    {
        yield return TestUtils.WaitUntil(() => Mathf.Abs(target.localScale.x - scale) < 0.005f, 3f, "the scale after " + what);
        Assert.AreEqual(target.localScale.x, target.localScale.y, 1e-5f);
    }

    [UnityTest]
    public IEnumerator Joystick_IsHiddenOnAComputer_AndDrivesTheLeftStick()
    {
        GameObject hud = Make("HUD");
        hud.AddComponent<MobileJoystick>();
        yield return null;
        yield return null;

        Assert.IsFalse(MobileJoystick.UsesTouch, "The editor is not a phone");
        GameObject canvas = hud.GetComponentsInChildren<Canvas>(true).First(c => c.name == "Joystick Canvas").gameObject;
        Assert.IsFalse(canvas.activeSelf);
        OnScreenStick stick = hud.GetComponentInChildren<OnScreenStick>(true);
        Assert.AreEqual("<Gamepad>/leftStick", stick.controlPath);
        Assert.IsTrue(stick.GetComponent<Image>().raycastTarget, "The knob needs the touches");
    }

    [UnityTest]
    public IEnumerator Fade_ReachesTheGoal()
    {
        var group = Make("Group").AddComponent<CanvasGroup>();
        group.alpha = 0f;

        yield return UiAnim.Fade(group, 1f, 0.1f);
        Assert.AreEqual(1f, group.alpha);

        yield return UiAnim.Fade(group, 0.25f, 0f);
        Assert.AreEqual(0.25f, group.alpha, "Zero seconds jumps straight to the goal");
    }

    [UnityTest]
    public IEnumerator FadeGraphic_ChangesOnlyTheAlpha()
    {
        Image image = Make("Image").AddComponent<Image>();
        image.color = new Color(0.2f, 0.4f, 0.6f, 1f);

        yield return UiAnim.FadeGraphic(image, 0f, 0.1f);

        Assert.AreEqual(new Color(0.2f, 0.4f, 0.6f, 0f), image.color);
    }

    [UnityTest]
    public IEnumerator Punch_GrowsAndComesBackToNormal()
    {
        Transform target = Make("Target").transform;
        float largest = 0f;
        IEnumerator punch = UiAnim.Punch(target, 0.5f, 0.2f);
        while (punch.MoveNext())
        {
            largest = Mathf.Max(largest, target.localScale.x);
            yield return punch.Current;
        }

        Assert.Greater(largest, 1.3f);
        Assert.LessOrEqual(largest, 1.5f);
        Assert.AreEqual(Vector3.one, target.localScale);
    }

    [UnityTest]
    public IEnumerator Typewriter_WritesTheWholeText_OneLetterAtATime()
    {
        Text text = Make("Text").AddComponent<Text>();
        text.text = "old";
        IEnumerator writing = UiAnim.Typewriter(text, "Hi, you.", 1000f);
        int steps = 0;
        while (writing.MoveNext())
        {
            steps++;
            Assert.AreEqual("Hi, you.".Substring(0, steps), text.text);
            yield return writing.Current;
        }

        Assert.AreEqual("Hi, you.", text.text);
        Assert.AreEqual(8, steps);
    }

    [UnityTest]
    public IEnumerator Typewriter_EmptyText_LeavesItEmpty()
    {
        Text text = Make("Text").AddComponent<Text>();
        text.text = "old";

        yield return UiAnim.Typewriter(text, "", 1000f);

        Assert.AreEqual("", text.text);
    }
}
