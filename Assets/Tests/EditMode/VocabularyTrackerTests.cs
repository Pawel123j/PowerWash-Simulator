// Reconstructed on 2026-11-17 by decompiling Library/ScriptAssemblies/Zmywak.Tests.EditMode.dll (built 2026-10-11):
// the original source was lost from disk. Behaviour matches that build; original comments are gone.
using NUnit.Framework;
using Zmywak.Narrative.LanguageBarrier;

namespace Zmywak.Tests
{
    public class VocabularyTrackerTests
    {
        [Test]
        public void SeedWords_AreKnownImmediately()
        {
            VocabularyTracker vocabularyTracker = new VocabularyTracker(new string[2] { "hello", "chef" });
            Assert.IsTrue(vocabularyTracker.IsKnown("hello"));
            Assert.IsTrue(vocabularyTracker.IsKnown("CHEF"));
            Assert.IsFalse(vocabularyTracker.IsKnown("kitchen"));
        }

        [Test]
        public void Learn_AddsWord_CaseInsensitive()
        {
            VocabularyTracker vocabularyTracker = new VocabularyTracker();
            vocabularyTracker.Learn("Grand");
            Assert.IsTrue(vocabularyTracker.IsKnown("grand"));
            Assert.IsTrue(vocabularyTracker.IsKnown("GRAND"));
        }

        [Test]
        public void UnknownWord_IsNotKnown()
        {
            Assert.IsFalse(new VocabularyTracker().IsKnown("anything"));
        }
    }
}
