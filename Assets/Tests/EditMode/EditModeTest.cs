using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// The base of the edit mode tests. In edit mode Unity doesn't call Awake, OnEnable or Start by itself,
/// so the tests call them with TestUtils.Call. Everything made with Make or Track is destroyed after each test.
/// </summary>
public abstract class EditModeTest
{
    private readonly List<Object> created = new List<Object>();

    protected GameObject Make(string name = "Test Object")
    {
        return Track(new GameObject(name));
    }

    protected T Track<T>(T thing) where T : Object
    {
        created.Add(thing);
        return thing;
    }

    [TearDown]
    public void DestroyCreated()
    {
        foreach (Object thing in created)
        {
            if (thing != null)
            {
                Object.DestroyImmediate(thing);
            }
        }
        created.Clear();
    }
}
