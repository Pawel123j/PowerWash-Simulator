using System.Collections.Generic;
using UnityEngine;
using Zmywak.Core.Diagnostics;

namespace Zmywak.Core.Data
{
    /// <summary>
    /// Every kind of shift the schedule can plan (<see cref="ShiftSchedule.Archetypes"/>), loaded from Resources: the hub
    /// reads a shift's name and quota from it for the "go to work" button, the kitchen the whole definition. Built by the
    /// setup with the shift assets.
    /// </summary>
    [CreateAssetMenu(menuName = "Zmywak/Shift Catalog")]
    public class ShiftCatalog : ScriptableObject
    {
        public const string ResourceName = "ShiftCatalog";

        [SerializeField] private List<ShiftDefinition> shifts = new List<ShiftDefinition>();

        private static ShiftCatalog _loaded;

        public IReadOnlyList<ShiftDefinition> Shifts => shifts;

        /// <summary>The catalog in Resources (looked up once); null when the project has none.</summary>
        public static ShiftCatalog Load()
        {
            if (_loaded == null)
            {
                using var _load = StartupTimeline.Measure("assets: Resources.Load ShiftCatalog");
                _loaded = Resources.Load<ShiftCatalog>(ResourceName);
            }

            return _loaded;
        }

        public ShiftDefinition Find(string id)
        {
            foreach (ShiftDefinition shift in shifts)
            {
                if (shift != null && shift.Id == id)
                {
                    return shift;
                }
            }

            return null;
        }

        public void SetShifts(IEnumerable<ShiftDefinition> definitions)
        {
            shifts.Clear();
            shifts.AddRange(definitions);
        }
    }
}
