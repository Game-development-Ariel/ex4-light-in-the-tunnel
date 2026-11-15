using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Shakes the camera for a moment when an event happens (the player is caught).
/// CameraFollow moves the camera every frame, so we can't just move it ourselves.
/// Instead the shake is added only while the camera draws the frame, and removed right after,
/// so CameraFollow never sees it.
/// </summary>
[RequireComponent(typeof(Camera))]
public class ScreenShake : MonoBehaviour
{
    [Tooltip("The event that starts a shake")]
    [SerializeField] private GameEvent shakeOn;

    [Tooltip("How far the camera moves, in world units. It fades out over the seconds")]
    [SerializeField] private float strength = 0.12f;
    [SerializeField] private float seconds = 0.5f;

    private Camera cam;
    private float timeLeft;
    private Vector3 offset;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Shake()
    {
        timeLeft = seconds;
    }

    // The render pipeline calls these right before and right after it draws each camera.
    private void OnEnable()
    {
        shakeOn.Subscribe(Shake);
        RenderPipelineManager.beginCameraRendering += BeforeDraw;
        RenderPipelineManager.endCameraRendering += AfterDraw;
    }

    private void OnDisable()
    {
        shakeOn.Unsubscribe(Shake);
        RenderPipelineManager.beginCameraRendering -= BeforeDraw;
        RenderPipelineManager.endCameraRendering -= AfterDraw;
    }

    private void Update()
    {
        timeLeft -= Time.deltaTime;
    }

    private void BeforeDraw(ScriptableRenderContext context, Camera drawing)
    {
        if (drawing != cam || timeLeft <= 0f)
        {
            return;
        }
        offset = Random.insideUnitCircle * strength * (timeLeft / seconds);
        transform.position += offset;
    }

    private void AfterDraw(ScriptableRenderContext context, Camera drawing)
    {
        if (drawing != cam)
        {
            return;
        }
        transform.position -= offset;
        offset = Vector3.zero;
    }
}
