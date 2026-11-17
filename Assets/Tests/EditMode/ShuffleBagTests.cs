// Reconstructed on 2026-11-17 by decompiling Library/ScriptAssemblies/Zmywak.Tests.EditMode.dll (built 2026-10-11):
// the original source was lost from disk. Behaviour matches that build; original comments are gone.
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Zmywak.Core.Data;

namespace Zmywak.Tests
{
    public class ShuffleBagTests
    {
        private static readonly string[] Mix = new string[13]
        {
            "plate", "plate", "plate", "pan", "pan", "glass", "glass", "bowl", "bowl", "mug",
            "mug", "pot", "roaster"
        };

        private static ShuffleBag<string> Bag(int seed, bool shuffle = true, string[] opening = null, bool repeat = true)
        {
            return new ShuffleBag<string>(opening, Mix, (string kind) => kind, seed, shuffle, repeat);
        }

        private static List<string> Deal(ShuffleBag<string> bag, int count)
        {
            List<string> dealt = new List<string>();
            for (int i = 0; i < count; i++)
            {
                dealt.Add(bag.Next());
            }
            return dealt;
        }

        [Test]
        public void EveryRound_HoldsTheMixExactly_InAnOrderThatChanges()
        {
            List<string> dealt = Deal(Bag(7), Mix.Length * 4);
            for (int round = 0; round < 4; round++)
            {
                CollectionAssert.AreEquivalent(Mix, dealt.Skip(round * Mix.Length).Take(Mix.Length), $"Round {round + 1} is not the mix");
            }
            CollectionAssert.AreNotEqual(dealt.Take(Mix.Length).ToList(), dealt.Skip(Mix.Length).Take(Mix.Length).ToList(), "Two rounds came in the same order");
            CollectionAssert.AreNotEqual(Mix, dealt.Take(Mix.Length), "The first round came in the mix's own order");
        }

        [Test]
        public void TheSameSeed_DealsTheSameDay_AndAnotherDayDiffers()
        {
            CollectionAssert.AreEqual(Deal(Bag(42), 30), Deal(Bag(42), 30));
            CollectionAssert.AreNotEqual(Deal(Bag(42), 30), Deal(Bag(43), 30));
        }

        [Test]
        public void NoKind_ComesMoreThanTwiceInARow_AcrossRoundsAndSeeds()
        {
            for (int seed = 0; seed < 200; seed++)
            {
                List<string> dealt = Deal(Bag(seed), Mix.Length * 3);
                int run = 1;
                for (int i = 1; i < dealt.Count; i++)
                {
                    run = ((!(dealt[i] == dealt[i - 1])) ? 1 : (run + 1));
                    Assert.LessOrEqual(run, 2, $"Seed {seed}: {run} x {dealt[i]} in a row at {i}");
                }
            }
        }

        [Test]
        public void TheOpening_ComesFirst_InItsOwnOrder_ThenTheShuffledMix()
        {
            string[] opening = new string[3] { "plate", "pan", "glass" };
            List<string> dealt = Deal(Bag(5, shuffle: true, opening), opening.Length + Mix.Length);
            CollectionAssert.AreEqual(opening, dealt.Take(3));
            CollectionAssert.AreEquivalent(Mix, dealt.Skip(3), "The opening is on top of the mix, not taken from it");
        }

        [Test]
        public void Unshuffled_ItGoesRoundTheMixInItsOrder()
        {
            List<string> dealt = Deal(Bag(99, shuffle: false), Mix.Length * 2);
            CollectionAssert.AreEqual(Mix.Concat(Mix), dealt);
        }

        [Test]
        public void ABagThatDoesNotRepeat_RunsOut()
        {
            ShuffleBag<string> shuffleBag = Bag(1, shuffle: true, null, repeat: false);
            Deal(shuffleBag, Mix.Length);
            Assert.IsNull(shuffleBag.Next());
            Assert.IsNull(new ShuffleBag<string>(null, new string[0], (string kind) => kind, 0).Next(), "An empty mix deals nothing");
        }
    }
}
