using NUnit.Framework;
using UnityEngine;

/// <summary>
/// The code loads pictures, sounds and fonts by name from Assets/Resources. A renamed file would only show up
/// as a missing picture or a silent sound, so every name the code uses is checked here.
/// </summary>
public class ResourcesTests
{
    [TestCase("Band")]
    [TestCase("BarBack")]
    [TestCase("BarFill")]
    [TestCase("Beam")]
    [TestCase("Button")]
    [TestCase("Chevron")]
    [TestCase("Clock")]
    [TestCase("Drop")]
    [TestCase("Eye")]
    [TestCase("Glow")]
    [TestCase("Hope")]
    [TestCase("JoystickKnob")]
    [TestCase("JoystickRing")]
    [TestCase("Key")]
    [TestCase("Vignette")]
    public void UiSprite_Exists(string name)
    {
        Assert.IsNotNull(UiTheme.Sprite(name), $"Resources/UI/{name} is missing or is not a sprite");
    }

    [TestCase("Wake")]
    [TestCase("Caught")]
    [TestCase("Win")]
    [TestCase("Lose")]
    [TestCase("Water")]
    [TestCase("Hint")]
    [TestCase("Memory")]
    [TestCase("Click")]
    [TestCase("CameraHum")]
    [TestCase("Step1")]
    public void Sound_Exists(string name)
    {
        Assert.IsNotNull(Resources.Load<AudioClip>("Audio/" + name), $"Resources/Audio/{name} is missing");
    }

    [Test]
    public void Fonts_Exist()
    {
        Assert.IsNotNull(UiTheme.TitleFont);
        Assert.IsNotNull(UiTheme.ItalicFont);
        Assert.IsNotNull(UiTheme.ThoughtFont);
    }

    [Test]
    public void UnknownSprite_IsNull_AndDoesNotThrow()
    {
        Assert.IsNull(UiTheme.Sprite("No Such Sprite"));
    }
}
