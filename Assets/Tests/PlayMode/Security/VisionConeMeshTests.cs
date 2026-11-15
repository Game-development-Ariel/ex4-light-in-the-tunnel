using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>The cone on the floor is drawn from the same rays the sensor sees with, so it stops at walls.</summary>
public class VisionConeMeshTests : PlayModeTest
{
    private const int Rays = 5;

    private VisionSensor sensor;
    private VisionConeMesh cone;
    private Mesh mesh;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        GameObject watcher = MakeOff("Camera");
        sensor = watcher.AddComponent<VisionSensor>();
        TestUtils.Set(sensor, "stats", Track(TestData.Vision(60f, 5f)));
        TestUtils.Set(sensor, "targetSeen", Track(TestData.Event()));
        watcher.AddComponent<MeshFilter>();
        watcher.AddComponent<MeshRenderer>();
        cone = watcher.AddComponent<VisionConeMesh>();
        TestUtils.Set(cone, "sensor", sensor);
        TestUtils.Set(cone, "rayCount", Rays);
        watcher.SetActive(true);
        yield return null;
        mesh = TestUtils.Get<Mesh>(cone, "mesh");
    }

    [Test]
    public void Mesh_IsAFanOfTriangles()
    {
        Assert.AreEqual(Rays + 1, mesh.vertexCount);
        Assert.AreEqual((Rays - 1) * 3, mesh.triangles.Length);
        Assert.AreEqual(0, mesh.triangles[0], "Every triangle starts at the sensor");
    }

    [UnityTest]
    public IEnumerator WithNoWalls_EveryRayReachesTheViewDistance()
    {
        yield return null;

        for (int i = 1; i <= Rays; i++)
        {
            Assert.AreEqual(5f, mesh.vertices[i].magnitude, 1e-3f);
        }
        Assert.AreEqual(Vector3.zero, mesh.vertices[0]);
    }

    [UnityTest]
    public IEnumerator Rays_StopAtAWall()
    {
        TestHero.Wall(Make("Wall"), new Vector3(0f, 2f, 0f), new Vector3(0.5f, 0.2f, 2f));
        Physics.SyncTransforms();
        yield return null;

        Vector3 middle = mesh.vertices[Rays / 2 + 1]; // The ray that goes straight up.
        Assert.AreEqual(1.9f, middle.y, 0.02f);
        Assert.AreEqual(5f, mesh.vertices[1].magnitude, 1e-3f, "The edge rays pass the narrow wall");
    }

    [UnityTest]
    public IEnumerator Cone_SpansTheViewAngle()
    {
        yield return null;

        Assert.AreEqual(60f, Vector3.Angle(mesh.vertices[1], mesh.vertices[Rays]), 0.1f);
    }

    [UnityTest]
    public IEnumerator Cone_TurnsRed_WhenAlarmed()
    {
        TestUtils.Set(sensor, "alarmTimer", 10f);
        yield return null;

        Assert.AreEqual(TestUtils.Get<Color>(cone, "alarmColor"), mesh.colors[0]);
    }

    [UnityTest]
    public IEnumerator Cone_FadesWithDistance()
    {
        yield return null;

        Assert.Less(mesh.colors[1].a, mesh.colors[0].a);
        Assert.AreEqual(TestUtils.Get<Color>(cone, "normalColor"), mesh.colors[0]);
    }
}
