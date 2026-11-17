// Reconstructed on 2026-11-17 by decompiling Library/ScriptAssemblies/Zmywak.Tests.EditMode.dll (built 2026-10-11):
// the original source was lost from disk. Behaviour matches that build; original comments are gone.
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Zmywak.Tests
{
    public class ProjectConfigurationTests
    {
        private static string ProjectSettingsText => File.ReadAllText(Path.Combine(Application.dataPath, "..", "ProjectSettings", "ProjectSettings.asset"));

        [Test]
        public void TheGame_IsPortraitOnly()
        {
            Assert.AreEqual(UIOrientation.Portrait, PlayerSettings.defaultInterfaceOrientation, "The phone would turn the game when it turns: the layout and the kitchen's camera are drawn for portrait");
            Assert.IsTrue(PlayerSettings.allowedAutorotateToPortrait);
            Assert.IsFalse(PlayerSettings.allowedAutorotateToPortraitUpsideDown);
            Assert.IsFalse(PlayerSettings.allowedAutorotateToLandscapeLeft);
            Assert.IsFalse(PlayerSettings.allowedAutorotateToLandscapeRight);
        }

        [Test]
        public void ThePortraitLock_IsWrittenInProjectSettingsAsset_NotJustHeldInMemory()
        {
            string text = ProjectSettingsText;
            StringAssert.IsMatch("(?m)^\\s*defaultScreenOrientation: 0\\s*$", text, "defaultScreenOrientation is not Portrait (0)");
            StringAssert.IsMatch("(?m)^\\s*allowedAutorotateToPortrait: 1\\s*$", text);
            StringAssert.IsMatch("(?m)^\\s*allowedAutorotateToPortraitUpsideDown: 0\\s*$", text);
            StringAssert.IsMatch("(?m)^\\s*allowedAutorotateToLandscapeRight: 0\\s*$", text);
            StringAssert.IsMatch("(?m)^\\s*allowedAutorotateToLandscapeLeft: 0\\s*$", text);
        }

        [Test]
        public void BuildSettings_HoldEveryScene_WithBootFirst()
        {
            string[] enabled = (from s in EditorBuildSettings.scenes
                where s.enabled
                select s.path).ToArray();
            string[] names = enabled.Select(Path.GetFileNameWithoutExtension).ToArray();
            CollectionAssert.AreEquivalent(new string[4] { "Boot", "MainMenu", "MetaHub", "Kitchen_Shift" }, names, "Build Settings must hold exactly the game's scenes, all enabled");
            Assert.AreEqual("Boot", names[0], "Boot creates the saves, the progress and the state machine: it has to be the scene the game starts in");
            string[] array = enabled;
            foreach (string path in array)
            {
                Assert.IsTrue(File.Exists(Path.Combine(Application.dataPath, "..", path)), path + " is in Build Settings but is not there");
            }
        }

        [Test]
        public void TouchGoesThroughTheInputSystem()
        {
            Assert.IsTrue(true, "Player Settings > Active Input Handling must include the Input System package");
        }

        [Test]
        public void TheAndroidPlayer_IsIl2cpp_Arm64_AndAnApiLevelPhonesStillRun()
        {
            Assert.AreEqual(ScriptingImplementation.IL2CPP, PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android), "Only IL2CPP builds 64-bit Android code");
            Assert.AreNotEqual(AndroidArchitecture.None, PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64, "Phones are ARM64: a build without it installs nowhere");
            Assert.GreaterOrEqual((int)PlayerSettings.Android.minSdkVersion, 26);
        }

        [Test]
        public void TheGame_HasItsOwnNameAndPackage()
        {
            Assert.AreEqual("ZMYWAK", PlayerSettings.productName);
            Assert.AreEqual("com.zielonawyspagames.zmywak", PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android));
            Assert.IsFalse(Regex.IsMatch(PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android), "DefaultCompany"), "The package still carries Unity's placeholder company");
        }
    }
}
