using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Runs the editor tools that build the scene (Tools > Rebuild Scene) twice, and checks what they made.
/// The tools save the scene and some assets, so every file they touch is put back as it was after the tests.
/// </summary>
public class SceneBuildTests
{
    // Folders the tools may write to. Scripts and Tests are not touched by the tools.
    private static readonly string[] WatchedFolders = { "Assets/Scenes", "Assets/Prefabs", "Assets/Data", "Assets/Materials", "Assets/Settings", "Assets/Sprites", "Assets/Tiles" };
    private static readonly string[] WatchedFiles = { "ProjectSettings/EditorBuildSettings.asset" };

    private readonly Dictionary<string, byte[]> savedFiles = new Dictionary<string, byte[]>();

    private List<string> committedCounts;
    private List<string> firstBuildCounts;
    private List<string> secondBuildCounts;
    private List<string> missingScripts;
    private List<string> emptyReferences;

    private static IEnumerable<string> WatchedPaths()
    {
        return WatchedFolders.Where(Directory.Exists)
            .SelectMany(folder => Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
            .Concat(WatchedFiles.Where(File.Exists))
            .Select(path => path.Replace('\\', '/'));
    }

    [OneTimeSetUp]
    public void BuildTwice()
    {
        foreach (string path in WatchedPaths())
        {
            savedFiles[path] = File.ReadAllBytes(path);
        }

        committedCounts = SceneChecks.ComponentCounts(EditorSceneManager.OpenScene(TunnelAssets.ScenePath));

        ProjectBuilder.BuildScene();
        Scene built = SceneManager.GetActiveScene();
        firstBuildCounts = SceneChecks.ComponentCounts(built);
        missingScripts = SceneChecks.MissingScripts(built);
        emptyReferences = SceneChecks.EmptyReferences(built);

        ProjectBuilder.BuildScene();
        secondBuildCounts = SceneChecks.ComponentCounts(SceneManager.GetActiveScene());
    }

    [OneTimeTearDown]
    public void PutTheFilesBack()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        foreach (string path in WatchedPaths().ToList())
        {
            if (!savedFiles.ContainsKey(path))
            {
                File.Delete(path); // Made by the tools.
            }
        }
        foreach (KeyValuePair<string, byte[]> saved in savedFiles)
        {
            if (!File.Exists(saved.Key) || !File.ReadAllBytes(saved.Key).SequenceEqual(saved.Value))
            {
                File.WriteAllBytes(saved.Key, saved.Value);
            }
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
    }

    [Test]
    public void BuiltScene_HasNoMissingScripts()
    {
        Assert.IsEmpty(missingScripts, string.Join("\n", missingScripts));
    }

    [Test]
    public void BuiltScene_HasNoEmptyReferences()
    {
        Assert.IsEmpty(emptyReferences, string.Join("\n", emptyReferences));
    }

    // Regression (cleanup): every rebuild added one more copy of the dust.
    [Test]
    public void BuildingTwice_GivesTheSameObjects()
    {
        CollectionAssert.AreEqual(firstBuildCounts, secondBuildCounts, Difference(firstBuildCounts, secondBuildCounts));
    }

    [Test]
    public void BuildingTwice_DoesNotAddMoreDust()
    {
        string dust = firstBuildCounts.Single(line => line.StartsWith("DustMotes:"));
        CollectionAssert.Contains(secondBuildCounts, dust);
        CollectionAssert.Contains(committedCounts, dust);
    }

    // The committed scene is what the tools make, so a hand edit to the scene would be lost on the next rebuild.
    [Test]
    public void TheCommittedScene_IsWhatTheToolsBuild()
    {
        CollectionAssert.AreEqual(committedCounts, firstBuildCounts, Difference(committedCounts, firstBuildCounts));
    }

    [Test]
    public void Build_PutsTheSceneFirstInTheBuildSettings()
    {
        Assert.AreEqual(TunnelAssets.ScenePath, EditorBuildSettings.scenes[0].path);
    }

    private static string Difference(List<string> before, List<string> after)
    {
        return "Only before: " + string.Join(", ", before.Except(after)) + "\nOnly after: " + string.Join(", ", after.Except(before));
    }
}
