using UnityEditor;
using UnityEditor.Scripting;
using UnityEditor.Scripting.Compilers;

namespace CustomUnityCompiler
{
    class CustomCSharpLanguage: CSharpLanguage
    {
        public override ScriptCompilerBase CreateCompiler(MonoIsland island, bool buildingForEditor, BuildTarget targetPlatform, bool runUpdater)
        {
            var compiler = base.CreateCompiler(island, buildingForEditor, targetPlatform, runUpdater);
            if (compiler.GetType() == typeof(MonoCSharpCompiler))
            {
                compiler = new CustomCSharpCompiler(island, runUpdater);
            }

            return compiler;
        }
    }
}
