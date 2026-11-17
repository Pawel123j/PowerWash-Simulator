using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zmywak.Core.Audio;
using Zmywak.Core.Data;
using Zmywak.Core.Events.Channels;
using Zmywak.Core.GameState;
using Zmywak.Core.GameState.States;
using Zmywak.Core.Haptics;
using Zmywak.Core.Services;
using Zmywak.Core.Settings;
using Zmywak.Kitchen.Dishes;
using Zmywak.Kitchen.Dishwasher;
using Zmywak.Kitchen.Feedback;
using Zmywak.Kitchen.Flow;
using Zmywak.Kitchen.Input;
using Zmywak.Kitchen.Orders;
using Zmywak.Kitchen.Pile;
using Zmywak.Kitchen.Rack;
using Zmywak.Kitchen.Shifts;
using Zmywak.Kitchen.Tools;
using Zmywak.UI.Common;
using Zmywak.Core.Diagnostics;

namespace Zmywak.UI.HUD
{
    /// <summary>
    /// The kitchen's read-outs, each shown when it matters. Always: the clock, the quota (with its bar) and the chef's call
    /// when there is one - the critical ones. Then, when they mean something: the Flow (from the first streak), the
    /// breakages (from the first), the pile's gauge (once the pile is growing), the rack's count (once there is a load), the
    /// dishwasher's button with what it can and cannot do right now (and the full rack's countdown), and one line of hint -
    /// in the first days on every step, later only when Kuba stands still or something needs saying (the wrong tool, a
    /// breakdown). What the first days have not brought in yet is not on screen at all. Popups mark the moments (a Flow
    /// streak, a full rack, a friend helping, the rush), and the shift ends with the bell and its result screen.
    /// </summary>
    public class ShiftHUD : MonoBehaviour
    {
        [SerializeField] private ShiftController shiftController;
        [SerializeField] private ShiftCompletedEventChannel shiftCompletedChannel;
        [SerializeField] private ChefOrderController chefOrderController;
        [SerializeField] private FlowStreakController flowController;
        [SerializeField] private DishwasherController dishwasher;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text dishesText;
        [SerializeField] private Image quotaFill;
        [SerializeField] private TMP_Text brokenText;
        [SerializeField] private GameObject brokenChip;
        [SerializeField] private TMP_Text flowText;
        [SerializeField] private GameObject flowChip;
        [SerializeField] private TMP_Text orderText;
        [SerializeField] private Button startDishwasherButton;
        [SerializeField] private Image dishwasherProgress;
        [SerializeField] private WashStation washStation;
        [SerializeField] private TMP_Text stageHintText;
        [SerializeField] private GameObject hintPill;
        [Tooltip("Shown/hidden with the order (the text's background pill). Falls back to the text itself.")]
        [SerializeField] private GameObject orderBanner;
        [SerializeField] private DishPileController pileController;
        [Tooltip("Pile height against the collapse height (sliced image, sized by FillBar).")]
        [SerializeField] private Image pileMeterFill;
        [SerializeField] private CanvasGroup pileMeter;

        private static readonly Color PileSafe = new Color(0.3f, 0.8f, 0.45f);
        private static readonly Color PileWarning = new Color(1f, 0.75f, 0.2f);
        private static readonly Color PileDanger = new Color(1f, 0.3f, 0.25f);
        private static readonly Color DishwasherBroken = new Color(0.8f, 0.2f, 0.16f);
        private static readonly Color DishwasherReady = new Color(0.16f, 0.6f, 0.38f);

        [Tooltip("\"Flow x2!\" popup, shown briefly when the streak grows.")]
        [SerializeField] private TMP_Text flowPopup;
        [Tooltip("Full-screen red vignette flashed when a dish smashes.")]
        [SerializeField] private Image breakFlash;
        [Tooltip("Repair card opened by the dishwasher button while the machine is broken.")]
        [SerializeField] private DishwasherRepairPanel repairPanel;
        [Tooltip("How full the rack is (\"Kosz: 12/24\"), next to the rack.")]
        [SerializeField] private TMP_Text rackFillText;
        [SerializeField] private GameObject rackPill;

        [Header("The end, the lessons, what comes later")]
        [SerializeField] private ShiftResultScreen resultScreen;
        [SerializeField] private TMP_Text endStamp;
        [SerializeField] private KitchenTutorial tutorial;
        [SerializeField] private GameObject soakPanel;
        [SerializeField] private GameObject bigDishwasherCard;
        [SerializeField] private RectTransform toolbar;

        private const float PunchSeconds = 0.3f;
        private const float PopupSeconds = 1.3f;
        private const float FlashSeconds = 0.5f;
        private const float StampSeconds = 1.5f;
        private const float IdleHintSeconds = 3.5f;
        private const float WrongToolSeconds = 0.6f;

        private static readonly Color QuotaMet = new Color(0.5f, 1f, 0.6f);
        private static readonly Color UrgentColor = new Color(0.85f, 0.25f, 0.12f, 0.95f);
        private static readonly Color HintColor = new Color(0.88f, 0.95f, 1f);
        private static readonly Color WarningColor = new Color(1f, 0.72f, 0.35f);
        private static readonly int[] FlowMilestones = { 3, 5, 8, 12 };

        private TMP_Text _dishwasherLabel;
        private Image _dishwasherImage;
        private Color _dishwasherIdleColor;
        private Image _orderBannerImage;
        private Color _orderBannerColor;
        private Color _flashColor = new Color(1f, 0.15f, 0.1f);
        private int _shownCleaned;
        private int _shownLabelCleaned = -1;
        private int _shownLabelQuota = -1;
        private int _shownStreak;
        private float _punchRemaining;
        private float _popupRemaining;
        private float _flashRemaining;
        private bool _wasFullLoad;
        private CareerRecord _careerAtStart;
        private bool _ending;

        // What the read-outs show now: a string is only built when what it says has changed (no garbage every frame).
        private int _shownSeconds = -1;
        private int _shownBroken = -1;
        private int _shownRackCells = -1;
        private int _shownOrderSeconds = -1;
        private DishDefinition _shownOrder;
        private bool _shownUrgent;
        private int _shownDishwasherKey = int.MinValue;
        private float _nextHint;
        private int _hintKey = int.MinValue;
        private const float HintRefreshSeconds = 0.15f;

        // The hint's idea of progress: the stage of the dish in the sink, and when it last changed.
        private DishController _hintDish;
        private WashStage _hintStage;
        private float _stageSince;
        private float _wrongToolFor;
        private float _lastScrubbing = -10f;
        private float _nextWrongToolSound;
        private float _hintMessageUntil;
        private string _hintMessage;

        private void OnEnable()
        {
            using var _startup = StartupTimeline.Measure("ShiftHUD.OnEnable");
            if (shiftCompletedChannel != null)
            {
                shiftCompletedChannel.OnRaised += ShowResult;
            }

            if (flowController != null)
            {
                flowController.OnStreakChanged += OnFlowStreakChanged;
            }

            KitchenFeedbackEvents.Broken += OnDishBroken;
            KitchenFeedbackEvents.Scrubbing += OnScrubbing;
            DishInputRouter.IsOverControl = IsOverButton;
            _shownStreak = flowController != null ? flowController.CurrentStreak : 0;
            _shownCleaned = shiftController != null ? shiftController.DishesCleanedTotal : 0;
            HideFlourishes();

            if (startDishwasherButton != null && _dishwasherImage == null)
            {
                _dishwasherImage = startDishwasherButton.GetComponent<Image>();
                _dishwasherIdleColor = _dishwasherImage != null ? _dishwasherImage.color : Color.white;
            }

            startDishwasherButton?.onClick.AddListener(OnStartDishwasherClicked);
            if (resultScreen != null)
            {
                resultScreen.HomePressed += OnBackToMenuClicked;
            }

            OnFlowStreakChanged(flowController != null ? flowController.CurrentStreak : 0);
            if (shiftController != null)
            {
                shiftController.OnFullRackLoaded += OnFullRackLoaded;
                shiftController.OnServicePhaseChanged += OnServicePhaseChanged;
                shiftController.OnCrewHelped += OnCrewHelped;
            }

            // Kuba's record as the shift begins: the result screen measures the night against it.
            _careerAtStart = ServiceLocator.TryGet(out IPlayerCondition condition) && condition.WorkRecord != null
                ? condition.WorkRecord.Clone()
                : null;
        }

        private void OnDisable()
        {
            if (shiftCompletedChannel != null)
            {
                shiftCompletedChannel.OnRaised -= ShowResult;
            }

            if (flowController != null)
            {
                flowController.OnStreakChanged -= OnFlowStreakChanged;
            }

            KitchenFeedbackEvents.Broken -= OnDishBroken;
            KitchenFeedbackEvents.Scrubbing -= OnScrubbing;
            if (DishInputRouter.IsOverControl == IsOverButton)
            {
                DishInputRouter.IsOverControl = null;
            }

            if (shiftController != null)
            {
                shiftController.OnFullRackLoaded -= OnFullRackLoaded;
                shiftController.OnServicePhaseChanged -= OnServicePhaseChanged;
                shiftController.OnCrewHelped -= OnCrewHelped;
            }

            if (resultScreen != null)
            {
                resultScreen.HomePressed -= OnBackToMenuClicked;
            }

            startDishwasherButton?.onClick.RemoveListener(OnStartDishwasherClicked);
        }

        private bool Guiding => tutorial != null && tutorial.Guiding;

        private void Update()
        {
            if (shiftController == null)
            {
                return;
            }

            UpdateTimer();
            UpdateQuota();
            UpdateChips();
            UpdateOrderText();
            UpdateStageHint();
            UpdatePileMeter();
            UpdateRackFill();
            UpdateDishwasherButton();
            UpdateUnlocked();
            UpdateFlourishes();
        }

        private void UpdateTimer()
        {
            if (timerText == null)
            {
                return;
            }

            if (shiftController.TutorialHold)
            {
                if (_shownSeconds != -2)
                {
                    _shownSeconds = -2;
                    timerText.text = "Nauka";
                    timerText.color = new Color(0.6f, 0.9f, 1f);
                }

                return;
            }

            int seconds = Mathf.CeilToInt(shiftController.RemainingSeconds);
            if (seconds != _shownSeconds)
            {
                _shownSeconds = seconds;
                timerText.text = $"{seconds / 60:00}:{seconds % 60:00}";
            }

            // The last stretch: the clock warms to red, breathing.
            bool closing = seconds <= 20 && !shiftController.IsShiftOver;
            float pulse = closing ? 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f) : 0f;
            timerText.color = closing ? Color.Lerp(new Color(1f, 0.55f, 0.45f), Color.white, pulse * 0.4f) : Color.white;
        }

        private void UpdateQuota()
        {
            if (dishesText == null)
            {
                return;
            }

            // Washed against the quota - a fixed goal, not a count of what has arrived so far.
            int cleaned = shiftController.DishesCleanedTotal;
            if (cleaned != _shownLabelCleaned || shiftController.Quota != _shownLabelQuota)
            {
                _shownLabelCleaned = cleaned;
                _shownLabelQuota = shiftController.Quota;
                dishesText.text = _shownLabelQuota > 0 ? $"Umyte: {cleaned} / {_shownLabelQuota}" : $"Umyte: {cleaned}";
            }

            if (quotaFill != null && shiftController.Quota > 0)
            {
                float target = Mathf.Clamp01(cleaned / (float)shiftController.Quota);
                float shown = FillBar.Get(quotaFill);
                if (Mathf.Abs(shown - target) > 0.002f)
                {
                    FillBar.Set(quotaFill, Mathf.Lerp(shown, target, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime)));
                }

                quotaFill.color = cleaned >= shiftController.Quota ? QuotaMet : new Color(0.45f, 0.75f, 1f);
            }
        }

        /// <summary>The Flow and the breakages show once there is something to show.</summary>
        private void UpdateChips()
        {
            int broken = shiftController.DishesBrokenTotal;
            if (brokenText != null && broken != _shownBroken)
            {
                _shownBroken = broken;
                brokenText.text = $"Stłuczone: {broken}";
            }

            brokenChip?.SetActive(broken > 0);
            flowChip?.SetActive(_shownStreak > 0 && !Guiding);
        }

        /// <summary>What the first days have not brought in is not on screen: the soak, the big dishwasher.</summary>
        private void UpdateUnlocked()
        {
            bool soak = shiftController.IsUnlocked(Mechanic.Soaking) && !Guiding;
            if (soakPanel != null && soakPanel.activeSelf != soak)
            {
                soakPanel.SetActive(soak);
            }

            bool big = shiftController.IsUnlocked(Mechanic.BigDishwasher) && !Guiding;
            if (bigDishwasherCard != null && bigDishwasherCard.activeSelf != big)
            {
                bigDishwasherCard.SetActive(big);
            }
        }

        /// <summary>
        /// Small flourishes that make progress and trouble readable at a glance while the eyes are on the
        /// sink: the washed counter punches up when it grows, a "Flow ×N!" popup rises, and the screen edges
        /// flash red when something smashes.
        /// </summary>
        private void UpdateFlourishes()
        {
            float dt = Time.unscaledDeltaTime;

            int cleaned = shiftController.DishesCleanedTotal;
            if (cleaned > _shownCleaned)
            {
                _punchRemaining = PunchSeconds;
            }

            _shownCleaned = cleaned;
            if (dishesText != null)
            {
                float punch = _punchRemaining > 0f ? Mathf.Sin((1f - _punchRemaining / PunchSeconds) * Mathf.PI) : 0f;
                dishesText.transform.localScale = Vector3.one * (1f + 0.25f * punch);
                // Green once the quota is met, and it flashes green as it grows before that.
                bool quotaMet = shiftController.Quota > 0 && cleaned >= shiftController.Quota;
                dishesText.color = quotaMet ? QuotaMet : Color.Lerp(Color.white, QuotaMet, punch);
            }

            _punchRemaining = Mathf.Max(0f, _punchRemaining - dt);

            if (flowPopup != null)
            {
                bool visible = _popupRemaining > 0f;
                flowPopup.gameObject.SetActive(visible);
                if (visible)
                {
                    float t = 1f - _popupRemaining / PopupSeconds;
                    flowPopup.transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.15f, Mathf.Min(1f, t * 5f));
                    flowPopup.rectTransform.anchoredPosition = new Vector2(0f, 40f * t);
                    flowPopup.alpha = t < 0.7f ? 1f : 1f - (t - 0.7f) / 0.3f;
                }

                _popupRemaining = Mathf.Max(0f, _popupRemaining - dt);
            }

            if (breakFlash != null)
            {
                float flash = _flashRemaining / FlashSeconds;
                breakFlash.enabled = flash > 0f;
                float strength = AccessibilitySettings.ScreenEffects ? 0.55f : 0.18f; // quieter with the screen's effects off
                breakFlash.color = new Color(_flashColor.r, _flashColor.g, _flashColor.b, strength * flash * flash);
                _flashRemaining = Mathf.Max(0f, _flashRemaining - dt);
            }
        }

        private void HideFlourishes()
        {
            _punchRemaining = _popupRemaining = _flashRemaining = 0f;
            if (flowPopup != null)
            {
                flowPopup.gameObject.SetActive(false);
            }

            if (breakFlash != null)
            {
                breakFlash.enabled = false;
            }

            if (endStamp != null)
            {
                endStamp.gameObject.SetActive(false);
            }
        }

        private void Popup(string text, Color color)
        {
            if (flowPopup == null)
            {
                return;
            }

            flowPopup.text = text;
            flowPopup.color = color;
            _popupRemaining = PopupSeconds;
        }

        private void Flash(Color color, float seconds = FlashSeconds)
        {
            _flashColor = color;
            _flashRemaining = seconds;
        }

        private void UpdateRackFill()
        {
            RackGrid rack = shiftController.Rack;
            if (rackFillText == null || rack == null)
            {
                return;
            }

            bool full = rack.IsFullLoad;
            if (rack.OccupiedCells != _shownRackCells)
            {
                _shownRackCells = rack.OccupiedCells;
                rackFillText.text = full ? $"Pełny kosz: {rack.OccupiedCells}/{rack.CellCount}" : $"Kosz: {rack.OccupiedCells}/{rack.CellCount}";
                rackFillText.color = full ? new Color(0.5f, 1f, 0.6f) : Color.white;
            }

            // On screen once there is a load, or a clean dish on its way to the rack.
            DishController dish = washStation != null ? washStation.Occupant : null;
            bool wanted = rack.OccupiedCells > 0 || (dish != null && dish.CurrentStage == WashStage.ReadyForRack);
            if (rackPill != null && rackPill.activeSelf != wanted)
            {
                rackPill.SetActive(wanted);
            }

            // The moment the rack fills: say so, ring, and the machine's button lights up.
            if (full && !_wasFullLoad && !shiftController.IsShiftOver)
            {
                Popup("Pełny kosz!", new Color(0.55f, 1f, 0.65f));
                GameAudio.Play(GameSound.FullRack, 0.55f);
                HapticsService.Pulse(HapticPulse.Light);
            }

            _wasFullLoad = full;
        }

        private void OnFullRackLoaded()
        {
            // Flow has just been extended (same frame), so say both.
            Popup($"Pełny kosz!\n<size=60%>Flow ×{_shownStreak}</size>", new Color(1f, 0.85f, 0.3f));
        }

        private void OnDishBroken(Vector3 position)
        {
            Flash(new Color(1f, 0.15f, 0.1f));
        }

        private void OnScrubbing(DishController dish, Vector3 point, ToolDefinition tool, float intensity)
        {
            _lastScrubbing = Time.unscaledTime;
        }

        private void OnCrewHelped(string who, int dishes)
        {
            Popup($"{who}: \"Mam te {dishes}!\"\n<size=60%>zdejmuje naczynia z Góry</size>", new Color(0.6f, 0.95f, 1f));
            GameAudio.Play(GameSound.UiConfirm, 0.5f, 0.9f);
            ShowMessage($"{who} z ekipy bierze {PolishPlural.Dishes(dishes)} z Góry na swój zlew. Swoi pomagają swoim.");
        }

        /// <summary>The rhythm of the service, called out over the sink: the room filling up, the rush, a breather.</summary>
        private void OnServicePhaseChanged(ServicePhase phase, string callout)
        {
            string line = phase == ServicePhase.Rising ? "Sala się zapełnia..."
                : phase == ServicePhase.Peak ? (string.IsNullOrEmpty(callout) ? "Szczyt!" : callout)
                : phase == ServicePhase.Easing ? "Trochę oddechu."
                : null;
            if (line == null)
            {
                return;
            }

            Popup(line, phase == ServicePhase.Peak ? new Color(1f, 0.6f, 0.4f) : new Color(1f, 0.85f, 0.3f));
        }

        private void UpdatePileMeter()
        {
            if (pileMeterFill == null || pileController == null || pileController.CollapseHeight <= 0f)
            {
                return;
            }

            float ratio = Mathf.Clamp01(pileController.CurrentHeight / pileController.CollapseHeight);
            float shownRatio = FillBar.Get(pileMeterFill, true);
            if (Mathf.Abs(shownRatio - ratio) > 0.002f)
            {
                FillBar.Set(pileMeterFill, Mathf.Lerp(shownRatio, ratio, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime)), true);
            }
            Color color = ratio < 0.6f
                ? Color.Lerp(PileSafe, PileWarning, ratio / 0.6f)
                : Color.Lerp(PileWarning, PileDanger, (ratio - 0.6f) / 0.4f);
            if (ratio > 0.8f)
            {
                // Close to collapsing: pulse, so it is noticed while busy at the sink.
                color = Color.Lerp(color, Color.white, (Mathf.Sin(Time.unscaledTime * 10f) * 0.5f + 0.5f) * 0.35f);
            }

            pileMeterFill.color = color;
            if (pileMeter != null)
            {
                // Only once the pile is growing (and never over the guided plate).
                float wanted = !Guiding && ratio > 0.2f ? 1f : 0f;
                if (!Mathf.Approximately(pileMeter.alpha, wanted))
                {
                    pileMeter.alpha = Mathf.MoveTowards(pileMeter.alpha, wanted, Time.unscaledDeltaTime * 3f);
                }
            }
        }

        private void UpdateDishwasherButton()
        {
            if (startDishwasherButton == null || shiftController == null)
            {
                return;
            }

            bool shown = tutorial == null || tutorial.ShowsDishwasherButton;
            if (startDishwasherButton.gameObject.activeSelf != shown)
            {
                startDishwasherButton.gameObject.SetActive(shown);
            }

            if (!shown)
            {
                return;
            }

            // Always pressable: when it cannot start, a press says why.
            startDishwasherButton.interactable = true;
            bool broken = shiftController.IsDishwasherBroken;
            bool running = shiftController.IsDishwasherRunning;
            bool can = shiftController.CanStartDishwasher;
            RackGrid rack = shiftController.Rack;
            bool full = can && rack != null && rack.IsFullLoad;
            float countdown = shiftController.AutoStartIn;
            float pulse = Mathf.Sin(Time.unscaledTime * 6f) * 0.5f + 0.5f;

            if (_dishwasherImage != null)
            {
                Color color = broken ? Color.Lerp(DishwasherBroken, Color.white, pulse * 0.25f)
                    : full ? Color.Lerp(DishwasherReady, Color.white, pulse * 0.2f)
                    : can ? _dishwasherIdleColor
                    : new Color(_dishwasherIdleColor.r * 0.6f, _dishwasherIdleColor.g * 0.6f, _dishwasherIdleColor.b * 0.6f, 0.85f);
                _dishwasherImage.color = color;
            }

            if (dishwasherProgress != null)
            {
                if (running && !broken && dishwasher != null)
                {
                    FillBar.Set(dishwasherProgress, dishwasher.Progress01);
                }
                else
                {
                    dishwasherProgress.enabled = false;
                }
            }

            if (_dishwasherLabel == null)
            {
                _dishwasherLabel = startDishwasherButton.GetComponentInChildren<TMP_Text>();
            }

            if (_dishwasherLabel == null)
            {
                return;
            }

            int left = running && dishwasher != null ? Mathf.CeilToInt((1f - dishwasher.Progress01) * dishwasher.CycleSeconds) : 0;
            int state = broken ? 1 : running ? 2 : countdown >= 0f ? 3 : full ? 4 : can ? 5 : 6;
            int number = state == 2 ? left : state == 3 ? Mathf.CeilToInt(countdown) : rack != null ? rack.OccupiedCells : 0;
            int key = state * 1000 + number;
            if (key == _shownDishwasherKey)
            {
                return;
            }

            _shownDishwasherKey = key;
            string label;
            if (broken)
            {
                label = "Napraw zmywarkę!";
            }
            else if (running)
            {
                label = $"Zmywarka myje...\n<size=65%>jeszcze {left} s</size>";
            }
            else if (countdown >= 0f)
            {
                label = $"Start za {Mathf.CeilToInt(countdown)}...\n<size=65%>stuknij, by dołożyć</size>";
            }
            else if (full)
            {
                label = "Pełny kosz - uruchom!";
            }
            else if (can && rack != null)
            {
                label = $"Uruchom zmywarkę\n<size=65%>kosz {rack.OccupiedCells}/{rack.CellCount}</size>";
            }
            else
            {
                label = "Uruchom zmywarkę\n<size=65%>kosz pusty</size>";
            }

            if (_dishwasherLabel.text != label)
            {
                _dishwasherLabel.text = label;
            }
        }

        // ------------------------------------------------------------------ the hint

        /// <summary>The early days explain every step; later the hint keeps quiet unless Kuba stands still or something is wrong.</summary>
        private bool HintEveryStep => shiftController.Day <= 2 || shiftController.Day == Zmywak.Core.Data.MechanicUnlocks.Everything;

        private void ShowMessage(string message, float seconds = 4f)
        {
            _hintMessage = message;
            _hintMessageUntil = Time.unscaledTime + seconds;
        }

        private void SetHint(string text, Color color)
        {
            if (stageHintText == null)
            {
                return;
            }

            bool shown = !string.IsNullOrEmpty(text) && !Guiding;
            if (hintPill != null && hintPill.activeSelf != shown)
            {
                hintPill.SetActive(shown);
            }

            if (shown && stageHintText.text != text)
            {
                stageHintText.text = text;
            }

            stageHintText.color = color;
        }

        /// <summary>One line telling the player what the dish in the sink needs next (and which tool
        /// works on the dirt still on it) - or what is wrong.</summary>
        private void UpdateStageHint()
        {
            if (stageHintText == null)
            {
                return;
            }

            ActiveToolSelector selector = ActiveToolSelector.Instance;
            bool rubbing = Time.unscaledTime - _lastScrubbing < 0.25f;
            _wrongToolFor = selector != null && selector.WrongToolInHand && rubbing ? _wrongToolFor + Time.unscaledDeltaTime : 0f;
            // The hint is words to read: a few updates a second is plenty, and saves the strings - but a new step (another dish,
            // the next stage, the glass to the light, the machine stopping) shows at once.
            DishController dish = washStation != null ? washStation.Occupant : null;
            int key = (dish != null ? dish.GetHashCode() : 0) * 31 + (dish != null ? (int)dish.CurrentStage * 4 + (dish.NeedsInspection ? 2 : 0) : 0) +
                      (shiftController.IsDishwasherBroken ? 1 : 0) + (washStation != null && washStation.Soaking != null ? 7 : 0);
            if (key == _hintKey && Time.unscaledTime < _nextHint)
            {
                return;
            }

            _hintKey = key;
            _nextHint = Time.unscaledTime + HintRefreshSeconds;
            if (dish != _hintDish || (dish != null && dish.CurrentStage != _hintStage))
            {
                _hintDish = dish;
                _hintStage = dish != null ? dish.CurrentStage : WashStage.AwaitingScrape;
                _stageSince = Time.unscaledTime;
            }

            if (Time.unscaledTime < _hintMessageUntil)
            {
                SetHint(_hintMessage, WarningColor);
                return;
            }

            // The wrong tool, rubbing on: say which one works, and point it out on the bar.
            if (_wrongToolFor > WrongToolSeconds && selector?.Suggested != null && dish != null)
            {
                SetHint($"{selector.CurrentTool?.DisplayName} nie ruszy tego brudu: {DescribeRemainingDirt(dish)}", WarningColor);
                if (Time.unscaledTime >= _nextWrongToolSound)
                {
                    _nextWrongToolSound = Time.unscaledTime + 4f;
                    GameAudio.Play(GameSound.WrongTool, 0.45f);
                    NudgeTool(selector.Suggested);
                }

                return;
            }

            string text = StageHint(dish, out bool warning);
            bool idle = Time.unscaledTime - _stageSince > IdleHintSeconds;
            SetHint(warning || HintEveryStep || idle ? text : null, warning ? WarningColor : HintColor);
        }

        private void NudgeTool(ToolDefinition tool)
        {
            if (toolbar == null)
            {
                return;
            }

            foreach (ToolButton button in toolbar.GetComponentsInChildren<ToolButton>())
            {
                if (button.Tool == tool)
                {
                    button.Nudge();
                }
            }
        }

        private string StageHint(DishController dish, out bool warning)
        {
            warning = false;
            // A stalled machine matters most when there is nothing in hand (or the dish is ready to rack).
            if (shiftController.IsDishwasherBroken && (repairPanel == null || !repairPanel.IsOpen) &&
                (dish == null || dish.CurrentStage == WashStage.ReadyForRack))
            {
                warning = true;
                return "Zmywarka stanęła! Stuknij czerwony przycisk, żeby ją naprawić.";
            }

            DishController soaking = washStation != null ? washStation.Soaking : null;
            if (dish == null && soaking != null)
            {
                int percent = Mathf.FloorToInt(soaking.SoakProgress01 * 100f);
                return $"{soaking.Definition.DisplayName} moczy się ({percent}%) - brud zmięknie. Wyjmij z wody albo weź następne naczynie z Góry.";
            }

            if (dish == null)
            {
                // Between dishes, a tired Kuba is told why the sponge feels heavy today.
                return Fatigue.IsTired(shiftController.EnergyAtStart)
                    ? $"Kuba jest zmęczony (energia {shiftController.EnergyAtStart:0}) - szoruje wolniej. Dotknij naczynia w Górze."
                    : "Dotknij naczynia w Górze, żeby wziąć je do zlewu.";
            }

            switch (dish.CurrentStage)
            {
                case WashStage.AwaitingScrape:
                    return "Przesuń szybko palcem po naczyniu - zeskrob resztki.";
                case WashStage.AwaitingRinse:
                    return "Przytrzymaj palec na naczyniu - płukanie.";
                case WashStage.AwaitingScrub:
                    if (dish.NeedsInspection)
                    {
                        // Tilt steers the reflection on a phone with the sensor, when allowed; otherwise a finger does.
                        return DishController.TiltSteersReflection
                            ? "Pod światło! Przechyl telefon - zacieki wyjdą w odbiciu. Zetrzyj je gąbką."
                            : "Pod światło! Przeciągnij palcem obok szklanki, żeby przesunąć odbicie. Zacieki zetrzyj gąbką.";
                    }

                    // The scraper and the spray only take the thick of it off: say which tool finishes each dirt.
                    ToolDefinition current = ActiveToolSelector.Instance != null ? ActiveToolSelector.Instance.CurrentTool : null;
                    return NeedsASoak(dish) && shiftController.IsUnlocked(Mechanic.Soaking)
                        ? "Zaschnięte! Odstaw do moczenia - zejdzie dużo łatwiej. " + DescribeRemainingDirt(dish)
                        : current != null && current.ResidueFloor > 0f
                            ? $"{current.DisplayName} zdejmie wierzch. Dokończ: {DescribeRemainingDirt(dish)}"
                            : "Szoruj! " + DescribeRemainingDirt(dish);
                case WashStage.ReadyForRack:
                    RackGrid rack = shiftController.Rack;
                    if (rack != null && !rack.HasRoomFor(dish.Definition))
                    {
                        warning = true;
                        return "Na stojaku nie ma już miejsca - uruchom zmywarkę!";
                    }

                    return rack != null
                        ? $"Czyste! Puść, złap i przeciągnij na stojak lub rzuć w górę. Pełny kosz ({rack.FullLoadCells}/{rack.CellCount}) = Flow."
                        : "Czyste! Puść, złap ponownie i przeciągnij na stojak.";
                default:
                    return string.Empty;
            }
        }

        /// <summary>Dirt this hard (dried porridge) is a slog dry: say so while the dish could still go in to soak.</summary>
        private const float SoakWorthyDifficulty = 2.5f;

        private static bool NeedsASoak(DishController dish)
        {
            if (!dish.CanSoak || dish.SoakProgress01 >= 0.5f)
            {
                return false;
            }

            foreach (DirtLayerState layer in dish.DirtLayers)
            {
                if (layer.Type != null && layer.Remaining > 0f && layer.Type.ScrubDifficulty >= SoakWorthyDifficulty)
                {
                    return true;
                }
            }

            return false;
        }

        private static string DescribeRemainingDirt(DishController dish)
        {
            var parts = new List<string>();
            foreach (DirtLayerState layer in dish.DirtLayers)
            {
                // Streaks are not "dirt to scrub" until the glass is held to the light (its own hint).
                if (layer.Type == null || layer.Remaining <= 0f || layer.Type.ShowsOnlyInReflection)
                {
                    continue;
                }

                ToolDefinition tool = FindEffectiveTool(layer.Type);
                parts.Add(tool != null ? $"{layer.Type.DisplayName} → {tool.DisplayName}" : layer.Type.DisplayName);
            }

            return string.Join(", ", parts);
        }

        private static ToolDefinition FindEffectiveTool(DirtTypeDefinition dirt)
        {
            if (ActiveToolSelector.Instance == null)
            {
                return null;
            }

            foreach (ToolDefinition tool in ActiveToolSelector.Instance.AvailableTools)
            {
                // The tool that can get it right off (the scraper and the spray leave a film).
                if (tool != null && tool.IsEffectiveAgainst(dirt) && tool.ResidueFloor <= 0f)
                {
                    return tool;
                }
            }

            return null;
        }

        private void UpdateOrderText()
        {
            if (orderText == null)
            {
                return;
            }

            GameObject banner = orderBanner != null ? orderBanner : orderText.gameObject;
            if (_orderBannerImage == null && orderBanner != null && orderBanner.TryGetComponent(out _orderBannerImage))
            {
                _orderBannerColor = _orderBannerImage.color;
            }

            if (chefOrderController != null && chefOrderController.ActiveOrder != null && !shiftController.IsShiftOver)
            {
                int seconds = Mathf.CeilToInt(chefOrderController.OrderTimeRemaining);
                // Urgent: wanted on the rack now - red, and breathing, so it is noticed from the sink.
                bool urgent = chefOrderController.IsUrgent;
                if (seconds != _shownOrderSeconds || chefOrderController.ActiveOrder != _shownOrder || urgent != _shownUrgent)
                {
                    _shownOrderSeconds = seconds;
                    _shownOrder = chefOrderController.ActiveOrder;
                    _shownUrgent = urgent;
                    string dish = _shownOrder.DisplayName.ToUpperInvariant();
                    orderText.text = urgent ? $"PILNE: {dish} NA STOJAK! ({seconds}s)" : $"Szef woła: {dish}! ({seconds}s)";
                }

                if (_orderBannerImage != null)
                {
                    float pulse = Mathf.Sin(Time.unscaledTime * 8f) * 0.5f + 0.5f;
                    _orderBannerImage.color = urgent ? Color.Lerp(UrgentColor, Color.white, pulse * 0.2f) : _orderBannerColor;
                }

                banner.SetActive(true);
            }
            else
            {
                banner.SetActive(false);
            }
        }

        private void OnFlowStreakChanged(int streak)
        {
            if (flowText != null)
            {
                flowText.text = $"Flow ×{streak}";
            }

            if (streak > _shownStreak)
            {
                if (System.Array.IndexOf(FlowMilestones, streak) >= 0)
                {
                    // A long streak: the kitchen is in its rhythm.
                    Popup($"Idealny rytm!\n<size=60%>Flow ×{streak}</size>", new Color(1f, 0.85f, 0.3f));
                    GameAudio.Play(GameSound.FlowChime, 0.6f);
                    HapticsService.Pulse(HapticPulse.Medium);
                    if (AccessibilitySettings.ScreenEffects)
                    {
                        Flash(new Color(1f, 0.82f, 0.3f), 0.6f);
                    }
                }
                else
                {
                    Popup($"Flow ×{streak}!", new Color(1f, 0.85f, 0.3f));
                }
            }

            _shownStreak = streak;
        }

        private static readonly List<RaycastResult> UiHits = new List<RaycastResult>();

        /// <summary>Whether a screen point is on one of the HUD's buttons (for <see cref="DishInputRouter.IsOverControl"/>).</summary>
        private static bool IsOverButton(Vector2 screenPos)
        {
            EventSystem events = EventSystem.current;
            if (events == null)
            {
                return false;
            }

            UiHits.Clear();
            events.RaycastAll(new PointerEventData(events) { position = screenPos }, UiHits);
            foreach (RaycastResult hit in UiHits)
            {
                if (hit.gameObject != null && hit.gameObject.GetComponentInParent<Selectable>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnStartDishwasherClicked()
        {
            if (shiftController == null)
            {
                return;
            }

            if (shiftController.IsDishwasherBroken)
            {
                repairPanel?.Open();
                return;
            }

            if (shiftController.AutoStartIn >= 0f)
            {
                // The countdown stops: the player wants to pack more in first.
                shiftController.CancelAutoStart();
                ShowMessage("Dobrze, dołóż jeszcze coś. Zmywarkę uruchomisz tym samym przyciskiem.", 3f);
                return;
            }

            if (shiftController.IsDishwasherRunning)
            {
                ShowMessage("Zmywarka już myje - poczekaj na koniec cyklu. W tym czasie układaj następny kosz.", 3f);
                GameAudio.Play(GameSound.UiError, 0.4f);
                return;
            }

            if (!shiftController.CanStartDishwasher)
            {
                ShowMessage("Stojak jest pusty - najpierw ułóż na nim czyste naczynie.", 3f);
                GameAudio.Play(GameSound.UiError, 0.4f);
                return;
            }

            shiftController.TryStartDishwasherCycle();
        }

        private void ShowResult(ShiftResult result)
        {
            repairPanel?.Close();
            if (!_ending)
            {
                _ending = true;
                StartCoroutine(EndOfShift(result));
            }
        }

        /// <summary>Closing time: the bell and the stamp over the kitchen, then the result screen comes up.</summary>
        private IEnumerator EndOfShift(ShiftResult result)
        {
            GameAudio.Play(GameSound.ShiftEnd, 0.7f);
            HapticsService.Pulse(HapticPulse.Medium);
            if (endStamp != null)
            {
                endStamp.gameObject.SetActive(true);
                for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / StampSeconds)
                {
                    float land = Mathf.Clamp01(t / 0.25f);
                    endStamp.transform.localScale = Vector3.one * Mathf.Lerp(2.4f, 1f, 1f - (1f - land) * (1f - land));
                    endStamp.alpha = t < 0.8f ? Mathf.Clamp01(t * 6f) : 1f - (t - 0.8f) / 0.2f;
                    endStamp.transform.localRotation = Quaternion.Euler(0f, 0f, -6f);
                    yield return null;
                }

                endStamp.gameObject.SetActive(false);
            }

            resultScreen?.Show(result, _careerAtStart);
        }

        /// <summary>The result screen is up (or coming).</summary>
        public ShiftResultScreen ResultScreen => resultScreen;

        private void OnBackToMenuClicked()
        {
            if (ServiceLocator.TryGet(out GameStateMachine machine))
            {
                machine.ChangeState<MetaHubState>();
            }
        }
    }
}
