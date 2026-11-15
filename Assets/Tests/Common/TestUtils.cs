using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Small helpers for the tests: they reach the private [SerializeField] fields and the private Unity
/// messages (Awake, OnEnable, OnTriggerEnter...) the same way the Inspector and the engine do.
/// </summary>
public static class TestUtils
{
    private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags AnyStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    /// <summary>Sets a private field, like dragging a value into the Inspector.</summary>
    public static T Set<T>(T target, string field, object value) where T : class
    {
        FindField(target.GetType(), field, AnyInstance).SetValue(target, value);
        return target;
    }

    public static TValue Get<TValue>(object target, string field)
    {
        return (TValue)FindField(target.GetType(), field, AnyInstance).GetValue(target);
    }

    public static TValue GetStatic<TValue>(Type type, string field)
    {
        return (TValue)FindField(type, field, AnyStatic).GetValue(null);
    }

    public static void SetStatic(Type type, string field, object value)
    {
        FindField(type, field, AnyStatic).SetValue(null, value);
    }

    /// <summary>Calls a private method, for example a Unity message like "Awake" or "OnTriggerEnter".</summary>
    public static object Call(object target, string method, params object[] args)
    {
        MethodInfo info = target.GetType().GetMethod(method, AnyInstance);
        Assert.IsNotNull(info, $"{target.GetType().Name} has no method {method}");
        return info.Invoke(target, args);
    }

    public static object CallStatic(Type type, string method, params object[] args)
    {
        MethodInfo info = type.GetMethod(method, AnyStatic);
        Assert.IsNotNull(info, $"{type.Name} has no static method {method}");
        return info.Invoke(null, args);
    }

    /// <summary>Raises a C# event from outside its class, by calling the delegate behind it.</summary>
    public static void RaiseEvent(object target, string eventName, params object[] args)
    {
        var handler = (Delegate)FindField(target.GetType(), eventName, AnyInstance).GetValue(target);
        handler?.DynamicInvoke(args);
    }

    /// <summary>Waits (in play mode) until the condition is true, and fails the test if it takes too long.</summary>
    public static IEnumerator WaitUntil(Func<bool> condition, float timeoutSeconds, string what)
    {
        float giveUpAt = Time.realtimeSinceStartup + timeoutSeconds;
        while (!condition())
        {
            if (Time.realtimeSinceStartup > giveUpAt)
            {
                Assert.Fail($"Timed out after {timeoutSeconds} s waiting for: {what}");
            }
            yield return null;
        }
    }

    /// <summary>Waits this many seconds of game time (scaled by Time.timeScale).</summary>
    public static IEnumerator WaitGameSeconds(float seconds)
    {
        float until = Time.time + seconds;
        while (Time.time < until)
        {
            yield return null;
        }
    }

    public static IEnumerator WaitFrames(int frames)
    {
        for (int i = 0; i < frames; i++)
        {
            yield return null;
        }
    }

    private static FieldInfo FindField(Type type, string field, BindingFlags flags)
    {
        for (Type current = type; current != null; current = current.BaseType)
        {
            FieldInfo info = current.GetField(field, flags | BindingFlags.DeclaredOnly);
            if (info != null)
            {
                return info;
            }
        }
        Assert.Fail($"{type.Name} has no field {field}");
        return null;
    }
}
