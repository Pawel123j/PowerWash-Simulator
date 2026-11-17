using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zmywak.Core.Audio;
using Zmywak.Core.Data;
using Zmywak.Core.Haptics;
using Zmywak.Core.Save;
using Zmywak.Kitchen.Dishes;
using Zmywak.Kitchen.Dishwasher;
using Zmywak.Kitchen.Feedback;
using Zmywak.Kitchen.Orders;
using Zmywak.Kitchen.Pile;
using Zmywak.Kitchen.Rack;
using Zmywak.Kitchen.Shifts;
using Zmywak.Kitchen.Tools;
using Zmywak.Core.Diagnostics;

namespace Zmywak.UI.HUD
{
    /// <summary>The steps of the guided first plate.</summary>
    public enum TutorialStep
    {
        Off,
        PickDish,
        Scrape,
        Rinse,
        Dirt,
        Tool,
        Scrub,
        Rack,
        Dishwasher,
        Done
    }

    /// <summary>
    /// The first shift teaches the sink on one plate, a step at a time - each one shown only when it is needed, pointing at
    /// the thing to touch (a ring and an arrow over it, a finger showing the gesture) with one short line: take the plate
    /// from the pile, swipe the scraps off, hold to rinse, see what the dirt is, pick the tool for it, scrub, put it on the
    /// rack, start the dishwasher. Meanwhile the kitchen holds still (no clock, no more dishes, no chef) and only what the
    /// step needs is on screen - the tool bar comes up at the tool step, holding just the tool it teaches, the dishwasher's
    /// button at the last. After it, and on the days after, the first time something new turns up (two kinds of dirt on
    /// one dish, a glass, a bowl to soak, the chef's call, a breakdown, a new tool) one short card says what it is - once
    /// per life. Nothing here runs for the kitchen on its own (the editor, the tests).
    /// </summary>
    public class KitchenTutorial : MonoBehaviour
    {
        [SerializeField] private ShiftController shiftController;
        [SerializeField] private WashStation washStation;
        [SerializeField] private RackGrid rackGrid;
        [SerializeField] private DishwasherController dishwasher;
        [SerializeField] private DishPileController pile;
        [SerializeField] private ChefOrderController chefOrders;
        [SerializeField] private RectTransform toolbar;
        [SerializeField] private CanvasGroup toolbarGroup;
        [SerializeField] private RectTransform dishwasherButton;

        [Header("Overlay")]
        [SerializeField] private RectTransform overlay;
        [SerializeField] private RectTransform coachCard;
        [SerializeField] private TMP_Text coachText;
        [SerializeField] private TMP_Text stepText;
        [SerializeField] private RectTransform ring;
        [SerializeField] private RectTransform arrow;
        [SerializeField] private RectTransform finger;

        private const float TipSeconds = 5.5f;
        private const float DoneSeconds = 4f;

        private static readonly Vector2 CardTop = new Vector2(0.5f, 0.8f);
        private static readonly Vector2 CardMiddle = new Vector2(0.5f, 0.6f);

        private Camera _camera;
        private DishController _plate;
        private ToolDefinition _taughtTool;
        private float _stepTime;
        private float _tipUntil;
        private string _tipText;
        private readonly HashSet<string> _shownTips = new HashSet<string>();
        private readonly Queue<(string key, string text)> _pendingTips = new Queue<(string, string)>();
        private bool _scheduledTips;

        public TutorialStep Step { get; private set; } = TutorialStep.Off;

        /// <summary>The guided plate is on (the HUD keeps its own hints out of the way).</summary>
        public bool Guiding => Step != TutorialStep.Off && Step != TutorialStep.Done;

        /// <summary>A card is up (the guided plate's or a first-time one).</summary>
        public bool CardShown => coachCard != null && coachCard.gameObject.activeSelf;

        /// <summary>What the card says now - for tests.</summary>
        public string CardText => CardShown && coachText != null ? coachText.text : string.Empty;

        /// <summary>Whether the dishwasher's button is on screen yet (not until the guided plate reaches it).</summary>
        public bool ShowsDishwasherButton => !Guiding || Step >= TutorialStep.Dishwasher;

        private void Start()
        {
            using var _startup = StartupTimeline.Measure("KitchenTutorial.Start");
            _camera = Camera.main;
            if (shiftController == null)
            {
                shiftController = FindAnyObjectByType<ShiftController>();
            }

            Hide();
        }

        private void OnEnable()
        {
            KitchenFeedbackEvents.HeldToLight += OnHeldToLight;
            if (dishwasher != null)
            {
                dishwasher.OnBrokeDown += OnBrokeDown;
            }

            if (chefOrders != null)
            {
                chefOrders.OnOrderIssued += OnOrder;
            }
        }

        private void OnDisable()
        {
            DishController.ScrubHeld = false; // never left on past the kitchen (it is static)
            KitchenFeedbackEvents.HeldToLight -= OnHeldToLight;
            if (dishwasher != null)
            {
                dishwasher.OnBrokeDown -= OnBrokeDown;
            }

            if (chefOrders != null)
            {
                chefOrders.OnOrderIssued -= OnOrder;
            }
        }

        private void Update()
        {
            if (shiftController == null || shiftController.IsShiftOver)
            {
                Hide();
                return;
            }

            _scheduledTips = shiftController.IsScheduled;
            if (Step == TutorialStep.Off && shiftController.TutorialActive && shiftController.TutorialHold)
            {
                Go(TutorialStep.PickDish);
            }

            if (Guiding || Step == TutorialStep.Done)
            {
                UpdateGuide();
            }
            else
            {
                UpdateTips();
            }
        }

        // ------------------------------------------------------------------ the guided first plate

        private void Go(TutorialStep step)
        {
            Step = step;
            _stepTime = 0f;
            if (step != TutorialStep.PickDish && step != TutorialStep.Done)
            {
                GameAudio.Play(GameSound.UiConfirm, 0.45f, 1.1f);
                HapticsService.Pulse(HapticPulse.Light);
            }

            ActiveToolSelector selector = ActiveToolSelector.Instance;
            if (selector != null)
            {
                selector.OnlyTool = step == TutorialStep.Tool || step == TutorialStep.Scrub ? _taughtTool : null;
            }

            // While the card names the dirt and asks for its tool, rubbing does nothing: the default tool in hand must not
            // scrub the plate clean past the two lessons.
            DishController.ScrubHeld = step == TutorialStep.Dirt || step == TutorialStep.Tool;

            if (step == TutorialStep.Done)
            {
                shiftController.EndTutorialHold(); // the clock starts, the dishes come
            }
        }

        private void UpdateGuide()
        {
            _stepTime += Time.unscaledDeltaTime;
            if (_plate == null)
            {
                _plate = FirstDish();
            }

            DishController held = washStation != null ? washStation.Occupant : null;
            DishController dish = held != null ? held : _plate;
            bool toolbarOn = Step >= TutorialStep.Tool;
            if (toolbarGroup != null)
            {
                toolbarGroup.alpha = Mathf.MoveTowards(toolbarGroup.alpha, toolbarOn ? 1f : 0f, Time.unscaledDeltaTime * 4f);
                toolbarGroup.interactable = toolbarOn;
                toolbarGroup.blocksRaycasts = toolbarOn;
            }

            switch (Step)
            {
                case TutorialStep.PickDish:
                    Card(1, "To Góra brudnych naczyń. Dotknij talerza, żeby wziąć go do zlewu.", CardTop);
                    PointAt(dish != null && dish.IsSettledInPile ? WorldToCanvas(dish.transform.position) : (Vector2?)null, false);
                    if (held != null)
                    {
                        Go(TutorialStep.Scrape);
                    }

                    break;
                case TutorialStep.Scrape:
                    Card(2, "Na talerzu zostały resztki. Przesuń szybko palcem po talerzu, żeby je zeskrobać.", CardTop);
                    PointAt(held != null && !held.IsMoving ? WorldToCanvas(held.transform.position) : (Vector2?)null, true, swipe: true);
                    if (held != null && held.CurrentStage > WashStage.AwaitingScrape)
                    {
                        Go(TutorialStep.Rinse);
                    }

                    break;
                case TutorialStep.Rinse:
                    Card(3, "Teraz przytrzymaj palec na talerzu - spłuczesz go wodą z kranu.", CardTop);
                    PointAt(held != null ? WorldToCanvas(held.transform.position) : (Vector2?)null, true, hold: true);
                    if (held != null && held.CurrentStage > WashStage.AwaitingRinse)
                    {
                        Go(held.CurrentStage == WashStage.AwaitingScrub ? TutorialStep.Dirt : TutorialStep.Rack);
                    }

                    break;
                case TutorialStep.Dirt:
                    _taughtTool = held != null && ActiveToolSelector.Instance != null ? held.SuitingTool(ActiveToolSelector.Instance.AvailableTools) : null;
                    Card(4, $"Został brud: {DirtName(held)}. Każdy brud schodzi innym narzędziem.", CardTop);
                    PointAt(held != null ? WorldToCanvas(held.transform.position) : (Vector2?)null, true);
                    if (_stepTime > 2.6f || held == null || held.CurrentStage != WashStage.AwaitingScrub)
                    {
                        Go(held != null && held.CurrentStage == WashStage.AwaitingScrub ? TutorialStep.Tool : TutorialStep.Rack);
                    }

                    break;
                case TutorialStep.Tool:
                    string tool = _taughtTool != null ? _taughtTool.DisplayName : "narzędzie";
                    Card(5, $"Na ten brud: {tool}. Stuknij ją na pasku na dole.", CardMiddle);
                    PointAt(ToolButtonPosition(_taughtTool), false);
                    ActiveToolSelector selector = ActiveToolSelector.Instance;
                    if (selector == null || _taughtTool == null || selector.CurrentTool == _taughtTool)
                    {
                        Go(TutorialStep.Scrub);
                    }

                    break;
                case TutorialStep.Scrub:
                    Card(6, "Szoruj palcem po brudzie. Schodzi tam, gdzie pocierasz - przejedź cały talerz.", CardTop);
                    PointAt(held != null ? WorldToCanvas(held.transform.position) : (Vector2?)null, true, scrub: true);
                    if (held == null || held.CurrentStage >= WashStage.ReadyForRack)
                    {
                        Go(TutorialStep.Rack);
                    }

                    break;
                case TutorialStep.Rack:
                    Card(7, "Czyste! Puść i złap talerz ponownie. Przeciągnij go na stojak albo machnij w górę.", CardMiddle);
                    Vector3? spot = rackGrid != null && dish != null ? rackGrid.BestSpotFor(dish.Definition) : null;
                    PointAt(spot.HasValue ? WorldToCanvas(spot.Value) : (Vector2?)null, false);
                    if (rackGrid != null && rackGrid.DishCount > 0)
                    {
                        Go(TutorialStep.Dishwasher);
                    }

                    break;
                case TutorialStep.Dishwasher:
                    Card(8, "Uruchom zmywarkę - umyje wszystko, co stoi na stojaku. Pełny kosz to premia i Flow.", CardMiddle);
                    PointAt(dishwasherButton != null ? CanvasPosition(dishwasherButton) : (Vector2?)null, false);
                    if (dishwasher != null && dishwasher.IsRunning)
                    {
                        Go(TutorialStep.Done);
                    }

                    break;
                case TutorialStep.Done:
                    Card(0, $"Tak wygląda każde naczynie. Dziś norma: {shiftController.Quota}. Zegar rusza - powodzenia!", CardTop);
                    PointAt(null, false);
                    if (_stepTime > DoneSeconds)
                    {
                        Step = TutorialStep.Off;
                        Hide();
                    }

                    break;
            }
        }

        private DishController FirstDish()
        {
            foreach (DishController dish in FindObjectsByType<DishController>(FindObjectsInactive.Exclude))
            {
                return dish;
            }

            return null;
        }

        private static string DirtName(DishController dish)
        {
            if (dish == null)
            {
                return "brud";
            }

            foreach (DirtLayerState layer in dish.DirtLayers)
            {
                if (layer.Type != null && layer.Remaining > 0f && !layer.Type.ShowsOnlyInReflection)
                {
                    return layer.Type.DisplayName.ToLowerInvariant();
                }
            }

            return "brud";
        }

        private Vector2? ToolButtonPosition(ToolDefinition tool)
        {
            if (toolbar == null || tool == null)
            {
                return null;
            }

            foreach (ToolButton button in toolbar.GetComponentsInChildren<ToolButton>())
            {
                if (button.Tool == tool)
                {
                    return CanvasPosition((RectTransform)button.transform);
                }
            }

            return null;
        }

        // ------------------------------------------------------------------ first-time cards

        private void UpdateTips()
        {
            if (!_scheduledTips)
            {
                Hide();
                return;
            }

            ConsiderSinkTips();
            if (pile != null && pile.Fullness > 0.6f)
            {
                Tip("pile", "Góra rośnie. Zbyt wysoka się zawali, a stłuczki kosztują. Zdejmuj z niej na bieżąco.");
            }

            if (rackGrid != null && rackGrid.IsFullLoad)
            {
                Tip("full_rack", "Pełny kosz! Zmywarka ruszy sama za chwilę. Stuknij jej przycisk, jeśli chcesz dołożyć.");
            }

            if (shiftController.IsUnlocked(Mechanic.Scraper) && shiftController.Day == MechanicUnlocks.DayOf(Mechanic.Scraper))
            {
                Tip("tool_scraper", "Nowe narzędzie: Skrobak. Szybko zdejmuje przypalony wierzch - resztę dokończ druciakiem.");
            }

            if (shiftController.IsUnlocked(Mechanic.Shower) && shiftController.Day == MechanicUnlocks.DayOf(Mechanic.Shower))
            {
                Tip("tool_shower", "Nowe narzędzie: Prysznic. Szeroki strumień na sos i tłuszcz; resztkę dokończ gąbką.");
            }

            if (_tipUntil > 0f && Time.unscaledTime > _tipUntil)
            {
                _tipUntil = 0f;
                Hide();
            }

            if (_tipUntil <= 0f && _pendingTips.Count > 0)
            {
                (string key, string text) next = _pendingTips.Dequeue();
                _tipText = next.text;
                _tipUntil = Time.unscaledTime + TipSeconds;
                Card(0, _tipText, CardTop);
                PointAt(null, false);
                GameAudio.Play(GameSound.UiOpen, 0.4f);
            }
        }

        private void ConsiderSinkTips()
        {
            DishController dish = washStation != null ? washStation.Occupant : null;
            if (dish == null || dish.IsMoving)
            {
                return;
            }

            if (dish.CurrentStage == WashStage.AwaitingScrub)
            {
                var kinds = new List<string>();
                foreach (DirtLayerState layer in dish.DirtLayers)
                {
                    if (layer.Type != null && layer.Remaining > 0f && !layer.Type.ShowsOnlyInReflection)
                    {
                        kinds.Add(layer.Type.DisplayName.ToLowerInvariant());
                    }
                }

                if (kinds.Count >= 2)
                {
                    Tip("two_layers", $"Dwie warstwy brudu: {kinds[0]} i {kinds[1]}. Każdą zdejmij jej narzędziem - pulsuje na pasku.");
                }

                if (dish.Definition != null && dish.Definition.Id == "glass")
                {
                    Tip("glass", "Szklanka: tłuszcz zmyj Płynem, a potem obejrzyj ją pod światło - zacieki widać tylko w odbiciu.");
                }

                if (dish.Definition != null && dish.Definition.Id == "bowl" && dish.CanSoak)
                {
                    Tip("soak", "Zaschnięta owsianka! Odstaw miskę do moczenia - brud zmięknie, a ty umyjesz w tym czasie coś innego.");
                }
            }
        }

        private void OnHeldToLight(DishController dish)
        {
            Tip("light", "Pod światło! Zacieki widać w odbiciu - zetrzyj je gąbką.");
        }

        private void OnBrokeDown(DishwasherFault fault)
        {
            Tip("breakdown", "Zmywarka stanęła! Stuknij czerwony przycisk i napraw ją gestem pokazanym na karcie.");
        }

        private void OnOrder(DishDefinition dish)
        {
            if (chefOrders != null && chefOrders.IsUrgent)
            {
                Tip("urgent", "PILNE zamówienie: szef weźmie naczynie prosto ze stojaka. Liczy się, gdy trafi na stojak.");
            }
            else
            {
                Tip("chef_order", "Szef woła o naczynie! Umyj je i puść przez zmywarkę, zanim minie czas - to podbija Flow.");
            }
        }

        /// <summary>A first-time card, once per life (scheduled shifts only: the kitchen on its own teaches nothing).</summary>
        private void Tip(string key, string text)
        {
            if (!_scheduledTips || Guiding || _shownTips.Contains(key))
            {
                return;
            }

            _shownTips.Add(key);
            string tip = "tip_" + key;
            if (TutorialProgress.Has(tip))
            {
                return;
            }

            TutorialProgress.Mark(tip);
            _pendingTips.Enqueue((key, text));
        }

        // ------------------------------------------------------------------ the overlay

        private int _shownStep = -1;

        private void Card(int step, string text, Vector2 anchor)
        {
            if (coachCard == null)
            {
                return;
            }

            if (!coachCard.gameObject.activeSelf)
            {
                coachCard.gameObject.SetActive(true);
                coachCard.localScale = Vector3.one * 0.9f;
            }

            coachCard.localScale = Vector3.Lerp(coachCard.localScale, Vector3.one, 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
            coachCard.anchorMin = coachCard.anchorMax = Vector2.Lerp(coachCard.anchorMin, anchor, 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime));
            if (coachText != null && coachText.text != text)
            {
                coachText.text = text;
            }

            if (stepText != null && step != _shownStep)
            {
                _shownStep = step;
                stepText.text = step > 0 ? $"{step}/8" : string.Empty;
            }
        }

        private void PointAt(Vector2? target, bool onDish, bool swipe = false, bool hold = false, bool scrub = false)
        {
            bool show = target.HasValue;
            ring?.gameObject.SetActive(show);
            arrow?.gameObject.SetActive(show && !onDish);
            finger?.gameObject.SetActive(show && (swipe || hold || scrub));
            if (!show)
            {
                return;
            }

            float time = Time.unscaledTime;
            Vector2 at = target.Value;
            if (ring != null)
            {
                ring.anchoredPosition = at;
                float pulse = 1f + 0.12f * Mathf.Sin(time * 5f);
                ring.localScale = Vector3.one * (onDish ? 1.5f : 1f) * pulse;
            }

            if (arrow != null)
            {
                arrow.anchoredPosition = at + new Vector2(0f, 150f + 18f * Mathf.Sin(time * 6f));
            }

            if (finger != null)
            {
                Vector2 offset = Vector2.zero;
                if (swipe)
                {
                    float t = Mathf.Repeat(time * 1.2f, 1f);
                    offset = new Vector2(Mathf.Lerp(-150f, 150f, t), -40f);
                }
                else if (scrub)
                {
                    offset = new Vector2(Mathf.Cos(time * 5f) * 90f, Mathf.Sin(time * 10f) * 40f);
                }
                else if (hold)
                {
                    finger.localScale = Vector3.one * (0.9f + 0.1f * Mathf.Sin(time * 4f));
                }

                finger.anchoredPosition = at + offset + new Vector2(30f, -60f);
            }
        }

        private void Hide()
        {
            coachCard?.gameObject.SetActive(false);
            ring?.gameObject.SetActive(false);
            arrow?.gameObject.SetActive(false);
            finger?.gameObject.SetActive(false);
            if (Step == TutorialStep.Off && toolbarGroup != null)
            {
                toolbarGroup.alpha = 1f;
                toolbarGroup.interactable = true;
                toolbarGroup.blocksRaycasts = true;
            }
        }

        private Vector2? WorldToCanvas(Vector3 world)
        {
            if (_camera == null || overlay == null)
            {
                return null;
            }

            Vector3 screen = _camera.WorldToScreenPoint(world);
            if (screen.z < 0f)
            {
                return null;
            }

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(overlay, screen, null, out Vector2 local) ? local : (Vector2?)null;
        }

        private Vector2? CanvasPosition(RectTransform target)
        {
            if (target == null || overlay == null || !target.gameObject.activeInHierarchy)
            {
                return null;
            }

            Vector3 world = target.TransformPoint(target.rect.center);
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, world);
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(overlay, screen, null, out Vector2 local) ? local : (Vector2?)null;
        }
    }
}
