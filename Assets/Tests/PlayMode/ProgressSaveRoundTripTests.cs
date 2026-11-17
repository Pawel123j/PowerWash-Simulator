using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Zmywak.Core.Data;
using Zmywak.Core.Save;
using Zmywak.Core.Telemetry;
using Zmywak.Meta.Progress;
using Zmywak.Narrative.Progress;

namespace Zmywak.Tests.PlayMode
{
    /// <summary>
    /// MetaProgressController and NarrativeProgressController share one save file via read-modify-write.
    /// Interleaves changes from both, then recreates them from disk (a fresh session) and checks that
    /// everything survived and neither controller clobbered the other's fields. Runs in PlayMode because
    /// both are DontDestroyOnLoad MonoBehaviours that load in Awake; writes to a temp folder, never to the
    /// developer's real save.
    /// </summary>
    public class ProgressSaveRoundTripTests
    {
        [SetUp]
        public void SetUp()
        {
            TestSaveSandbox.Enter();
        }

        [TearDown]
        public void TearDown()
        {
            TestSaveSandbox.Exit();
        }

        [UnityTest]
        public IEnumerator MetaAndNarrativeProgress_SurviveReload_WithoutClobberingEachOther()
        {
            // Day three, a hundred euros: the pub and transfers home are open by now (MechanicUnlocks).
            SaveSystem.Save(new SaveData
            {
                dayNumber = 3, currentDateIso = "2004-05-03T00:00:00", walletEuros = 100f, energy = 100f, homesickness = 30f, integration = 10f
            });
            var meta = new GameObject("Meta").AddComponent<MetaProgressController>();
            var narrative = new GameObject("Narrative").AddComponent<NarrativeProgressController>();
            yield return null;

            Assert.AreEqual(100f, meta.Wallet, 1e-4f, "The save's wallet");
            Assert.IsFalse(narrative.HasSeenIntro);

            // Interleave writes from both controllers - each one re-reads the file before writing.
            Assert.IsTrue(meta.TryGoToPub());          // wallet 90, integration 20, energy 94 (a night out does not ease homesickness)
            narrative.LearnWord("kitchen");
            Assert.IsFalse(meta.CallMom(), "The evening went to the pub: the phone box waits for tomorrow");
            narrative.UnlockSlang("slang_grand");
            float pln = meta.SendMoneyHome(5f);        // wallet 85 (two days of bills, 47 EUR, stay put)
            narrative.MarkBeatSeen(NarrativeProgressController.ArrivalBeatId);
            narrative.SetFlag("ch1_refused_agency");
            meta.ApplyStoryOutcome(new StoryOutcome { moneyEuros = -4f, integration = 5f }); // wallet 81, integration 25

            Assert.AreEqual(20f, pln, 1e-3f, "No exchange table assigned -> flat fallback rate of 4 PLN/EUR");

            Object.Destroy(meta.gameObject);
            Object.Destroy(narrative.gameObject);
            yield return null;

            var reloadedMeta = new GameObject("Meta (reloaded)").AddComponent<MetaProgressController>();
            var reloadedNarrative = new GameObject("Narrative (reloaded)").AddComponent<NarrativeProgressController>();
            yield return null;

            try
            {
                Assert.AreEqual(81f, reloadedMeta.Wallet, 1e-4f);
                Assert.AreEqual(94f, reloadedMeta.Stats.Energy, 1e-4f);
                Assert.AreEqual(30f, reloadedMeta.Stats.Homesickness, 1e-4f);
                Assert.AreEqual(25f, reloadedMeta.Stats.Integration, 1e-4f);
                Assert.AreEqual(3, reloadedMeta.DayNumber);
                Assert.AreEqual(5f, reloadedMeta.Ledger.SentHomeEuros, 1e-4f);
                Assert.IsFalse(reloadedMeta.CallMom(), "Restarting the app gave the evening back for a call home");
                Assert.IsFalse(reloadedMeta.TryGoToPub(), "Restarting the app gave a second night out the same evening");
                Assert.IsFalse(reloadedMeta.TryRest(), "Restarting the app gave a second evening's plan");

                // The evening is in the local gameplay diary too.
                var actions = new System.Collections.Generic.List<string>();
                foreach (LifeLogEntry entry in GameplayLog.Read<LifeLogEntry>("life"))
                {
                    actions.Add(entry.action);
                }

                CollectionAssert.AreEqual(new[] { "pub", "send_home", "story" }, actions);

                Assert.IsTrue(reloadedNarrative.Vocabulary.IsKnown("kitchen"));
                Assert.IsTrue(reloadedNarrative.Slang.IsUnlocked("slang_grand"));
                Assert.IsTrue(reloadedNarrative.HasSeenIntro);
                Assert.IsTrue(reloadedNarrative.HasFlag("ch1_refused_agency"), "Story choices must survive a reload");
                Assert.IsFalse(reloadedNarrative.HasFlag("ch1_paid_agency"));
            }
            finally
            {
                Object.Destroy(reloadedMeta.gameObject);
                Object.Destroy(reloadedNarrative.gameObject);
            }
        }
    }
}
