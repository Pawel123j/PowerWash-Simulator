using UnityEngine;
using Zmywak.Core.Settings;

namespace Zmywak.Core.Haptics
{
    public enum HapticPulse
    {
        Light,
        Medium,
        Heavy,
        /// <summary>The faintest tick: felt under the finger while scrubbing gets somewhere, never more than a few a second.</summary>
        Tick
    }

    /// <summary>
    /// Thin wrapper over platform vibration. Android gets short pulses of different lengths through the
    /// system Vibrator (Handheld.Vibrate is a single long buzz - far too heavy for something that happens
    /// every few seconds, like racking a dish). iOS gets the Taptic Engine's light, medium and heavy impacts through a
    /// small native plugin (Assets/Plugins/iOS/ZmywakHaptics.mm, UIKit's UIImpactFeedbackGenerator). Both behind the
    /// same accessibility setting; the editor feels nothing.
    /// </summary>
    public static class HapticsService
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject _vibrator;
        private static bool _vibratorLookedUp;
        private static bool _vibratorFailed;
#endif

#if UNITY_IOS && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void ZmywakHapticPulse(int strength); // 0 light, 1 medium, 2 heavy
#endif

        public static void PlayFeedback()
        {
            Pulse(HapticPulse.Medium);
        }

        // Never a buzz: each strength waits this long after the last pulse of its kind (and a tick after any pulse at all).
        private static readonly float[] MinGap = { 0.08f, 0.15f, 0.2f, 0.28f };
        private static readonly float[] LastAt = { -10f, -10f, -10f, -10f };
        private static float _lastAny = -10f;

        /// <summary>Pulses actually sent (after the setting and the spacing) - for tests.</summary>
        public static int PulsesSent { get; private set; }

        public static void Pulse(HapticPulse strength)
        {
            if (!AccessibilitySettings.HapticsEnabled)
            {
                return;
            }

            float now = Time.realtimeSinceStartup;
            int kind = (int)strength;
            if (now - LastAt[kind] < MinGap[kind] || (strength == HapticPulse.Tick && now - _lastAny < MinGap[kind]))
            {
                return;
            }

            LastAt[kind] = now;
            _lastAny = now;
            PulsesSent++;

#if UNITY_ANDROID && !UNITY_EDITOR
            if (_vibratorFailed)
            {
                return;
            }

            long milliseconds = strength == HapticPulse.Tick ? 7 : strength == HapticPulse.Light ? 12 : strength == HapticPulse.Medium ? 28 : 70;
            AndroidJavaObject vibrator = GetVibrator();
            if (vibrator != null)
            {
                try
                {
                    vibrator.Call("vibrate", milliseconds);
                }
                catch (AndroidJavaException e)
                {
                    // A refused vibration (no permission, no motor) must not break the touch that asked for it, nor
                    // be tried and logged again on every dish: give up for this run.
                    Debug.LogWarning($"HapticsService: the vibrator refused ({e.Message}); haptics are off for this run.");
                    _vibratorFailed = true;
                }
            }
            else
            {
                Handheld.Vibrate(); // also what makes Unity add the VIBRATE permission to the manifest
            }
#elif UNITY_IOS && !UNITY_EDITOR
            ZmywakHapticPulse(strength == HapticPulse.Tick ? 0 : (int)strength);
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject GetVibrator()
        {
            if (_vibratorLookedUp)
            {
                return _vibrator;
            }

            _vibratorLookedUp = true;
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }
            }
            catch (AndroidJavaException e)
            {
                Debug.LogWarning($"HapticsService: no vibrator service ({e.Message}); falling back to Handheld.Vibrate.");
                _vibrator = null;
            }

            return _vibrator;
        }
#endif
    }
}
