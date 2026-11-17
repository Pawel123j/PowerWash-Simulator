// Reconstructed on 2026-11-17 by decompiling Library/ScriptAssemblies/Zmywak.Tests.EditMode.dll (built 2026-10-11):
// the original source was lost from disk. Behaviour matches that build; original comments are gone.
using System;
using NUnit.Framework;
using UnityEngine;
using Zmywak.Core.Data;
using Zmywak.Kitchen.Dishes;

namespace Zmywak.Tests
{
    public class DishMeshFactoryTests
    {
        private static readonly DishShape[] Shapes = (DishShape[])Enum.GetValues(typeof(DishShape));

        [Test]
        public void EveryShape_FitsTheSharedFootprint_AndIsBuilt()
        {
            DishShape[] shapes = Shapes;
            foreach (DishShape shape in shapes)
            {
                Mesh mesh = DishMeshFactory.Get(shape);
                Assert.Greater(mesh.triangles.Length, 0, $"{shape} has no triangles");
                Bounds bounds = mesh.bounds;
                Assert.LessOrEqual(Mathf.Max(Mathf.Abs(bounds.min.x), bounds.max.x), 0.5001f, $"{shape} is wider than the footprint");
                Assert.LessOrEqual(Mathf.Max(Mathf.Abs(bounds.min.z), bounds.max.z), 0.5001f, $"{shape} is deeper than the footprint");
                Assert.LessOrEqual(Mathf.Max(Mathf.Abs(bounds.min.y), bounds.max.y), 1.0001f, $"{shape} is taller than the footprint");
            }
        }

        [Test]
        public void EveryShape_HasItsFloorInside_WithRoomForTheLeftovers()
        {
            DishShape[] shapes = Shapes;
            foreach (DishShape shape in shapes)
            {
                Vector2 floor = DishMeshFactory.Floor(shape);
                Assert.That(floor.x, Is.InRange(0f, 0.5f), $"{shape}'s floor is not inside it");
                Assert.That(floor.y, Is.InRange(-1f, 1f), $"{shape}'s floor is not inside it");
                if (shape != DishShape.Cylinder)
                {
                    Assert.Greater(floor.x, 0.1f, $"{shape}'s floor leaves no room for its scraps");
                }
            }
        }

        [Test]
        public void TallDishes_DrawTheirWallsAsABand_AndKeepTheirOutsidesApart()
        {
            DishShape[] array = new DishShape[6]
            {
                DishShape.Pan,
                DishShape.Glass,
                DishShape.Bowl,
                DishShape.Pot,
                DishShape.Mug,
                DishShape.Roaster
            };
            foreach (DishShape shape in array)
            {
                Vector2? band = DishMeshFactory.WallBand(shape);
                Assert.IsTrue(band.HasValue, $"{shape} has walls but no wall band");
                Assert.Less(band.Value.x, band.Value.y);
                Assert.LessOrEqual(band.Value.y, 0.5f);
            }
            array = new DishShape[5]
            {
                DishShape.Pan,
                DishShape.Bowl,
                DishShape.Pot,
                DishShape.Mug,
                DishShape.Roaster
            };
            foreach (DishShape shape2 in array)
            {
                bool usesPatch = false;
                Vector2[] uv = DishMeshFactory.Get(shape2).uv;
                foreach (Vector2 uv2 in uv)
                {
                    usesPatch |= DishMeshFactory.ExteriorUv.Contains(uv2);
                }
                Assert.IsTrue(usesPatch, $"{shape2}'s outside shares the dirt's texture with its inside");
            }
        }
    }
}
