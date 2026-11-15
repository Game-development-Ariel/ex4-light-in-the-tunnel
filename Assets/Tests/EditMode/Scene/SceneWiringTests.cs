using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Opens the real Tutorial scene and checks that it is wired: no missing scripts, no empty references,
/// and every event the scripts use is a real asset with someone raising it and someone listening.
/// </summary>
public class SceneWiringTests
{
    private const string ScenePath = "Assets/Scenes/Tutorial.unity";

    private Scene scene;

    [OneTimeSetUp]
    public void OpenScene()
    {
        scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    [OneTimeTearDown]
    public void CloseScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    [Test]
    public void Scene_HasNoMissingScripts()
    {
        List<string> missing = SceneChecks.MissingScripts(scene);
        Assert.IsEmpty(missing, "Objects with a missing script:\n" + string.Join("\n", missing));
    }

    [Test]
    public void Scene_HasNoEmptyReferences()
    {
        List<string> empty = SceneChecks.EmptyReferences(scene);
        Assert.IsEmpty(empty, "Empty references:\n" + string.Join("\n", empty));
    }

    [Test]
    public void Scene_IsTheFirstSceneInTheBuild()
    {
        Assert.IsNotEmpty(EditorBuildSettings.scenes);
        Assert.AreEqual(ScenePath, EditorBuildSettings.scenes[0].path);
        Assert.IsTrue(EditorBuildSettings.scenes[0].enabled);
    }

    [Test]
    public void EveryEventTheSceneUses_IsAnAssetInDataEvents()
    {
        foreach (Object used in SceneChecks.EventUses(scene).Keys)
        {
            string path = AssetDatabase.GetAssetPath(used);
            StringAssert.StartsWith("Assets/Data/Events/", path, $"{used.name} is not an asset in Assets/Data/Events");
        }
    }

    [Test]
    public void EveryEventAsset_HasARaiserAndAListenerInTheScene()
    {
        Dictionary<Object, int> uses = SceneChecks.EventUses(scene);
        foreach (string path in AssetDatabase.FindAssets("t:Object", new[] { "Assets/Data/Events" }).Select(AssetDatabase.GUIDToAssetPath))
        {
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
            uses.TryGetValue(asset, out int count);
            Assert.GreaterOrEqual(count, 2, $"{asset.name} is held by {count} scripts. An event needs one that raises it and one that listens");
        }
    }

    [TestCase(typeof(PlayerMovement), 1)]
    [TestCase(typeof(Hope), 1)]
    [TestCase(typeof(TunnelRound), 1)]
    [TestCase(typeof(RoundStats), 1)]
    [TestCase(typeof(LevelFreeze), 1)]
    [TestCase(typeof(BottleCarrier), 1)]
    [TestCase(typeof(BottleSpawner), 1)]
    [TestCase(typeof(MattressGoal), 1)]
    [TestCase(typeof(NoiseWarning), 1)]
    [TestCase(typeof(EndScreen), 1)]
    [TestCase(typeof(TitleCard), 1)]
    [TestCase(typeof(GuardPatrol), 1)]
    public void Scene_HasExactlyOne(System.Type type, int expected)
    {
        int count = SceneChecks.AllObjects(scene).Count(go => go.GetComponent(type) != null);
        Assert.AreEqual(expected, count, $"{type.Name} objects in the scene");
    }

    [Test]
    public void Scene_HasWatchers_EachWithStats()
    {
        VisionSensor[] sensors = SceneChecks.AllObjects(scene).SelectMany(go => go.GetComponents<VisionSensor>()).ToArray();
        Assert.GreaterOrEqual(sensors.Length, 3, "Two cameras and a guard");
        foreach (VisionSensor sensor in sensors)
        {
            Assert.IsNotNull(sensor.Stats, sensor.name);
            StringAssert.StartsWith("Watchers", SceneChecks.PathOf(sensor.gameObject), "LevelFreeze stops only the watchers under Watchers");
        }
    }

    [Test]
    public void EveryLooseObject_HasNoiseAndTheLevelsWarning()
    {
        ImpactNoise[] noises = SceneChecks.AllObjects(scene).SelectMany(go => go.GetComponents<ImpactNoise>()).ToArray();
        Assert.IsNotEmpty(noises);
        NoiseWarning warning = SceneChecks.AllObjects(scene).Select(go => go.GetComponent<NoiseWarning>()).Single(w => w != null);
        foreach (ImpactNoise noise in noises)
        {
            Assert.AreSame(warning, TestUtils.Get<NoiseWarning>(noise, "warning"), noise.name);
            Assert.IsFalse(noise.GetComponent<Rigidbody>().isKinematic, noise.name + " must be pushable");
        }
    }

    // Regression (cleanup): after a map rebuild the guard's steps lost the hero's senses.
    [Test]
    public void GuardsHearing_IsWiredToTheHerosSenses()
    {
        HopeSenses hero = SceneChecks.AllObjects(scene).Select(go => go.GetComponent<HopeSenses>()).Single(s => s != null);
        foreach (HeardSteps steps in SceneChecks.AllObjects(scene).SelectMany(go => go.GetComponents<HeardSteps>()))
        {
            Assert.AreSame(hero, TestUtils.Get<HopeSenses>(steps, "heroSenses"));
        }
    }

    [Test]
    public void Player_IsOnTheIgnoreRaycastLayer_SoTheWatchersRaysStopOnlyAtWalls()
    {
        GameObject player = SceneChecks.AllObjects(scene).Single(go => go.GetComponent<PlayerMovement>() != null);
        Assert.AreEqual(LayerMask.NameToLayer("Ignore Raycast"), player.layer);
        var controller = player.GetComponent<CharacterController>();
        Assert.AreEqual(controller.radius * 2f, controller.height, 1e-4f, "The controller is a ball on the flat map");
    }

    [Test]
    public void TheBottleAndTheMattress_AreTriggers()
    {
        GameObject goal = SceneChecks.AllObjects(scene).Single(go => go.GetComponent<MattressGoal>() != null);
        Assert.IsTrue(goal.GetComponent<Collider>().isTrigger);
        GameObject bottle = TestUtils.Get<GameObject>(Object.FindAnyObjectByType<BottleSpawner>(), "bottlePrefab");
        Assert.IsNotNull(bottle.GetComponent<WaterBottle>());
        Assert.IsTrue(bottle.GetComponent<Collider>().isTrigger);
    }

    [Test]
    public void LevelFreeze_StopsThePlayerAndTheWatchersAtTheEnd()
    {
        LevelFreeze freeze = Object.FindAnyObjectByType<LevelFreeze>();
        GameObject[] stopped = TestUtils.Get<GameObject[]>(freeze, "stopWhenLevelEnds");
        CollectionAssert.Contains(stopped.Select(go => go.name).ToList(), "Player");
        CollectionAssert.Contains(stopped.Select(go => go.name).ToList(), "Watchers");
    }
}
