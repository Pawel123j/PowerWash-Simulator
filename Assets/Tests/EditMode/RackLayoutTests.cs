// Reconstructed on 2026-11-17 by decompiling Library/ScriptAssemblies/Zmywak.Tests.EditMode.dll (built 2026-10-11):
// the original source was lost from disk. Behaviour matches that build; original comments are gone.
using System;
using NUnit.Framework;
using UnityEngine;
using Zmywak.Kitchen.Rack;

namespace Zmywak.Tests
{
    public class RackLayoutTests
    {
        private static readonly Vector2Int Glass = Vector2Int.one;

        private static readonly Vector2Int Plate = new Vector2Int(2, 2);

        private static readonly Vector2Int Pan = new Vector2Int(4, 2);

        private static readonly Vector2 PanBody = new Vector2(-1f, 0f);

        [Test]
        public void EmptyRack_IsAllFree()
        {
            RackLayout rack = new RackLayout(6, 4);
            Assert.AreEqual(24, rack.CellCount);
            Assert.AreEqual(0, rack.OccupiedCells);
            Assert.AreEqual(0f, rack.FillRatio);
            Assert.IsTrue(rack.CanPlace(new RackPlacement(0, 0, 6, 4, turned: false)));
        }

        [Test]
        public void Placing_CoversTheFootprint_AndRefusesOverlapsAndOverhangs()
        {
            RackLayout rack = new RackLayout(6, 4);
            rack.Place(1, new RackPlacement(1, 1, 2, 2, turned: false));
            Assert.AreEqual(4, rack.OccupiedCells);
            Assert.AreEqual(1, rack.OwnerAt(2, 2));
            Assert.AreEqual(0, rack.OwnerAt(3, 2));
            Assert.IsFalse(rack.CanPlace(new RackPlacement(2, 2, 1, 1, turned: false)), "Overlapped a placed dish");
            Assert.IsFalse(rack.CanPlace(new RackPlacement(5, 0, 2, 2, turned: false)), "Hung over the right edge");
            Assert.IsFalse(rack.CanPlace(new RackPlacement(0, 3, 1, 2, turned: false)), "Hung over the back edge");
            Assert.Throws<InvalidOperationException>(() =>
            {
                rack.Place(2, new RackPlacement(0, 0, 2, 2, turned: false));
            });
        }

        [Test]
        public void Removing_FreesExactlyThatDish()
        {
            RackLayout rack = new RackLayout(6, 4);
            rack.Place(1, new RackPlacement(0, 0, 2, 2, turned: false));
            rack.Place(2, new RackPlacement(2, 0, 1, 1, turned: false));
            Assert.IsTrue(rack.Remove(1));
            Assert.AreEqual(1, rack.OccupiedCells);
            Assert.AreEqual(2, rack.OwnerAt(2, 0));
            Assert.IsFalse(rack.Remove(1), "Removed the same dish twice");
        }

        [Test]
        public void FindNear_LandsOnTheFreeSpotClosestToTheDish()
        {
            RackPlacement? spot = new RackLayout(6, 4).FindNear(new Vector2(3.1f, 2.2f), Plate, Vector2.zero, 1.3f);
            Assert.IsTrue(spot.HasValue);
            Assert.AreEqual(new RackPlacement(2, 1, 2, 2, turned: false), spot.Value);
        }

        [Test]
        public void FindNear_GivesUpWhenNothingFitsCloseEnough()
        {
            RackLayout rackLayout = new RackLayout(6, 4);
            rackLayout.Place(1, new RackPlacement(0, 0, 6, 2, turned: false));
            Assert.IsFalse(rackLayout.FindNear(new Vector2(3f, 0.5f), Plate, Vector2.zero, 1f).HasValue, "Snapped to a spot far away from the finger");
            Assert.IsTrue(rackLayout.FindNear(new Vector2(3f, 0.5f), Plate, Vector2.zero, 3f).HasValue);
        }

        [Test]
        public void FindNear_TurnsAPanToFitANarrowGap_HandleTowardsTheBack()
        {
            RackLayout rackLayout = new RackLayout(6, 4);
            rackLayout.Place(1, new RackPlacement(0, 0, 4, 4, turned: false));
            RackPlacement? spot = rackLayout.FindNear(new Vector2(5f, 1f), Pan, PanBody, 2f);
            Assert.IsTrue(spot.HasValue, "A pan should turn to fit a 2x4 gap");
            Assert.IsTrue(spot.Value.Turned);
            Assert.AreEqual(new RackPlacement(4, 0, 2, 4, turned: true), spot.Value);
            Vector2 body = spot.Value.Center + RackLayout.OrientedOffset(PanBody, turned: true);
            Assert.AreEqual(new Vector2(5f, 1f), body);
        }

        [Test]
        public void FindNear_PrefersTheWayRoundThatPutsTheBodyUnderTheFinger()
        {
            RackPlacement? spot = new RackLayout(6, 4).FindNear(new Vector2(1f, 1f), Pan, PanBody, 1.3f);
            Assert.IsTrue(spot.HasValue);
            Assert.IsFalse(spot.Value.Turned);
            Assert.AreEqual(new RackPlacement(0, 0, 4, 2, turned: false), spot.Value);
        }

        [Test]
        public void TightPacking_FillsTheRack_TwoPansAndTwoPlates()
        {
            RackLayout rack = new RackLayout(6, 4);
            int owner = 1;
            Vector2Int[] array = new Vector2Int[4] { Pan, Pan, Plate, Plate };
            foreach (Vector2Int dish in array)
            {
                RackPlacement? spot = rack.FindFirstFree(dish);
                Assert.IsTrue(spot.HasValue, $"No room for dish {owner}");
                rack.Place(owner++, spot.Value);
            }
            Assert.AreEqual(1.0, rack.FillRatio, 9.999999747378752E-06);
            Assert.IsFalse(rack.HasRoomFor(Glass), "A full rack still took a glass");
        }

        [Test]
        public void LoosePacking_LeavesNoRoomForAPan()
        {
            RackLayout rack = new RackLayout(6, 4);
            rack.Place(1, new RackPlacement(1, 0, 2, 2, turned: false));
            rack.Place(2, new RackPlacement(4, 0, 2, 2, turned: false));
            rack.Place(3, new RackPlacement(1, 2, 2, 2, turned: false));
            Assert.AreEqual(0.5, rack.FillRatio, 9.999999747378752E-06);
            Assert.IsFalse(rack.HasRoomFor(Pan), "Half the rack is free, but no 4x2 (or 2x4) block is");
            Assert.IsTrue(rack.HasRoomFor(Plate));
        }

        [Test]
        public void Clear_EmptiesTheRack()
        {
            RackLayout rack = new RackLayout(6, 4);
            rack.Place(1, new RackPlacement(0, 0, 4, 2, turned: false));
            rack.Clear();
            Assert.AreEqual(0, rack.OccupiedCells);
            Assert.IsTrue(rack.HasRoomFor(Pan));
        }
    }
}
