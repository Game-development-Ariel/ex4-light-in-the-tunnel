using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Darkens the edges of the screen with a soft picture, under all the other UI, so the eye goes to the middle.
/// </summary>
public class ScreenVignette : MonoBehaviour
{
    // Under every other canvas.
    private const int CanvasSortingOrder = -10;

    [Tooltip("The color of the vignette picture. Its alpha is how dark the edges are")]
    [SerializeField] private Color tint = new Color(1f, 1f, 1f, 0.7f);

    private void Awake()
    {
        Canvas canvas = UiFactory.CreateCanvas("Vignette Canvas", CanvasSortingOrder);
        canvas.transform.SetParent(transform, false);
        Image vignette = UiFactory.CreateImage(canvas.transform, "Vignette", UiTheme.Sprite("Vignette"), tint);
        UiFactory.Stretch(vignette.rectTransform);
    }
}
