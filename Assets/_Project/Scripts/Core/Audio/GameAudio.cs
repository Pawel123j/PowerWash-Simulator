using System.Collections.Generic;
using UnityEngine;
using Zmywak.Core.GameState;
using Zmywak.Core.Settings;
using Zmywak.Core.Diagnostics;

namespace Zmywak.Core.Audio
{
    /// <summary>
    /// The game's sound outside the sink: one persistent object (made on first use, kept across scenes) playing the
    /// interface's one-shots, the music and the ambience loops - each loop on a named channel ("music", "rain", ...) that
    /// fades in and out, so a scene asks for what it wants to hear and the next scene's loops cross over. Music follows
    /// the music level, everything else the effects level (<see cref="AccessibilitySettings.MusicGain"/>,
    /// <see cref="AccessibilitySettings.SfxGain"/>); the sound switch silences all of it. The interface's sounds play on
    /// through a paused kitchen (they belong to the pause menu); the loops pause with it.
    /// </summary>
    public sealed class GameAudio : MonoBehaviour, IKeepAcrossRestart
    {
        private const int Voices = 8;
        private const float DefaultFade = 1.2f;

        private sealed class Loop
        {
            public AudioSource Source;
            public GameSound Sound;
            public float Target;
            public float Level; // 0-1 of the target, fading
            public float FadeSeconds;
            public bool Stopping;
        }

        private static GameAudio _instance;
        private static GameSoundBank _bank;
        private static bool _bankLooked;
        private static readonly Dictionary<GameSound, AudioClip> Synthesised = new Dictionary<GameSound, AudioClip>();
        private static readonly Dictionary<GameSound, int> LastTake = new Dictionary<GameSound, int>();

        private readonly Dictionary<string, Loop> _loops = new Dictionary<string, Loop>();
        private AudioSource[] _voices;
        private int _nextVoice;

        /// <summary>One-shots started so far (after the sound switch) - lets tests hear the interface react.</summary>
        public static int OneShotsPlayed { get; private set; }

        /// <summary>The last one-shot played (for tests).</summary>
        public static GameSound? LastPlayed { get; private set; }

        /// <summary>The player, made when first needed (never in edit mode).</summary>
        public static GameAudio Instance
        {
            get
            {
                if (_instance == null && Application.isPlaying)
                {
                    var go = new GameObject("GameAudio");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<GameAudio>();
                }

                return _instance;
            }
        }

        /// <summary>The recorded and baked clips (Resources), looked up once; null without one.</summary>
        public static GameSoundBank Bank
        {
            get
            {
                if (!_bankLooked)
                {
                    _bank = Resources.Load<GameSoundBank>(GameSoundBank.ResourceName);
                    _bankLooked = true;
                }

                return _bank;
            }
        }

        private void Awake()
        {
            using var _startup = StartupTimeline.Measure("GameAudio.Awake");
            _voices = new AudioSource[Voices];
            for (int i = 0; i < Voices; i++)
            {
                _voices[i] = gameObject.AddComponent<AudioSource>();
                _voices[i].playOnAwake = false;
                _voices[i].ignoreListenerPause = true;
                _voices[i].spatialBlend = 0f;
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>A clip of the sound: a recorded take (never the same twice running) or the synthesised one.</summary>
        public static AudioClip ClipOf(GameSound sound)
        {
            IReadOnlyList<AudioClip> takes = Bank != null ? Bank.TakesOf(sound) : null;
            if (takes != null && takes.Count > 0)
            {
                int last = LastTake.TryGetValue(sound, out int previous) ? previous : -1;
                int take = takes.Count == 1 ? 0 : (last + 1 + Random.Range(0, takes.Count - 1)) % takes.Count;
                LastTake[sound] = take;
                if (takes[take] != null)
                {
                    return takes[take];
                }
            }

            if (!Synthesised.TryGetValue(sound, out AudioClip clip) || clip == null)
            {
                clip = GameSynth.Clip(sound);
                Synthesised[sound] = clip;
            }

            return clip;
        }

        /// <summary>A one-shot at the effects level (no-op outside play mode or with the sound off).</summary>
        public static void Play(GameSound sound, float volume = 1f, float pitch = 1f, float pan = 0f)
        {
            if (!Application.isPlaying || volume <= 0f)
            {
                return;
            }

            float gain = GameSounds.IsMusic(sound) ? AccessibilitySettings.MusicGain : AccessibilitySettings.SfxGain;
            if (gain <= 0f)
            {
                return;
            }

            Instance.PlayOneShot(sound, volume * gain, pitch, pan);
        }

        private void PlayOneShot(GameSound sound, float volume, float pitch, float pan)
        {
            AudioSource voice = _voices[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _voices.Length;
            voice.pitch = pitch;
            voice.panStereo = Mathf.Clamp(pan, -1f, 1f);
            voice.PlayOneShot(ClipOf(sound), volume);
            OneShotsPlayed++;
            LastPlayed = sound;
        }

        /// <summary>
        /// Plays a loop on a channel at a volume (0-1 of its level), fading in - or, if the channel already plays this
        /// sound, just moving to the new volume. Another sound on the channel crossfades through silence.
        /// </summary>
        public static void SetLoop(string channel, GameSound sound, float volume, float fadeSeconds = DefaultFade)
        {
            if (!Application.isPlaying)
            {
                return;
            }

            Instance.StartLoop(channel, sound, volume, fadeSeconds);
        }

        /// <summary>Fades a channel's loop out (and stops it once silent).</summary>
        public static void StopLoop(string channel, float fadeSeconds = DefaultFade)
        {
            if (_instance == null || !_instance._loops.TryGetValue(channel, out Loop loop))
            {
                return;
            }

            loop.Stopping = true;
            loop.FadeSeconds = Mathf.Max(0.01f, fadeSeconds);
        }

        /// <summary>Whether a channel is playing (or fading in) - for tests.</summary>
        public static bool IsLooping(string channel) =>
            _instance != null && _instance._loops.TryGetValue(channel, out Loop loop) && !loop.Stopping;

        /// <summary>The sound a channel plays, if any.</summary>
        public static GameSound? LoopOn(string channel) =>
            _instance != null && _instance._loops.TryGetValue(channel, out Loop loop) && !loop.Stopping ? loop.Sound : (GameSound?)null;

        private void StartLoop(string channel, GameSound sound, float volume, float fadeSeconds)
        {
            if (_loops.TryGetValue(channel, out Loop loop) && loop.Sound == sound)
            {
                loop.Target = Mathf.Clamp01(volume);
                loop.Stopping = false;
                loop.FadeSeconds = Mathf.Max(0.01f, fadeSeconds);
                return;
            }

            if (loop != null)
            {
                // A different sound on the channel: the old one fades away on a channel of its own.
                _loops.Remove(channel);
                string leaving = channel + "~" + Time.frameCount + "~" + Random.Range(0, 1 << 20);
                loop.Stopping = true;
                _loops[leaving] = loop;
            }

            var source = gameObject.AddComponent<AudioSource>();
            source.clip = ClipOf(sound);
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0f;
            source.time = source.clip != null && source.clip.length > 1f ? Random.Range(0f, source.clip.length * 0.9f) : 0f;
            source.Play();
            _loops[channel] = new Loop { Source = source, Sound = sound, Target = Mathf.Clamp01(volume), FadeSeconds = Mathf.Max(0.01f, fadeSeconds) };
        }

        private readonly List<string> _finished = new List<string>();

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float music = AccessibilitySettings.MusicGain;
            float sfx = AccessibilitySettings.SfxGain;
            _finished.Clear();
            foreach (KeyValuePair<string, Loop> pair in _loops)
            {
                Loop loop = pair.Value;
                float step = dt / loop.FadeSeconds;
                loop.Level = Mathf.MoveTowards(loop.Level, loop.Stopping ? 0f : 1f, step);
                float gain = GameSounds.IsMusic(loop.Sound) ? music : sfx;
                if (loop.Source != null)
                {
                    loop.Source.volume = loop.Target * loop.Level * gain;
                }

                if (loop.Stopping && loop.Level <= 0f)
                {
                    _finished.Add(pair.Key);
                }
            }

            foreach (string key in _finished)
            {
                if (_loops[key].Source != null)
                {
                    Destroy(_loops[key].Source);
                }

                _loops.Remove(key);
            }
        }
    }
}
