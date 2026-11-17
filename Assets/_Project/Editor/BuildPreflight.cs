using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Zmywak.Core.GameState;
using Zmywak.Core.GameState.States;

namespace Zmywak.EditorTools
{
    /// <summary>Checks shared by the headless player build entry points.</summary>
    internal static class BuildPreflight
    {
        /// <summary>Enabled Build Settings scenes, or null (with the reason logged) if the project can't be
        /// built into something playable yet.</summary>
        internal static string[] GetScenesOrLogError()
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                Debug.LogError("Zmywak: no scenes registered in Build Settings. Run 'Zmywak/Setup/Run All (1-8)' first.");
                return null;
            }

            string sceneProblem = SceneProblem(scenes);
            if (sceneProblem != null)
            {
                Debug.LogError($"Zmywak: {sceneProblem} Run 'Zmywak/Setup/Run All (1-8)' to register them again.");
                return null;
            }

            SerializedProperty inputHandler = VerticalSliceSetup.FindActiveInputHandlerProperty();
            if (inputHandler == null || inputHandler.intValue == 0)
            {
                // Would build fine and then ignore every touch on the device.
                Debug.LogError("Zmywak: Active Input Handling is 'Input Manager (Old)'. Run 'Zmywak/Setup/0d. Enable Input System Backends' first.");
                return null;
            }

            return scenes;
        }

        /// <summary>
        /// What is wrong with the enabled scenes, or null. The game loads its scenes by name (the state machine's
        /// <c>SceneManager.LoadScene</c>), and a player throws for one that is not in the build - on the phone, the
        /// first time the player taps "go to work". Boot comes first because it creates everything that lives between
        /// scenes (the saves, the progress, the state machine); the other scenes redirect through it when started alone.
        /// </summary>
        internal static string SceneProblem(IReadOnlyList<string> enabledScenes)
        {
            string[] required =
            {
                StartFromAnyScene.BootSceneName, MainMenuState.MainMenuSceneName,
                MetaHubState.MetaHubSceneName, KitchenShiftState.KitchenShiftSceneName
            };

            if (enabledScenes.Count == 0 || Path.GetFileNameWithoutExtension(enabledScenes[0]) != required[0])
            {
                return $"the first scene in Build Settings must be {required[0]}.";
            }

            foreach (string name in required)
            {
                if (!enabledScenes.Any(path => Path.GetFileNameWithoutExtension(path) == name))
                {
                    return $"the scene {name} is not enabled in Build Settings.";
                }
            }

            return null;
        }

        /// <summary>Only quits when running headless (-batchmode) - never the user's interactive Editor.</summary>
        internal static void ExitIfHeadless(int code)
        {
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(code);
            }
        }
    }
}
