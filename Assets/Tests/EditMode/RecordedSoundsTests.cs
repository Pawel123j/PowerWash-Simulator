// Reconstructed on 2026-11-17 by decompiling Library/ScriptAssemblies/Zmywak.Tests.EditMode.dll (built 2026-10-11):
// the original source was lost from disk. Behaviour matches that build; original comments are gone.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Zmywak.Kitchen.Feedback;

namespace Zmywak.Tests
{
    public class RecordedSoundsTests
    {
        private static string Folder => Path.Combine(Application.dataPath, "_Project/Audio/Kitchen");

        private static IEnumerable<ProceduralSounds.Sound> AllSounds => Enum.GetValues(typeof(ProceduralSounds.Sound)).Cast<ProceduralSounds.Sound>();

        private static IEnumerable<string> Files
        {
            get
            {
                if (!Directory.Exists(Folder))
                {
                    return new string[0];
                }
                return from f in Directory.GetFiles(Folder, "*.wav").Select(Path.GetFileName)
                    orderby f
                    select f;
            }
        }

        [TearDown]
        public void TearDown()
        {
            KitchenSounds.Reset();
        }

        [TestCaseSource("AllSounds")]
        public void EverySound_HasARecordedTake(ProceduralSounds.Sound sound)
        {
            KitchenSounds.Reset();
            Assert.IsTrue(KitchenSounds.IsRecorded(sound), $"{sound} would still be synthesised");
            Assert.IsTrue(KitchenSounds.Bank.TakesOf(sound).All((AudioClip c) => c != null), $"{sound} has a missing take");
        }

        [Test]
        public void EveryTake_IsCredited()
        {
            string credits = File.ReadAllText(Path.Combine(Application.dataPath, "../CREDITS.md"));
            Assert.IsNotEmpty(Files.ToList(), "No recordings in the project");
            foreach (string file in Files)
            {
                StringAssert.Contains(file, credits, file + " is not in CREDITS.md");
            }
        }

        [TestCaseSource("Files")]
        public void Take_IsAShortMonoClip_ThatEndsCleanly(string file)
        {
            float[] samples = ReadWav(Path.Combine(Folder, file), out var channels, out var rate, out var bits);
            Assert.AreEqual(1, channels, "Not mono");
            Assert.AreEqual(44100, rate);
            Assert.AreEqual(16, bits);
            Assert.That((float)samples.Length / (float)rate, Is.InRange(0.05f, 5f), file + " has an odd length");
            float peak = ((IEnumerable<float>)samples).Max((Func<float, float>)Mathf.Abs);
            Assert.Greater(peak, 0.1f, file + " is too quiet to hear in the mix");
            if (ProceduralSounds.IsLoop((ProceduralSounds.Sound)Enum.Parse(typeof(ProceduralSounds.Sound), file.Substring(0, file.LastIndexOf('_')))))
            {
                float[] steps = new float[samples.Length - 1];
                for (int i = 0; i < steps.Length; i++)
                {
                    steps[i] = Mathf.Abs(samples[i + 1] - samples[i]);
                }
                Array.Sort(steps);
                Assert.LessOrEqual(Mathf.Abs(samples[0] - samples[^1]), steps[(int)((float)steps.Length * 0.99f)], file + " ticks when it loops");
            }
            else
            {
                float tail = 0f;
                for (int j = samples.Length - rate / 1000; j < samples.Length; j++)
                {
                    tail = Mathf.Max(tail, Mathf.Abs(samples[j]));
                }
                Assert.Less(tail, peak * 0.05f, file + " is cut off instead of fading out");
            }
        }

        [Test]
        public void ATake_IsNeverPlayedTwiceRunning()
        {
            for (int count = 2; count <= 5; count++)
            {
                for (int last = 0; last < count; last++)
                {
                    HashSet<int> seen = new HashSet<int>();
                    for (int roll = 0; roll < 50; roll++)
                    {
                        int take = KitchenSounds.NextTake(last, count, roll);
                        Assert.That(take, Is.InRange(0, count - 1));
                        Assert.AreNotEqual(last, take);
                        seen.Add(take);
                    }
                    Assert.AreEqual(count - 1, seen.Count, "Some take is never picked");
                }
            }
            Assert.AreEqual(0, KitchenSounds.NextTake(0, 1, 7), "A single take just repeats");
            Assert.That(KitchenSounds.NextTake(-1, 3, int.MinValue), Is.InRange(0, 2));
        }

        [Test]
        public void WithoutATake_TheSynthesisedSoundPlays()
        {
            KitchenSoundBank bank = ScriptableObject.CreateInstance<KitchenSoundBank>();
            try
            {
                KitchenSounds.Use(bank);
                Assert.IsFalse(KitchenSounds.IsRecorded(ProceduralSounds.Sound.Clink));
                Assert.AreSame(ProceduralSounds.Clip(ProceduralSounds.Sound.Clink), KitchenSounds.Clip(ProceduralSounds.Sound.Clink));
                AudioClip recorded = AudioClip.Create("take", 100, 1, 44100, stream: false);
                bank.SetTakes(ProceduralSounds.Sound.Clink, new AudioClip[1] { recorded });
                Assert.AreSame(recorded, KitchenSounds.Clip(ProceduralSounds.Sound.Clink));
                UnityEngine.Object.DestroyImmediate(recorded);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(bank);
            }
        }

        private static float[] ReadWav(string path, out int channels, out int rate, out int bits)
        {
            channels = (rate = (bits = 0));
            using (BinaryReader r = new BinaryReader(File.OpenRead(path)))
            {
                Assert.AreEqual("RIFF", new string(r.ReadChars(4)));
                r.ReadInt32();
                Assert.AreEqual("WAVE", new string(r.ReadChars(4)));
                while (r.BaseStream.Position < r.BaseStream.Length)
                {
                    string id = new string(r.ReadChars(4));
                    int size = r.ReadInt32();
                    if (id == "fmt ")
                    {
                        Assert.AreEqual(1, r.ReadInt16(), "Not PCM");
                        channels = r.ReadInt16();
                        rate = r.ReadInt32();
                        r.ReadInt32();
                        r.ReadInt16();
                        bits = r.ReadInt16();
                        r.ReadBytes(size - 16);
                        continue;
                    }
                    if (id == "data")
                    {
                        float[] samples = new float[size / 2 / Math.Max(1, channels)];
                        for (int i = 0; i < samples.Length; i++)
                        {
                            samples[i] = (float)r.ReadInt16() / 32768f;
                            for (int c = 1; c < channels; c++)
                            {
                                r.ReadInt16();
                            }
                        }
                        return samples;
                    }
                    r.ReadBytes(size);
                }
            }
            Assert.Fail(path + " has no data");
            return null;
        }
    }
}
