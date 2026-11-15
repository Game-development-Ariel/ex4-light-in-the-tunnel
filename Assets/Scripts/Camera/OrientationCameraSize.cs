using UnityEngine;

/// <summary>
/// Keeps objects the same size on the screen when the phone turns between landscape and portrait.
///
/// Why objects grow in portrait: orthographicSize is half of the world HEIGHT that the camera shows.
/// Unity keeps that height fixed and stretches it over the screen's height in pixels.
/// In portrait the screen is much taller, so every world unit gets more pixels and looks bigger
/// (and the visible width shrinks).
///
/// The fix: keep the number of pixels per world unit fixed.
/// The short side of the screen is the same in both orientations, so we use it as the reference:
/// the Size set in the Inspector is the size for landscape (where the height is the short side),
/// and in portrait we grow the size by (long side / short side).
/// Only the camera is changed, no other object in the game. The ScreenSizeWatcher tells it when the screen turns.
/// </summary>
[RequireComponent(typeof(Camera), typeof(ScreenSizeWatcher))]
public class OrientationCameraSize : MonoBehaviour
{
    private Camera cam;
    private ScreenSizeWatcher screenSize;

    // The orthographicSize from the Inspector: the wanted size when the screen is in landscape.
    private float landscapeSize;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        screenSize = GetComponent<ScreenSizeWatcher>();
        landscapeSize = cam.orthographicSize;
        UpdateSize();
    }

    private void OnEnable()
    {
        screenSize.Changed += UpdateSize;
    }

    private void OnDisable()
    {
        screenSize.Changed -= UpdateSize;
    }

    /// <summary>Sets orthographicSize so one world unit takes the same number of pixels as in landscape.</summary>
    private void UpdateSize()
    {
        int shortSide = Mathf.Min(Screen.width, Screen.height);

        // In landscape the height is the short side, so the ratio is 1 and the size stays as set.
        float heightToShortSide = (float)Screen.height / shortSide;
        cam.orthographicSize = landscapeSize * heightToShortSide;
    }
}
