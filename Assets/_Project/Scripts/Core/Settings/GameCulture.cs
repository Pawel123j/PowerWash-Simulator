using System.Globalization;
using UnityEngine;

namespace Zmywak.Core.Settings
{
    /// <summary>
    /// The game speaks Polish, and writes its numbers the Polish way on every phone: "20,00 EUR", "awans 40%", "1,2x".
    /// Interpolated strings and ToString follow the thread's culture - the phone's language - so an English phone wrote
    /// "20.00 EUR" and "awans 40 %" between the Polish words. Set before the first scene, for the main thread and every
    /// thread started after it. Nothing reads numbers back with the current culture (the save is JSON, which is
    /// culture-free, and the rest names the invariant culture), so only what is shown changes.
    /// </summary>
    public static class GameCulture
    {
        private static CultureInfo _polish;

        /// <summary>pl-PL, or the invariant culture should a runtime ever lack the Polish tables.</summary>
        public static CultureInfo Polish
        {
            get
            {
                if (_polish == null)
                {
                    try
                    {
                        _polish = CultureInfo.GetCultureInfo("pl-PL");
                    }
                    catch (CultureNotFoundException)
                    {
                        _polish = CultureInfo.InvariantCulture;
                    }
                }

                return _polish;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Apply()
        {
            CultureInfo.DefaultThreadCurrentCulture = Polish;
            CultureInfo.CurrentCulture = Polish;
        }
    }
}
