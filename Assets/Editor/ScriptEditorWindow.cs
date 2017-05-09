using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEditor;
using UnityEditor.Scripting;
using UnityEditor.Scripting.Compilers;
using UnityEditorInternal;

public class ScriptEditorWindow : EditorWindow
{
    private static string _workingPath;
    private static string WorkingPath
    {
        get
        {
            if (string.IsNullOrEmpty(_workingPath))
            {
                _workingPath = Application.dataPath.Replace("/Assets", "");
            }
            return _workingPath;
        }
    }

    private static string EditorScriptPath
    {
        get
        {
            return WorkingPath + "/EditorScript";
        }
    }

    private static string RuntimeScriptPath
    {
        get
        {
            return WorkingPath + "/RuntimeScript";
        }
    }


    private Vector2 _scrollPos;


    [MenuItem("Custom/Script/Window")]
    private static ScriptEditorWindow GetWindow()
    {
        return GetWindow<ScriptEditorWindow>(typeof(ScriptEditorWindow).Name.Replace("EditorWindow", ""));
    }


    private void OnEnable()
    {
        CreateDirectory(EditorScriptPath);
        CreateDirectory(RuntimeScriptPath);
    }

    private void OnGUI()
    {
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("EditorScriptPath", EditorScriptPath);
            if (GUILayout.Button("打开"))
            {
                EditorUtility.RevealInFinder(EditorScriptPath);
            }
            if (GUILayout.Button("编译"))
            {
                CompileEditorScript();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("RuntimeScriptPath", RuntimeScriptPath);
            if (GUILayout.Button("打开"))
            {
                EditorUtility.RevealInFinder(RuntimeScriptPath);
            }
            if (GUILayout.Button("编译"))
            {
                CompileRuntimeScript();
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private static bool CreateDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            return true;
        }

        return false;
    }

    private static IList GetMonoIslands()
    {
        return (IList)typeof(InternalEditorUtility).GetMethod("GetMonoIslands", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
    }

    private static IList GetMonoIslandsForPlayer()
    {
        return (IList)typeof(InternalEditorUtility).GetMethod("GetMonoIslandsForPlayer", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
    }

    #region 弃用的麻烦方法
    /// <summary>
    /// 这个方法比较准确，但实际上不需要搞这么复杂
    /// </summary>
    /// <param name="island"></param>
    /// <param name="buildingForEditor"></param>
    /// <param name="target"></param>
    /// <param name="runUpdater"></param>
    /// <returns></returns>
    private static string[] Compile(object island, bool buildingForEditor, BuildTarget target, bool runUpdater)
    {
        var assembly = Assembly.GetAssembly(typeof(InternalEditorUtility));
        var compilersType = assembly.GetType("UnityEditor.Scripting.ScriptCompilers");
        var compiler = compilersType.GetMethod("CreateCompilerInstance", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new[] { island, buildingForEditor, target, runUpdater });

        var compilerType = assembly.GetType(compiler.GetType().FullName);
        compilerType.GetMethod("BeginCompiling").Invoke(compiler, null);
        while (!(bool)compilerType.GetMethod("Poll").Invoke(compiler, null))
        {
            Thread.Sleep(50);
        }
        var messageType = assembly.GetType("UnityEditor.Scripting.Compilers.CompilerMessage");
        var messageField = messageType.GetField("message");
        var msgs = (IList)compilerType.GetMethod("GetCompilerMessages").Invoke(compiler, null);
        var array = new string[msgs.Count];
        for (int i = msgs.Count - 1; i >= 0; i--)
        {
            array[i] = messageField.GetValue(msgs[i]).ToString();
        }
        compilerType.GetMethod("Dispose").Invoke(compiler, null);

        return array;
    }

    private static object FilterMonoIsland(object island, string rootFolder = null)
    {
        var assembly = Assembly.GetAssembly(typeof(InternalEditorUtility));
        var islandType = assembly.GetType("UnityEditor.Scripting.MonoIsland");

        var extension = islandType.GetMethod("GetExtensionOfSourceFiles").Invoke(island, null).ToString();
        if (extension != "cs")
        {
            return null;
        }

        if (!string.IsNullOrEmpty(rootFolder))
        {
            var outputFiled = islandType.GetField("_output");
            var output = outputFiled.GetValue(island).ToString();
            outputFiled.SetValue(island, rootFolder + "/" + Path.GetFileName(output));
        }

        return island;
    }
    #endregion


    #region 偷懒方便的编译方法
    private static List<string> _haveCompileAssemblyList = new List<string>();
    private static List<string> _haveCompileNameList = new List<string>();

    private static void BeginCompile()
    {
        _haveCompileAssemblyList.Clear();
        _haveCompileNameList.Clear();
    }

    private static string[] Compile(object island, bool buildingForEditor, string rootFolder)
    {
        var assembly = Assembly.GetAssembly(typeof(InternalEditorUtility));
        var islandType = assembly.GetType("UnityEditor.Scripting.MonoIsland");

        var extension = islandType.GetMethod("GetExtensionOfSourceFiles").Invoke(island, null).ToString();
        if (extension != "cs")
        {
            return new string[0];
        }

        var output = islandType.GetField("_output").GetValue(island).ToString();
        var name = Path.GetFileName(output);
        if (!buildingForEditor && name.Contains("Editor"))
        {
            return new string[0];
        }
        var newOutput = rootFolder + "/" + name;
        _haveCompileNameList.Add(name);
        _haveCompileAssemblyList.Add(newOutput);

        var files = (string[])islandType.GetField("_files").GetValue(island);
        var references = (string[])islandType.GetField("_references").GetValue(island);
        for (int i = 0; i < references.Length; i++)
        {
            var index = _haveCompileNameList.IndexOf(Path.GetFileName(references[i]));
            if (index >= 0)
            {
                references[i] = _haveCompileAssemblyList[index];
            }
        }
        var defines = (string[])islandType.GetField("_defines").GetValue(island);

        var results = EditorUtility.CompileCSharp(files, references, defines, newOutput);
        foreach (var result in results)
        {
            if (result.Contains("error"))
            {
                throw new Exception(string.Join("\n", results));
            }
        }
        return results;
    }
    #endregion

    private static void CompileEditorScript()
    {
        BeginCompile();
        foreach (var island in GetMonoIslands())
        {
            foreach (var str in Compile(island, true, EditorScriptPath))
            {
                Debug.Log(str);
            }
        }
    }

    private static void CompileRuntimeScript()
    {
        BeginCompile();
        foreach (var island in GetMonoIslands())
        {
            foreach (var str in Compile(island, false, RuntimeScriptPath))
            {
                Debug.Log(str);
            }
        }
    }
}
