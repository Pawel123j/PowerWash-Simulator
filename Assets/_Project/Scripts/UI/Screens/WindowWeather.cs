using System;
using UnityEngine;
using Zmywak.Core.Data;
using Zmywak.Core.Graphics;
using Zmywak.Core.Services;
using Zmywak.Meta.Progress;

namespace Zmywak.UI.Screens
{
    /// <summary>
    /// Kuba's window tonight (Core.Data.Weather, by the date and the place): rain streaming down the glass, drizzle,
    /// a storm, snow drifting past, fog swallowing the city, or a clear night - and, after the move, Kraków's or the
    /// Pilbara's night instead of Dublin's behind the glass. Built by RoomArt.
    /// </summary>
    public class WindowWeather : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int EmissionMapId = Shader.PropertyToID("_EmissionMap");

        [SerializeField] private Renderer rain;
        [SerializeField] private Renderer snow;
        [SerializeField] private Renderer fog;
        [Tooltip("The city behind the glass (an emissive picture).")]
        [SerializeField] private Renderer city;
        [SerializeField] private Texture2D krakowView;
        [SerializeField] private Texture2D pilbaraView;

        private MetaProgressController _progress;
        private MaterialPropertyBlock _cityBlock;
        private Color _rainColour;

        /// <summary>The weather on show.</summary>
        public WeatherKind Current { get; private set; }

        private void Start()
        {
            _rainColour = rain != null ? rain.sharedMaterial.GetColor(BaseColorId) : Color.white;
            if (ServiceLocator.TryGet(out _progress))
            {
                _progress.OnProgressChanged += Refresh;
            }

            Refresh();
        }

        private void OnDestroy()
        {
            if (_progress != null)
            {
                _progress.OnProgressChanged -= Refresh;
            }
        }

        public void Refresh()
        {
            DateTime date = _progress != null ? _progress.CurrentDate : new DateTime(2004, 5, 1);
            Place place = _progress != null ? _progress.Place : Place.Dublin;
            Show(Weather.On(date, place), place);
        }

        /// <summary>Puts a weather and a place's view in the window (Refresh does it from the game's day).</summary>
        public void Show(WeatherKind weather, Place place)
        {
            Current = weather;
            bool wet = weather == WeatherKind.Rain || weather == WeatherKind.Drizzle || weather == WeatherKind.Storm;
            if (rain != null)
            {
                rain.gameObject.SetActive(wet);
                if (wet)
                {
                    // Drizzle is a few slow streaks, a storm a sheet of water.
                    float strength = weather == WeatherKind.Drizzle ? 0.45f : weather == WeatherKind.Storm ? 1.4f : 1f;
                    rain.material.SetColor(BaseColorId, new Color(_rainColour.r, _rainColour.g, _rainColour.b, Mathf.Clamp01(_rainColour.a * strength)));
                    if (rain.TryGetComponent(out TextureScroller drops))
                    {
                        drops.Speed = weather == WeatherKind.Storm ? new Vector2(0.04f, 0.2f) : new Vector2(0f, 0.09f * strength);
                    }
                }
            }

            if (snow != null)
            {
                snow.gameObject.SetActive(weather == WeatherKind.Snow);
            }

            if (fog != null)
            {
                fog.gameObject.SetActive(weather == WeatherKind.Fog);
            }

            if (city != null)
            {
                _cityBlock ??= new MaterialPropertyBlock();
                city.GetPropertyBlock(_cityBlock);
                Texture2D view = place == Place.Krakow ? krakowView : place == Place.Pilbara ? pilbaraView : null;
                if (view != null)
                {
                    _cityBlock.SetTexture(EmissionMapId, view);
                }

                // Fog and storms swallow the lights; snow throws them back; a clear night is a touch brighter.
                float glow = weather == WeatherKind.Fog ? 0.45f : weather == WeatherKind.Storm ? 0.6f
                    : weather == WeatherKind.Snow ? 1.25f : weather == WeatherKind.Clear || weather == WeatherKind.Heat ? 1.25f : 1.15f;
                _cityBlock.SetColor(EmissionColorId, new Color(glow, glow, glow * 1.04f));
                city.SetPropertyBlock(_cityBlock);
            }
        }
    }
}
