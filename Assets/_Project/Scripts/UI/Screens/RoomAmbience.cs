using UnityEngine;
using Zmywak.Core.Audio;
using Zmywak.Core.Data;
using Zmywak.Core.Services;
using Zmywak.Meta.Progress;

namespace Zmywak.UI.Screens
{
    /// <summary>
    /// What Kuba's room sounds like. Behind the menu: a quiet piano piece and soft rain on the window. In the evening
    /// between shifts: no music, only the room - the rain the window shows (harder in a storm, with thunder now and then,
    /// nothing on a clear or snowy night), the city's traffic far off, the room's own hum - and the building around it: a
    /// creak, a door somewhere, the neighbour upstairs walking about; more doors and feet in a hostel dorm. Never loud:
    /// it sits under everything else.
    /// </summary>
    public class RoomAmbience : MonoBehaviour
    {
        public const string MusicChannel = "music";
        public const string RainChannel = "rain";
        public const string CityChannel = "city";
        public const string RoomChannel = "room";

        [SerializeField] private bool isMenu;

        private WindowWeather _window;
        private MetaProgressController _progress;
        private float _nextEvent;
        private float _nextThunder;
        private WeatherKind _shownWeather = (WeatherKind)(-1);

        public bool IsMenu
        {
            get => isMenu;
            set => isMenu = value;
        }

        /// <summary>The room playing now: a scene's ambience only stops the sound if no newer one has taken it over (the next
        /// scene's may wake before this one is gone).</summary>
        private static RoomAmbience _active;

        private void OnEnable()
        {
            _active = this;
            _window = FindAnyObjectByType<WindowWeather>();
            ServiceLocator.TryGet(out _progress);
            _shownWeather = (WeatherKind)(-1);
            _nextEvent = Time.unscaledTime + Random.Range(6f, 14f);
            _nextThunder = Time.unscaledTime + Random.Range(8f, 20f);
            Apply();
        }

        private void OnDisable()
        {
            if (_active != this)
            {
                return;
            }

            _active = null;
            GameAudio.StopLoop(MusicChannel, 1.5f);
            GameAudio.StopLoop(RainChannel, 1f);
            GameAudio.StopLoop(CityChannel, 1f);
            GameAudio.StopLoop(RoomChannel, 1f);
        }

        private WeatherKind CurrentWeather => _window != null ? _window.Current : WeatherKind.Rain;

        private Place CurrentPlace => _progress != null ? _progress.Place : Place.Dublin;

        private bool InHostel => _progress != null && _progress.Ledger.InHostel;

        private void Apply()
        {
            WeatherKind weather = CurrentWeather;
            _shownWeather = weather;
            if (isMenu)
            {
                GameAudio.SetLoop(MusicChannel, GameSound.MenuMusic, 0.7f, 2.5f);
                GameAudio.SetLoop(RainChannel, GameSound.AmbRain, 0.22f, 2f);
                GameAudio.SetLoop(RoomChannel, GameSound.AmbRoom, 0.18f, 2f);
                GameAudio.StopLoop(CityChannel);
                return;
            }

            GameAudio.StopLoop(MusicChannel, 2f);
            float rain = RainLevel(weather);
            if (rain > 0f)
            {
                GameAudio.SetLoop(RainChannel, GameSound.AmbRain, rain, 2f);
            }
            else
            {
                GameAudio.StopLoop(RainChannel, 2f);
            }

            // The traffic: a city's, fainter in Kraków's side street, almost none at a mining camp.
            Place place = CurrentPlace;
            float city = place == Place.Pilbara ? 0.06f : place == Place.Krakow ? 0.16f : 0.22f;
            if (weather == WeatherKind.Snow)
            {
                city *= 0.5f; // snow muffles the street
            }

            GameAudio.SetLoop(CityChannel, GameSound.AmbCity, city, 2f);
            GameAudio.SetLoop(RoomChannel, GameSound.AmbRoom, place == Place.Pilbara ? 0.4f : 0.28f, 2f); // the camp's air-con hums louder
        }

        /// <summary>How loud the rain on the window is, by the weather it shows (0: none).</summary>
        public static float RainLevel(WeatherKind weather)
        {
            switch (weather)
            {
                case WeatherKind.Storm:
                    return 0.6f;
                case WeatherKind.Rain:
                    return 0.42f;
                case WeatherKind.Drizzle:
                    return 0.22f;
                default:
                    return 0f;
            }
        }

        private void Update()
        {
            if (!isMenu && CurrentWeather != _shownWeather)
            {
                Apply(); // the day moved on under the hub (a time skip, a move)
            }

            if (isMenu)
            {
                return;
            }

            float now = Time.unscaledTime;
            if (now >= _nextEvent)
            {
                // The building: a hostel is busier and closer.
                bool hostel = InHostel;
                _nextEvent = now + (hostel ? Random.Range(7f, 16f) : Random.Range(13f, 30f));
                float roll = Random.value;
                float near = hostel ? 1.5f : 1f;
                float pan = Random.Range(-0.6f, 0.6f);
                if (roll < 0.35f)
                {
                    GameAudio.Play(GameSound.Creak, 0.18f * near, Random.Range(0.85f, 1.1f), pan);
                }
                else if (roll < 0.65f)
                {
                    GameAudio.Play(GameSound.Door, 0.16f * near, Random.Range(0.9f, 1.05f), pan);
                }
                else
                {
                    GameAudio.Play(GameSound.Footsteps, 0.18f * near, Random.Range(0.9f, 1.05f), pan);
                }
            }

            if (CurrentWeather == WeatherKind.Storm && now >= _nextThunder)
            {
                _nextThunder = now + Random.Range(18f, 40f);
                GameAudio.Play(GameSound.Thunder, Random.Range(0.25f, 0.45f), Random.Range(0.85f, 1.05f), Random.Range(-0.4f, 0.4f));
            }
        }
    }
}
