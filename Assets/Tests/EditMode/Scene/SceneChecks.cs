using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Checks of a scene that the scene tests and the editor tool tests share: missing scripts, references
/// that nobody filled in, and how many objects of each kind there are.
/// </summary>
public static class SceneChecks
{
    /// <summary>
    /// Fields that may stay empty on purpose. "Type.field". Each one is optional in the code
    /// (the script checks for null and does something sensible).
    /// </summary>
    public static readonly HashSet<string> OptionalReferences = new HashSet<string>
    {
    };

    public static IEnumerable<GameObject> AllObjects(Scene scene)
    {
        return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject);
    }

    public static IEnumerable<MonoBehaviour> GameScripts(Scene scene)
    {
        return AllObjects(scene)
            .SelectMany(go => go.GetComponents<MonoBehaviour>())
            .Where(script => script != null && script.GetType().Assembly == typeof(GameEvent).Assembly);
    }

    public static string PathOf(GameObject go)
    {
        string path = go.name;
        for (Transform parent = go.transform.parent; parent != null; parent = parent.parent)
        {
            path = parent.name + "/" + path;
        }
        return path;
    }

    public static List<string> MissingScripts(Scene scene)
    {
        return AllObjects(scene)
            .Where(go => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go) > 0)
            .Select(PathOf)
            .ToList();
    }

    /// <summary>Every object field of the game's scripts that is empty, or points to something that was deleted.</summary>
    public static List<string> EmptyReferences(Scene scene)
    {
        var problems = new List<string>();
        foreach (MonoBehaviour script in GameScripts(scene))
        {
            var fields = new SerializedObject(script);
            SerializedProperty property = fields.GetIterator();
            while (property.NextVisible(true))
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference || property.objectReferenceValue != null)
                {
                    continue;
                }
                string field = property.propertyPath.Split('.')[0];
                string key = script.GetType().Name + "." + field;
                bool deleted = property.objectReferenceInstanceIDValue != 0;
                if (deleted || !OptionalReferences.Contains(key))
                {
                    problems.Add($"{PathOf(script.gameObject)}: {script.GetType().Name}.{property.propertyPath}{(deleted ? " points to a deleted object" : " is empty")}");
                }
            }
        }
        return problems;
    }

    /// <summary>Every event asset that a script in the scene holds, with how many scripts hold it.</summary>
    public static Dictionary<Object, int> EventUses(Scene scene)
    {
        var uses = new Dictionary<Object, int>();
        foreach (MonoBehaviour script in GameScripts(scene))
        {
            var fields = new SerializedObject(script);
            SerializedProperty property = fields.GetIterator();
            while (property.NextVisible(true))
            {
                Object value = property.propertyType == SerializedPropertyType.ObjectReference ? property.objectReferenceValue : null;
                if (value is GameEvent || value is TextEvent)
                {
                    uses.TryGetValue(value, out int count);
                    uses[value] = count + 1;
                }
            }
        }
        return uses;
    }

    /// <summary>How many objects of each component type the scene has, as sorted "Type: count" lines.</summary>
    public static List<string> ComponentCounts(Scene scene)
    {
        List<GameObject> objects = AllObjects(scene).ToList();
        var lines = objects
            .SelectMany(go => go.GetComponents<Component>())
            .Where(component => component != null)
            .GroupBy(component => component.GetType().Name)
            .Select(group => $"{group.Key}: {group.Count()}")
            .ToList();
        lines.Add($"GameObject: {objects.Count}");
        lines.Sort();
        return lines;
    }
}
