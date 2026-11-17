using System;
using System.Collections.Generic;
using UnityEngine;
using Zmywak.Core.Data;
using Zmywak.Kitchen.Dishes;
using Zmywak.Kitchen.Rack;

namespace Zmywak.Kitchen.Dishwasher
{
    /// <summary>
    /// Runs the ~20s wash cycle. Starting it takes the whole load off the rack - the dishes slide into the
    /// machine and the rack is free for the next load while this one washes. The machine can break down
    /// mid-cycle: the cycle stops where it is until the repair is finished, then carries on.
    /// </summary>
    public class DishwasherController : MonoBehaviour
    {
        private const float EarliestBreakdown = 0.3f; // fraction of the cycle
        private const float LatestBreakdown = 0.7f;
        private const float SlideInSeconds = 0.4f;

        [SerializeField] private RackGrid rackGrid;
        [SerializeField] private float cycleSeconds = 20f;

        private DishwasherBreakdownSchedule _schedule = DishwasherBreakdownSchedule.Never;
        private float _elapsed;
        private float _breakAt = -1f;
        private int _cyclesStarted;
        private DishwasherFault? _lastFault;
        private readonly List<DishController> _load = new List<DishController>();
        private readonly List<Vector3> _loadStartPositions = new List<Vector3>();
        private readonly List<Vector3> _loadStartScales = new List<Vector3>();
        private float _slideIn = 1f;

        public bool IsRunning { get; private set; }
        public float CycleSeconds => cycleSeconds;

        /// <summary>0 at the start of a running cycle, 1 when it finishes; 0 while idle. Holds still while broken.</summary>
        public float Progress01 => IsRunning && cycleSeconds > 0f ? Mathf.Clamp01(_elapsed / cycleSeconds) : 0f;

        /// <summary>The repair in progress, or null while the machine works.</summary>
        public DishwasherRepair ActiveRepair { get; private set; }

        public bool IsBroken => ActiveRepair != null;

        /// <summary>Dishes inside the machine this cycle.</summary>
        public int LoadCount => _load.Count;

        /// <summary>Times the machine has broken down since the scene began (one shift).</summary>
        public int Breakdowns { get; private set; }

        /// <summary>Fired with the definitions of every dish washed this cycle (chef orders match against this).</summary>
        public event Action<IReadOnlyList<DishDefinition>> OnCycleCompleted;

        /// <summary>Fired as a cycle starts, with the load and how full the rack was (0-1).</summary>
        public event Action<IReadOnlyList<DishDefinition>, float> OnCycleStarted;

        public event Action<DishwasherFault> OnBrokeDown;
        public event Action OnRepaired;

        /// <summary>Which cycles break down this shift (set by the shift; by default the machine never fails).</summary>
        public void ConfigureBreakdowns(DishwasherBreakdownSchedule schedule)
        {
            _schedule = schedule ?? DishwasherBreakdownSchedule.Never;
        }

        public bool StartCycle()
        {
            if (IsRunning || IsBroken || rackGrid == null || rackGrid.DishCount == 0)
            {
                return false;
            }

            float fill = rackGrid.FillRatio;
            _load.Clear();
            _load.AddRange(rackGrid.TakeAll());
            _loadStartPositions.Clear();
            _loadStartScales.Clear();
            var loaded = new List<DishDefinition>();
            foreach (DishController dish in _load)
            {
                _loadStartPositions.Add(dish.transform.position);
                _loadStartScales.Add(dish.transform.localScale);
                loaded.Add(dish.Definition);
            }

            _slideIn = 0f;
            IsRunning = true;
            _elapsed = 0f;
            _cyclesStarted++;
            _breakAt = _schedule.WillBreak(_cyclesStarted, UnityEngine.Random.value)
                ? UnityEngine.Random.Range(EarliestBreakdown, LatestBreakdown)
                : -1f;
            OnCycleStarted?.Invoke(loaded, fill);
            return true;
        }

        /// <summary>
        /// Break the machine now - running or idle (an idle broken machine will not start). The scheduled
        /// breakdowns go through here; story events and tests can call it directly.
        /// </summary>
        public void BreakDown(DishwasherFault fault)
        {
            if (IsBroken)
            {
                return;
            }

            _lastFault = fault;
            Breakdowns++;
            ActiveRepair = DishwasherRepair.Create(fault, UnityEngine.Random.Range(int.MinValue, int.MaxValue));
            OnBrokeDown?.Invoke(fault);
        }

        private void Update()
        {
            SlideLoadIn();

            if (IsBroken)
            {
                if (!ActiveRepair.IsDone)
                {
                    return; // the cycle waits where it stopped
                }

                ActiveRepair = null;
                OnRepaired?.Invoke();
            }

            if (!IsRunning)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            if (_breakAt >= 0f && Progress01 >= _breakAt)
            {
                _breakAt = -1f;
                BreakDown(PickFault());
                return;
            }

            if (_elapsed >= cycleSeconds)
            {
                CompleteCycle();
            }
        }

        /// <summary>The load glides from the rack into the machine's door and shrinks out of sight.</summary>
        private void SlideLoadIn()
        {
            if (_slideIn >= 1f)
            {
                return;
            }

            _slideIn = Mathf.Min(1f, _slideIn + Time.deltaTime / SlideInSeconds);
            float t = _slideIn * _slideIn * (3f - 2f * _slideIn);
            Vector3 door = transform.position + new Vector3(0f, 0.45f, -0.5f);
            for (int i = 0; i < _load.Count; i++)
            {
                DishController dish = _load[i];
                if (dish == null)
                {
                    continue;
                }

                dish.transform.position = Vector3.Lerp(_loadStartPositions[i], door, t);
                dish.transform.localScale = Vector3.Lerp(_loadStartScales[i], _loadStartScales[i] * 0.25f, t);
                if (_slideIn >= 1f)
                {
                    dish.gameObject.SetActive(false); // washing, out of sight, until the cycle ends
                }
            }
        }

        /// <summary>Any fault but the last one, so two breakdowns in a shift are two different repairs.</summary>
        private DishwasherFault PickFault()
        {
            var faults = (DishwasherFault[])Enum.GetValues(typeof(DishwasherFault));
            DishwasherFault fault;
            do
            {
                fault = faults[UnityEngine.Random.Range(0, faults.Length)];
            }
            while (_lastFault.HasValue && fault == _lastFault.Value && faults.Length > 1);

            return fault;
        }

        private void CompleteCycle()
        {
            var washed = new List<DishDefinition>();
            foreach (DishController dish in _load)
            {
                if (dish == null)
                {
                    continue;
                }

                dish.ConfirmDone();
                washed.Add(dish.Definition);
                Destroy(dish.gameObject);
            }

            _load.Clear();

            IsRunning = false;
            OnCycleCompleted?.Invoke(washed);
        }
    }
}
