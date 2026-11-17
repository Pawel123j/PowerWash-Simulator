using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Zmywak.Core.Data;

namespace Zmywak.Kitchen.Dishes
{
    /// <summary>
    /// The dirt of the next dish of each kind, made ahead on a worker thread. Working out a dish's dirt masks (noise,
    /// splotches, tide lines - tens of milliseconds at full sharpness) used to happen the moment the dish dropped onto
    /// the pile, a hitch every few seconds on a phone. Now each kind of the shift has one canvas waiting; a dish takes
    /// it and the next is started at once. The thread gets only plain numbers (the coverages, the patterns, the seed);
    /// if a canvas is not ready, or the thread failed, the dish makes its own as before.
    /// </summary>
    public static class DirtPrewarm
    {
        private sealed class Pending
        {
            public Task<DirtCanvas> Task;
            public int Resolution;
        }

        private static readonly Dictionary<DishDefinition, Pending> Ready = new Dictionary<DishDefinition, Pending>();

        /// <summary>Canvases taken ready-made, and dishes that had to make their own - for tests and the log.</summary>
        public static int Hits { get; private set; }

        public static int Misses { get; private set; }

        /// <summary>Starts a canvas for the next dish of this kind (nothing if one is already on its way).</summary>
        public static void Prepare(DishDefinition definition, int resolution)
        {
            if (definition == null || Ready.TryGetValue(definition, out Pending pending) && pending.Resolution == resolution && !pending.Task.IsFaulted)
            {
                return;
            }

            // Everything the thread needs, read here on the main thread.
            var coverages = new List<float>();
            var patterns = new List<DirtPattern>();
            foreach (DirtLayer layer in definition.DirtLayers)
            {
                coverages.Add(Mathf.Clamp01(layer.coverage));
                patterns.Add(layer.dirtType != null ? layer.dirtType.Pattern : DirtPattern.Splotches);
            }

            bool circular = definition.PlaceholderPrimitive == PrimitiveType.Cylinder;
            Vector2? wallBand = circular ? DishMeshFactory.WallBand(definition.Shape) : null;
            int seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            Ready[definition] = new Pending
            {
                Resolution = resolution,
                Task = Task.Run(() => new DirtCanvas(coverages, seed, circular, resolution, patterns, wallBand))
            };
        }

        /// <summary>The canvas waiting for a dish of this kind at this sharpness, if it is ready (null otherwise).</summary>
        public static DirtCanvas Take(DishDefinition definition, int resolution)
        {
            if (definition == null || !Ready.TryGetValue(definition, out Pending pending) || pending.Resolution != resolution ||
                !pending.Task.IsCompleted)
            {
                Misses++;
                return null;
            }

            Ready.Remove(definition);
            if (pending.Task.IsFaulted || pending.Task.IsCanceled)
            {
                Debug.LogWarning($"DirtPrewarm: a {definition.Id}'s dirt could not be made ahead ({pending.Task.Exception?.GetBaseException().Message}); making it now.");
                Misses++;
                return null;
            }

            Hits++;
            return pending.Task.Result;
        }

        /// <summary>Drops what is waiting (a new kitchen starts afresh).</summary>
        public static void Clear()
        {
            Ready.Clear();
        }
    }
}
