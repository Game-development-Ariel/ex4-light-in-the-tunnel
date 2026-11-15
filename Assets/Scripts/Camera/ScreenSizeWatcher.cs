using System;
using UnityEngine;

/// <summary>
/// Notices when the screen changes size (a phone turned, a browser window resized) and tells whoever listens.
/// One watcher for the level, so every script that cares about the screen size doesn't check it on its own.
/// </summary>
public class ScreenSizeWatcher : MonoBehaviour
{
    private int lastWidth;
    private int lastHeight;

    /// <summary>Happens on the frame the screen size changes.</summary>
    public event Action Changed;

    private void Awake()
    {
        lastWidth = Screen.width;
        lastHeight = Screen.height;
    }

    private void Update()
    {
        if (Screen.width == lastWidth && Screen.height == lastHeight)
        {
            return;
        }
        lastWidth = Screen.width;
        lastHeight = Screen.height;
        Changed?.Invoke();
    }
}
