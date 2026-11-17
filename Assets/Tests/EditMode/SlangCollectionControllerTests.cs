// Reconstructed on 2026-11-17 by decompiling Library/ScriptAssemblies/Zmywak.Tests.EditMode.dll (built 2026-10-11):
// the original source was lost from disk. Behaviour matches that build; original comments are gone.
using NUnit.Framework;
using Zmywak.Narrative.SlangDictionary;

namespace Zmywak.Tests
{
    public class SlangCollectionControllerTests
    {
        [Test]
        public void Unlock_MakesEntryUnlocked()
        {
            SlangCollectionController slangCollectionController = new SlangCollectionController();
            Assert.IsFalse(slangCollectionController.IsUnlocked("slang_grand"));
            slangCollectionController.Unlock("slang_grand");
            Assert.IsTrue(slangCollectionController.IsUnlocked("slang_grand"));
        }

        [Test]
        public void UnlockingTwice_IsHarmless()
        {
            SlangCollectionController collection = new SlangCollectionController();
            collection.Unlock("slang_grand");
            collection.Unlock("slang_grand");
            Assert.AreEqual(1, collection.UnlockedIds.Count);
        }
    }
}
