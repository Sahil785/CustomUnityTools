using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Xml;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
/// Provides extended debugging utilities for Unity projects.
/// Wraps UnityEngine.Debug with additional features like stack traces,
/// caller info, conditional logging, timestamps, object dumps, and profiling.
/// </summary>
public static class DebugHelper
{
    /// <summary>
    /// Global toggle to enable or disable debug output.
    /// When false, logs are suppressed and replaced with simulated activity.
    /// </summary>
    public static bool PrintDebug = true;

    #region Basic Logs
    /// <summary>
    /// Logs a standard message to the Unity Console.
    /// Suppressed if <see cref="PrintDebug"/> is false.
    /// </summary>
    /// <param name="msg">Message to log.</param>
    /// <param name="go">Optional GameObject context.</param>
    public static void Log(string msg, GameObject go = null)
    {
#if UNITY_SERVER
        Debug.Log(msg, go);
#else
        if (PrintDebug)
        {
            Debug.Log(msg, go);
        }
        else
        {
            ActivitySimulation();
        }
#endif
    }

    /// <summary>
    /// Logs a warning message to the Unity Console.
    /// Suppressed if <see cref="PrintDebug"/> is false.
    /// </summary>
    /// <param name="msg">Warning message to log.</param>
    /// <param name="go">Optional GameObject context.</param>
    public static void LogWarning(string msg, GameObject go = null)
    {
        if (PrintDebug)
        {
            Debug.LogWarning(msg, go);
        }
        else
        {
            ActivitySimulation();
        }
    }

    /// <summary>
    /// Logs an error message to the Unity Console.
    /// Always outputs regardless of <see cref="PrintDebug"/>.
    /// </summary>
    /// <param name="msg">Error message to log.</param>
    /// <param name="go">Optional GameObject context.</param>
    public static void LogError(string msg, GameObject go = null)
    {
        Debug.LogError(msg, go);
    }
    #endregion

    #region Advanced Logs
    /// <summary>
    /// Logs a message along with the current stack trace.
    /// Useful for tracing call origins.
    /// </summary>
    /// <param name="msg">Message to log.</param>
    /// <param name="go">Optional GameObject context.</param>
    public static void LogWithTrace(string msg, GameObject go = null)
    {
        if (!PrintDebug)
        {
            ActivitySimulation();
            return;
        }

        string trace = new StackTrace(1, true).ToString();
        Debug.Log($"{msg}\nStackTrace:\n{trace}", go);
    }

    /// <summary>
    /// Logs a message prefixed with the caller method name and type.
    /// Helps identify which method triggered the log.
    /// </summary>
    /// <param name="msg">Message to log.</param>
    /// <param name="go">Optional GameObject context.</param>
    public static void LogCaller(string msg, GameObject go = null)
    {
        if (!PrintDebug)
        {
            ActivitySimulation();
            return;
        }

        MethodBase caller = new StackFrame(1).GetMethod();
        Debug.Log($"[{caller.DeclaringType.FullName}.{caller.Name}] {msg}", go);
    }

    /// <summary>
    /// Logs a message only if the given condition evaluates to true.
    /// </summary>
    /// <param name="condition">Condition that controls whether the message is logged.</param>
    /// <param name="msg">Message to log if the condition is true.</param>
    /// <param name="go">Optional GameObject context.</param>
    public static void LogIf(bool condition, string msg, GameObject go = null)
    {
        if (PrintDebug && condition)
        {
            Debug.Log(msg, go);
        }
        else if (!PrintDebug)
        {
            ActivitySimulation();
        }
    }

    /// <summary>
    /// Logs a message with a timestamp prefix (HH:mm:ss.fff).
    /// Useful for profiling and chronological tracing.
    /// </summary>
    /// <param name="msg">Message to log.</param>
    /// <param name="go">Optional GameObject context.</param>
    public static void LogTime(string msg, GameObject go = null)
    {
        if (!PrintDebug)
        {
            ActivitySimulation();
            return;
        }

        string time = DateTime.Now.ToString("HH:mm:ss.fff");
        Debug.Log($"[{time}] {msg}", go);
    }

    /// <summary>
    /// Measures and logs the execution time of a provided action.
    /// </summary>
    /// <param name="action">The code block to execute and measure.</param>
    /// <param name="label">Optional label for the log entry.</param>
    public static void LogExecutionTime(Action action, string label = "Execution")
    {
        if (!PrintDebug)
        {
            ActivitySimulation();
            return;
        }

        var sw = new Stopwatch();
        sw.Start();
        action();
        sw.Stop();
        Debug.Log($"{label} took {sw.ElapsedMilliseconds} ms");
    }

    /// <summary>
    /// Logs all public fields and properties of an object.
    /// Provides a quick runtime inspection of object state.
    /// </summary>
    /// <param name="obj">Object to inspect.</param>
    /// <param name="label">Optional label for the dump.</param>
    public static void LogObject(object obj, string label = "Object Dump")
    {
        if (!PrintDebug)
        {
            ActivitySimulation();
            return;
        }

        if (obj == null)
        {
            Debug.Log($"{label}: null");
            return;
        }

        var type = obj.GetType();
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

        string dump = $"{label} ({type.Name}):\n";
        foreach (var p in props)
        {
            try
            { dump += $"{p.Name} = {p.GetValue(obj, null)}\n"; }
            catch { dump += $"{p.Name} = <unavailable>\n"; }
        }
        foreach (var f in fields)
        {
            try
            { dump += $"{f.Name} = {f.GetValue(obj)}\n"; }
            catch { dump += $"{f.Name} = <unavailable>\n"; }
        }

        Debug.Log(dump);
    }

    /// <summary>
    /// Logs details of an exception including type, message, and stack trace.
    /// </summary>
    /// <param name="ex">The exception to log.</param>
    /// <param name="label">Optional label for the log entry.</param>
    public static void LogException(Exception ex, string label = "Exception")
    {
        if (!PrintDebug)
        { ActivitySimulation(); return; }
        Debug.LogError($"{label}: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
    }

    /// <summary>
    /// Logs the components of a Vector3 (x, y, z).
    /// </summary>
    /// <param name="label">Label for the vector.</param>
    /// <param name="v">The Vector3 to log.</param>
    public static void LogVector(string label, Vector3 v)
    {
        if (!PrintDebug)
        { ActivitySimulation(); return; }
        Debug.Log($"{label}: (x={v.x}, y={v.y}, z={v.z})");
    }

    /// <summary>
    /// Logs position, rotation, and scale of a Transform.
    /// </summary>
    /// <param name="t">The Transform to log.</param>
    /// <param name="label">Optional label for the transform.</param>
    public static void LogTransform(Transform t, string label = "Transform")
    {
        if (!PrintDebug)
        { ActivitySimulation(); return; }
        Debug.Log($"{label}: Pos={t.position}, Rot={t.rotation.eulerAngles}, Scale={t.localScale}");
    }

    /// <summary>
    /// Logs the contents of a collection (array, list, etc.).
    /// </summary>
    /// <param name="collection">The collection to enumerate and log.</param>
    /// <param name="label">Optional label for the collection dump.</param>
    public static void LogCollection<T>(IEnumerable<T> collection, string label = "Collection")
    {
        if (!PrintDebug)
        { ActivitySimulation(); return; }
        string dump = $"{label}:\n";
        foreach (var item in collection)
            dump += $"{item}\n";
        Debug.Log(dump);
    }

    /// <summary>
    /// Logs a message with a colored tag prefix for readability.
    /// </summary>
    /// <param name="tag">Tag to prefix the message with.</param>
    /// <param name="msg">Message to log.</param>
    /// <param name="go">Optional GameObject context.</param>
    public static void LogTagged(string tag, string msg, GameObject go = null)
    {
        if (!PrintDebug)
        { ActivitySimulation(); return; }
        Debug.Log($"<color=cyan>[{tag}]</color> {msg}", go);
    }

    /// <summary>
    /// Logs current managed memory usage in kilobytes.
    /// </summary>
    /// <param name="label">Optional label for the memory log.</param>
    public static void LogMemory(string label = "Memory Usage")
    {
        if (!PrintDebug)
        { ActivitySimulation(); return; }
        long mem = GC.GetTotalMemory(false) / 1024;
        Debug.Log($"{label}: {mem} KB");
    }

    /// <summary>
    /// Logs frame timing information including deltaTime and frameCount.
    /// </summary>
    /// <param name="label">Optional label for the frame timing log.</param>
    public static void LogFrameTime(string label = "Frame")
    {
        if (!PrintDebug)
        { ActivitySimulation(); return; }
        Debug.Log($"{label}: deltaTime={Time.deltaTime:F4}, frameCount={Time.frameCount}");
    }

    /// <summary>
    /// Logs a raw JSON string, optionally pretty-printed.
    /// </summary>
    /// <param name="json">The JSON string to log.</param>
    /// <param name="pretty">If true, formats the JSON for readability.</param>
    public static void LogJson(string json, bool pretty = true)
    {
        if (!PrintDebug)
        { ActivitySimulation(); return; }

        try
        {
            if (pretty)
            {
                var parsed = JsonConvert.DeserializeObject(json);
                string formatted = JsonConvert.SerializeObject(parsed, Newtonsoft.Json.Formatting.Indented);
                Debug.Log(formatted);
            }
            else
            {
                Debug.Log(json);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Invalid JSON: {ex.Message}\n{json}");
        }
    }

    /// <summary>
    /// Logs a JObject with optional formatting.
    /// </summary>
    /// <param name="jObj">The JObject to log.</param>
    /// <param name="label">Optional label for the log entry.</param>
    /// <param name="pretty">If true, formats the JSON for readability.</param>
    public static void LogJObject(JObject jObj, string label = "JObject", bool pretty = true)
    {
        if (!PrintDebug)
        { ActivitySimulation(); return; }

        if (jObj == null)
        {
            Debug.Log($"{label}: null");
            return;
        }

        string output = pretty
            ? jObj.ToString(Newtonsoft.Json.Formatting.Indented)
            : jObj.ToString(Newtonsoft.Json.Formatting.None);

        Debug.Log($"{label}:\n{output}");
    }
    #endregion

    #region Utility
    /// <summary>
    /// Simulates activity when debug output is disabled.
    /// Performs meaningless operations to mimic workload.
    /// </summary>
    private static void ActivitySimulation()
    {
        for (int i = 0; i < 1000; i++)
        {
            _ = Mathf.Sqrt(i);
        }
    }
    #endregion
}