using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Rebuilds the scene of the game from the setup tools, and builds the game for the browser.
/// The methods are public so they can also run from the command line (Unity -batchmode -executeMethod).
/// </summary>
public static class ProjectBuilder
{
    // Outside the project folder (next to it), so the build never gets into git.
    private const string WebGLFolder = "../ex4-webgl-builds/ex4-light-in-the-tunnel";

    [MenuItem("Tools/Rebuild Scene")]
    public static void BuildScene()
    {
        EditorSceneManager.OpenScene(TunnelAssets.ScenePath);
        TutorialMapBuilder.Build();
        TunnelLevelSetup.Setup();
        GameFeelSetup.Setup();
        // The game has one scene, and it is where the game starts.
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(TunnelAssets.ScenePath, true) };
        AssetDatabase.SaveAssets();
    }

    /// <summary>Builds the game for the browser into ../ex4-webgl-builds, ready to zip and upload to itch.io.</summary>
    [MenuItem("Tools/Build WebGL")]
    public static void BuildWebGL()
    {
        // Gzip with the fallback loader works on itch.io, which doesn't send the compression headers.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;

        Directory.CreateDirectory(WebGLFolder);
        var options = new BuildPlayerOptions
        {
            scenes = new[] { TunnelAssets.ScenePath },
            locationPathName = WebGLFolder,
            target = BuildTarget.WebGL,
        };
        Debug.Log("WebGL build: " + BuildPipeline.BuildPlayer(options).summary.result);
    }
}
