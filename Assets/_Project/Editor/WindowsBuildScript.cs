using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Zmywak.EditorTools
{
    /// <summary>Playable Windows x64 build, also available through -executeMethod.</summary>
    public static class WindowsBuildScript
    {
        [MenuItem("Zmywak/Build/Build Windows (EXE)")]
        public static void BuildWindows()
        {
            const BuildTarget target = BuildTarget.StandaloneWindows64;
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, target))
            {
                Debug.LogError("Zmywak: Windows Build Support is not installed for this Unity editor.");
                BuildPreflight.ExitIfHeadless(1);
                return;
            }

            string[] scenes = BuildPreflight.GetScenesOrLogError();
            if (scenes == null)
            {
                BuildPreflight.ExitIfHeadless(1);
                return;
            }

            string exePath = Path.GetFullPath("Builds/Windows/Zmywak.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(exePath));
            ScriptingImplementation previousBackend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone);
            FullScreenMode previousMode = PlayerSettings.fullScreenMode;
            bool previousNative = PlayerSettings.defaultIsNativeResolution;
            bool previousResizable = PlayerSettings.resizableWindow;
            int previousWidth = PlayerSettings.defaultScreenWidth;
            int previousHeight = PlayerSettings.defaultScreenHeight;
            int exitCode = 1;

            try
            {
                // Mono needs no external C++ toolchain. The game's UI uses a portrait canvas.
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
                PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
                PlayerSettings.defaultIsNativeResolution = false;
                PlayerSettings.resizableWindow = true;
                PlayerSettings.defaultScreenWidth = 450;
                PlayerSettings.defaultScreenHeight = 800;

                BuildSummary summary = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = exePath,
                    target = target,
                    options = BuildOptions.None
                }).summary;

                if (summary.result == BuildResult.Succeeded && File.Exists(exePath))
                {
                    Debug.Log($"Zmywak: Windows build succeeded - {exePath} ({summary.totalSize / 1024 / 1024} MB, {summary.totalTime}).");
                    exitCode = 0;
                }
                else
                {
                    Debug.LogError($"Zmywak: Windows build failed - {summary.result}, errors={summary.totalErrors}.");
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, previousBackend);
                PlayerSettings.fullScreenMode = previousMode;
                PlayerSettings.defaultIsNativeResolution = previousNative;
                PlayerSettings.resizableWindow = previousResizable;
                PlayerSettings.defaultScreenWidth = previousWidth;
                PlayerSettings.defaultScreenHeight = previousHeight;
                AssetDatabase.SaveAssets();
            }

            BuildPreflight.ExitIfHeadless(exitCode);
        }
    }
}
