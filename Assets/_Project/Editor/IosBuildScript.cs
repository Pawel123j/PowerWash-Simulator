using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Zmywak.EditorTools
{
    /// <summary>
    /// Generates the Xcode project only - compiling, signing and uploading an iOS app needs Xcode, which
    /// runs on macOS alone. Run headlessly with -buildTarget iOS
    /// -executeMethod Zmywak.EditorTools.IosBuildScript.BuildXcodeProject, then open
    /// Builds/iOS/Xcode/Unity-iPhone.xcodeproj on a Mac.
    /// </summary>
    public static class IosBuildScript
    {
        private const string OutputDir = "Builds/iOS/Xcode";

        [MenuItem("Zmywak/Build/Generate iOS Xcode Project (Development)")]
        public static void BuildXcodeProject()
        {
            string[] scenes = BuildPreflight.GetScenesOrLogError();
            if (scenes == null)
            {
                BuildPreflight.ExitIfHeadless(1);
                return;
            }

            Directory.CreateDirectory(OutputDir);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputDir,
                target = BuildTarget.iOS,
                options = BuildOptions.Development
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"Zmywak: Xcode project generated in {OutputDir} ({summary.totalTime}). Build it on a Mac.");
                BuildPreflight.ExitIfHeadless(0);
            }
            else
            {
                Debug.LogError($"Zmywak: Xcode project generation failed - result={summary.result}, errors={summary.totalErrors}, warnings={summary.totalWarnings}.");
                BuildPreflight.ExitIfHeadless(1);
            }
        }
    }
}
