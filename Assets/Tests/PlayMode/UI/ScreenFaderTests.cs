using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>The fader's static flag. Loading a scene through it is tested in PlayAgainTests, with the real scene.</summary>
public class ScreenFaderTests : PlayModeTest
{
    [TearDown]
    public void ClearTheFlag()
    {
        TestUtils.SetStatic(typeof(ScreenFader), "isFading", false);
    }

    // Regression (cleanup): domain reload is off, so a fade cut short by stopping the game left the flag on,
    // and Play again did nothing in every later run.
    [Test]
    public void ResetStatics_ClearsTheFlag_AtTheStartOfARun()
    {
        TestUtils.SetStatic(typeof(ScreenFader), "isFading", true);

        TestUtils.CallStatic(typeof(ScreenFader), "ResetStatics");

        Assert.IsFalse(TestUtils.GetStatic<bool>(typeof(ScreenFader), "isFading"));
    }

    [Test]
    public void ResetStatics_RunsAtTheStartOfEveryRun()
    {
        var method = typeof(ScreenFader).GetMethod("ResetStatics", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var attribute = (RuntimeInitializeOnLoadMethodAttribute)System.Attribute.GetCustomAttribute(method, typeof(RuntimeInitializeOnLoadMethodAttribute));

        Assert.IsNotNull(attribute);
        Assert.AreEqual(RuntimeInitializeLoadType.SubsystemRegistration, attribute.loadType);
    }

    [UnityTest]
    public IEnumerator FadeAndLoad_DuringAFade_DoesNothing()
    {
        TestUtils.SetStatic(typeof(ScreenFader), "isFading", true);

        ScreenFader.FadeAndLoad("Tutorial");
        yield return null;

        Assert.IsNull(GameObject.Find("Screen Fader"), "A second fade would load the scene twice");
    }
}
