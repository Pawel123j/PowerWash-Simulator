// Reconstructed on 2026-11-17 by decompiling Library/ScriptAssemblies/Zmywak.Tests.EditMode.dll (built 2026-10-11):
// the original source was lost from disk. Behaviour matches that build; original comments are gone.
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Zmywak.Core.Data;
using Zmywak.Kitchen.Radio;

namespace Zmywak.Tests
{
    public class RadioTests
    {
        private static IEnumerable<RadioStation> OnAir => new RadioStation[3]
        {
            RadioStation.Trad,
            RadioStation.Pop,
            RadioStation.News
        };

        [TestCaseSource("OnAir")]
        public void AStation_IsAudible_FiniteAndNeverClips(RadioStation station)
        {
            float[] samples = RadioMusic.Generate(station);
            Assert.That((float)samples.Length / 22050f, Is.InRange(8f, 30f), $"{station} loop has an odd length");
            Assert.IsTrue(samples.All((float v) => !float.IsNaN(v) && !float.IsInfinity(v)));
            float arg = ((IEnumerable<float>)samples).Max((Func<float, float>)Mathf.Abs);
            Assert.LessOrEqual(arg, 1f, $"{station} would clip");
            Assert.Greater(arg, 0.3f);
            Assert.Greater(Math.Sqrt(samples.Sum((float v) => (double)v * (double)v) / (double)samples.Length), 0.05, $"{station} is nearly silent");
        }

        [TestCaseSource("OnAir")]
        public void AStation_LoopsWithoutAClick(RadioStation station)
        {
            float[] samples = RadioMusic.Generate(station);
            float[] steps = new float[samples.Length - 1];
            for (int i = 0; i < steps.Length; i++)
            {
                steps[i] = Mathf.Abs(samples[i + 1] - samples[i]);
            }
            Array.Sort(steps);
            float typicalLargeStep = steps[(int)((float)steps.Length * 0.99f)];
            float wrapStep = Mathf.Abs(samples[0] - samples[^1]);
            Assert.LessOrEqual(wrapStep, typicalLargeStep, $"{station} jumps by {wrapStep:F3} when it loops");
        }

        [TestCaseSource("OnAir")]
        public void AStation_IsTheSameEveryTime(RadioStation station)
        {
            CollectionAssert.AreEqual(RadioMusic.Generate(station), RadioMusic.Generate(station));
        }

        [Test]
        public void TheSmallSpeaker_HasNoRealBass()
        {
            int n = 22050;
            float[] mix = (from i in Enumerable.Range(0, n)
                select 0.3f * Mathf.Sin((float)Math.PI * 120f * (float)i / (float)n) + 0.3f * Mathf.Sin((float)Math.PI * 2000f * (float)i / (float)n)).ToArray();
            float[] heard = RadioMusic.SmallSpeaker(mix);
            Assert.Less(Amplitude(60f), 0.5 * Amplitude(1000f), "A kitchen radio's speaker should not carry bass");
            Assert.IsEmpty(RadioMusic.Generate(RadioStation.Off), "Off plays nothing");
            double Amplitude(float hz)
            {
                double sin = 0.0;
                double cos = 0.0;
                for (int i = 0; i < n; i++)
                {
                    double phase = Math.PI * 2.0 * (double)hz * (double)i / (double)n;
                    sin += (double)heard[i] * Math.Sin(phase);
                    cos += (double)heard[i] * Math.Cos(phase);
                }
                return 2.0 * Math.Sqrt(sin * sin + cos * cos) / (double)n;
            }
        }

        [Test]
        public void TheDial_GoesTradPopNewsOffAndRound()
        {
            Assert.AreEqual(RadioStation.Pop, RadioStations.Next(RadioStation.Trad));
            Assert.AreEqual(RadioStation.News, RadioStations.Next(RadioStation.Pop));
            Assert.AreEqual(RadioStation.Off, RadioStations.Next(RadioStation.News));
            Assert.AreEqual(RadioStation.Trad, RadioStations.Next(RadioStation.Off));
            foreach (RadioStation value in Enum.GetValues(typeof(RadioStation)))
            {
                Assert.IsNotEmpty(RadioStations.Label(value));
            }
        }

        [Test]
        public void TheNews_FollowsTheYearInTheGame()
        {
            StringAssert.Contains("Unii", string.Join(" ", RadioNews.For(new DateTime(2004, 5, 1))), "Day one: the EU opening its jobs");
            StringAssert.Contains("dźwigów", string.Join(" ", RadioNews.For(new DateTime(2006, 5, 1))), "The boom");
            StringAssert.Contains("depozyty", string.Join(" ", RadioNews.For(new DateTime(2008, 9, 29))), "The bank guarantee");
            StringAssert.Contains("MFW", string.Join(" ", RadioNews.For(new DateTime(2010, 11, 29))), "The rescue");
            Assert.IsNotEmpty(RadioNews.For(new DateTime(2003, 1, 1)), "Before the story starts there is still news");
        }

        [Test]
        public void EveryDayOfTheStory_HasSomethingOnTheNews()
        {
            DateTime day = new DateTime(2004, 5, 1);
            while (day <= new DateTime(2010, 12, 31))
            {
                Assert.GreaterOrEqual(RadioNews.For(day).Count, 2, $"Thin news on {day:yyyy-MM-dd}");
                day = day.AddDays(17.0);
            }
        }

        [Test]
        public void AfterTheMove_TheRadioIsTheirs()
        {
            StringAssert.Contains("Niemcy", string.Join(" ", RadioNews.For(new DateTime(2011, 6, 1), Place.Krakow)), "Kraków, 2011: Germany opens its jobs");
            StringAssert.Contains("Irlandia zagra w Poznaniu", string.Join(" ", RadioNews.For(new DateTime(2012, 3, 1), Place.Krakow)));
            StringAssert.Contains("Ruda żelaza", string.Join(" ", RadioNews.For(new DateTime(2010, 12, 1), Place.Pilbara)));
            StringAssert.Contains("Folk", RadioStations.Label(RadioStation.Trad, Place.Krakow));
            StringAssert.Contains("News", RadioStations.Label(RadioStation.News, Place.Pilbara));
            Assert.AreEqual(RadioStations.Label(RadioStation.Trad), RadioStations.Label(RadioStation.Trad, Place.Dublin));
        }
    }
}
