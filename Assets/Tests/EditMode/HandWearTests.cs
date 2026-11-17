// Reconstructed on 2026-11-17 by decompiling Library/ScriptAssemblies/Zmywak.Tests.EditMode.dll (built 2026-10-11):
// the original source was lost from disk. Behaviour matches that build; original comments are gone.
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Zmywak.Kitchen.Feedback;

namespace Zmywak.Tests
{
    public class HandWearTests
    {
        [Test]
        public void ARestedKuba_StartsTheShiftWithFreshHands()
        {
            HandLook look = HandWearModel.Evaluate(100f, 0f, 0f, HandWearModel.FirstShift);
            Assert.AreEqual(0f, look.Tiredness);
            Assert.AreEqual(0f, look.Soak);
            Assert.AreEqual(0f, look.Weathering);
            Assert.AreEqual(0f, look.Tremor);
            Assert.That(look.Heat, Is.InRange(0.05f, 0.2f), "The water is hot from the first plate");
        }

        [Test]
        public void Tiredness_IsMostlyHowHeCameIn_AndTheShiftAddsToIt()
        {
            Assert.Greater(HandWearModel.Tiredness(30f, 0f), HandWearModel.Tiredness(80f, 0f));
            Assert.Greater(HandWearModel.Tiredness(80f, 1f), HandWearModel.Tiredness(80f, 0f));
            Assert.Greater(HandWearModel.Tiredness(20f, 0f), HandWearModel.Tiredness(100f, 1f), "Coming in exhausted should show more than a whole shift on a good night's sleep");
            Assert.AreEqual(1f, HandWearModel.Tiredness(0f, 1f));
        }

        [Test]
        public void OnlyAWornOutKuba_Shakes()
        {
            Assert.AreEqual(0f, HandWearModel.Evaluate(80f, 1f, 1f, HandWearModel.FirstShift).Tremor, "A usual week, end of the shift");
            Assert.AreEqual(0f, HandWearModel.Evaluate(40f, 0f, 0f, HandWearModel.FirstShift).Tremor, "Tired, but only just started");
            Assert.Greater(HandWearModel.Evaluate(15f, 1f, 1f, HandWearModel.FirstShift).Tremor, 0.5f, "Exhausted, end of the shift");
        }

        [Test]
        public void TheWater_SoaksInThroughTheShift_AndScrubbingFlushesTheSkin()
        {
            float previous = -1f;
            for (float progress = 0f; progress <= 1f; progress += 0.1f)
            {
                float num = HandWearModel.Soak(progress);
                Assert.GreaterOrEqual(num, previous);
                previous = num;
            }
            Assert.AreEqual(1.0, HandWearModel.Soak(0.8f), 9.999999747378752E-05, "Wet to the wrist four fifths of the way through");
            Assert.Greater(HandWearModel.Heat(0.3f, 1f), HandWearModel.Heat(0.3f, 0f) + 0.3f);
            Assert.Greater(HandWearModel.Heat(1f, 0f), HandWearModel.Heat(0f, 0f));
        }

        [Test]
        public void YearsAtTheSink_LeaveTheirMark()
        {
            Assert.AreEqual(0f, HandWearModel.Weathering(HandWearModel.FirstShift));
            Assert.That(HandWearModel.Weathering(new DateTime(2006, 5, 1)), Is.InRange(0.45f, 0.55f), "Two years in, chapter 4");
            Assert.AreEqual(1f, HandWearModel.Weathering(new DateTime(2010, 11, 29)), "The epilogue");
            Assert.AreEqual(0f, HandWearModel.Weathering(new DateTime(2003, 1, 1)), "Before Dublin");
        }

        [Test]
        public void TheHandShader_CompilesAndDrawsTheArm()
        {
            Shader shader = Shader.Find("Zmywak/Hand");
            Assert.IsNotNull(shader, "No Zmywak/Hand shader");
            Assert.IsFalse(ShaderUtil.ShaderHasError(shader), "Zmywak/Hand does not compile");
            string[] array = new string[4] { "NitrileGlove", "NitrileGloveCuff", "Skin", "ChefSleeve" };
            foreach (string name in array)
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/" + name + ".mat");
                Assert.IsNotNull(material, "No " + name + " material");
                Assert.AreEqual(shader, material.shader, name + " is not drawn by Zmywak/Hand");
            }
            Assert.Less(AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/Skin.mat").GetFloat("_WristZ"), 0f, "The skin does not know where the cuff ends");
        }

        [Test]
        public void TheGlove_KnowsItsRestPose_AndWhereTheFingertipsAre()
        {
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/_Project/Art/Meshes/GloveHand.asset");
            Assert.IsNotNull(mesh, "No modelled glove (Tools/Blender/glove.py)");
            List<Vector4> rest = new List<Vector4>();
            mesh.GetUVs(2, rest);
            Assert.AreEqual(mesh.vertexCount, rest.Count, "The glove's wear has nothing to go on");
            Assert.AreEqual(0f, rest.Min((Vector4 r) => r.w), "The palm is not a fingertip");
            Assert.Greater(rest.Max((Vector4 r) => r.w), 0.85f, "No fingertips found");
            Assert.That(rest.Max((Vector4 r) => r.z) - rest.Min((Vector4 r) => r.z), Is.InRange(0.1f, 0.3f), "Rest positions should be in metres");
        }
    }
}
