using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Scripting;
using UnityEditor.Scripting.Compilers;
using UnityEngine;

namespace CustomUnityCompiler
{
    [InitializeOnLoad]
    public static class CustomCompilerHacker
    {
        static CustomCompilerHacker()
        {
            Debug.Log("Start Hack Compiler");

            var list = GetSupportedLanguages();
            for (int i = 0; i < list.Count; i++)
            {
                var type = list[i].GetType();
                if (type == typeof(CSharpLanguage))
                {
                    list[i] = new CustomCSharpLanguage();
                }
            }
        }

        private static List<SupportedLanguage> GetSupportedLanguages()
        {
            var fieldInfo = typeof(ScriptCompilers).GetField("_supportedLanguages", BindingFlags.NonPublic | BindingFlags.Static);
            var languages = (List<SupportedLanguage>)fieldInfo.GetValue(null);
            return languages;
        }
    }
}
