using UnityEngine;
using Zmywak.Core.Data;
using Zmywak.Core.Settings;
using Zmywak.Kitchen.Dishes;
using Zmywak.Kitchen.Dishwasher;
using Zmywak.Kitchen.Rack;
using Zmywak.Core.Diagnostics;

namespace Zmywak.Kitchen.Feedback
{
    /// <summary>
    /// The kitchen's sound: loops that follow what is happening (water while rinsing or spraying, sponge, steel
    /// wool or scraper while scrubbing - louder the faster you rub - and the dishwasher's hum while it runs) plus one-shots
    /// for the moments (scrape, squeak of a clean plate, clinks, smashes, the cycle-done ding). Clips come from
    /// <see cref="KitchenSounds"/> - recordings, or synthesised where there is none; nothing here knows about gameplay
    /// beyond the events.
    /// </summary>
    public class KitchenAudio : MonoBehaviour
    {
        private const int OneShotVoices = 6;
        private const float LoopFadeSpeed = 8f;
        private const float RinseHoldSeconds = 0.12f;

        [SerializeField] private DishwasherController dishwasher;
        [Tooltip("Kuba's hand: the spray it holds runs water like the tap.")]
        [SerializeField] private GlovedHand hand;
        [SerializeField, Range(0f, 1f)] private float masterVolume = 0.9f;

        private AudioSource _water;
        private AudioSource _scrubSoft;
        private AudioSource _scrubHarsh;
        private AudioSource _hum;
        private AudioSource[] _voices;
        private int _nextVoice;

        private float _lastRinseTime = float.NegativeInfinity;
        private float _scrubLevel;
        private bool _scrubHarshTool;
        private float _scrubHarshPitch = 1f;
        private DishwasherRepair _repair;

        /// <summary>One-shot sounds started so far (after the Sound setting gate); lets tests see the mix react.</summary>
        public int OneShotsPlayed { get; private set; }

        private void Awake()
        {
            using var _startup = StartupTimeline.Measure("KitchenAudio.Awake");
            _water = CreateLoop("Water", ProceduralSounds.Sound.Water);
            _scrubSoft = CreateLoop("ScrubSoft", ProceduralSounds.Sound.ScrubSoft);
            _scrubHarsh = CreateLoop("ScrubHarsh", ProceduralSounds.Sound.ScrubHarsh);
            _hum = CreateLoop("DishwasherHum", ProceduralSounds.Sound.Hum);

            _voices = new AudioSource[OneShotVoices];
            for (int i = 0; i < OneShotVoices; i++)
            {
                _voices[i] = gameObject.AddComponent<AudioSource>();
                _voices[i].playOnAwake = false;
            }
        }

        private void OnEnable()
        {
            KitchenFeedbackEvents.Claimed += OnClaimed;
            KitchenFeedbackEvents.Scraped += OnScraped;
            KitchenFeedbackEvents.Rinsing += OnRinsing;
            KitchenFeedbackEvents.Scrubbing += OnScrubbing;
            KitchenFeedbackEvents.Cleaned += OnCleaned;
            KitchenFeedbackEvents.HeldToLight += OnHeldToLight;
            KitchenFeedbackEvents.Soaked += OnSoaked;
            KitchenFeedbackEvents.Racked += OnRacked;
            KitchenFeedbackEvents.DropMissed += OnDropMissed;
            KitchenFeedbackEvents.Broken += OnBroken;
            KitchenFeedbackEvents.UrgentOrder += OnUrgentOrder;
            KitchenFeedbackEvents.Impact += OnImpact;
            if (dishwasher != null)
            {
                dishwasher.OnCycleCompleted += OnCycleCompleted;
                dishwasher.OnCycleStarted += OnCycleStarted;
                dishwasher.OnBrokeDown += OnDishwasherBrokeDown;
                dishwasher.OnRepaired += OnDishwasherRepaired;
            }
        }

        private void OnDisable()
        {
            KitchenFeedbackEvents.Claimed -= OnClaimed;
            KitchenFeedbackEvents.Scraped -= OnScraped;
            KitchenFeedbackEvents.Rinsing -= OnRinsing;
            KitchenFeedbackEvents.Scrubbing -= OnScrubbing;
            KitchenFeedbackEvents.Cleaned -= OnCleaned;
            KitchenFeedbackEvents.HeldToLight -= OnHeldToLight;
            KitchenFeedbackEvents.Soaked -= OnSoaked;
            KitchenFeedbackEvents.Racked -= OnRacked;
            KitchenFeedbackEvents.DropMissed -= OnDropMissed;
            KitchenFeedbackEvents.Broken -= OnBroken;
            KitchenFeedbackEvents.UrgentOrder -= OnUrgentOrder;
            KitchenFeedbackEvents.Impact -= OnImpact;
            ListenToRepair(null);
            if (dishwasher != null)
            {
                dishwasher.OnCycleCompleted -= OnCycleCompleted;
                dishwasher.OnCycleStarted -= OnCycleStarted;
                dishwasher.OnBrokeDown -= OnDishwasherBrokeDown;
                dishwasher.OnRepaired -= OnDishwasherRepaired;
            }
        }

        private void Update()
        {
            float master = AccessibilitySettings.SfxGain * masterVolume;
            float t = 1f - Mathf.Exp(-LoopFadeSpeed * Time.unscaledDeltaTime);

            bool spraying = hand != null && hand.SprayNozzle != null;
            bool rinsing = Time.time - _lastRinseTime < RinseHoldSeconds;
            _water.volume = Mathf.Lerp(_water.volume, rinsing || spraying ? 0.6f * master : 0f, t);
            _water.pitch = Mathf.Lerp(_water.pitch, spraying ? 1.35f : 1f, t); // the spray hisses higher than the tap

            // Scrub loudness follows how hard the player is rubbing, then dies away when they stop.
            _scrubLevel = Mathf.MoveTowards(_scrubLevel, 0f, Time.unscaledDeltaTime * 4f);
            float scrub = Mathf.Clamp01(_scrubLevel) * master;
            _scrubSoft.volume = Mathf.Lerp(_scrubSoft.volume, _scrubHarshTool ? 0f : scrub * 0.7f, t);
            _scrubHarsh.volume = Mathf.Lerp(_scrubHarsh.volume, _scrubHarshTool ? scrub * 0.55f : 0f, t);
            _scrubSoft.pitch = 0.9f + _scrubLevel * 0.25f;
            _scrubHarsh.pitch = _scrubHarshPitch * (0.95f + _scrubLevel * 0.3f);

            bool running = dishwasher != null && dishwasher.IsRunning && !dishwasher.IsBroken;
            _hum.volume = Mathf.Lerp(_hum.volume, running ? 0.3f * master : 0f, t * 0.5f);
        }

        private void OnClaimed(DishController dish) => Play(ProceduralSounds.Sound.Pickup, 0.6f, 0.1f);

        private void OnScraped(DishController dish) => Play(ProceduralSounds.Sound.Scrape, 0.9f, 0.12f);

        private void OnRinsing(DishController dish) => _lastRinseTime = Time.time;

        private void OnScrubbing(DishController dish, Vector3 point, ToolDefinition tool, float intensity)
        {
            string id = tool != null ? tool.Id : string.Empty;
            if (id == "shower")
            {
                return; // nothing rubs: the spray's water is all you hear
            }

            _scrubLevel = Mathf.Max(_scrubLevel, 0.35f + intensity * 0.65f);
            // Steel wool rasps; the scraper's blade grinds the same way, lower.
            _scrubHarshTool = id == "steel_wool" || id == "scraper";
            _scrubHarshPitch = id == "scraper" ? 0.62f : 1f;
        }

        private void OnCleaned(DishController dish) => Play(ProceduralSounds.Sound.Squeak, 1f, 0.08f);

        /// <summary>A light glassy ting as the glass goes up to the light.</summary>
        private void OnHeldToLight(DishController dish) => Play(ProceduralSounds.Sound.GlassClink, 0.45f, 0.05f, 1.25f);

        /// <summary>A splash as it goes under, and the water's rush for a moment.</summary>
        private void OnSoaked(DishController dish)
        {
            Play(ProceduralSounds.Sound.Splash, 0.7f, 0.08f);
            _lastRinseTime = Time.time + 0.3f;
        }

        private void OnRacked(DishController dish, Vector3 position) => Play(ProceduralSounds.Sound.Clink, 0.8f, 0.15f);

        private void OnDropMissed(DishController dish) => Play(ProceduralSounds.Sound.Pickup, 0.5f, 0f, 0.7f);

        private void OnBroken(Vector3 position) => Play(ProceduralSounds.Sound.Crash, 1f, 0.1f);

        /// <summary>The pass's bell, quick and high: something is wanted now.</summary>
        private void OnUrgentOrder(DishDefinition dish) => Play(ProceduralSounds.Sound.Ding, 0.85f, 0f, 1.35f);

        private void OnImpact(Vector3 position, float speed, DishMaterial material)
        {
            float level = Mathf.Clamp01(speed / 5f) * 0.6f;
            if (material == DishMaterial.Steel)
            {
                Play(ProceduralSounds.Sound.Clang, level, 0.2f); // steel rings low and long
            }
            else if (material == DishMaterial.Glass)
            {
                Play(ProceduralSounds.Sound.GlassClink, level, 0.25f); // glass rings higher
            }
            else
            {
                Play(ProceduralSounds.Sound.Clink, level, 0.35f, 0.8f);
            }
        }

        private void OnCycleCompleted(System.Collections.Generic.IReadOnlyList<DishDefinition> washed) =>
            Play(ProceduralSounds.Sound.Ding, 0.8f, 0f);

        /// <summary>The load rattling into the machine; a full rack gets a bright little chime on top.</summary>
        private void OnCycleStarted(System.Collections.Generic.IReadOnlyList<DishDefinition> load, float fill)
        {
            Play(ProceduralSounds.Sound.Scrape, 0.7f, 0.05f, 0.6f);
            if (fill >= RackGrid.FullLoadFill - 1e-4f)
            {
                Play(ProceduralSounds.Sound.Ding, 0.55f, 0f, 1.5f);
            }
        }

        private void OnDishwasherBrokeDown(DishwasherFault fault)
        {
            Play(ProceduralSounds.Sound.Fault, 1f, 0f);
            ListenToRepair(dishwasher.ActiveRepair);
        }

        private void OnDishwasherRepaired()
        {
            ListenToRepair(null);
            Play(ProceduralSounds.Sound.PowerUp, 0.9f, 0f);
        }

        private void ListenToRepair(DishwasherRepair repair)
        {
            if (_repair != null)
            {
                _repair.Stepped -= OnRepairStepped;
            }

            _repair = repair;
            if (_repair != null)
            {
                _repair.Stepped += OnRepairStepped;
            }
        }

        /// <summary>Each bit of the fix makes its own noise: a scrap slapping off, the arm ratcheting round, the breaker clicking in.</summary>
        private void OnRepairStepped()
        {
            switch (_repair?.Fault)
            {
                case DishwasherFault.CloggedFilter:
                    Play(ProceduralSounds.Sound.Scrape, 0.8f, 0.15f, 1.2f);
                    break;
                case DishwasherFault.StuckSprayArm:
                    Play(ProceduralSounds.Sound.Clang, 0.5f, 0.1f, 1.3f);
                    break;
                case DishwasherFault.TrippedReset:
                    Play(ProceduralSounds.Sound.Click, 0.9f, 0f);
                    break;
            }
        }

        private void Play(ProceduralSounds.Sound sound, float volume, float pitchJitter, float basePitch = 1f)
        {
            float gain = AccessibilitySettings.SfxGain;
            if (gain <= 0f || volume <= 0f)
            {
                return;
            }

            AudioSource voice = _voices[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _voices.Length;
            voice.pitch = basePitch * (1f + Random.Range(-pitchJitter, pitchJitter));
            voice.PlayOneShot(KitchenSounds.Clip(sound), volume * masterVolume * gain);
            OneShotsPlayed++;
        }

        private AudioSource CreateLoop(string label, ProceduralSounds.Sound sound)
        {
            var go = new GameObject(label);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.clip = KitchenSounds.Clip(sound);
            source.loop = true;
            source.volume = 0f;
            source.playOnAwake = false;
            source.Play(); // runs silently; volume brings it in and out, so loops never restart audibly
            return source;
        }
    }
}
