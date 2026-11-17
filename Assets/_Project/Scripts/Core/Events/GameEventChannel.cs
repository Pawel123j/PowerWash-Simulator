using System;
using UnityEngine;

namespace Zmywak.Core.Events
{
    /// <summary>
    /// Base for a ScriptableObject-based event channel. Publishers Raise(), subscribers add to OnRaised.
    /// Lets Kitchen/Meta/UI/Narrative talk to each other without referencing each other's assemblies.
    /// Concrete non-generic subclasses (e.g. ShiftCompletedEventChannel) are what get turned into assets.
    /// </summary>
    public abstract class GameEventChannel<T> : ScriptableObject
    {
        public event Action<T> OnRaised;

        public void Raise(T payload)
        {
            OnRaised?.Invoke(payload);
        }
    }
}
