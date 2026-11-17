using System;
using UnityEngine;
using Zmywak.Core.Data;
using Zmywak.Core.Events.Channels;
using Zmywak.Core.Save;
using Zmywak.Core.Services;
using Zmywak.Core.Settings;
using Zmywak.Core.Telemetry;
using Zmywak.Meta.Economy;
using Zmywak.Meta.Stats;

namespace Zmywak.Meta.Progress
{
    /// <summary>
    /// Persistent (DontDestroyOnLoad) home of everything that survives between shifts. The rules and the
    /// state live in a <see cref="LifeLedger"/>; this component feeds it the finished shifts (from the same
    /// <see cref="ShiftCompletedEventChannel"/> asset the Kitchen scene raises, whichever scene is loaded),
    /// the hub's buttons and the story, and autosaves after every change.
    /// </summary>
    public class MetaProgressController : MonoBehaviour, IPlayerCondition
    {
        [SerializeField] private ShiftCompletedEventChannel shiftCompletedChannel;
        [SerializeField] private ExchangeRateTableAsset exchangeRateTable;

        private LifeLedger _ledger;
        private int _lifeGeneration; // SaveSystem.LifeGeneration when this life was loaded (see there)
        private bool _lifeBegun; // false until there is a life to keep: a save loaded, the hub reached, or something done in the week
        private bool _shiftPaid; // a shift was just paid: the next save that lands also clears the big dishwasher's owed credit

        public LifeLedger Ledger => _ledger;
        public LifeRules Rules => _ledger.Rules;
        public PlayerStatsController Stats => _ledger.Stats;
        public float Energy => _ledger.Stats.Energy;
        public float Integration => _ledger.Stats.Integration;
        public CareerRecord WorkRecord => _ledger.WorkRecord;
        public float Wallet => _ledger.Wallet;

        /// <summary>Where Kuba lives - and so the money the wallet and the kitchen's pay are in.</summary>
        public Place Place => _ledger.Place;

        /// <summary>His rank at the sink (IPlayerCondition: the kitchen pays it).</summary>
        public int Rank => _ledger.Rank;
        public int DayNumber => _ledger.DayNumber;
        public DateTime CurrentDate => _ledger.CurrentDate;
        public DateTime Today => _ledger.CurrentDate; // IPlayerCondition: the kitchen pays the wage of the day, where Kuba is

        public event Action OnProgressChanged;
        public event Action<string> OnNotice; // flavor lines: "Jesz coś ciepłego?", week-settlement summary, ...

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            ServiceLocator.Register(this);
            ServiceLocator.Register<IPlayerCondition>(this); // the kitchen reads how tired Kuba is through this

            _ledger = new LifeLedger(LifeRules.Default, new ExchangeRateService(exchangeRateTable));
            _lifeGeneration = SaveSystem.LifeGeneration;
            SaveData data = SaveSystem.Load();
            _lifeBegun = data != null;
            if (data != null)
            {
                _ledger.LoadFrom(data);
            }

            _ledger.OnNotice += RelayNotice;
            if (shiftCompletedChannel != null)
            {
                shiftCompletedChannel.OnRaised += OnShiftCompleted;
            }
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<MetaProgressController>();
            ServiceLocator.Unregister<IPlayerCondition>();
            _ledger.OnNotice -= RelayNotice;
            if (shiftCompletedChannel != null)
            {
                shiftCompletedChannel.OnRaised -= OnShiftCompleted;
            }
        }

        private void RelayNotice(string message)
        {
            OnNotice?.Invoke(message);
        }

        // Every change is saved as it happens; these also flush when the phone backgrounds the app - on mobile
        // OnApplicationQuit often never comes, the app is simply killed while paused.
        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                SaveIfLifeBegun();
                AccessibilitySettings.Flush(); // a sound level set a moment ago, before the app may be killed
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                SaveIfLifeBegun();
            }
        }

        /// <summary>The app being sent away must not make a save of a game nobody has started: the menu would offer to continue it.</summary>
        private void SaveIfLifeBegun()
        {
            if (_lifeBegun)
            {
                Save();
            }
        }

        /// <summary>
        /// A life just begun has nothing of its week on disk until something changes it. The hub calls this on arrival, so
        /// the starting wallet and day are there before the story writes its first flags next to them (the menu read such a
        /// save as "Dzień 1 · ?", and loading it gave Kuba 0 EUR and no energy). Not at boot: a first-time player who never
        /// started a game must not find one to continue.
        /// </summary>
        public void EnsureOnDisk()
        {
            _lifeBegun = true; // the hub is reached: this is a life now
            SaveData data = SaveSystem.Load();
            if (data == null || data.dayNumber <= 0)
            {
                Save();
            }
        }

        private void Save()
        {
            if (_lifeGeneration != SaveSystem.LifeGeneration)
            {
                return; // its slot was deleted or another was picked since: this life must not write itself back
            }

            // Read-modify-write: other persistent controllers (e.g. narrative progress) share this
            // same flat file, each only touching their own fields, so we must not blind-overwrite it.
            SaveData data = SaveSystem.Load() ?? new SaveData();
            _ledger.WriteTo(data);
            if (_shiftPaid)
            {
                // The dishes taken out of the big dishwasher were counted in the shift just paid: in this very save (the wage and
                // the end of their credit together) they stop being owed. A kill before it leaves them for the next shift.
                data.bigDishwasherPendingDishes = 0;
            }

            if (SaveSystem.Save(data))
            {
                _shiftPaid = false;
            }
        }

        private void Changed()
        {
            _lifeBegun = true;
            OnProgressChanged?.Invoke();
            Save();
        }

        private void OnShiftCompleted(ShiftResult result)
        {
            float billsPaid = 0f;
            void OnSettled(float before, float after) => billsPaid = before - after;
            _ledger.OnWeekSettled += OnSettled;
            _ledger.CompleteShift(result);
            _ledger.OnWeekSettled -= OnSettled;
            _shiftPaid = true;

            Log("shift_paid", result.Wage);
            if (billsPaid > 0f)
            {
                Log("week_settled", -billsPaid);
            }

            Changed();
        }

        public bool CallMom()
        {
            bool called = _ledger.TryCallMom();
            if (called)
            {
                Log("call_mom");
                Changed();
            }

            return called;
        }

        public bool TryGoToPub()
        {
            bool went = _ledger.TryGoToPub();
            if (went)
            {
                Log("pub", -Rules.PubCost);
                Changed();
            }

            return went;
        }

        /// <summary>An evening's rest at home.</summary>
        public bool TryRest() => Act(_ledger.TryRest(), "rest");

        /// <summary>Dinner with the crew, when they ask.</summary>
        public bool TryCrewDinner()
        {
            float before = Wallet;
            bool went = _ledger.TryCrewDinner();
            if (went)
            {
                Log("crew_dinner", Wallet - before);
                Changed();
            }

            return went;
        }

        /// <summary>A letter home, when Kuba is homesick enough to write one.</summary>
        public bool TryWriteLetter() => Act(_ledger.TryWriteLetter(), "letter");

        /// <summary>Buys something once (a mattress, a dictionary, a mobile).</summary>
        public bool TryBuy(Purchase purchase)
        {
            float before = Wallet;
            bool bought = _ledger.TryBuy(purchase);
            if (bought)
            {
                Log("buy_" + purchase.ToString().ToLowerInvariant(), Wallet - before);
                Changed();
            }

            return bought;
        }

        private bool Act(bool done, string action)
        {
            if (done)
            {
                Log(action);
                Changed();
            }

            return done;
        }

        /// <summary>A chapter that begins years later moves the calendar there (<see cref="LifeLedger.SkipTo"/>).</summary>
        public bool SkipTo(DateTime date)
        {
            if (!_ledger.SkipTo(date))
            {
                return false;
            }

            Log("time_skip");
            Changed();
            return true;
        }

        /// <summary>Kuba moves (the epilogue sends him where chapter 5's choice took him): the place's rules and money
        /// from now on (<see cref="LifeLedger.MoveTo"/>).</summary>
        public bool MoveTo(Place place)
        {
            if (!_ledger.MoveTo(place))
            {
                return false;
            }

            Log("move");
            Changed();
            return true;
        }

        /// <summary>Whether a story choice costing this many euros leaves the week's bills untouched
        /// (<see cref="LifeLedger.CanSpareEuros"/>).</summary>
        public bool CanSpareEuros(float euros) => _ledger.CanSpareEuros(euros);

        public bool CanSaveCurrentLife => _lifeGeneration == SaveSystem.LifeGeneration;

        /// <summary>Evaluate the existing economy rules on a draft. A disk failure cannot leave a reward in RAM.</summary>
        public void PrepareStorySnapshot(SaveData data, StoryOutcome outcome)
        {
            _ledger.WriteTo(data);
            if (_shiftPaid) data.bigDishwasherPendingDishes = 0;
            var draft = new LifeLedger(Rules, new ExchangeRateService(exchangeRateTable));
            draft.LoadFrom(data);
            draft.ApplyStoryOutcome(outcome);
            draft.WriteTo(data);
        }

        /// <summary>Called after the shared dialogue snapshot was saved; does not write again.</summary>
        public void AcceptStorySnapshot(SaveData data, bool appliedOutcome)
        {
            float before = Wallet;
            _ledger.LoadFrom(data);
            _lifeBegun = true;
            _shiftPaid = false;
            if (appliedOutcome)
            {
                Log("story", Wallet - before);
                float change = Wallet - before;
                if (change != 0f) PostNotice(change > 0f ? $"+{_ledger.Format(change)}" : _ledger.Format(change));
            }
            OnProgressChanged?.Invoke();
        }

        /// <summary>What a story moment does to Kuba's week (see <see cref="LifeLedger.ApplyStoryOutcome"/>).</summary>
        public void ApplyStoryOutcome(StoryOutcome outcome)
        {
            if (outcome.IsEmpty)
            {
                return;
            }

            float before = Wallet;
            _ledger.ApplyStoryOutcome(outcome);
            Log("story", Wallet - before);
            Changed();
        }

        /// <summary>The local gameplay diary: what happened and where Kuba's life stands after it.</summary>
        private void Log(string action, float amount = 0f)
        {
            GameplayLog.Append(new LifeLogEntry
            {
                action = action,
                day = DayNumber,
                currency = PlaceMoney.Currency(Place),
                amountEuros = amount,
                walletEuros = Wallet,
                energy = Stats.Energy,
                homesickness = Stats.Homesickness,
                integration = Stats.Integration,
                sentHomeEuros = _ledger.SentHomeEuros
            });
        }

        /// <summary>A line for the hub's notice strip from outside the week's own events (the story, for one).</summary>
        public void PostNotice(string message)
        {
            OnNotice?.Invoke(message);
        }

        public float SendMoneyHome(float amount)
        {
            float before = Wallet;
            float pln = _ledger.SendMoneyHome(amount);
            if (pln > 0f)
            {
                Log("send_home", Wallet - before);
                Changed();
            }

            return pln;
        }
    }
}
