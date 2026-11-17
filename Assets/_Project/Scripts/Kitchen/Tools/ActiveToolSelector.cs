using System;
using System.Collections.Generic;
using UnityEngine;
using Zmywak.Core.Data;
using Zmywak.Core.Settings;
using Zmywak.Kitchen.Dishes;

namespace Zmywak.Kitchen.Tools
{
    /// <summary>
    /// Scene-local record of which wash tool the player has selected (set by the tool bar UI) and which tools are on the bar
    /// at all: the first days bring the scraper and the spray in later (<see cref="MechanicUnlocks"/>), and the guided first
    /// plate holds the bar to the one tool it is teaching. It also knows the tool the dirt in the sink wants right now
    /// (<see cref="Suggested"/>, the button that pulses) - and with the easier-sink setting on, it picks that tool by itself.
    /// </summary>
    public class ActiveToolSelector : MonoBehaviour
    {
        public static ActiveToolSelector Instance { get; private set; }

        [SerializeField] private ToolDefinition[] availableTools;

        private readonly List<ToolDefinition> _onTheBar = new List<ToolDefinition>();
        private Func<ToolDefinition, bool> _unlocked = _ => true;
        private WashStation _washStation;
        private DishController _assistedDish;
        private ToolDefinition _assistedFor;

        public ToolDefinition CurrentTool { get; private set; }

        /// <summary>The tools on the bar (unlocked so far).</summary>
        public IReadOnlyList<ToolDefinition> AvailableTools => _onTheBar;

        /// <summary>Every tool the kitchen has, unlocked or not.</summary>
        public IReadOnlyList<ToolDefinition> AllTools => availableTools;

        /// <summary>While the guided first plate teaches one tool, only it can be picked (null: any).</summary>
        public ToolDefinition OnlyTool { get; set; }

        /// <summary>The tool the dirt in the sink wants now, if the one in hand does not suit it (null otherwise).</summary>
        public ToolDefinition Suggested { get; private set; }

        /// <summary>The tool in hand does not work on the dirt in front of it.</summary>
        public bool WrongToolInHand { get; private set; }

        /// <summary>The selection changed (by the bar, or by the easier-sink setting).</summary>
        public event Action<ToolDefinition> Changed;

        private void Awake()
        {
            Instance = this;
            Refresh();
            if (_onTheBar.Count > 0)
            {
                CurrentTool = _onTheBar[0];
            }
        }

        private void Start()
        {
            _washStation = FindAnyObjectByType<WashStation>();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>Which tools are on the bar (set by the shift from the day's unlocks).</summary>
        public void SetUnlocked(Func<ToolDefinition, bool> unlocked)
        {
            _unlocked = unlocked ?? (_ => true);
            Refresh();
            if (CurrentTool != null && !_onTheBar.Contains(CurrentTool))
            {
                Select(_onTheBar.Count > 0 ? _onTheBar[0] : null);
            }
        }

        public bool IsOnTheBar(ToolDefinition tool) => tool != null && _onTheBar.Contains(tool);

        /// <summary>Whether a tool's button can be pressed now (on the bar, and not held back by the guided plate).</summary>
        public bool CanPick(ToolDefinition tool) => IsOnTheBar(tool) && (OnlyTool == null || OnlyTool == tool);

        private void Refresh()
        {
            _onTheBar.Clear();
            if (availableTools == null)
            {
                return;
            }

            foreach (ToolDefinition tool in availableTools)
            {
                if (tool != null && _unlocked(tool))
                {
                    _onTheBar.Add(tool);
                }
            }
        }

        public void SelectTool(ToolDefinition tool)
        {
            if (tool == null || !CanPick(tool))
            {
                return;
            }

            Select(tool);
        }

        private void Select(ToolDefinition tool)
        {
            if (CurrentTool == tool)
            {
                return;
            }

            CurrentTool = tool;
            Changed?.Invoke(tool);
        }

        private void Update()
        {
            DishController dish = _washStation != null ? _washStation.Occupant : null;
            bool scrubbing = dish != null && !dish.IsMoving && dish.CurrentStage == WashStage.AwaitingScrub;
            ToolDefinition wanted = scrubbing ? dish.SuitingTool(_onTheBar) : null;
            WrongToolInHand = scrubbing && wanted != null && !dish.ToolSuits(CurrentTool);
            Suggested = WrongToolInHand ? wanted : null;

            // The easier sink: the right tool picked by itself as the dish comes up, and again when a layer is done -
            // only once per change, so the player can still take another tool by hand.
            if (AccessibilitySettings.ToolAssist && scrubbing && wanted != null && (dish != _assistedDish || wanted != _assistedFor))
            {
                _assistedDish = dish;
                _assistedFor = wanted;
                if (!dish.ToolSuits(CurrentTool) && CanPick(wanted))
                {
                    Select(wanted);
                }
            }
        }
    }
}
