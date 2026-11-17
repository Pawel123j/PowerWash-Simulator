using System.Collections.Generic;
using UnityEngine;

namespace Zmywak.Core.Settings
{
    /// <summary>
    /// Device/user preferences (not game progress, so PlayerPrefs rather than the JSON save file). Read through a small
    /// cache: the sound levels are asked for every frame, and on Android every PlayerPrefs read is a call into Java.
    /// </summary>
    public static class AccessibilitySettings
    {
        private const string TextScaleKey = "zmywak_text_scale";
        private const string ColorBlindModeKey = "zmywak_colorblind_mode";
        private const string HapticsEnabledKey = "zmywak_haptics_enabled";
        private const string RelaxedModeKey = "zmywak_relaxed_mode";
        private const string SoundEnabledKey = "zmywak_sound_enabled";
        private const string TiltEnabledKey = "zmywak_tilt_enabled";
        private const string RadioStationKey = "zmywak_radio_station";
        private const string MasterVolumeKey = "zmywak_volume_master";
        private const string MusicVolumeKey = "zmywak_volume_music";
        private const string SfxVolumeKey = "zmywak_volume_sfx";
        private const string ScreenEffectsKey = "zmywak_screen_effects";
        private const string ToolAssistKey = "zmywak_tool_assist";
        private const string AutoDishwasherKey = "zmywak_auto_dishwasher";
        private const string QualityChoiceKey = "zmywak_quality_choice";
        private const string LastQualityTierKey = "zmywak_quality_last";

        private static readonly string[] AllKeys =
        {
            TextScaleKey, ColorBlindModeKey, HapticsEnabledKey, RelaxedModeKey, SoundEnabledKey, TiltEnabledKey, RadioStationKey,
            MasterVolumeKey, MusicVolumeKey, SfxVolumeKey, ScreenEffectsKey, ToolAssistKey, AutoDishwasherKey, QualityChoiceKey, LastQualityTierKey
        };

        private static readonly Dictionary<string, float> Cache = new Dictionary<string, float>();
        private static string _keyPrefix = string.Empty;

        /// <summary>
        /// Tests keep their settings under their own keys (TestSaveSandbox sets this), so a test run killed
        /// halfway - one was, mid-way through a relaxed-mode test - cannot leave the player's settings changed.
        /// </summary>
        public static string KeyPrefix
        {
            get => _keyPrefix;
            set
            {
                _keyPrefix = value ?? string.Empty;
                Cache.Clear();
                Changed?.Invoke();
            }
        }

        /// <summary>Raised when any setting changes (sound levels, effects...), for whoever mirrors one.</summary>
        public static event System.Action Changed;

        private static string Key(string name) => KeyPrefix + name;

        /// <summary>Forgets every setting under the current <see cref="KeyPrefix"/> (tests clean up with it).</summary>
        public static void DeleteAll()
        {
            foreach (string key in AllKeys)
            {
                PlayerPrefs.DeleteKey(Key(key));
            }

            PlayerPrefs.Save();
            Cache.Clear();
            Changed?.Invoke();
        }

        private static float GetFloat(string name, float fallback)
        {
            string key = Key(name);
            if (!Cache.TryGetValue(key, out float value))
            {
                value = PlayerPrefs.GetFloat(key, fallback);
                Cache[key] = value;
            }

            return value;
        }

        private static void SetFloat(string name, float value)
        {
            string key = Key(name);
            if (Cache.TryGetValue(key, out float current) && current == value)
            {
                return;
            }

            PlayerPrefs.SetFloat(key, value);
            Cache[key] = value;
            // A slider being dragged changes its value every frame: written to the disk at most once a second, and
            // whatever is left when the card closes or the app pauses (Flush) - not sixty file writes a second.
            if (Time.realtimeSinceStartup - _lastFloatSave >= 1f)
            {
                _lastFloatSave = Time.realtimeSinceStartup;
                PlayerPrefs.Save();
                _floatSavePending = false;
            }
            else
            {
                _floatSavePending = true;
            }

            Changed?.Invoke();
        }

        private static float _lastFloatSave = -10f;
        private static bool _floatSavePending;

        /// <summary>Writes a slider's last value to the disk if it is still waiting (the settings card closing, the app pausing).</summary>
        public static void Flush()
        {
            if (_floatSavePending)
            {
                _floatSavePending = false;
                _lastFloatSave = Time.realtimeSinceStartup;
                PlayerPrefs.Save();
            }
        }

        private static bool GetBool(string name, bool fallback)
        {
            string key = Key(name);
            if (!Cache.TryGetValue(key, out float value))
            {
                value = PlayerPrefs.GetInt(key, fallback ? 1 : 0);
                Cache[key] = value;
            }

            return value > 0.5f;
        }

        private static void SetBool(string name, bool value)
        {
            string key = Key(name);
            PlayerPrefs.SetInt(key, value ? 1 : 0);
            PlayerPrefs.Save();
            Cache[key] = value ? 1f : 0f;
            Changed?.Invoke();
        }

        public static float TextScale
        {
            get => GetFloat(TextScaleKey, 1f);
            set => SetFloat(TextScaleKey, Mathf.Clamp(value, 0.8f, 1.5f));
        }

        public static bool ColorBlindMode
        {
            get => GetBool(ColorBlindModeKey, false);
            set => SetBool(ColorBlindModeKey, value);
        }

        public static bool HapticsEnabled
        {
            get => GetBool(HapticsEnabledKey, true);
            set => SetBool(HapticsEnabledKey, value);
        }

        /// <summary>All sound on or off: off silences everything, whatever the levels below say.</summary>
        public static bool SoundEnabled
        {
            get => GetBool(SoundEnabledKey, true);
            set => SetBool(SoundEnabledKey, value);
        }

        /// <summary>The overall level, 0-1.</summary>
        public static float MasterVolume
        {
            get => GetFloat(MasterVolumeKey, 1f);
            set => SetFloat(MasterVolumeKey, Mathf.Clamp01(value));
        }

        /// <summary>Music and the radio, 0-1.</summary>
        public static float MusicVolume
        {
            get => GetFloat(MusicVolumeKey, 0.8f);
            set => SetFloat(MusicVolumeKey, Mathf.Clamp01(value));
        }

        /// <summary>Everything else - the kitchen, the room, the interface, 0-1.</summary>
        public static float SfxVolume
        {
            get => GetFloat(SfxVolumeKey, 1f);
            set => SetFloat(SfxVolumeKey, Mathf.Clamp01(value));
        }

        /// <summary>What music and the radio play at: the master level times theirs, and nothing with the sound off.</summary>
        public static float MusicGain => SoundEnabled ? MasterVolume * MusicVolume : 0f;

        /// <summary>What sound effects and ambience play at.</summary>
        public static float SfxGain => SoundEnabled ? MasterVolume * SfxVolume : 0f;

        /// <summary>
        /// Checking glasses by tilting the phone. Off (or on a device without an accelerometer), the reflection
        /// moves over the glass by itself - for playing lying down, or hands that cannot tilt comfortably.
        /// </summary>
        public static bool TiltEnabled
        {
            get => GetBool(TiltEnabledKey, true);
            set => SetBool(TiltEnabledKey, value);
        }

        private static int GetInt(string name, int fallback)
        {
            string key = Key(name);
            if (!Cache.TryGetValue(key, out float value))
            {
                value = PlayerPrefs.GetInt(key, fallback);
                Cache[key] = value;
            }

            return Mathf.RoundToInt(value);
        }

        private static void SetInt(string name, int value)
        {
            string key = Key(name);
            PlayerPrefs.SetInt(key, value);
            PlayerPrefs.Save();
            Cache[key] = value;
            Changed?.Invoke();
        }

        /// <summary>The graphics the player chose: -1 automatic (the device and the frame rate decide), 0 Low, 1 Medium, 2 High.</summary>
        public static int QualityChoice
        {
            get => GetInt(QualityChoiceKey, -1);
            set => SetInt(QualityChoiceKey, Mathf.Clamp(value, -1, 2));
        }

        /// <summary>The tier the automatic graphics settled on last (-1 never): the next start begins there.</summary>
        public static int LastQualityTier
        {
            get => GetInt(LastQualityTierKey, -1);
            set => SetInt(LastQualityTierKey, Mathf.Clamp(value, -1, 2));
        }

        /// <summary>Where the kitchen radio's dial was left (KitchenRadio's station, as a number; -1 = never touched).</summary>
        public static int RadioStation
        {
            get
            {
                string key = Key(RadioStationKey);
                if (!Cache.TryGetValue(key, out float value))
                {
                    value = PlayerPrefs.GetInt(key, -1);
                    Cache[key] = value;
                }

                return Mathf.RoundToInt(value);
            }
            set
            {
                string key = Key(RadioStationKey);
                PlayerPrefs.SetInt(key, value);
                PlayerPrefs.Save();
                Cache[key] = value;
                Changed?.Invoke();
            }
        }

        /// <summary>Longer shift timers, no pile collapse - for players who want the washing without the pressure.</summary>
        public static bool RelaxedMode
        {
            get => GetBool(RelaxedModeKey, false);
            set => SetBool(RelaxedModeKey, value);
        }

        /// <summary>The screen shaking when something smashes, red flashes and big popups. Off: the same news, quietly.</summary>
        public static bool ScreenEffects
        {
            get => GetBool(ScreenEffectsKey, true);
            set => SetBool(ScreenEffectsKey, value);
        }

        /// <summary>
        /// An easier sink: the tool for the dirt in front of Kuba is picked for him as the dish comes up to scrub. Off by
        /// default - choosing the tool is part of the game.
        /// </summary>
        public static bool ToolAssist
        {
            get => GetBool(ToolAssistKey, false);
            set => SetBool(ToolAssistKey, value);
        }

        /// <summary>A full rack starts the dishwasher by itself after a short countdown (a tap cancels it).</summary>
        public static bool AutoDishwasher
        {
            get => GetBool(AutoDishwasherKey, true);
            set => SetBool(AutoDishwasherKey, value);
        }
    }
}
