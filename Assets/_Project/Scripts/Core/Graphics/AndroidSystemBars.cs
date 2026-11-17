using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
using UnityEngine.SceneManagement;
#endif

namespace Zmywak.Core.Graphics
{
    /// <summary>
    /// Keeps Android's status and navigation bars hidden while the game runs, as the player settings ask: full screen,
    /// the bars brought back for a moment by a swipe from the edge. Seen on Android 11 (emulator, 2026-10-07): the window
    /// lost that behavior, and the system fell back to "show the bars on any touch" - the touch that showed them never
    /// reached the game (the first tap on the menu did nothing), and the three-button bar then stayed over the bottom of
    /// the screen, across the kitchen's tools. Asking once at start was not enough: loading the next scene lost it again.
    /// So the window is asked after every scene load and every return to the front, a few times a second apart, and a
    /// bar still showing later is hidden again.
    /// </summary>
    public sealed class AndroidSystemBars : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var go = new GameObject(nameof(AndroidSystemBars));
            DontDestroyOnLoad(go);
            go.AddComponent<AndroidSystemBars>();
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private const float CheckEverySeconds = 1f;
        private const int SettleAsks = 5;

        private static int _sdkInt;

        private float _nextCheck;
        private int _asksLeft;
        private bool _failed;

        private void Start()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            Settle();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Settle();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                Settle();
            }
        }

        /// <summary>Asks now, then a few more times a second apart (counted in asks: a scene load's long frames would eat a time window).</summary>
        private void Settle()
        {
            Apply();
            _asksLeft = SettleAsks;
            _nextCheck = Time.unscaledTime + CheckEverySeconds;
        }

        private void Update()
        {
            if (_failed || Time.unscaledTime < _nextCheck)
            {
                return;
            }

            _nextCheck = Time.unscaledTime + CheckEverySeconds;
            if (_asksLeft > 0)
            {
                _asksLeft--;
                Apply();
                return;
            }

            // A bar swiped in on purpose is shown "transiently" and does not count as visible here; one that is visible
            // lost the behavior again.
            bool showing;
            try
            {
                AndroidWindowInsets insets = AndroidApplication.currentWindowInsets;
                showing = insets.IsVisible(AndroidWindowInsets.Type.NavigationBars) ||
                          insets.IsVisible(AndroidWindowInsets.Type.StatusBars);
            }
            catch (System.Exception e)
            {
                Fail(e);
                return;
            }

            if (showing)
            {
                Apply();
            }
        }

        private void Apply()
        {
            if (_failed || !Screen.fullScreen)
            {
                return;
            }

            try
            {
                if (SdkInt() >= 30)
                {
                    AskTheWindow();
                    return;
                }

                // Android 8-10: Unity's own path (system UI flags), which it also restores when the game regains focus.
                AndroidWindowInsets insets = AndroidApplication.currentWindowInsets;
                insets.Hide(AndroidWindowInsets.Type.NavigationBars);
                insets.Hide(AndroidWindowInsets.Type.StatusBars);
            }
            catch (System.Exception e)
            {
                Fail(e);
            }
        }

        private static int SdkInt()
        {
            if (_sdkInt == 0)
            {
                using var version = new AndroidJavaClass("android.os.Build$VERSION");
                _sdkInt = version.GetStatic<int>("SDK_INT");
            }

            return _sdkInt;
        }

        /// <summary>Android 11 and later: straight to the window's insets controller, on the UI thread.</summary>
        private static void AskTheWindow()
        {
            using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity");
            if (activity == null)
            {
                return;
            }

            activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                try
                {
                    using AndroidJavaObject window = activity.Call<AndroidJavaObject>("getWindow");
                    using AndroidJavaObject controller = window?.Call<AndroidJavaObject>("getInsetsController");
                    if (controller == null)
                    {
                        return;
                    }

                    using var types = new AndroidJavaClass("android.view.WindowInsets$Type");
                    // Through another value first, so the ask is always a change for the window to pass on, whatever
                    // it held before.
                    const int showBarsBySwipe = 1; // BEHAVIOR_SHOW_BARS_BY_SWIPE (BEHAVIOR_DEFAULT from Android 12)
                    const int showTransientBarsBySwipe = 2; // BEHAVIOR_SHOW_TRANSIENT_BARS_BY_SWIPE
                    controller.Call("setSystemBarsBehavior", showBarsBySwipe);
                    controller.Call("setSystemBarsBehavior", showTransientBarsBySwipe);
                    controller.Call("hide", types.CallStatic<int>("navigationBars") | types.CallStatic<int>("statusBars"));
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Zmywak: could not hide the system bars ({e.Message}).");
                }
                finally
                {
                    activity.Dispose();
                }
            }));
        }

        private void Fail(System.Exception e)
        {
            _failed = true; // the bars stay as Unity left them; no point retrying every second
            Debug.LogWarning($"Zmywak: could not hide the system bars ({e.Message}).");
        }
#endif
    }
}
