using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build.Player;

namespace Onion.SceneManagement.Tests {
    // The Editor compiles with UNITY_EDITOR, so editor-only code that leaks into the Runtime
    // assembly only breaks player builds. This compiles the player scripts without building.
    public class PlayerCompilationTests {
        private string _outputPath;

        [SetUp]
        public void SetUp() {
            _outputPath = FileUtil.GetUniqueTempPathInProject();
        }

        [TearDown]
        public void TearDown() {
            if (Directory.Exists(_outputPath)) {
                Directory.Delete(_outputPath, recursive: true);
            }
        }

        [Test]
        public void RuntimeAssembly_CompilesForThePlayer() {
            var target = EditorUserBuildSettings.activeBuildTarget;
            var settings = new ScriptCompilationSettings {
                target = target,
                group = BuildPipeline.GetBuildTargetGroup(target),
                options = ScriptCompilationOptions.None,
            };

            var result = PlayerBuildInterface.CompilePlayerScripts(settings, _outputPath);

            // A failed compilation returns no assemblies; the compile errors are in the Editor log.
            Assert.That(result.assemblies, Is.Not.Empty,
                "The player scripts failed to compile. Look for editor-only code outside #if UNITY_EDITOR in the Editor log.");
            Assert.That(result.assemblies, Does.Contain("Onion.SceneManagement.Runtime.dll"),
                $"Compiled: {string.Join(", ", result.assemblies.OrderBy(name => name))}");
        }
    }
}
