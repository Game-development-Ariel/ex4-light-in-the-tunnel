using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A water drop at the end of the hope bar: faint while the hero has no water, full while he carries the bottle.
/// </summary>
public class WaterIndicator : MonoBehaviour
{
    private const int CanvasSortingOrder = 10;

    // The drop at the end of the hope bar, in UI pixels from the top-left corner of the screen. Down is minus.
    private static readonly Vector2 DropSize = new Vector2(40f, 40f);
    private static readonly Vector2 DropPosition = new Vector2(428f, -60f);

    [Header("Listens to")]
    [SerializeField] private GameEvent bottlePicked;
    [SerializeField] private GameEvent bottleDropped;

    [Tooltip("How see-through the drop is when the hero has no water")]
    [Range(0f, 1f)]
    [SerializeField] private float emptyAlpha = 0.18f;

    [Tooltip("Seconds the drop takes to fill or empty")]
    [SerializeField] private float fadeSeconds = 0.6f;

    [Tooltip("How much the drop grows for a moment when the bottle is picked up, and for how many seconds")]
    [SerializeField] private Vector2 pickPunch = new Vector2(0.3f, 0.5f);

    [Tooltip("How loud the sound of the water is when the bottle is picked up")]
    [SerializeField] private float pickVolume = 0.8f;

    private Image drop;

    // The UI is created in Awake, before OnEnable starts listening.
    private void Awake()
    {
        Canvas canvas = UiFactory.CreateCanvas("Water Canvas", CanvasSortingOrder);
        canvas.transform.SetParent(transform, false);
        drop = UiFactory.CreateIcon(canvas.transform, "Drop", DropSize);
        UiFactory.Place(drop.rectTransform, UiFactory.TopLeft, DropPosition, DropSize);
        drop.rectTransform.pivot = UiFactory.Center;
        drop.color = new Color(1f, 1f, 1f, emptyAlpha);
    }

    private void OnEnable()
    {
        bottlePicked.Subscribe(OnBottlePicked);
        bottleDropped.Subscribe(OnBottleDropped);
    }

    private void OnDisable()
    {
        bottlePicked.Unsubscribe(OnBottlePicked);
        bottleDropped.Unsubscribe(OnBottleDropped);
    }

    private void OnBottlePicked()
    {
        Sfx.Play("Water", pickVolume);
        StartCoroutine(UiAnim.Punch(drop.transform, pickPunch.x, pickPunch.y));
        StartCoroutine(UiAnim.FadeGraphic(drop, 1f, fadeSeconds));
    }

    private void OnBottleDropped()
    {
        StartCoroutine(UiAnim.FadeGraphic(drop, emptyAlpha, fadeSeconds));
    }
}
