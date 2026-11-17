// Reconstructed on 2026-11-17 by decompiling Library/ScriptAssemblies/Zmywak.Tests.EditMode.dll (built 2026-10-11):
// the original source was lost from disk. Behaviour matches that build; original comments are gone.
using NUnit.Framework;
using Zmywak.Core.GameState;
using Zmywak.Core.GameState.States;

namespace Zmywak.Tests
{
    public class StartFromAnySceneTests
    {
        [Test]
        public void TheGamesScenes_ComeBackThroughBoot_TheRestStartAsTheyAre()
        {
            Assert.AreEqual(typeof(MainMenuState), StartFromAnyScene.StateFor("MainMenu"));
            Assert.AreEqual(typeof(MetaHubState), StartFromAnyScene.StateFor("MetaHub"));
            Assert.AreEqual(typeof(KitchenShiftState), StartFromAnyScene.StateFor("Kitchen_Shift"));
            Assert.IsNull(StartFromAnyScene.StateFor("Boot"), "Boot is the start already");
            Assert.IsNull(StartFromAnyScene.StateFor("InitTestScene637000000000000000"), "The test runner's own scene is left alone");
            Assert.IsFalse(StartFromAnyScene.TakeReturn(out var _), "Nothing to return to without a redirected start");
        }
    }
}
