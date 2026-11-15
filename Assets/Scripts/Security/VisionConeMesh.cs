using UnityEngine;

/// <summary>
/// Draws on the floor what a VisionSensor sees: a cone built from rays that stop at the first wall.
/// So if the cone touches the player, the player is caught, and behind a wall there is no cone and no danger.
/// The cone is a fan of triangles: vertex 0 is the sensor, and the others are the ends of the rays.
/// </summary>
[RequireComponent(typeof(MeshFilter))]
public class VisionConeMesh : MonoBehaviour
{
    [SerializeField] private VisionSensor sensor;

    [Tooltip("How many rays build the cone. More rays = the cone follows the walls more exactly")]
    [SerializeField] private int rayCount = 40;

    [Tooltip("A cold, pale light: the camera is a machine, not a lamp")]
    [SerializeField] private Color normalColor = new Color(0.66f, 0.76f, 0.95f, 0.13f);
    [SerializeField] private Color alarmColor = new Color(0.8f, 0.1f, 0.08f, 0.32f);

    [Tooltip("How see-through the far end of the cone is, compared to the sensor's end. Looks like light fading")]
    [SerializeField] private float endAlpha = 0.1f;

    private Mesh mesh;
    private Vector3[] vertices;
    private Color[] colors;

    private void Awake()
    {
        mesh = new Mesh { name = "Vision Cone" };
        mesh.MarkDynamic(); // Tells Unity the mesh changes every frame.
        GetComponent<MeshFilter>().mesh = mesh;

        vertices = new Vector3[rayCount + 1];
        colors = new Color[rayCount + 1];

        // The triangles never change, only where the ray ends are, so they are set once here.
        mesh.vertices = vertices;
        mesh.triangles = FanTriangles();
    }

    // LateUpdate: after the sensor turned this frame.
    private void LateUpdate()
    {
        float viewAngle = sensor.Stats.viewAngle;
        vertices[0] = Vector3.zero;
        for (int i = 0; i < rayCount; i++)
        {
            // From the left edge of the view to the right edge, in the sensor's own (local) directions.
            float angle = Mathf.Lerp(viewAngle / 2f, -viewAngle / 2f, i / (rayCount - 1f));
            Vector3 localDirection = Quaternion.Euler(0f, 0f, angle) * Vector3.up;
            Vector3 worldDirection = sensor.transform.TransformDirection(localDirection);
            vertices[i + 1] = localDirection * sensor.DistanceToWall(worldDirection);
        }

        // The mesh blends the colors between the corners, so the light seems to fade with distance.
        Color near = sensor.IsAlarmed ? alarmColor : normalColor;
        Color far = new Color(near.r, near.g, near.b, near.a * endAlpha);
        colors[0] = near;
        for (int i = 1; i < colors.Length; i++)
        {
            colors[i] = far;
        }

        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.RecalculateBounds();
    }

    private int[] FanTriangles()
    {
        int[] triangles = new int[(rayCount - 1) * 3];
        for (int i = 0; i < rayCount - 1; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }
        return triangles;
    }
}
