// Reconstructed on 2026-11-17 by decompiling Library/ScriptAssemblies/Zmywak.Tests.EditMode.dll (built 2026-10-11):
// the original source was lost from disk. Behaviour matches that build; original comments are gone.
using NUnit.Framework;
using Zmywak.Kitchen.Prep;

namespace Zmywak.Tests
{
    public class PrepRoundTests
    {
        private const float Step = 0.01f;

        private static void CutAtTheMark(PrepRound round)
        {
            for (int i = 0; i < 10000; i++)
            {
                if (round.IsFinished)
                {
                    break;
                }
                round.Tick(0.01f);
                if (round.FedMm >= 3f)
                {
                    round.Cut();
                }
            }
        }

        private static void CutEvery(PrepRound round, float interval)
        {
            float sinceCut = 0f;
            for (int i = 0; i < 10000; i++)
            {
                if (round.IsFinished)
                {
                    break;
                }
                round.Tick(0.01f);
                sinceCut += 0.01f;
                if (sinceCut >= interval)
                {
                    round.Cut();
                    sinceCut = 0f;
                }
            }
        }

        [Test]
        public void OnTheMark_EveryCubeIsEven_AndThePrepPaysInFull()
        {
            PrepRound round = new PrepRound();
            CutAtTheMark(round);
            Assert.IsTrue(round.IsFinished);
            Assert.Greater(round.Slices, 50);
            Assert.AreEqual(round.Slices, round.Even, "A cut at the mark is three millimetres");
            Assert.AreEqual(0, round.Fingers);
            Assert.AreEqual(1.0, round.Completion, 9.999999747378752E-05);
            Assert.Less(round.Elapsed, 40f, "Perfect prep should finish well inside the time");
            Assert.AreEqual(6.0, round.BonusEuros, 9.999999747378752E-05);
            StringAssert.StartsWith("Declan", round.Verdict);
        }

        [Test]
        public void LateCutsAreThick_EarlyOnesThin()
        {
            PrepRound late = new PrepRound();
            CutEvery(late, 0.75f);
            Assert.Greater(late.Thick, (float)late.Slices * 0.8f, $"{late.Thick} of {late.Slices} thick");
            PrepRound early = new PrepRound();
            CutEvery(early, 0.27f);
            Assert.Greater(early.Thin, (float)early.Slices * 0.8f, $"{early.Thin} of {early.Slices} thin");
            Assert.LessOrEqual(early.Fingers, early.VegetableCount - 1, "Thin is not yet the fingers - only a cut just as a new vegetable comes in may be that early");
        }

        [Test]
        public void FarTooEarly_TheKnifeFindsTheClaw_AndThePlasterStopsTheBoard()
        {
            PrepRound round = new PrepRound();
            for (int i = 0; i < 50; i++)
            {
                round.Tick(0.01f);
            }
            Assert.AreEqual(PrepCut.Even, round.Cut());
            for (int j = 0; j < 15; j++)
            {
                round.Tick(0.01f);
            }
            Assert.AreEqual(PrepCut.Finger, round.Cut());
            Assert.AreEqual(1, round.Fingers);
            Assert.AreEqual(2.5, round.PlasterRemaining, 9.999999747378752E-05);
            float fed = round.FedMm;
            for (int k = 0; k < 100; k++)
            {
                round.Tick(0.01f);
            }
            Assert.AreEqual(fed, round.FedMm, 9.999999747378752E-05, "Nothing moves while the plaster goes on");
            Assert.IsNull(round.Cut(), "No cutting with a plaster half on");
        }

        [Test]
        public void ASecondTap_WhileTheKnifeIsStillComingUp_DoesNothing()
        {
            PrepRound round = new PrepRound();
            for (int i = 0; i < 50; i++)
            {
                round.Tick(0.01f);
            }
            Assert.AreEqual(PrepCut.Even, round.Cut());
            round.Tick(0.05f);
            Assert.IsNull(round.Cut(), "A double tap is not a second cut - and not a nicked finger either");
            Assert.AreEqual(0, round.Fingers);
        }

        [Test]
        public void NobodyCutting_TheVegetableRunsPastInChunks_AndTimeRunsOut()
        {
            PrepRound round = new PrepRound();
            for (int i = 0; i < 10000; i++)
            {
                if (round.IsFinished)
                {
                    break;
                }
                round.Tick(0.01f);
            }
            Assert.IsTrue(round.IsFinished);
            Assert.Greater(round.Chunks, 0);
            Assert.AreEqual(0, round.Even);
            Assert.AreEqual(0f, round.BonusEuros, "Chunks earn nothing");
        }

        [Test]
        public void ATiredHand_PushesUnevenly_SoCuttingByTheClockMissesMore()
        {
            PrepRound rested = new PrepRound();
            PrepRound prepRound = new PrepRound(0.7f);
            CutEvery(rested, 0.5f);
            CutEvery(prepRound, 0.5f);
            Assert.Less(prepRound.Evenness, rested.Evenness, "Tiredness should make the beat wander");
            PrepRound prepRound2 = new PrepRound(0.7f);
            CutAtTheMark(prepRound2);
            Assert.Greater(prepRound2.Evenness, 0.9f, "Watching the mark still works when tired");
        }

        [Test]
        public void TheBonus_IsEvennessTimesCompletion_LessAPlaster_ToTheHalfEuro()
        {
            PrepRound round = new PrepRound();
            CutAtTheMark(round);
            Assert.AreEqual(6.0, round.BonusEuros, 9.999999747378752E-05);
            PrepRound nicked = new PrepRound();
            for (int i = 0; i < 50; i++)
            {
                nicked.Tick(0.01f);
            }
            nicked.Cut();
            for (int j = 0; j < 15; j++)
            {
                nicked.Tick(0.01f);
            }
            nicked.Cut();
            CutAtTheMark(nicked);
            Assert.AreEqual(1, nicked.Fingers);
            Assert.AreEqual(5.0, nicked.BonusEuros, 9.999999747378752E-05, "Six for even, finished prep, less a euro for the plaster");
        }
    }
}
