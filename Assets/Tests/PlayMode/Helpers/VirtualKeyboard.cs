using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>
/// A keyboard made in code, so a test can "press" keys. While it exists the input system also listens
/// without a focused game window (there is none in batch mode). Remove it at the end of the test.
/// </summary>
public sealed class VirtualKeyboard
{
    private readonly InputSettings originalSettings;
    private readonly InputSettings testSettings;

    public Keyboard Device { get; }

    public VirtualKeyboard()
    {
        // The input system destroys the settings it lets go of, so both are copies.
        originalSettings = UnityEngine.Object.Instantiate(InputSystem.settings);
        testSettings = UnityEngine.Object.Instantiate(InputSystem.settings);
        testSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        testSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings = testSettings;
        Device = InputSystem.AddDevice<Keyboard>("Test Keyboard");
    }

    /// <summary>Holds these keys down (and lets go of every other key). Takes effect on the next frame.</summary>
    public void Hold(params Key[] keys)
    {
        InputSystem.QueueStateEvent(Device, new KeyboardState(keys));
    }

    public void ReleaseAll()
    {
        InputSystem.QueueStateEvent(Device, new KeyboardState());
    }

    public void Remove()
    {
        InputSystem.RemoveDevice(Device);
        InputSystem.settings = originalSettings;
    }
}
