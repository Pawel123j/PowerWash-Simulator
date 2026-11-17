using System.Collections.Generic;

namespace Zmywak.Core.Save
{
    /// <summary>
    /// Which of the kitchen's lessons Kuba has had - the guided first plate, the first-time cards (the glass, the soak, the
    /// chef's urgent call...) - kept in the save so each is taught once per life (a new save slot teaches again). Read and
    /// written straight through the save file, touching only its own list, like every persistent part of the game.
    /// </summary>
    public static class TutorialProgress
    {
        /// <summary>The guided first plate (the first shift's opening).</summary>
        public const string FirstDish = "first_dish";

        public static bool Has(string tip)
        {
            SaveData data = SaveSystem.Load();
            return data?.seenTips != null && data.seenTips.Contains(tip);
        }

        public static void Mark(string tip)
        {
            if (string.IsNullOrEmpty(tip))
            {
                return;
            }

            SaveData data = SaveSystem.Load() ?? new SaveData();
            data.seenTips ??= new List<string>();
            if (data.seenTips.Contains(tip))
            {
                return;
            }

            data.seenTips.Add(tip);
            SaveSystem.Save(data);
        }
    }
}
