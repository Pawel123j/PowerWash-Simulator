using System.Collections.Generic;

namespace Zmywak.Narrative.SlangDictionary
{
    /// <summary>Which slang ids the player has unlocked so far. Plain C# so it is unit-testable.</summary>
    public class SlangCollectionController
    {
        private readonly HashSet<string> _unlocked = new HashSet<string>();

        public IReadOnlyCollection<string> UnlockedIds => _unlocked;

        public void Unlock(string slangId)
        {
            if (!string.IsNullOrEmpty(slangId))
            {
                _unlocked.Add(slangId);
            }
        }

        public bool IsUnlocked(string slangId)
        {
            return !string.IsNullOrEmpty(slangId) && _unlocked.Contains(slangId);
        }
    }
}
