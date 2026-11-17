// Reconstructed on 2026-11-17 by decompiling Library/ScriptAssemblies/Zmywak.Tests.PlayMode.dll (built 2026-10-11):
// the original source was lost from disk. Behaviour matches that build; original comments are gone.
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Zmywak.Kitchen.Feedback;
using Zmywak.Kitchen.Shifts;

namespace Zmywak.Tests.PlayMode
{
    public class HandWearPlayModeTests
    {
        private static readonly string[] ArmParts = new string[5] { "Glove", "Cuff", "Arm", "SleeveRoll", "Sleeve" };

        [SetUp]
        public void SetUp()
        {
            TestSaveSandbox.Enter();
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            TestSaveSandbox.DestroyPersistentObjects();
            TestSaveSandbox.Exit();
        }

        [UnityTest]
        public IEnumerator TheArm_IsDrawnByTheHandShader_AndTheShiftSoaksIn()
        {
            SceneManager.LoadScene("Kitchen_Shift", LoadSceneMode.Single);
            yield return null;
            yield return null;
            HandWear wear = Object.FindAnyObjectByType<HandWear>();
            Assert.IsNotNull(wear, "The hand has no HandWear");
            string[] armParts = ArmParts;
            foreach (string part in armParts)
            {
                Transform found = FindDeep(wear.transform, part);
                Assert.IsNotNull(found, "The arm has no " + part);
                Assert.AreEqual("Zmywak/Hand", found.GetComponent<Renderer>().sharedMaterial.shader.name, part + " is not drawn by Zmywak/Hand");
            }
            Assert.AreEqual(0.0, Shader.GetGlobalFloat("_HandSoak"), 0.05000000074505806, "Dry hands at the start of the shift");
            ShiftController shift = Object.FindAnyObjectByType<ShiftController>();
            Time.timeScale = 8f;
            float deadline = Time.realtimeSinceStartup + 20f;
            while (shift.Progress01 < 0.3f && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Time.timeScale = 1f;
            Assert.GreaterOrEqual(shift.Progress01, 0.3f, "The shift's clock did not run");
            yield return null;
            HandLook look = wear.Current;
            Assert.Greater(look.Soak, 0.2f, "A third of the way through, the water should be in");
            Assert.AreEqual(look.Soak, Shader.GetGlobalFloat("_HandSoak"), 9.999999747378752E-05, "The shader is not told");
            Assert.AreEqual(look.Tiredness, Shader.GetGlobalFloat("_HandTiredness"), 9.999999747378752E-05);
            Assert.AreEqual(0f, look.Tremor, "A rested Kuba (a kitchen on its own starts at full energy) does not shake");
            wear.Show(new HandLook
            {
                Tiredness = 1f,
                Heat = 1f,
                Soak = 1f,
                Weathering = 1f,
                Tremor = 1f
            });
            yield return null;
            Assert.AreEqual(1.0, Shader.GetGlobalFloat("_HandWeathering"), 9.999999747378752E-05);
            wear.Release();
        }

        private static Transform FindDeep(Transform root, string name)
        {
            Transform[] componentsInChildren = root.GetComponentsInChildren<Transform>(includeInactive: true);
            foreach (Transform child in componentsInChildren)
            {
                if (child.name == name)
                {
                    return child;
                }
            }
            return null;
        }
    }
}
