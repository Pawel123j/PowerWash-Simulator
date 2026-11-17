using System;
using System.Collections.Generic;

namespace Zmywak.Core.Services
{
    /// <summary>
    /// Minimal process-wide registry for cross-scene services (game state machine, save system, etc).
    /// Cleared automatically on domain reload so Play Mode sessions never see stale references.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> Services = new Dictionary<Type, object>();

#if UNITY_EDITOR
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnDomainReload()
        {
            Services.Clear();
        }
#endif

        public static void Register<T>(T service) where T : class
        {
            Services[typeof(T)] = service;
        }

        public static void Unregister<T>() where T : class
        {
            Services.Remove(typeof(T));
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            if (Services.TryGetValue(typeof(T), out object raw))
            {
                service = (T)raw;
                return true;
            }

            service = null;
            return false;
        }

        public static T Get<T>() where T : class
        {
            if (TryGet(out T service))
            {
                return service;
            }

            throw new InvalidOperationException($"Service of type {typeof(T).Name} is not registered.");
        }
    }
}
