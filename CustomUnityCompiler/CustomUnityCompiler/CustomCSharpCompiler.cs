using System.IO;
using System.Linq;
using UnityEditor.Scripting;
using UnityEditor.Scripting.Compilers;
using UnityEngine;

namespace CustomUnityCompiler
{
    class CustomCSharpCompiler: MonoCSharpCompiler
    {
        public CustomCSharpCompiler(MonoIsland island, bool runUpdater) : base(island, runUpdater)
        {
            Debug.Log("Compile output: " + island._output);

            //            HackIslandByAddDefine(ref _island);
            HackIslandByRemoveFile(ref _island);
        }

        private static void HackIslandByAddDefine(ref MonoIsland island)
        {
            if (Path.GetFileName(island._output) == "Assembly-CSharp-firstpass.dll")
            {
                var defineEditor = island._defines.Contains("UNITY_EDITOR");
                if (!defineEditor)
                {
                    Debug.Log("Hack To Add define");

                    var list = island._defines.ToList();
                    list.Add("UNITY_EDITOR");
                    island = new MonoIsland(island._target, island._classlib_profile, island._files, island._references, list.ToArray(), island._output);
                }
            }
        }

        private static void HackIslandByRemoveFile(ref MonoIsland island)
        {
            if (Path.GetFileName(island._output) == "Assembly-CSharp.dll")
            {
                var defineEditor = island._defines.Contains("UNITY_EDITOR");
                if (!defineEditor)
                {
                    Debug.Log("Hack To Remove File");

                    var list = island._files.ToList();
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        if (Path.GetFileName(list[i]) == "Script.cs")
                        {
                            list.RemoveAt(i);
                        }
                    }
                    island = new MonoIsland(island._target, island._classlib_profile, list.ToArray(), island._references, island._defines, island._output);
                }
            }

        }
    }
}
