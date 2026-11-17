// Reconstructed on 2026-11-17 by decompiling Library/ScriptAssemblies/Zmywak.Tests.EditMode.dll (built 2026-10-11):
// the original source was lost from disk. Behaviour matches that build; original comments are gone.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Zmywak.Core.Telemetry;

namespace Zmywak.Tests
{
    public class GameplayLogTests
    {
        private string _dir;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "ZmywakGameplayLogTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            GameplayLog.DirectoryOverride = _dir;
        }

        [TearDown]
        public void TearDown()
        {
            GameplayLog.DirectoryOverride = null;
            Directory.Delete(_dir, recursive: true);
        }

        [Test]
        public void EachEntry_IsOneJsonLine_ThatReadsBack()
        {
            GameplayLog.Append(new ShiftLogEntry
            {
                shiftId = "vertical_slice",
                dishesCleaned = 7,
                scrapeSeconds = 1.5f
            });
            GameplayLog.Append(new LifeLogEntry
            {
                action = "pub",
                day = 3,
                walletEuros = 42f
            });
            GameplayLog.Append(new ShiftLogEntry
            {
                shiftId = "vertical_slice",
                dishesCleaned = 9
            });
            string[] lines = File.ReadAllLines(GameplayLog.FilePath);
            Assert.AreEqual(3, lines.Length);
            string[] array = lines;
            foreach (string line in array)
            {
                StringAssert.StartsWith("{", line);
                StringAssert.EndsWith("}", line);
                StringAssert.Contains("\"time\":\"", line);
            }
            List<ShiftLogEntry> shifts = GameplayLog.Read<ShiftLogEntry>("shift");
            Assert.AreEqual(2, shifts.Count);
            Assert.AreEqual(7, shifts[0].dishesCleaned);
            Assert.AreEqual(1.5f, shifts[0].scrapeSeconds);
            Assert.AreEqual(9, shifts[1].dishesCleaned);
            List<LifeLogEntry> life = GameplayLog.Read<LifeLogEntry>("life");
            Assert.AreEqual(1, life.Count);
            Assert.AreEqual("pub", life[0].action);
            Assert.AreEqual(42f, life[0].walletEuros);
        }

        [Test]
        public void PastTheCap_TheFileRollsOver_KeepingThePreviousOne()
        {
            LifeLogEntry entry = new LifeLogEntry
            {
                action = new string('x', 900)
            };
            int written = 0;
            while (!File.Exists(GameplayLog.PreviousFilePath) && written < 5000)
            {
                GameplayLog.Append(entry);
                written++;
            }
            Assert.IsTrue(File.Exists(GameplayLog.PreviousFilePath), "Never rolled over");
            Assert.LessOrEqual(new FileInfo(GameplayLog.PreviousFilePath).Length, 1048576L);
            Assert.AreEqual(1, File.ReadAllLines(GameplayLog.FilePath).Length, "The fresh file starts with the entry that did not fit");
        }

        [Test]
        public void AWriteThatFails_IsAWarning_NotACrash()
        {
            GameplayLog.DirectoryOverride = Path.Combine(_dir, "does", "not", "exist");
            LogAssert.Expect(LogType.Warning, new Regex("GameplayLog: could not write"));
            Assert.DoesNotThrow(() =>
            {
                GameplayLog.Append(new LifeLogEntry
                {
                    action = "pub"
                });
            });
        }
    }
}
