// Reconstructed on 2026-11-17 by decompiling Library/ScriptAssemblies/Zmywak.Tests.EditMode.dll (built 2026-10-11):
// the original source was lost from disk. Behaviour matches that build; original comments are gone.
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Zmywak.Kitchen.Feedback;

namespace Zmywak.Tests
{
    public class ProceduralSoundsTests
    {
        private static IEnumerable<ProceduralSounds.Sound> AllSounds => Enum.GetValues(typeof(ProceduralSounds.Sound)).Cast<ProceduralSounds.Sound>();

        private static IEnumerable<ProceduralSounds.Sound> Loops => AllSounds.Where(ProceduralSounds.IsLoop);

        private static IEnumerable<ProceduralSounds.Sound> OneShots => AllSounds.Where((ProceduralSounds.Sound s) => !ProceduralSounds.IsLoop(s));

        [TestCaseSource("AllSounds")]
        public void Sound_IsAudible_FiniteAndNeverClips(ProceduralSounds.Sound sound)
        {
            float[] samples = ProceduralSounds.Generate(sound);
            Assert.That((float)samples.Length / 22050f, Is.InRange(0.05f, 5f), $"{sound} has an odd length");
            Assert.IsTrue(samples.All((float v) => !float.IsNaN(v) && !float.IsInfinity(v)), $"{sound} has non-finite samples");
            float arg = ((IEnumerable<float>)samples).Max((Func<float, float>)Mathf.Abs);
            Assert.LessOrEqual(arg, 1f, $"{sound} would clip");
            Assert.Greater(arg, 0.2f, $"{sound} is too quiet to hear in the mix");
            Assert.Greater(Rms(samples, 0, samples.Length), 0.02f, $"{sound} is nearly silent");
        }

        [TestCaseSource("AllSounds")]
        public void Sound_IsDeterministic(ProceduralSounds.Sound sound)
        {
            CollectionAssert.AreEqual(ProceduralSounds.Generate(sound), ProceduralSounds.Generate(sound));
        }

        [TestCaseSource("Loops")]
        public void Loop_WrapsWithoutAClick(ProceduralSounds.Sound sound)
        {
            float[] samples = ProceduralSounds.Generate(sound);
            float[] steps = new float[samples.Length - 1];
            for (int i = 0; i < steps.Length; i++)
            {
                steps[i] = Mathf.Abs(samples[i + 1] - samples[i]);
            }
            Array.Sort(steps);
            float typicalLargeStep = steps[(int)((float)steps.Length * 0.99f)];
            float wrapStep = Mathf.Abs(samples[0] - samples[^1]);
            Assert.LessOrEqual(wrapStep, typicalLargeStep, $"{sound} jumps by {wrapStep:F3} when it loops (99th percentile step {typicalLargeStep:F3})");
        }

        [TestCaseSource("OneShots")]
        public void OneShot_FadesOutInsteadOfCuttingOff(ProceduralSounds.Sound sound)
        {
            float[] samples = ProceduralSounds.Generate(sound);
            int tail = 22;
            float tailPeak = 0f;
            for (int i = samples.Length - tail; i < samples.Length; i++)
            {
                tailPeak = Mathf.Max(tailPeak, Mathf.Abs(samples[i]));
            }
            float peak = ((IEnumerable<float>)samples).Max((Func<float, float>)Mathf.Abs);
            Assert.Less(tailPeak, peak * 0.05f, $"{sound} is still at {tailPeak / peak:P0} of its peak when it ends");
        }

        [Test]
        public void Clip_IsMonoAtTheGeneratorRate_AndCached()
        {
            AudioClip clip = ProceduralSounds.Clip(ProceduralSounds.Sound.Clink);
            Assert.IsNotNull(clip);
            Assert.AreEqual(1, clip.channels);
            Assert.AreEqual(22050, clip.frequency);
            Assert.AreEqual(ProceduralSounds.Generate(ProceduralSounds.Sound.Clink).Length, clip.samples);
            Assert.AreSame(clip, ProceduralSounds.Clip(ProceduralSounds.Sound.Clink), "Clips should be generated once and reused");
        }

        private static float Rms(float[] samples, int start, int end)
        {
            double sum = 0.0;
            for (int i = start; i < end; i++)
            {
                sum += (double)(samples[i] * samples[i]);
            }
            return (float)Math.Sqrt(sum / (double)Math.Max(1, end - start));
        }
    }
}
