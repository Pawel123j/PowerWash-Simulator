using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zmywak.Core.Audio;
using Zmywak.Core.Data;
using Zmywak.Core.GameState;
using Zmywak.Core.GameState.States;
using Zmywak.Core.Services;
using Zmywak.Kitchen.Shifts;
using Zmywak.Meta.Progress;
using Zmywak.Narrative.LanguageBarrier;
using Zmywak.Narrative.Progress;
using Zmywak.Narrative.SlangDictionary;
using Zmywak.UI.Common;

namespace Zmywak.UI.Screens
{
    /// <summary>
    /// Home base between shifts: the day, the wallet and the rank, the three stats and a line on how Kuba is feeling, and the
    /// evening - one plan for it (rest, a call home from the phone box, the pub, the crew's dinner when they ask, a letter
    /// when home aches), money home, a few things worth buying, the slang he has picked up, the map - and the way to work.
    /// The first days add what is new that day; the closer Kuba is to the crew, the more of their English he catches.
    /// Back (or the menu button) opens the evening's menu: the settings, and the way to the main menu.
    /// </summary>
    public class MetaHubScreen : MonoBehaviour
    {
        [SerializeField] private TMP_Text dayText;
        [SerializeField] private TMP_Text walletText;
        [Tooltip("Plain-text stats readout (used when the bar rows below are not set up).")]
        [SerializeField] private TMP_Text statsText;
        [Header("Stat rows (label + 0-100 bar)")]
        [SerializeField] private TMP_Text energyLabel;
        [SerializeField] private Image energyFill;
        [SerializeField] private TMP_Text homesicknessLabel;
        [SerializeField] private Image homesicknessFill;
        [SerializeField] private TMP_Text integrationLabel;
        [SerializeField] private Image integrationFill;
        [SerializeField] private TMP_Text moodText;
        [SerializeField] private TMP_Text noticeText;
        [SerializeField] private GameObject noticeBackdrop;
        [SerializeField] private Button goToWorkButton;
        [Tooltip("The map of the city: its pins do what these buttons do.")]
        [SerializeField] private CityMapPanel cityMap;
        [SerializeField] private Button callMomButton;
        [SerializeField] private Button pubButton;
        [SerializeField] private Button sendMoneyButton;
        [SerializeField] private Button slangToggleButton;
        [SerializeField] private GameObject slangPanel;
        [SerializeField] private TMP_Text slangListText;
        [SerializeField] private List<SlangEntry> allSlangEntries = new List<SlangEntry>();

        [Header("The evening's plan")]
        [SerializeField] private TMP_Text eveningLabel;
        [SerializeField] private Button restButton;
        [SerializeField] private Button dinnerButton;
        [SerializeField] private Button letterButton;

        [Header("Things to buy")]
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private Button[] buyButtons = new Button[3];
        [SerializeField] private TMP_Text[] buyNotes = new TMP_Text[3];

        [Header("The evening's menu")]
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Button menuResumeButton;
        [SerializeField] private Button menuSettingsButton;
        [SerializeField] private Button menuMainMenuButton;
        [SerializeField] private TMP_Text menuNote;
        [SerializeField] private StoryBeatTrigger story;

        private static readonly Purchase[] Shop = { Purchase.Mattress, Purchase.Dictionary, Purchase.Phone };

        private MetaProgressController _progress;
        private NarrativeProgressController _narrativeProgress;
        private float _shownEnergy = -1f;
        private float _shownHomesickness = -1f;
        private float _shownIntegration = -1f;
        private Vector3 _noticeHome;
        private float _noticeAge = 10f;

        private void OnEnable()
        {
            ServiceLocator.TryGet(out _progress);
            ServiceLocator.TryGet(out _narrativeProgress);
            LoadingTips.Glossary = allSlangEntries; // the loading screen quotes the slang Kuba knows

            if (_progress != null)
            {
                _progress.EnsureOnDisk(); // a new life's starting week, on disk before the story writes anything next to it
                _progress.OnProgressChanged += RefreshReadout;
                _progress.OnNotice += ShowNotice;
            }

            if (_narrativeProgress != null)
            {
                _narrativeProgress.OnProgressChanged += RefreshSlangList;
                _narrativeProgress.OnProgressChanged += LabelWork; // tonight's scene may hand him the knife
            }

            goToWorkButton?.onClick.AddListener(OnGoToWorkClicked);
            if (cityMap != null)
            {
                cityMap.PinTapped += OnMapPin;
            }

            callMomButton?.onClick.AddListener(OnCallMomClicked);
            pubButton?.onClick.AddListener(OnPubClicked);
            sendMoneyButton?.onClick.AddListener(OnSendMoneyClicked);
            slangToggleButton?.onClick.AddListener(OnSlangToggleClicked);
            restButton?.onClick.AddListener(OnRestClicked);
            dinnerButton?.onClick.AddListener(OnDinnerClicked);
            letterButton?.onClick.AddListener(OnLetterClicked);
            menuResumeButton?.onClick.AddListener(CloseMenu);
            menuSettingsButton?.onClick.AddListener(OpenSettings);
            menuMainMenuButton?.onClick.AddListener(GoToMainMenu);
            for (int i = 0; i < buyButtons.Length && i < Shop.Length; i++)
            {
                Purchase purchase = Shop[i];
                buyButtons[i]?.onClick.AddListener(() => OnBuy(purchase));
            }

            if (noticeBackdrop != null)
            {
                _noticeHome = noticeBackdrop.transform.localScale;
            }

            // The evening opens on what is new today, or what the next promotion still asks for.
            ShowNotice(OpeningNotice());
            slangPanel?.SetActive(false);
            shopPanel?.SetActive(false);
            menuPanel?.SetActive(false);
            CatchCrewWords();
            LabelActions();
            LabelWork();
            RefreshReadout();
            RefreshSlangList();
        }

        private void OnDisable()
        {
            if (_progress != null)
            {
                _progress.OnProgressChanged -= RefreshReadout;
                _progress.OnNotice -= ShowNotice;
            }

            if (_narrativeProgress != null)
            {
                _narrativeProgress.OnProgressChanged -= RefreshSlangList;
                _narrativeProgress.OnProgressChanged -= LabelWork;
            }

            goToWorkButton?.onClick.RemoveListener(OnGoToWorkClicked);
            if (cityMap != null)
            {
                cityMap.PinTapped -= OnMapPin;
            }

            callMomButton?.onClick.RemoveListener(OnCallMomClicked);
            pubButton?.onClick.RemoveListener(OnPubClicked);
            sendMoneyButton?.onClick.RemoveListener(OnSendMoneyClicked);
            slangToggleButton?.onClick.RemoveListener(OnSlangToggleClicked);
            restButton?.onClick.RemoveListener(OnRestClicked);
            dinnerButton?.onClick.RemoveListener(OnDinnerClicked);
            letterButton?.onClick.RemoveListener(OnLetterClicked);
            menuResumeButton?.onClick.RemoveListener(CloseMenu);
            menuSettingsButton?.onClick.RemoveListener(OpenSettings);
            menuMainMenuButton?.onClick.RemoveListener(GoToMainMenu);
            foreach (Button button in buyButtons)
            {
                button?.onClick.RemoveAllListeners();
            }
        }

        private void Update()
        {
            // The notice pops in when it changes.
            _noticeAge += Time.unscaledDeltaTime;
            if (noticeBackdrop != null && noticeBackdrop.activeSelf && _noticeAge < 0.25f + Time.unscaledDeltaTime)
            {
                float t = Mathf.Clamp01(_noticeAge / 0.25f);
                noticeBackdrop.transform.localScale = _noticeHome * (t < 0.6f ? Mathf.Lerp(0.92f, 1.03f, t / 0.6f) : Mathf.Lerp(1.03f, 1f, (t - 0.6f) / 0.4f));
            }

            // The stat bars slide to their new values.
            if (_progress != null)
            {
                SlideStat(ref _shownEnergy, _progress.Stats.Energy, energyFill);
                SlideStat(ref _shownHomesickness, _progress.Stats.Homesickness, homesicknessFill);
                SlideStat(ref _shownIntegration, _progress.Stats.Integration, integrationFill);
            }
        }

        private static void SlideStat(ref float shown, float value, Image fill)
        {
            if (fill == null)
            {
                return;
            }

            if (Mathf.Approximately(shown, value))
            {
                return; // there: nothing to slide
            }

            shown = shown < 0f || Mathf.Abs(shown - value) < 0.05f ? value : Mathf.Lerp(shown, value, 1f - Mathf.Exp(-6f * Time.unscaledDeltaTime));
            FillBar.Set(fill, shown / 100f);
        }

        private string OpeningNotice()
        {
            if (_progress == null)
            {
                return string.Empty;
            }

            IReadOnlyList<Mechanic> news = MechanicUnlocks.NewOn(_progress.DayNumber);
            var said = new List<string>();
            foreach (Mechanic mechanic in news)
            {
                string line = MechanicUnlocks.Describe(mechanic);
                if (!string.IsNullOrEmpty(line))
                {
                    said.Add(line);
                }
            }

            return said.Count > 0
                ? "Nowe od dziś: " + string.Join(", ", said) + "."
                : Career.NextGoal(_progress.Ledger.WorkRecord, _progress.Rank);
        }

        /// <summary>The closer Kuba is to the crew (and with a dictionary), the more of their everyday English he catches.</summary>
        private void CatchCrewWords()
        {
            if (_progress == null || _narrativeProgress == null)
            {
                return;
            }

            float level = _progress.Stats.Integration + (_progress.Ledger.Has(Purchase.Dictionary) ? CrewWords.DictionaryBoost : 0f);
            var learned = new List<string>();
            foreach (string word in CrewWords.WordsFor(level))
            {
                if (!_narrativeProgress.Vocabulary.IsKnown(word))
                {
                    _narrativeProgress.LearnWord(word);
                    learned.Add(word);
                }
            }

            if (learned.Count > 0)
            {
                ShowNotice($"Kuba łapie od ekipy nowe słowa: {string.Join(", ", learned.GetRange(0, Mathf.Min(5, learned.Count)))}" +
                           (learned.Count > 5 ? "..." : "."));
            }
        }

        private void RefreshReadout()
        {
            if (_progress == null)
            {
                return;
            }

            if (dayText != null)
            {
                WeatherKind weather = Weather.On(_progress.CurrentDate, _progress.Place); // what the window shows, too
                dayText.text = $"Dzień {_progress.DayNumber} - {_progress.CurrentDate:dd.MM.yyyy} · {Weather.Word(weather)}";
            }

            LabelActions(); // a move (the epilogue's first scene) changes the prices and the money

            if (walletText != null)
            {
                // In debt the number turns red; under it Kuba's rank at the sink and how close the next one is - and the
                // hostel, while he is in one.
                string money = _progress.Wallet < 0f
                    ? $"<color=#FF7A6B>Dług: {_progress.Ledger.Format(-_progress.Wallet)}</color>"
                    : $"Portfel: {_progress.Ledger.Format(_progress.Wallet)}";
                int rank = _progress.Rank;
                string standing = rank >= Career.KingRank
                    ? Career.Title(rank)
                    : $"{Career.Title(rank)} · awans {Career.ProgressToNext(_progress.Ledger.WorkRecord, rank):P0}";
                if (_progress.Ledger.InHostel)
                {
                    standing += " · hostel";
                }

                walletText.text = $"{money}\n<size=55%>{standing}</size>";
            }

            // Below the rested line the kitchen feels it next shift - say so where the number is.
            SetStatRow(energyLabel, energyFill, Fatigue.IsTired(_progress.Stats.Energy) ? "Energia (zmęczony)" : "Energia", _progress.Stats.Energy);
            SetStatRow(homesicknessLabel, homesicknessFill, "Tęsknota", _progress.Stats.Homesickness);
            SetStatRow(integrationLabel, integrationFill, "Integracja", _progress.Stats.Integration);
            if (moodText != null)
            {
                moodText.text = Mood();
            }

            if (statsText != null)
            {
                statsText.text = $"Energia: {_progress.Stats.Energy:0}\n" +
                                  $"Tęsknota: {_progress.Stats.Homesickness:0}\n" +
                                  $"Integracja: {_progress.Stats.Integration:0}";
            }

            RefreshEvening();
            RefreshShop();
        }

        /// <summary>How Kuba is, in a few words: what his tiredness, homesickness and place in the crew mean tonight.</summary>
        private string Mood()
        {
            LifeLedger ledger = _progress.Ledger;
            var parts = new List<string>();
            float energy = ledger.Stats.Energy, home = ledger.Stats.Homesickness, crew = ledger.Stats.Integration;
            if (energy < Fatigue.RestedEnergy)
            {
                parts.Add("zmęczony - jutro będzie szorował wolniej, przyda się odpoczynek");
            }

            if (home >= ledger.Rules.HomesickSleepThreshold)
            {
                parts.Add("tęskni tak, że źle śpi");
            }
            else if (home >= ledger.Rules.LetterThreshold)
            {
                parts.Add("myśli o domu");
            }
            else if (home <= ledger.Rules.SettledThreshold)
            {
                parts.Add("czuje się tu coraz bardziej u siebie");
            }

            if (crew >= ShiftController.CrewHelpIntegration)
            {
                parts.Add("ekipa pomoże mu w ciężką noc");
            }
            else if (crew < 20f)
            {
                parts.Add("w kuchni wciąż jest obcy");
            }

            return parts.Count > 0 ? "Kuba " + string.Join("; ", parts) + "." : "Kuba jest w dobrej formie.";
        }

        private void SetStatRow(TMP_Text label, Image fill, string name, float value)
        {
            if (label != null)
            {
                label.text = $"{name} <b>{value:0}</b>";
            }
        }

        // ------------------------------------------------------------------ the evening

        private void RefreshEvening()
        {
            LifeLedger ledger = _progress.Ledger;
            EveningActivity tonight = ledger.TonightsActivity;
            if (eveningLabel != null)
            {
                eveningLabel.text = tonight == EveningActivity.None
                    ? "Wieczór - wybierz jedno (albo nic: spokojny sen):"
                    : $"Dzisiejszy wieczór: {PlanName(tonight)}";
            }

            Plan(restButton, EveningActivity.Rest, "Odpocznij", $"+{ledger.Rules.RestEnergy:0} energii");
            Plan(callMomButton, EveningActivity.CallHome, "Zadzwoń do domu", ledger.Has(Purchase.Phone) ? "z komórki" : "z budki na rogu");
            // The price goes on the small line (a long name with its price wrapped badly on a narrow phone), shown always.
            Plan(pubButton, EveningActivity.Pub, ledger.Rules.PubName, "integracja rośnie", WholeSum(ledger.Rules.PubCost));

            // The crew's dinner and the letter home are only on offer some evenings: the one that is, in one slot.
            bool dinner = ledger.CrewDinnerOffered;
            bool letter = !dinner && ledger.LetterOffered;
            dinnerButton?.gameObject.SetActive(dinner);
            letterButton?.gameObject.SetActive(letter);
            if (dinner)
            {
                Plan(dinnerButton, EveningActivity.CrewDinner, ledger.Rules.CrewDinnerName, "ekipa zaprasza", WholeSum(ledger.Rules.CrewDinnerCost));
            }

            if (letter)
            {
                Plan(letterButton, EveningActivity.Letter, "Napisz list do domu", "tęsknota zelżeje");
            }

            if (sendMoneyButton != null)
            {
                bool open = MechanicUnlocks.IsUnlocked(Mechanic.SendMoney, ledger.DayNumber);
                sendMoneyButton.interactable = open && ledger.Sendable >= 0.01f;
            }
        }

        /// <summary>A price on an evening's button, in whole units of the place's money ("10 EUR"): the cents made the small line wrap.</summary>
        private string WholeSum(float amount) => $"{amount:0} {PlaceMoney.Unit(_progress.Place)}";

        /// <summary>An evening's plan on its button: what it is, and under it what it costs (always) and what it does - or why not tonight.</summary>
        private void Plan(Button button, EveningActivity activity, string title, string detail, string price = null)
        {
            if (button == null)
            {
                return;
            }

            LifeLedger ledger = _progress.Ledger;
            bool can = ledger.CanDo(activity, out string why);
            button.interactable = can;
            string cost = string.IsNullOrEmpty(price) ? string.Empty : $"-{price} · ";
            SetLabel(button, $"{title}\n<size=60%>{cost}{(can || string.IsNullOrEmpty(why) ? detail : why)}</size>");
        }

        private static string PlanName(EveningActivity activity)
        {
            switch (activity)
            {
                case EveningActivity.Rest:
                    return "odpoczynek";
                case EveningActivity.Pub:
                    return "pub z ekipą";
                case EveningActivity.CrewDinner:
                    return "kolacja z ekipą";
                case EveningActivity.Letter:
                    return "list do domu";
                case EveningActivity.CallHome:
                    return "telefon do domu";
                default:
                    return "w domu";
            }
        }

        private void RefreshShop()
        {
            if (_progress == null)
            {
                return;
            }

            LifeLedger ledger = _progress.Ledger;
            for (int i = 0; i < buyButtons.Length && i < Shop.Length; i++)
            {
                Purchase purchase = Shop[i];
                bool has = ledger.Has(purchase);
                float price = ledger.PriceOf(purchase);
                if (buyButtons[i] != null)
                {
                    buyButtons[i].interactable = !has && ledger.Wallet >= price && ledger.CanSpare(price);
                    SetLabel(buyButtons[i], has ? $"{ShopName(purchase)}\n<size=60%>kupione</size>" : $"{ShopName(purchase)}\n<size=60%>{ledger.Format(price)}</size>");
                }

                if (i < buyNotes.Length && buyNotes[i] != null)
                {
                    buyNotes[i].text = ShopNote(purchase);
                }
            }
        }

        public static string ShopName(Purchase purchase)
        {
            switch (purchase)
            {
                case Purchase.Mattress:
                    return "Materac";
                case Purchase.Dictionary:
                    return "Słownik kieszonkowy";
                default:
                    return "Komórka na kartę";
            }
        }

        private static string ShopNote(Purchase purchase)
        {
            switch (purchase)
            {
                case Purchase.Mattress:
                    return "Zamiast kanapy. Każda noc oddaje trochę więcej energii.";
                case Purchase.Dictionary:
                    return "Angielski szybciej wpada do ucha: mniej rozmytych słów.";
                default:
                    return "Telefon do domu bez budki: nie zajmuje wieczoru i koi bardziej.";
            }
        }

        private void OnBuy(Purchase purchase)
        {
            if (_progress != null && _progress.TryBuy(purchase))
            {
                GameAudio.Play(GameSound.Reward, 0.5f, 0.9f);
                if (purchase == Purchase.Dictionary)
                {
                    CatchCrewWords();
                }
            }
            else
            {
                GameAudio.Play(GameSound.UiError, 0.5f);
            }
        }

        // ------------------------------------------------------------------ the evening's menu

        /// <summary>Back with nothing open, or the menu button: the evening's menu.</summary>
        public void OpenMenu()
        {
            bool storyOn = story != null && story.PlayingBeat != null;
            if (menuMainMenuButton != null)
            {
                menuMainMenuButton.interactable = !storyOn; // mid-scene, leaving would replay it: finish it first
            }

            if (menuNote != null)
            {
                menuNote.text = storyOn ? "Najpierw dokończ rozmowę." : "Wszystko zapisuje się na bieżąco.";
            }

            menuPanel?.SetActive(true);
        }

        private void CloseMenu()
        {
            menuPanel?.SetActive(false);
        }

        private void OpenSettings()
        {
            menuPanel?.SetActive(false);
            settingsPanel?.SetActive(true);
        }

        private void GoToMainMenu()
        {
            menuPanel?.SetActive(false);
            if (ServiceLocator.TryGet(out GameStateMachine machine))
            {
                machine.ChangeState<MainMenuState>();
            }
        }

        private void RefreshSlangList()
        {
            if (slangListText == null)
            {
                return;
            }

            int unlockedCount = 0;
            var sb = new StringBuilder();
            foreach (SlangEntry entry in allSlangEntries)
            {
                if (entry == null)
                {
                    continue;
                }

                bool unlocked = _narrativeProgress != null && _narrativeProgress.Slang.IsUnlocked(entry.Id);
                if (!unlocked)
                {
                    continue;
                }

                unlockedCount++;
                sb.AppendLine($"<b>{entry.Phrase}</b> - {entry.Meaning}");
            }

            slangListText.text = unlockedCount > 0
                ? sb.ToString()
                : "Jeszcze żadnego irlandzkiego powiedzonka. Posłuchaj uważnie w kuchni.";
        }

        private void ShowNotice(string message)
        {
            if (noticeText != null)
            {
                noticeText.text = message;
            }

            _noticeAge = 0f;
            // Over the room behind, a line needs its own dark ground - only while there is a line.
            if (noticeBackdrop != null)
            {
                noticeBackdrop.SetActive(!string.IsNullOrEmpty(message));
            }
        }

        /// <summary>
        /// Kuba works the board from the evening he took Declan's knife (chapter 2) or the board (chapter 3): "two hours
        /// of prep before every shift, the sink after". Staying at the sink keeps the shifts as they were.
        /// </summary>
        private bool WorksTheBoard => _narrativeProgress != null &&
                                      (_narrativeProgress.HasFlag("ch2_prep_cook") || _narrativeProgress.HasFlag("ch3_at_board"));

        /// <summary>Tonight's shift: a wedding, an inspection, the lasagne tray, St. Patrick's - or a plain one.</summary>
        private SpecialShift TodaysSpecial => _progress != null
            ? SpecialShifts.On(_progress.CurrentDate, _progress.DayNumber, _progress.Place)
            : SpecialShift.None;

        private void LabelWork()
        {
            string work = WorksTheBoard ? "Idź do pracy: deska i zlew" : "Idź do pracy";
            // What the night will be and what it asks for. The inspection is unannounced: the button does not give it away
            // (it shows the plain shift the inspection falls on).
            SpecialShift special = TodaysSpecial;
            string kind = special != SpecialShift.None && special != SpecialShift.Inspection ? SpecialShifts.Rules(special).Title : null;
            if (_progress != null && ShiftCatalog.Load() is ShiftCatalog catalog)
            {
                ScheduledShift plan = _progress.Ledger.PlanShift(special);
                ShiftDefinition shift = catalog.Find(plan.ArchetypeId);
                if (shift != null)
                {
                    int quota = ShiftSchedule.QuotaFor(shift.Quota, plan.Pace, SpecialShifts.Rules(special).PaceScale);
                    kind = $"{kind ?? shift.DisplayName} · norma {quota}";
                }
            }

            if (kind != null)
            {
                work += $"\n<size=70%>{kind}</size>";
            }

            SetLabel(goToWorkButton, work);
        }

        private void OnGoToWorkClicked()
        {
            ShiftPlan.PrepBeforeShift = WorksTheBoard;
            ShiftPlan.Special = TodaysSpecial;
            // The day's kind of shift, pace and deal - and what the dishwasher may do, given the days before.
            ShiftPlan.Schedule = _progress != null ? _progress.Ledger.PlanShift(TodaysSpecial) : (ScheduledShift?)null;
            if (ServiceLocator.TryGet(out GameStateMachine machine))
            {
                machine.ChangeState<KitchenShiftState>();
            }
        }

        /// <summary>A place on the map was tapped: it does what the hub's button for it does.</summary>
        private void OnMapPin(MapAction action)
        {
            switch (action)
            {
                case MapAction.Work:
                    OnGoToWorkClicked();
                    break;
                case MapAction.Pub:
                    OnPubClicked();
                    break;
                case MapAction.CallHome:
                    OnCallMomClicked();
                    break;
                case MapAction.SendMoney:
                    OnSendMoneyClicked();
                    break;
                case MapAction.Home:
                    ShowNotice("Tu Kuba śpi. I tu spędzi wieczór, jeśli nigdzie nie pójdzie.");
                    break;
            }
        }

        private void OnCallMomClicked()
        {
            if (_progress != null && !_progress.CallMom())
            {
                GameAudio.Play(GameSound.UiError, 0.4f);
            }
        }

        private void OnPubClicked()
        {
            if (_progress != null && !_progress.TryGoToPub())
            {
                GameAudio.Play(GameSound.UiError, 0.4f);
            }
        }

        private void OnRestClicked()
        {
            _progress?.TryRest();
        }

        private void OnDinnerClicked()
        {
            if (_progress != null && !_progress.TryCrewDinner())
            {
                GameAudio.Play(GameSound.UiError, 0.4f);
            }
            else
            {
                CatchCrewWords(); // an evening with the crew: their English sticks
            }
        }

        private void OnLetterClicked()
        {
            _progress?.TryWriteLetter();
        }

        private void OnSendMoneyClicked()
        {
            if (_progress != null && _progress.SendMoneyHome(_progress.Rules.SendHomeStep) > 0f)
            {
                GameAudio.Play(GameSound.Reward, 0.45f, 0.8f);
            }
        }

        /// <summary>Prices and amounts on the buttons come from the rules, so they cannot drift from what is charged.</summary>
        private void LabelActions()
        {
            if (_progress == null)
            {
                return;
            }

            string unit = PlaceMoney.Unit(_progress.Place);
            SetLabel(pubButton, $"{_progress.Rules.PubName}\n<size=60%>-{_progress.Rules.PubCost:0} {unit}</size>");
            SetLabel(sendMoneyButton, $"Wyślij {_progress.Rules.SendHomeStep:0} {unit} {_progress.Rules.SendHomeTo}");
        }

        private static void SetLabel(Button button, string text)
        {
            TMP_Text label = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
            if (label != null)
            {
                label.text = text;
            }
        }

        private void OnSlangToggleClicked()
        {
            if (slangPanel != null)
            {
                slangPanel.SetActive(!slangPanel.activeSelf);
            }
        }
    }
}
