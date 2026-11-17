using System.IO;
using UnityEngine;
using Zmywak.Core.Save;
using Zmywak.Core.Settings;
using Zmywak.Core.Telemetry;

namespace Zmywak.Tests.PlayMode
{
    /// <summary>
    /// Points SaveSystem and the gameplay diary at a throwaway folder, and the settings at test-only keys, for the duration of a test, so scenes that load or autosave
    /// progress (Boot's persistent controllers, a finished shift raising ShiftCompleted) neither depend on
    /// nor overwrite the developer's real save in persistentDataPath.
    /// </summary>
    public static class TestSaveSandbox
    {
        private static string _dir;

        public static void Enter()
        {
            _dir = Path.Combine(Path.GetTempPath(), "ZmywakPlayModeTests_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            SaveSystem.DirectoryOverride = _dir;
            SaveSystem.ResetSlot(); // the first slot, in memory only
            GameplayLog.DirectoryOverride = _dir;
            // Settings too: tests flip relaxed mode, sound and tilt; under their own keys, starting from defaults.
            AccessibilitySettings.KeyPrefix = TestSettingsPrefix;
            AccessibilitySettings.DeleteAll();
        }

        public const string TestSettingsPrefix = "test_";

        public static void Exit()
        {
            SaveSystem.DirectoryOverride = null;
            SaveSystem.ResetSlot(); // back to the developer's remembered slot
            GameplayLog.DirectoryOverride = null;
            AccessibilitySettings.DeleteAll();
            AccessibilitySettings.KeyPrefix = string.Empty;
            if (_dir != null && Directory.Exists(_dir))
            {
                Directory.Delete(_dir, true);
            }

            _dir = null;
        }

        /// <summary>Destroys the game's own objects parked in DontDestroyOnLoad (Boot's persistent
        /// controllers) so nothing from one test keeps listening/saving during the next one. Only roots
        /// carrying a Zmywak component - the test runner keeps its own controller in DontDestroyOnLoad too.</summary>
        public static void DestroyPersistentObjects()
        {
            var probe = new GameObject("DontDestroyOnLoadProbe");
            Object.DontDestroyOnLoad(probe);
            foreach (GameObject root in probe.scene.GetRootGameObjects())
            {
                if (root == probe || HasGameComponent(root))
                {
                    Object.Destroy(root);
                }
            }
        }

        private static bool HasGameComponent(GameObject root)
        {
            foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                string ns = behaviour != null ? behaviour.GetType().Namespace : null;
                if (ns != null && ns.StartsWith("Zmywak"))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
