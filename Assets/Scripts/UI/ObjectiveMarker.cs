using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows the player where to go: a faint light-colored chevron floats over the current goal
/// (the water bottle, and after picking it up, the mattress).
/// When the goal is off the screen, a second arrow on the screen's edge points toward it.
/// </summary>
public class ObjectiveMarker : MonoBehaviour
{
    // Just above the HUD (10), so the edge arrow is never hidden behind the hope bar.
    private const int CanvasSortingOrder = 12;

    // The floating arrow is drawn above the walls and the player, and its glow right behind it.
    private const int ArrowSortingOrder = 20;

    // The chevron sprite is drawn pointing down. Rotating by (angle + 90) points it at the angle.
    private const float SpriteDownOffset = 90f;

    // The size of the arrow on the screen's edge, in UI pixels.
    private static readonly Vector2 EdgeArrowSize = new Vector2(60f, 60f);

    // A tiny length, in UI pixels, so the math never divides by zero when the goal is straight above, below or to the side.
    private const float MinDirection = 0.001f;

    [Tooltip("Where to go after the bottle is picked up")]
    [SerializeField] private Transform mattress;

    [Tooltip("Where the bottle is, while it is on the floor")]
    [SerializeField] private BottleSpawner spawner;

    [Header("Listens to")]
    [SerializeField] private GameEvent bottlePicked;
    [SerializeField] private GameEvent bottleDropped;

    [Header("Look")]
    [Tooltip("The color of both arrows, the floating one and the one on the edge")]
    [SerializeField] private Color markerColor = new Color(1f, 0.95f, 0.85f, 0.75f);

    [Tooltip("A faint glow behind the floating arrow, so it reads in the dark without shouting")]
    [SerializeField] private Color glowColor = new Color(1f, 0.85f, 0.6f, 0.25f);

    [Tooltip("The size of the glow, compared to the arrow")]
    [SerializeField] private float glowScale = 1.6f;

    [Tooltip("How fast the edge arrow pulses, and how much it grows. 0.06 = 6 percent")]
    [SerializeField] private Vector2 edgePulse = new Vector2(2.5f, 0.06f);

    [Tooltip("How high over the goal the arrow floats, in world units")]
    [SerializeField] private float heightAbove = 1f;

    [Tooltip("The size of the floating arrow in the world")]
    [SerializeField] private float worldArrowScale = 0.8f;

    [Tooltip("Distance of the edge arrow from the screen's edge, in UI pixels")]
    [SerializeField] private float edgeMargin = 90f;

    private Camera cam;
    private Transform worldArrow;
    private RectTransform canvasRect;
    private RectTransform edgeArrow;

    // True while the player carries the bottle, so the goal is the mattress.
    private bool carrying;

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
        carrying = true;
    }

    private void OnBottleDropped()
    {
        carrying = false;
    }

    private void Start()
    {
        cam = Camera.main;
        CreateWorldArrow();

        Canvas canvas = UiFactory.CreateCanvas("Objective Canvas", CanvasSortingOrder);
        canvas.transform.SetParent(transform, false);
        canvasRect = canvas.GetComponent<RectTransform>();
        Image arrow = UiFactory.CreateIcon(canvas.transform, "Chevron", EdgeArrowSize);
        arrow.color = markerColor;
        edgeArrow = arrow.rectTransform;
        UiFactory.Place(edgeArrow, UiFactory.Center, Vector2.zero, EdgeArrowSize);
    }

    /// <summary>
    /// The floating arrow: a parent that follows the goal, and a child sprite that bobs.
    /// The bob moves the child, so following and bobbing don't fight each other.
    /// </summary>
    private void CreateWorldArrow()
    {
        worldArrow = new GameObject("Objective Arrow").transform;
        worldArrow.SetParent(transform, false);

        var sprite = new GameObject("Sprite");
        sprite.transform.SetParent(worldArrow, false);
        sprite.transform.localScale = Vector3.one * worldArrowScale;
        SpriteRenderer spriteRenderer = sprite.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = UiTheme.Sprite("Chevron");
        spriteRenderer.color = markerColor;
        spriteRenderer.sortingOrder = ArrowSortingOrder;
        sprite.AddComponent<Bob>();

        var glow = new GameObject("Glow");
        glow.transform.SetParent(sprite.transform, false);
        glow.transform.localScale = Vector3.one * glowScale;
        SpriteRenderer glowRenderer = glow.AddComponent<SpriteRenderer>();
        glowRenderer.sprite = UiTheme.Sprite("Glow");
        glowRenderer.color = glowColor;
        glowRenderer.sortingOrder = ArrowSortingOrder - 1;
    }

    // LateUpdate: after the camera and the player moved this frame.
    private void LateUpdate()
    {
        Transform goal = carrying ? mattress : spawner.Bottle;
        worldArrow.gameObject.SetActive(goal != null);
        edgeArrow.gameObject.SetActive(false);
        if (goal == null)
        {
            return;
        }

        worldArrow.position = goal.position + Vector3.up * heightAbove;

        // Viewport: (0,0) is the bottom left of the screen and (1,1) the top right.
        Vector3 viewport = cam.WorldToViewportPoint(goal.position);
        bool onScreen = viewport.x > 0f && viewport.x < 1f && viewport.y > 0f && viewport.y < 1f;
        if (!onScreen)
        {
            PlaceEdgeArrow(viewport);
        }
    }

    /// <summary>Puts the edge arrow on the line from the screen's center toward the goal, just inside the edge.</summary>
    private void PlaceEdgeArrow(Vector3 viewport)
    {
        Vector2 half = canvasRect.rect.size / 2f;
        Vector2 direction = new Vector2((viewport.x - 0.5f) * half.x * 2f, (viewport.y - 0.5f) * half.y * 2f);

        // Stretch the direction until it touches the nearer edge (left/right or top/bottom).
        float toSide = (half.x - edgeMargin) / Mathf.Max(Mathf.Abs(direction.x), MinDirection);
        float toTop = (half.y - edgeMargin) / Mathf.Max(Mathf.Abs(direction.y), MinDirection);
        Vector2 position = direction * Mathf.Min(toSide, toTop);

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float pulse = 1f + Mathf.Sin(Time.time * edgePulse.x) * edgePulse.y;

        edgeArrow.gameObject.SetActive(true);
        edgeArrow.anchoredPosition = position;
        edgeArrow.localRotation = Quaternion.Euler(0f, 0f, angle + SpriteDownOffset);
        edgeArrow.localScale = Vector3.one * pulse;
    }
}
