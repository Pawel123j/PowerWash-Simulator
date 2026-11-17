using System.Collections.Generic;
using UnityEngine;
using Zmywak.Core.Data;

namespace Zmywak.Kitchen.Dishes
{
    /// <summary>
    /// How a dish behaves in the pile by what it is made of. Glass and china break when they hit something hard
    /// enough - glass at a smaller knock - while steel only rings; steel also slides more readily and bounces a
    /// little more. Speeds are along the contact's normal, in scene units a second: the pile collapses at about 1.4
    /// units tall, and a dish thrown off its top lands at about 5, so the top of a collapsing pile breaks what can
    /// break, while a glass that slips half a unit down the heap survives.
    /// </summary>
    public static class DishPhysics
    {
        public const float GlassBreakSpeed = 3.2f;
        public const float CeramicBreakSpeed = 3.8f;

        private static readonly Dictionary<DishMaterial, PhysicsMaterial> Surfaces = new Dictionary<DishMaterial, PhysicsMaterial>();

        public static bool CanBreak(DishMaterial material) => material != DishMaterial.Steel;

        /// <summary>The knock, along the contact normal, that breaks it (infinite for steel).</summary>
        public static float BreakSpeed(DishMaterial material)
        {
            switch (material)
            {
                case DishMaterial.Glass: return GlassBreakSpeed;
                case DishMaterial.Ceramic: return CeramicBreakSpeed;
                default: return float.PositiveInfinity;
            }
        }

        public static bool Breaks(DishMaterial material, float normalImpactSpeed) => normalImpactSpeed >= BreakSpeed(material);

        /// <summary>How far it can fall onto something hard and survive: v^2 / 2g with Unity's gravity.</summary>
        public static float SafeDropHeight(DishMaterial material)
        {
            float speed = BreakSpeed(material);
            return float.IsInfinity(speed) ? float.PositiveInfinity : speed * speed / (2f * Mathf.Abs(Physics.gravity.y));
        }

        /// <summary>
        /// The knock of a contact along its normal - a dish landing flat counts in full, one grazing past barely at
        /// all. The largest over the contact's points.
        /// </summary>
        public static float NormalImpactSpeed(Vector3 relativeVelocity, IReadOnlyList<Vector3> contactNormals)
        {
            float speed = 0f;
            foreach (Vector3 normal in contactNormals)
            {
                speed = Mathf.Max(speed, Mathf.Abs(Vector3.Dot(relativeVelocity, normal.normalized)));
            }

            return speed;
        }

        /// <summary>Static friction, dynamic friction and bounciness of the surface.</summary>
        public static (float staticFriction, float dynamicFriction, float bounciness) Surface(DishMaterial material)
        {
            // Grippy enough that the pile settles (PhysX has no rolling resistance; DishController damps slow
            // rolling too) - glaze a touch less than unglazed, glass less again, steel slides and rings.
            switch (material)
            {
                case DishMaterial.Glass: return (0.7f, 0.55f, 0.06f);
                case DishMaterial.Steel: return (0.6f, 0.45f, 0.12f);
                default: return (0.85f, 0.7f, 0.04f);
            }
        }

        /// <summary>The shared physics material for a dish of this material.</summary>
        public static PhysicsMaterial SurfaceMaterial(DishMaterial material)
        {
            if (!Surfaces.TryGetValue(material, out PhysicsMaterial surface) || surface == null)
            {
                (float staticFriction, float dynamicFriction, float bounciness) = Surface(material);
                surface = new PhysicsMaterial($"Dish{material}")
                {
                    staticFriction = staticFriction,
                    dynamicFriction = dynamicFriction,
                    bounciness = bounciness,
                    frictionCombine = PhysicsMaterialCombine.Average,
                    bounceCombine = PhysicsMaterialCombine.Minimum // a plate dropped on a pan does not ring off like steel on steel
                };
                Surfaces[material] = surface;
            }

            return surface;
        }
    }
}
