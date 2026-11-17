using UnityEngine;
using Zmywak.Core.Data;

namespace Zmywak.Core.Events.Channels
{
    [CreateAssetMenu(menuName = "Zmywak/Events/Shift Completed Channel", fileName = "ShiftCompletedEventChannel")]
    public class ShiftCompletedEventChannel : GameEventChannel<ShiftResult>
    {
    }
}
