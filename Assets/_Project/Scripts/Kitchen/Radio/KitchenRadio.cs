using System;
using System.Collections.Generic;
using UnityEngine;
using Zmywak.Core.Data;
using Zmywak.Core.Services;
using Zmywak.Core.Settings;
using Zmywak.Core.Diagnostics;

namespace Zmywak.Kitchen.Radio
{
    /// <summary>
    /// The radio on the kitchen wall: plays the station on its dial (<see cref="RadioMusic"/>, a loop each, quietly
    /// under the kitchen) and keeps a caption for the dial window - the programme on a music station, the headlines of
    /// the day (<see cref="RadioNews"/>, by the date in the game) on the news. A turn of the dial is remembered for the
    /// next shift. The kitchen's sound setting mutes it; pausing the game pauses it with everything else.
    /// </summary>
    public class KitchenRadio : MonoBehaviour
    {
        private const float Volume = 0.3f;
        private const float HeadlineSeconds = 7f;

        private static readonly Dictionary<RadioStation, AudioClip> Clips = new Dictionary<RadioStation, AudioClip>();

        private AudioSource _source;
        private IReadOnlyList<string> _headlines;
        private int _headline;
        private float _headlineTimer;

        public RadioStation Station { get; private set; } = RadioStation.Off;

        /// <summary>Where the radio is: which stations and which news are on the air.</summary>
        public Place Place { get; private set; } = Place.Dublin;

        /// <summary>What the dial window shows under the station.</summary>
        public string Caption { get; private set; } = string.Empty;

        /// <summary>The station or the caption changed.</summary>
        public event Action Changed;

        /// <summary>Playing a station (asked to, whatever the audio device makes of it).</summary>
        public bool IsOn { get; private set; }

        private void Awake()
        {
            using var _startup = StartupTimeline.Measure("KitchenRadio.Awake");
            _source = gameObject.AddComponent<AudioSource>();
            _source.loop = true;
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            _source.volume = Volume;
        }

        private void Start()
        {
            using var _startup = StartupTimeline.Measure("KitchenRadio.Start");
            bool living = ServiceLocator.TryGet(out IPlayerCondition condition);
            DateTime today = living ? condition.Today : new DateTime(2004, 5, 1);
            Place = living ? condition.Place : Place.Dublin; // Irish, Polish or Australian radio
            _headlines = RadioNews.For(today, Place);
            _headline = Mathf.Abs(today.DayOfYear) % Mathf.Max(1, _headlines.Count); // a different story leads each day
            int saved = AccessibilitySettings.RadioStation;
            Tune(saved < 0 ? RadioStation.Trad : (RadioStation)saved); // the trad station, until the player turns the dial
        }

        /// <summary>A turn of the dial: Trad, Pop, News, off, and round again.</summary>
        public void NextStation()
        {
            Tune(RadioStations.Next(Station));
            AccessibilitySettings.RadioStation = (int)Station;
        }

        private void Tune(RadioStation station)
        {
            if (!Enum.IsDefined(typeof(RadioStation), station))
            {
                station = RadioStation.Trad;
            }

            Station = station;
            _headlineTimer = HeadlineSeconds;
            IsOn = station != RadioStation.Off;
            if (!IsOn)
            {
                _source.Stop();
            }
            else
            {
                _source.clip = ClipFor(station);
                _source.Play();
            }

            RefreshCaption();
        }

        private void Update()
        {
            if (_source == null)
            {
                return;
            }

            _source.volume = Volume * AccessibilitySettings.MusicGain; // the music level; nothing with the sound off
            if (Station != RadioStation.News || _headlines == null || _headlines.Count == 0)
            {
                return;
            }

            _headlineTimer -= Time.unscaledDeltaTime;
            if (_headlineTimer <= 0f)
            {
                _headlineTimer = HeadlineSeconds;
                _headline = (_headline + 1) % _headlines.Count;
                RefreshCaption();
            }
        }

        private void RefreshCaption()
        {
            Caption = Station == RadioStation.News && _headlines != null && _headlines.Count > 0
                ? _headlines[_headline % _headlines.Count]
                : RadioStations.Programme(Station, Place);
            Changed?.Invoke();
        }

        private static AudioClip ClipFor(RadioStation station)
        {
            if (!Clips.TryGetValue(station, out AudioClip clip) || clip == null)
            {
                using var _synth = StartupTimeline.Measure("audio: radio programme synthesised (first use)");
                float[] samples = RadioMusic.Generate(station);
                clip = AudioClip.Create($"Radio{station}", samples.Length, 1, RadioMusic.SampleRate, false);
                clip.SetData(samples, 0);
                Clips[station] = clip;
            }

            return clip;
        }
    }
}
