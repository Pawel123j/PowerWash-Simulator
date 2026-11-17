// Reconstructed on 2026-11-17 by decompiling Library/ScriptAssemblies/Zmywak.Tests.PlayMode.dll (built 2026-10-11):
// the original source was lost from disk. Behaviour matches that build; original comments are gone.
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Zmywak.Core.Save;
using Zmywak.Kitchen.Shifts;
using Zmywak.Meta.Progress;
using Zmywak.Narrative.Progress;

namespace Zmywak.Tests.PlayMode
{
    public class AppLifecyclePlayModeTests
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
        public IEnumerator GoingToTheBackground_WithNowhereToSave_DoesNotBreakTheGame()
        {
            Scene previouslyActive = SceneManager.GetActiveScene();
            SceneManager.LoadScene("Boot", LoadSceneMode.Single);
            PlayDeadline deadline = new PlayDeadline(10f);
            while ((SceneManager.GetActiveScene() == previouslyActive || SceneManager.GetActiveScene().name != "MainMenu") && !deadline.Passed)
            {
                yield return null;
            }
            MetaProgressController meta = Object.FindAnyObjectByType<MetaProgressController>();
            NarrativeProgressController narrative = Object.FindAnyObjectByType<NarrativeProgressController>();
            meta.CallMom();
            narrative.SetFlag("ch1_refused_agency");
            string folder = SaveSystem.DirectoryOverride;
            SaveSystem.DirectoryOverride = Path.Combine(folder, "gone", "away");
            for (int attempt = 0; attempt < 3; attempt++)
            {
                LogAssert.Expect(LogType.Warning, new Regex("could not save"));
            }
            meta.SendMessage("OnApplicationPause", true);
            narrative.SendMessage("OnApplicationPause", true);
            meta.SendMessage("OnApplicationFocus", false);
            yield return null;
            Assert.IsNotNull(Object.FindAnyObjectByType<MetaProgressController>(), "The game did not survive the failed save");
            SaveSystem.DirectoryOverride = folder;
            narrative.SetFlag("ch1_after_the_failed_save");
            SaveData saveData = SaveSystem.Load();
            Assert.IsNotNull(saveData, "Nothing was saved once the disk worked again");
            CollectionAssert.Contains(saveData.storyFlags, "ch1_after_the_failed_save");
        }

        [UnityTest]
        public IEnumerator KilledMidShift_TheGameComesBackAtTheLastSavedMoment()
        {
            Scene previouslyActive = SceneManager.GetActiveScene();
            SceneManager.LoadScene("Boot", LoadSceneMode.Single);
            PlayDeadline deadline = new PlayDeadline(10f);
            while ((SceneManager.GetActiveScene() == previouslyActive || SceneManager.GetActiveScene().name != "MainMenu") && !deadline.Passed)
            {
                yield return null;
            }
            MetaProgressController meta = Object.FindAnyObjectByType<MetaProgressController>();
            NarrativeProgressController narrative = Object.FindAnyObjectByType<NarrativeProgressController>();
            meta.CallMom();
            narrative.SetFlag("ch1_refused_agency");
            int day = meta.DayNumber;
            float wallet = meta.Wallet;
            float homesickness = meta.Stats.Homesickness;
            SceneManager.LoadScene("Kitchen_Shift", LoadSceneMode.Single);
            yield return new WaitForSeconds(2f);
            Assert.IsFalse(Object.FindAnyObjectByType<ShiftController>().IsShiftOver);
            meta.SendMessage("OnApplicationPause", true);
            narrative.SendMessage("OnApplicationPause", true);
            TestSaveSandbox.DestroyPersistentObjects();
            yield return null;
            Assert.IsNotNull(SaveSystem.Load(), "The save did not survive the app being killed");
            MetaProgressController relaunchedMeta = new GameObject("Meta (relaunched)").AddComponent<MetaProgressController>();
            NarrativeProgressController relaunchedNarrative = new GameObject("Narrative (relaunched)").AddComponent<NarrativeProgressController>();
            yield return null;
            Assert.AreEqual(day, relaunchedMeta.DayNumber);
            Assert.AreEqual(wallet, relaunchedMeta.Wallet, 9.999999747378752E-05);
            Assert.AreEqual(homesickness, relaunchedMeta.Stats.Homesickness, 9.999999747378752E-05);
            Assert.IsTrue(relaunchedNarrative.HasFlag("ch1_refused_agency"));
        }
    }
}
