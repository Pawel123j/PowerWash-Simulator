using NUnit.Framework;
using UnityEngine;
using Zmywak.Kitchen.Dishes;

namespace Zmywak.Tests
{
    public class DirtCanvasTests
    {
        private static readonly float[] UnitPower = { 1f };
        private static readonly float[] UnitDifficulty = { 1f };

        [TestCase(0.4f)]
        [TestCase(0.7f)]
        [TestCase(1f)]
        public void InitialPattern_CoversRoughlyTheRequestedShareOfTheDish(float coverage)
        {
            var canvas = new DirtCanvas(new[] { coverage }, seed: 7, circular: true);

            int dirty = 0;
            int inside = 0;
            for (int y = 0; y < canvas.Resolution; y++)
            {
                for (int x = 0; x < canvas.Resolution; x++)
                {
                    var uv = new Vector2((x + 0.5f) / canvas.Resolution, (y + 0.5f) / canvas.Resolution);
                    if ((uv - new Vector2(0.5f, 0.5f)).magnitude > 0.5f)
                    {
                        continue;
                    }

                    inside++;
                    if (canvas.DirtAt(0, uv) > 0.35f)
                    {
                        dirty++;
                    }
                }
            }

            Assert.AreEqual(coverage, (float)dirty / inside, 0.05f);
            Assert.AreEqual(1f, canvas.RemainingFraction(0), 1e-5f);
        }

        [Test]
        public void ZeroCoverage_StartsClean()
        {
            var canvas = new DirtCanvas(new[] { 0f }, seed: 1, circular: true);
            Assert.AreEqual(0f, canvas.RemainingFraction(0));
        }

        [Test]
        public void SameSeed_SamePattern_DifferentSeed_DifferentPattern()
        {
            var a = new DirtCanvas(new[] { 0.5f }, seed: 42, circular: true);
            var b = new DirtCanvas(new[] { 0.5f }, seed: 42, circular: true);
            var c = new DirtCanvas(new[] { 0.5f }, seed: 43, circular: true);

            bool anyDifference = false;
            for (int i = 0; i < 64; i++)
            {
                var uv = new Vector2(0.2f + i * 0.009f, 0.5f);
                Assert.AreEqual(a.DirtAt(0, uv), b.DirtAt(0, uv));
                anyDifference |= !Mathf.Approximately(a.DirtAt(0, uv), c.DirtAt(0, uv));
            }

            Assert.IsTrue(anyDifference);
        }

        [Test]
        public void Scrub_OnlyRemovesDirtUnderTheBrush()
        {
            var canvas = new DirtCanvas(new[] { 1f }, seed: 3, circular: true);
            var centre = new Vector2(0.5f, 0.5f);
            var farAway = new Vector2(0.5f, 0.85f);
            float farBefore = canvas.DirtAt(0, farAway);

            bool changed = canvas.ScrubAt(centre, 0.1f, UnitPower, UnitDifficulty, 10f);

            Assert.IsTrue(changed);
            Assert.AreEqual(0f, canvas.DirtAt(0, centre));
            Assert.AreEqual(farBefore, canvas.DirtAt(0, farAway));
            Assert.Less(canvas.RemainingFraction(0), 1f);
            Assert.Greater(canvas.RemainingFraction(0), 0.5f);
        }

        [Test]
        public void Scrub_OutsideTheDish_ChangesNothing()
        {
            var canvas = new DirtCanvas(new[] { 1f }, seed: 3, circular: true);

            bool changed = canvas.ScrubAt(new Vector2(0.03f, 0.03f), 0.05f, UnitPower, UnitDifficulty, 10f);

            Assert.IsFalse(changed);
            Assert.AreEqual(1f, canvas.RemainingFraction(0));
        }

        [Test]
        public void Scrub_StrongerToolRemovesMoreDirtPerSecond()
        {
            var weak = new DirtCanvas(new[] { 1f }, seed: 5, circular: true);
            var strong = new DirtCanvas(new[] { 1f }, seed: 5, circular: true);
            var uv = new Vector2(0.5f, 0.5f);

            weak.ScrubAt(uv, 0.2f, new[] { 0.15f }, UnitDifficulty, 0.1f);
            strong.ScrubAt(uv, 0.2f, new[] { 1f }, UnitDifficulty, 0.1f);

            Assert.Less(strong.RemainingFraction(0), weak.RemainingFraction(0));
        }

        [Test]
        public void Scrub_EachLayerComesOffAtItsOwnRate()
        {
            var canvas = new DirtCanvas(new[] { 1f, 1f }, seed: 9, circular: true);

            canvas.ScrubAt(new Vector2(0.5f, 0.5f), 0.2f, new[] { 1f, 0f }, new[] { 1f, 1f }, 0.2f);

            Assert.Less(canvas.RemainingFraction(0), 1f);
            Assert.AreEqual(1f, canvas.RemainingFraction(1), "A layer the tool has no power against must not change");
        }

        [Test]
        public void WriteColors_CleanTexelShowsCleanColour_DirtyTexelShowsDirt()
        {
            var canvas = new DirtCanvas(new[] { 1f }, seed: 2, circular: true);
            canvas.ScrubAt(new Vector2(0.5f, 0.5f), 0.1f, UnitPower, UnitDifficulty, 10f);
            var pixels = new Color32[canvas.Resolution * canvas.Resolution];
            Color clean = Color.white;
            Color dirt = Color.black;

            canvas.WriteColors(pixels, clean, new[] { dirt });

            int centre = (canvas.Resolution / 2) * canvas.Resolution + canvas.Resolution / 2;
            int edge = (canvas.Resolution / 2) * canvas.Resolution + 6; // left side of the disc, untouched
            Assert.AreEqual((Color32)clean, pixels[centre]);
            Assert.Less(pixels[edge].r, 100);
        }

        [Test]
        public void ASeeThroughGlass_IsClearWhereClean_AndCloudedUnderDirt()
        {
            var canvas = new DirtCanvas(new[] { 1f }, seed: 2, circular: true) { CleanOpacity = 0.3f };
            canvas.ScrubAt(new Vector2(0.5f, 0.5f), 0.1f, UnitPower, UnitDifficulty, 10f);
            var pixels = new Color32[canvas.Resolution * canvas.Resolution];
            var film = new Color(0.6f, 0.5f, 0.1f, 0.6f); // a see-through grease layer

            canvas.WriteColors(pixels, Color.white, new[] { film });

            int centre = (canvas.Resolution / 2) * canvas.Resolution + canvas.Resolution / 2;
            int edge = (canvas.Resolution / 2) * canvas.Resolution + 6; // left side of the disc, untouched
            Assert.AreEqual(0.3f, pixels[centre].a / 255f, 0.01f, "Clean glass is mostly see-through");
            Assert.Greater(pixels[edge].a / 255f, 0.5f, "Grime clouds the glass (alpha is opacity here, not gloss)");
        }

        [Test]
        public void WetGlaze_IsBeadedWithWater_OnlyWhereItIsClean()
        {
            var canvas = new DirtCanvas(new[] { 0.5f }, seed: 3, circular: true);
            int res = canvas.Resolution;
            var flatDirt = new[] { 0f }; // the dirt itself without relief: any slope left is a bead
            var normals = new Color32[res * res];

            canvas.WriteNormals(normals, flatDirt, 2f);
            foreach (Color32 n in normals)
            {
                Assert.That(n.r, Is.InRange(126, 130), "Dry glaze should be flat");
                Assert.That(n.g, Is.InRange(126, 130), "Dry glaze should be flat");
            }

            canvas.Wet = true;
            canvas.WriteNormals(normals, flatDirt, 2f);
            int beadedClean = 0;
            for (int y = 1; y < res - 1; y++)
            {
                for (int x = 1; x < res - 1; x++)
                {
                    bool tilted = Mathf.Abs(normals[y * res + x].r - 128) > 6 || Mathf.Abs(normals[y * res + x].g - 128) > 6;
                    bool deepInDirt = true; // this texel and its neighbours, which its slope is taken from
                    bool clean = true;
                    for (int oy = -1; oy <= 1; oy++)
                    {
                        for (int ox = -1; ox <= 1; ox++)
                        {
                            float dirt = canvas.DirtAt(0, new Vector2((x + ox + 0.5f) / res, (y + oy + 0.5f) / res));
                            deepInDirt &= dirt >= 0.25f;
                            clean &= dirt <= 0f;
                        }
                    }

                    if (deepInDirt)
                    {
                        Assert.IsFalse(tilted, $"A water bead showed through the dirt at {x},{y}");
                    }
                    else if (clean && tilted)
                    {
                        beadedClean++;
                    }
                }
            }

            Assert.Greater(beadedClean, 10, "No water beads on the clean, wet glaze");
        }

        [Test]
        public void OnATallDishsWalls_DirtRunsRoundTheWall_NotDownIt()
        {
            // A pan's wall is projected onto a thin ring of the texture: dirt that changes going round the ring
            // showed as stripes running down the wall. With the wall band it changes far less round it.
            var band = new Vector2(0.4f, 0.45f);
            int ChangesRoundTheRing(DirtCanvas canvas)
            {
                int changes = 0;
                bool previous = Dirty(canvas, 0);
                for (int step = 1; step <= 180; step++)
                {
                    bool current = Dirty(canvas, step * 2f * Mathf.Deg2Rad);
                    changes += current != previous ? 1 : 0;
                    previous = current;
                }

                return changes;
            }

            bool Dirty(DirtCanvas canvas, float angle) =>
                canvas.DirtAt(0, new Vector2(0.5f + Mathf.Cos(angle) * 0.425f, 0.5f + Mathf.Sin(angle) * 0.425f)) > 0.35f;

            int total = 0;
            int totalWithBand = 0;
            for (int seed = 1; seed <= 6; seed++)
            {
                total += ChangesRoundTheRing(new DirtCanvas(new[] { 0.5f }, seed, circular: true));
                totalWithBand += ChangesRoundTheRing(new DirtCanvas(new[] { 0.5f }, seed, circular: true, wallBand: band));
            }

            Assert.Less(totalWithBand * 2, total, $"Round the wall: {totalWithBand} changes with the band, {total} without");
        }

        [Test]
        public void ClearLayer_RemovesEverything()
        {
            var canvas = new DirtCanvas(new[] { 0.6f }, seed: 4, circular: true);
            canvas.ClearLayer(0);
            Assert.AreEqual(0f, canvas.RemainingFraction(0));
            Assert.AreEqual(0f, canvas.DirtAt(0, new Vector2(0.5f, 0.5f)));
        }
    }
}
