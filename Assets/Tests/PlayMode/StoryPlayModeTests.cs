using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Zmywak.Core.Data;
using Zmywak.Core.Events.Channels;
using Zmywak.Meta.Progress;
using Zmywak.Narrative.Dialogue;
using Zmywak.Narrative.Progress;
using Zmywak.UI.Screens;

namespace Zmywak.Tests.PlayMode
{
    /// <summary>
    /// Plays the story through the real home hub, the way a player meets it: each evening at home one
    /// scene plays, then a shift passes (the kitchen's "shift completed" event) and the next day's scene is
    /// waiting; when a chapter ends, the next one starts the day after. Runs on a sandboxed save, so the
    /// developer's own progress is untouched.
    /// </summary>
    public class StoryPlayModeTests
    {
        [SetUp]
        public void SetUp()
        {
            TestSaveSandbox.Enter();
        }

        [TearDown]
        public void TearDown()
        {
            TestSaveSandbox.DestroyPersistentObjects();
            TestSaveSandbox.Exit();
        }

        [UnityTest]
        public IEnumerator ThePortrait_IsTheSpeakers_NobodysForTheNarrator_AndKubasAtAChoice()
        {
            yield return BootToMainMenu();
            yield return LoadHub(); // day 1: the arrival is playing
            var runner = Object.FindAnyObjectByType<DialogueRunner>();
            var screen = Object.FindAnyObjectByType<DialogueScreen>();
            string speaker = null;
            bool choosing = false;
            runner.OnLineShown += (id, _) => speaker = id;
            runner.OnChoicesShown += _ => choosing = true;

            int someoneElse = 0;
            for (int step = 0; step < 60 && !choosing; step++)
            {
                runner.Advance();
                yield return null;
                if (choosing || speaker == null)
                {
                    continue;
                }

                string id = SpeakerNames.PortraitId(speaker);
                Sprite shown = screen.ShownPortrait;
                if (id == null)
                {
                    Assert.IsNull(shown, $"A portrait ({shown?.name}) stayed up while the narrator spoke");
                    continue;
                }

                Assert.IsNotNull(shown, $"No portrait for {speaker}");
                Assert.AreEqual($"Portrait_{id}", shown.name);
                if (id != "kuba" && someoneElse++ == 0)
                {
                    yield return new WaitForSecondsRealtime(0.4f); // risen and faded in
                    TestScreenshot.Capture(Camera.main, "28_dialogue_portrait.png");
                }
            }

            Assert.IsTrue(choosing, "The first scene never reached a choice");
            Assert.Greater(someoneElse, 0, "Nobody but Kuba and the narrator spoke before the first choice");
            Assert.AreEqual("Portrait_kuba", screen.ShownPortrait?.name, "The choice is Kuba's: his face should be up");
            Assert.AreEqual("Kuba (myśli)", GetPrivateField<TMP_Text>(screen, "speakerText").text,
                "Another speaker's name stood over Kuba's face at his choice");
        }

        [UnityTest]
        public IEnumerator ANewLife_IsOnDisk_TheMomentTheHubOpens()
        {
            yield return BootToMainMenu();
            yield return LoadHub(); // a new life: day 1, the arrival playing
            Zmywak.Core.Save.SaveData data = Zmywak.Core.Save.SaveSystem.Load();
            Assert.IsNotNull(data, "Nothing on disk once the hub was open");
            Assert.AreEqual(1, data.dayNumber, "The new life's week was not written before the story could write next to it");
            Assert.AreEqual(Zmywak.Meta.Economy.LifeRules.Default.StartingWallet, data.walletEuros, 1e-3f);
            Assert.IsFalse(string.IsNullOrEmpty(data.currentDateIso), "The menu would read this save as \"Dzień 1 · ?\"");
        }

        [UnityTest]
        public IEnumerator AfterAScene_TheHubsActionsComeBack_WithoutTakingTheTapMeantForTheLastLine()
        {
            yield return BootToMainMenu();
            yield return LoadHub(); // day 1: the arrival is playing
            var trigger = Object.FindAnyObjectByType<StoryBeatTrigger>();
            Assert.IsFalse(trigger.ActionsTakeTaps, "The hub's buttons answered while the scene played");

            yield return PlayThrough(trigger, new List<IReadOnlyList<string>>());
            // "Dalej" sits over "Idź do pracy": the next quick tap must not send Kuba to work and lose the evening.
            Assert.IsFalse(trigger.ActionsTakeTaps, "The hub's buttons took taps the moment the scene ended");

            yield return new WaitForSecondsRealtime(StoryBeatTrigger.ActionsReturnSeconds + 0.1f);
            Assert.IsTrue(trigger.ActionsTakeTaps, "The hub's buttons never came back after the scene");
            Assert.AreEqual(1f, GameObject.Find("GoToWorkButton").transform.parent.GetComponent<CanvasGroup>().alpha, 1e-3f,
                "The hub's actions did not fade all the way back in");
        }

        [UnityTest]
        public IEnumerator ATapAsTheChoicesAppear_PicksNothing_ADeliberateOneDoes()
        {
            yield return BootToMainMenu();
            yield return LoadHub(); // day 1: the arrival is playing
            var runner = Object.FindAnyObjectByType<DialogueRunner>();
            bool choosing = false;
            int linesShown = 0;
            runner.OnChoicesShown += _ => choosing = true;
            runner.OnLineShown += (_, _) => linesShown++;
            for (int step = 0; step < 60 && !choosing; step++)
            {
                runner.Advance();
                yield return null;
            }

            Assert.IsTrue(choosing, "The first scene never reached a choice");
            var first = GameObject.Find("Choice_0").GetComponent<UnityEngine.UI.Button>();
            int before = linesShown;

            first.onClick.Invoke(); // the quick tap meant for the next line
            yield return null;
            Assert.AreEqual(before, linesShown, "A tap the moment the choices appeared picked one");
            Assert.IsTrue(first.gameObject.activeInHierarchy, "The choices went away after a tap that should not count");

            yield return new WaitForSecondsRealtime(DialogueScreen.ChoiceTapGuardSeconds + 0.1f);
            first.onClick.Invoke();
            yield return null;
            Assert.Greater(linesShown, before, "A deliberate tap on a choice did not take it");
        }

        [UnityTest]
        public IEnumerator Chapter1_PlaysAScene_EachDay_UntilItIsComplete()
        {
            yield return BootToMainMenu();
            var narrative = Object.FindAnyObjectByType<NarrativeProgressController>();
            var meta = Object.FindAnyObjectByType<MetaProgressController>();
            ShiftCompletedEventChannel shiftCompleted = GetPrivateField<ShiftCompletedEventChannel>(meta, "shiftCompletedChannel");

            string[] expected = { "ch1_arrival", "ch1_house", "ch1_pps_office", "ch1_bank", "ch1_way_out", "ch1_number" };
            for (int day = 1; day <= expected.Length; day++)
            {
                yield return LoadHub();
                var trigger = Object.FindAnyObjectByType<StoryBeatTrigger>();
                Assert.IsNotNull(trigger.PlayingBeat, $"No story scene on day {day}");
                Assert.AreEqual(expected[day - 1], trigger.PlayingBeat.beatId);

                var offered = new List<IReadOnlyList<string>>();
                string shot = day == 1 ? "15_chapter1_first_choice.png" : day == 5 ? "16_chapter1_way_out_choices.png" : null;
                yield return PlayThrough(trigger, offered, screenshotAtFirstChoice: shot);

                Assert.IsTrue(narrative.HasSeenBeat(expected[day - 1]), $"Day {day}'s scene was not recorded as seen");
                if (expected[day - 1] == "ch1_way_out")
                {
                    // Kuba's own thought at the bank (Sisyphus) unlocks a third way to ask Declan for help.
                    Assert.AreEqual(3, offered[0].Count, "The option unlocked by Kuba's monologue was not offered");
                }

                shiftCompleted.Raise(new ShiftResult { DishesCleaned = 4, Wage = 2f });
                Assert.AreEqual(day + 1, meta.DayNumber);
            }

            Assert.IsTrue(narrative.HasFlag("ch1_complete"), "Finishing the last scene did not complete the chapter");
            // Always the first choice: paid the agency, took the couch, took Declan's letter.
            Assert.IsTrue(narrative.HasFlag("ch1_paid_agency"));
            Assert.IsTrue(narrative.HasFlag("ch1_couch"));
            Assert.IsTrue(narrative.HasFlag("ch1_declan_letter"));
            Assert.IsFalse(narrative.HasFlag("ch1_fake_bill"));
            Assert.IsTrue(narrative.Vocabulary.IsKnown("passport"), "The chapter should have taught some English");
            Assert.IsTrue(narrative.Slang.IsUnlocked("slang_i_will_yeah"));
            Assert.Greater(meta.Stats.Energy, 40f, "Six days of shifts (and a couch) should tire Kuba, not flatten him");

            // Day 7: the chapter is over, and the next one begins.
            yield return LoadHub();
            var after = Object.FindAnyObjectByType<StoryBeatTrigger>();
            Assert.IsNotNull(after.PlayingBeat, "Chapter 2 did not start the day after chapter 1 ended");
            Assert.AreEqual("ch2_payday", after.PlayingBeat.beatId);
            Assert.AreEqual("ch2", after.CurrentChapter.ChapterId);
            StringAssert.StartsWith("Rozdział 2", GetPrivateField<TMP_Text>(after, "chapterText").text);
        }

        [UnityTest]
        public IEnumerator Chapter2_FollowsChapter1_AndRemembersItsChoices()
        {
            yield return BootToMainMenu();
            var narrative = Object.FindAnyObjectByType<NarrativeProgressController>();
            var meta = Object.FindAnyObjectByType<MetaProgressController>();
            ShiftCompletedEventChannel shiftCompleted = GetPrivateField<ShiftCompletedEventChannel>(meta, "shiftCompletedChannel");

            string[] expected =
            {
                "ch1_arrival", "ch1_house", "ch1_pps_office", "ch1_bank", "ch1_way_out", "ch1_number",
                "ch2_payday", "ch2_siobhan", "ch2_brunch", "ch2_pub", "ch2_tomek", "ch2_knife"
            };
            for (int day = 1; day <= expected.Length; day++)
            {
                yield return LoadHub();
                var trigger = Object.FindAnyObjectByType<StoryBeatTrigger>();
                Assert.IsNotNull(trigger.PlayingBeat, $"No story scene on day {day}");
                string beat = trigger.PlayingBeat.beatId;
                Assert.AreEqual(expected[day - 1], beat);

                // The first option everywhere (Declan's letter in chapter 1, the rent, the pub, covering for Tunde,
                // the knife) - except Tomek, who is sent to Declan: the option only Declan's letter opens.
                var offered = new List<IReadOnlyList<string>>();
                yield return PlayThrough(trigger, offered, beat == "ch2_tomek" ? 1 : 0,
                    screenshotAtFirstChoice: beat == "ch2_pub" ? "27_chapter2_pub_round.png" : null);
                if (beat == "ch2_tomek")
                {
                    Assert.AreEqual(3, offered[0].Count, "Declan's letter from chapter 1 did not open a way to help Tomek");
                }

                shiftCompleted.Raise(new ShiftResult { DishesCleaned = 8, Wage = 40f });
            }

            Assert.IsTrue(narrative.HasFlag("ch1_complete"));
            Assert.IsTrue(narrative.HasFlag("ch2_rent_on_time"));
            Assert.IsTrue(narrative.HasFlag("ch2_bought_round"), "Saying yes to Siobhán did not take Kuba to the pub");
            Assert.IsTrue(narrative.HasFlag("ch2_covered_tunde"));
            Assert.IsTrue(narrative.HasFlag("ch2_tomek_at_sink"));
            Assert.IsTrue(narrative.HasFlag("ch2_prep_cook"));
            Assert.IsTrue(narrative.HasFlag("ch2_complete"), "Finishing the last scene did not complete chapter 2");
            Assert.IsTrue(narrative.Slang.IsUnlocked("slang_jacks"));
            Assert.IsTrue(narrative.Slang.IsUnlocked("slang_sound"));
            Assert.IsTrue(narrative.Vocabulary.IsKnown("wages"));

            // Day 13: chapter 3 begins, with Kuba already at the board (he took the knife on day 12).
            yield return LoadHub();
            var after = Object.FindAnyObjectByType<StoryBeatTrigger>();
            Assert.IsNotNull(after.PlayingBeat, "Chapter 3 did not start the day after chapter 2 ended");
            Assert.AreEqual("ch3_board", after.PlayingBeat.beatId);
            StringAssert.StartsWith("Rozdział 3", GetPrivateField<TMP_Text>(after, "chapterText").text);
        }

        [UnityTest]
        public IEnumerator Chapters3ToTheEpilogue_PickUpWhatCameBefore_AndTheCalendarJumps()
        {
            yield return BootToMainMenu();
            var narrative = Object.FindAnyObjectByType<NarrativeProgressController>();
            var meta = Object.FindAnyObjectByType<MetaProgressController>();
            ShiftCompletedEventChannel shiftCompleted = GetPrivateField<ShiftCompletedEventChannel>(meta, "shiftCompletedChannel");

            // A different life from the test above: the fake bill (so no letter from Declan), the rent on time, covering
            // for Tunde, the pub, lending Tomek 30 EUR, the knife - in chapter 3 the careful cut, pierogi, and Tunde - and
            // in chapter 4 taking Bartek on, booking a flat off the plans, cookery school and going with Gavin - and in
            // chapter 5 lending Marek 50, seeing Tomek off, keeping Bartek, and Perth - so the epilogue is in Australia.
            var picks = new Dictionary<string, int>
            {
                { "ch1_way_out", 1 }, { "ch3_board", 1 }, { "ch3_gavin", 2 }, { "ch5_list", 2 }, { "ch5_choice", 2 }
            };
            float walletBeforeRepay = 0f;
            float walletBeforeTip = 0f;
            float walletBeforeMarek = 0f;
            for (int day = 1; day <= 35; day++)
            {
                yield return LoadHub();
                var trigger = Object.FindAnyObjectByType<StoryBeatTrigger>();
                Assert.IsNotNull(trigger.PlayingBeat, $"No story scene on day {day}");
                string beat = trigger.PlayingBeat.beatId;
                if (beat == "ch3_tomek")
                {
                    walletBeforeRepay = meta.Wallet;
                }

                if (beat == "ch4_anniversary")
                {
                    // Two years to the day after Kuba landed; the days lived carry on, the rent is 2006's.
                    Assert.AreEqual(new System.DateTime(2006, 5, 1), meta.CurrentDate, "Chapter 4 did not move the calendar to 1 May 2006");
                    Assert.AreEqual(19, meta.DayNumber);
                    Assert.AreEqual(meta.Rules.WeeklyBillsIn(2006), meta.Ledger.WeeklyBills, 1e-3f);
                }

                if (beat == "ch4_opening")
                {
                    walletBeforeTip = meta.Wallet;
                }

                if (beat == "ch6_others")
                {
                    walletBeforeMarek = meta.Wallet;
                }

                if (beat == "ch6_where")
                {
                    Assert.AreEqual(new System.DateTime(2010, 11, 29), meta.CurrentDate, "The epilogue did not move the calendar to November 2010");
                    // ...and Kuba to the Pilbara, as chapter 5 chose: its rules, and the wallet in Australian dollars.
                    Assert.AreEqual(Place.Pilbara, meta.Place, "Chapter 5's Perth did not take Kuba to Australia");
                    Assert.AreSame(Zmywak.Meta.Economy.LifeRules.Pilbara, meta.Rules);
                    StringAssert.Contains("AUD", GameObject.Find("WalletText").GetComponent<TMP_Text>().text, "The hub still counts in euros");
                }

                if (beat == "ch5_empty_room")
                {
                    Assert.AreEqual(new System.DateTime(2008, 9, 29), meta.CurrentDate, "Chapter 5 did not move the calendar to the week of the crash");
                    Assert.AreEqual(meta.Rules.WeeklyBillsIn(2008), meta.Ledger.WeeklyBills, 1e-3f);
                }

                var offered = new List<IReadOnlyList<string>>();
                yield return PlayThrough(trigger, offered, picks.TryGetValue(beat, out int pick) ? pick : 0,
                    screenshotAtFirstChoice: beat == "ch3_gavin" ? "29_chapter3_charity.png" : beat == "ch4_flats" ? "30_chapter4_flats.png"
                        : beat == "ch5_choice" ? "31_chapter5_choice.png" : beat == "ch6_new_kid" ? "32_epilogue_new_kid.png" : null);
                if (beat == "ch2_tomek")
                {
                    Assert.AreEqual(2, offered[0].Count, "Without Declan's letter there is no asking him about Tomek");
                }

                if (beat == "ch3_tomek")
                {
                    Assert.AreEqual(walletBeforeRepay + 30f, meta.Wallet, 1e-3f, "Tomek did not pay back the 30 EUR he borrowed");
                }

                if (beat == "ch3_gavin")
                {
                    Assert.AreEqual(3, offered[0].Count, "Covering for Tunde in chapter 2 did not bring him in on Sunday");
                }

                if (beat == "ch4_anniversary")
                {
                    Assert.AreEqual(2, offered[0].Count, "Without Declan's letter Kuba cannot vouch for Bartek the way Declan did for him");
                }

                if (beat == "ch5_list" || beat == "ch5_choice")
                {
                    Assert.AreEqual(3, offered[0].Count, $"{beat}: what chapter 4 decided (Bartek, Gavin) should open a third way");
                }

                if (beat == "ch4_opening")
                {
                    Assert.AreEqual(walletBeforeTip + 40f, meta.Wallet, 1e-3f, "Kuba's share of the developer's tip did not arrive");
                }

                if (beat == "ch6_where")
                {
                    // The scene over, the hub's buttons are back - priced in the camp's money.
                    StringAssert.Contains("AUD", GameObject.Find("PubButton").GetComponentInChildren<TMP_Text>().text, "The pub is still priced in euros");
                    StringAssert.Contains("AUD", GameObject.Find("SendMoneyButton").GetComponentInChildren<TMP_Text>().text);
                }

                if (beat == "ch6_others")
                {
                    // Fifty euros in 2010, at the NBP's rates: 3.9939 złoty to the euro, 2.7716 to the dollar.
                    Assert.AreEqual(walletBeforeMarek + Mathf.Round(50f * 3.9939f / 2.7716f * 100f) / 100f, meta.Wallet, 1e-2f,
                        "Marek's fifty euros did not arrive as Australian dollars");
                }

                shiftCompleted.Raise(new ShiftResult { DishesCleaned = 8, Wage = 60f });
            }

            Assert.IsTrue(narrative.HasFlag("ch1_fake_bill"));
            Assert.IsTrue(narrative.HasFlag("ch2_lent_tomek"));
            Assert.IsTrue(narrative.HasFlag("ch3_at_board"));
            Assert.IsTrue(narrative.HasFlag("ch3_careful"));
            Assert.IsTrue(narrative.HasFlag("ch3_tomek_repaid"));
            Assert.IsTrue(narrative.HasFlag("ch3_pierogi"));
            Assert.IsTrue(narrative.HasFlag("ch3_tunde_friend"));
            Assert.IsTrue(narrative.HasFlag("ch3_byrne_signed"), "Rent paid on time should have got Mrs Byrne's signature for free");
            Assert.IsTrue(narrative.HasFlag("ch3_helped_gavin"));
            Assert.IsTrue(narrative.HasFlag("ch3_complete"), "Finishing the critic's night did not complete chapter 3");
            Assert.IsTrue(narrative.Slang.IsUnlocked("slang_banjaxed"));
            Assert.IsTrue(narrative.HasFlag("ch4_mentor_bartek"));
            Assert.IsTrue(narrative.HasFlag("ch4_booked_flat"));
            Assert.IsTrue(narrative.HasFlag("ch4_promised_visit"));
            Assert.IsTrue(narrative.HasFlag("ch4_cookery_school"));
            Assert.IsTrue(narrative.HasFlag("ch4_with_gavin"));
            Assert.IsTrue(narrative.HasFlag("ch4_complete"), "The night of the cranes did not complete chapter 4");
            Assert.IsTrue(narrative.Slang.IsUnlocked("slang_begrudgers"));
            Assert.IsTrue(narrative.HasFlag("ch5_glass_closed"));
            Assert.IsTrue(narrative.HasFlag("ch5_lent_marek"));
            Assert.IsTrue(narrative.HasFlag("ch5_saw_tomek_off"));
            Assert.IsTrue(narrative.HasFlag("ch5_kept_bartek"));
            Assert.IsTrue(narrative.HasFlag("ch5_goes_to_perth"));
            Assert.IsTrue(narrative.HasFlag("ch5_complete"), "The choice did not complete chapter 5");
            Assert.IsTrue(narrative.Slang.IsUnlocked("slang_brutal"));
            // The epilogue went to Perth, the way chapter 5 chose; Marek paid back the 50 lent in the crash.
            Assert.IsTrue(narrative.HasFlag("ch6_two_homes"), "The epilogue did not follow Kuba to Australia");
            Assert.IsFalse(narrative.HasFlag("ch6_name_on_the_door"));
            Assert.IsTrue(narrative.HasFlag("ch6_marek_paid_back"));
            Assert.IsTrue(narrative.HasFlag("ch6_taught_the_new_boy"));
            Assert.IsTrue(narrative.HasFlag("ch6_complete"), "The last plate did not complete the epilogue");
            Assert.AreEqual(new System.DateTime(2010, 12, 4), meta.CurrentDate, "After the jump, a day a shift as before");

            // Day 36: no chapter left to tell - the hub is just the hub, and says so.
            yield return LoadHub();
            var after = Object.FindAnyObjectByType<StoryBeatTrigger>();
            Assert.IsNull(after.PlayingBeat, "A scene played after the last chapter ended");
            string title = GetPrivateField<TMP_Text>(after, "chapterText").text;
            StringAssert.StartsWith("Epilog", title);
            StringAssert.Contains("ukończony", title);

            // The end of the story is on the disk, not only in memory: a restart after the finale stays after it.
            Zmywak.Core.Save.SaveData saved = Zmywak.Core.Save.SaveSystem.Load();
            Assert.IsNotNull(saved, "Nothing saved after the finale");
            CollectionAssert.Contains(saved.storyFlags, "ch6_complete", "The finished epilogue was not saved");
            Assert.AreEqual((int)Place.Pilbara, saved.place, "The save forgot where the epilogue took Kuba");
            Assert.AreEqual(meta.DayNumber, saved.dayNumber, "The save is behind the day lived");
        }

        [UnityTest]
        public IEnumerator Chapter1_TheFakeBill_ChangesWhatHappensAtTheOffice()
        {
            yield return BootToMainMenu();
            var narrative = Object.FindAnyObjectByType<NarrativeProgressController>();
            var meta = Object.FindAnyObjectByType<MetaProgressController>();
            ShiftCompletedEventChannel shiftCompleted = GetPrivateField<ShiftCompletedEventChannel>(meta, "shiftCompletedChannel");
            float walletBefore = 0f;

            for (int day = 1; day <= 6; day++)
            {
                yield return LoadHub();
                var trigger = Object.FindAnyObjectByType<StoryBeatTrigger>();
                string beat = trigger.PlayingBeat.beatId;
                if (beat == "ch1_way_out")
                {
                    walletBefore = meta.Wallet;
                }

                // Pay Marek's man for the fake bill (the second option on day 5); the first option elsewhere.
                yield return PlayThrough(trigger, new List<IReadOnlyList<string>>(), beat == "ch1_way_out" ? 1 : 0);
                if (beat == "ch1_way_out")
                {
                    Assert.AreEqual(Mathf.Max(0f, walletBefore - 20f), meta.Wallet, 1e-3f, "The fake bill was not paid for");
                }

                // Paid well enough that the fake bill (20 EUR) does not have to come out of the rent - or it could not be bought.
                shiftCompleted.Raise(new ShiftResult { DishesCleaned = 4, Wage = 60f });
            }

            Assert.IsTrue(narrative.HasFlag("ch1_fake_bill"));
            Assert.IsTrue(narrative.HasFlag("ch1_clerk_noticed"), "With a fake bill, the clerk's scene should have been the one that noticed");
            Assert.IsFalse(narrative.HasFlag("ch1_declan_letter"));
            Assert.IsTrue(narrative.HasFlag("ch1_complete"));
        }

        // ------------------------------------------------------------------

        private static IEnumerator BootToMainMenu()
        {
            // Through Boot, like the game: it creates the persistent progress controllers the hub reads.
            Scene previouslyActive = SceneManager.GetActiveScene();
            SceneManager.LoadScene("Boot", LoadSceneMode.Single);
            var deadline = new PlayDeadline(10f);
            while ((SceneManager.GetActiveScene() == previouslyActive || SceneManager.GetActiveScene().name != "MainMenu")
                   && !deadline.Passed)
            {
                yield return null;
            }

            Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name, "Boot did not hand over to MainMenu");
        }

        private static IEnumerator LoadHub()
        {
            SceneManager.LoadScene("MetaHub", LoadSceneMode.Single);
            yield return null;
            yield return null; // Start() has run: today's scene, if any, is playing
            Assert.AreEqual("MetaHub", SceneManager.GetActiveScene().name);
        }

        /// <summary>Clicks through the scene playing now: "Dalej" on every line, the given option at every choice.</summary>
        private static IEnumerator PlayThrough(StoryBeatTrigger trigger, List<IReadOnlyList<string>> offered, int choice = 0,
            string screenshotAtFirstChoice = null)
        {
            var runner = Object.FindAnyObjectByType<DialogueRunner>();
            bool choicePending = false;
            void OnChoices(IReadOnlyList<string> choices)
            {
                offered.Add(choices);
                choicePending = true;
            }

            runner.OnChoicesShown += OnChoices;
            try
            {
                for (int step = 0; step < 200 && trigger.PlayingBeat != null; step++)
                {
                    if (choicePending)
                    {
                        choicePending = false;
                        if (screenshotAtFirstChoice != null && offered.Count == 1)
                        {
                            yield return new WaitForSecondsRealtime(0.4f); // the portrait has risen and faded in, as a reader sees it
                            TestScreenshot.Capture(Camera.main, screenshotAtFirstChoice);
                        }

                        runner.SelectChoice(Mathf.Min(choice, offered[offered.Count - 1].Count - 1));
                    }
                    else
                    {
                        runner.Advance();
                    }

                    yield return null;
                }
            }
            finally
            {
                runner.OnChoicesShown -= OnChoices;
            }

            Assert.IsNull(trigger.PlayingBeat, "The scene never finished");
        }

        private static T GetPrivateField<T>(object owner, string fieldName) where T : class
        {
            FieldInfo field = owner.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"{owner.GetType().Name}.{fieldName} field not found");
            var value = field.GetValue(owner) as T;
            Assert.IsNotNull(value, $"{owner.GetType().Name}.{fieldName} is not assigned");
            return value;
        }
    }
}
