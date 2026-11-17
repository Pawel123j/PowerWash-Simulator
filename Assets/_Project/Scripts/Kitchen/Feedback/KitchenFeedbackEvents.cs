using System;
using UnityEngine;
using Zmywak.Core.Data;
using Zmywak.Kitchen.Dishes;

namespace Zmywak.Kitchen.Feedback
{
    /// <summary>
    /// What just happened at the sink, for everything that only reacts to it - particles, sound,
    /// vibration, HUD flourishes. Gameplay code raises these and never knows who listens, so feedback can
    /// be added or tuned without touching the wash logic. Listeners subscribe in OnEnable and unsubscribe
    /// in OnDisable (static events outlive scenes).
    /// </summary>
    public static class KitchenFeedbackEvents
    {
        /// <summary>A dish was taken from the pile (it is on its way to the sink).</summary>
        public static event Action<DishController> Claimed;

        /// <summary>Leftovers swiped off.</summary>
        public static event Action<DishController> Scraped;

        /// <summary>Raised every frame water runs over the dish being rinsed.</summary>
        public static event Action<DishController> Rinsing;

        /// <summary>Raised every frame the brush removes dirt: world point, tool, rub intensity 0-1.</summary>
        public static event Action<DishController, Vector3, ToolDefinition, float> Scrubbing;

        /// <summary>The last of the dirt came off.</summary>
        public static event Action<DishController> Cleaned;

        /// <summary>One kind of dirt is gone from the dish (others may be left): the crust, the grease...</summary>
        public static event Action<DishController, DirtTypeDefinition> LayerCleared;

        /// <summary>A glass is being held to the light: its hidden streaks can now be found and rubbed off.</summary>
        public static event Action<DishController> HeldToLight;

        /// <summary>Lowered into the sink's water to soak.</summary>
        public static event Action<DishController> Soaked;

        /// <summary>Dropped into a rack slot.</summary>
        public static event Action<DishController, Vector3> Racked;

        /// <summary>Dropped off the rack - it springs back to the sink.</summary>
        public static event Action<DishController> DropMissed;

        /// <summary>A dish smashed (a hard knock in the pile, or off the counter onto the floor) at this position.</summary>
        public static event Action<Vector3> Broken;

        /// <summary>A pile dish hit something hard enough to be heard: position, impact speed, what it is made of (steel rings).</summary>
        public static event Action<Vector3, float, DishMaterial> Impact;

        /// <summary>The chef wants a dish on the rack now (an urgent order): the kitchen should hear and feel it.</summary>
        public static event Action<DishDefinition> UrgentOrder;

        internal static void RaiseClaimed(DishController dish) => Claimed?.Invoke(dish);
        internal static void RaiseScraped(DishController dish) => Scraped?.Invoke(dish);
        internal static void RaiseRinsing(DishController dish) => Rinsing?.Invoke(dish);
        internal static void RaiseScrubbing(DishController dish, Vector3 point, ToolDefinition tool, float intensity) =>
            Scrubbing?.Invoke(dish, point, tool, intensity);
        internal static void RaiseCleaned(DishController dish) => Cleaned?.Invoke(dish);
        internal static void RaiseLayerCleared(DishController dish, DirtTypeDefinition dirt) => LayerCleared?.Invoke(dish, dirt);
        internal static void RaiseHeldToLight(DishController dish) => HeldToLight?.Invoke(dish);
        internal static void RaiseSoaked(DishController dish) => Soaked?.Invoke(dish);
        internal static void RaiseRacked(DishController dish, Vector3 position) => Racked?.Invoke(dish, position);
        internal static void RaiseDropMissed(DishController dish) => DropMissed?.Invoke(dish);
        internal static void RaiseBroken(Vector3 position) => Broken?.Invoke(position);
        internal static void RaiseImpact(Vector3 position, float speed, DishMaterial material) => Impact?.Invoke(position, speed, material);
        internal static void RaiseUrgentOrder(DishDefinition dish) => UrgentOrder?.Invoke(dish);
    }
}
