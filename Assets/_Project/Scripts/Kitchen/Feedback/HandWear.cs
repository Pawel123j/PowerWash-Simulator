using System;
using UnityEngine;
using Zmywak.Core.Data;
using Zmywak.Core.Services;
using Zmywak.Kitchen.Shifts;
using Zmywak.Core.Diagnostics;

namespace Zmywak.Kitchen.Feedback
{
    /// <summary>
    /// What the shift does to Kuba's arm, worked out each frame (<see cref="HandWearModel"/>) from the energy he
    /// started with, how far into the shift he is, how hard he has been scrubbing lately and the date, and handed to
    /// the Zmywak/Hand shader as globals; <see cref="GlovedHand"/> reads the tremor. Feedback only: nothing in the game
    /// reads it back. Sits on the hand's root (KitchenArt.BuildHand).
    /// </summary>
    public class HandWear : MonoBehaviour
    {
        private static readonly int TirednessId = Shader.PropertyToID("_HandTiredness");
        private static readonly int HeatId = Shader.PropertyToID("_HandHeat");
        private static readonly int SoakId = Shader.PropertyToID("_HandSoak");
        private static readonly int WeatheringId = Shader.PropertyToID("_HandWeathering");

        // Scrubbing warms the skin up over a few seconds; it takes longer to cool.
        private const float WarmUpSeconds = 6f;
        private const float CoolDownSeconds = 20f;

        private ShiftController _shift;
        private GlovedHand _hand;
        private DateTime _today = HandWearModel.FirstShift;
        private float _exertion;
        private HandLook? _shown;

        /// <summary>The look the shader has this frame.</summary>
        public HandLook Current { get; private set; }

        private void Awake()
        {
            using var _startup = StartupTimeline.Measure("HandWear.Awake");
            _hand = GetComponent<GlovedHand>();
        }

        private void Start()
        {
            using var _startup = StartupTimeline.Measure("HandWear.Start");
            _shift = FindAnyObjectByType<ShiftController>();
            if (ServiceLocator.TryGet(out IPlayerCondition condition))
            {
                _today = condition.Today;
            }
        }

        /// <summary>Holds the arm at a given look (previews, tests) until <see cref="Release"/>.</summary>
        public void Show(HandLook look)
        {
            _shown = look;
            Apply(look);
        }

        public void Release()
        {
            _shown = null;
        }

        private void Update()
        {
            if (_shown.HasValue)
            {
                Apply(_shown.Value);
                return;
            }

            float scrubbing = _hand != null ? _hand.ScrubEnergy : 0f;
            _exertion = Mathf.MoveTowards(_exertion, scrubbing, Time.deltaTime / (scrubbing > _exertion ? WarmUpSeconds : CoolDownSeconds));
            float energy = _shift != null ? _shift.EnergyAtStart : 100f;
            float progress = _shift != null ? _shift.Progress01 : 0f;
            Apply(HandWearModel.Evaluate(energy, progress, _exertion, _today));
        }

        private void OnDisable()
        {
            Apply(default); // the plain materials outside the kitchen and in the editor
        }

        private void Apply(HandLook look)
        {
            Current = look;
            Shader.SetGlobalFloat(TirednessId, look.Tiredness);
            Shader.SetGlobalFloat(HeatId, look.Heat);
            Shader.SetGlobalFloat(SoakId, look.Soak);
            Shader.SetGlobalFloat(WeatheringId, look.Weathering);
        }
    }
}
