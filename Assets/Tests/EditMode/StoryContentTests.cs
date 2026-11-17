// Reconstructed on 2026-11-17 by decompiling Library/ScriptAssemblies/Zmywak.Tests.EditMode.dll (built 2026-10-11):
// the original source was lost from disk. Behaviour matches that build; original comments are gone.
using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Zmywak.Narrative.Dialogue;
using Zmywak.Narrative.LanguageBarrier;
using Zmywak.Narrative.SlangDictionary;
using Zmywak.Narrative.Story;

namespace Zmywak.Tests
{
    [TestFixture(new object[] { "Chapter_ch1.asset" })]
    [TestFixture(new object[] { "Chapter_ch2.asset" })]
    [TestFixture(new object[] { "Chapter_ch3.asset" })]
    [TestFixture(new object[] { "Chapter_ch4.asset" })]
    [TestFixture(new object[] { "Chapter_ch5.asset" })]
    [TestFixture(new object[] { "Chapter_ch6.asset" })]
    public class StoryContentTests
    {
        private const string ContentFolder = "Assets/_Project/ScriptableObjects";

        private static readonly string[] ChaptersInOrder = new string[6] { "Chapter_ch1.asset", "Chapter_ch2.asset", "Chapter_ch3.asset", "Chapter_ch4.asset", "Chapter_ch5.asset", "Chapter_ch6.asset" };

        private readonly string _assetName;

        private StoryChapter _chapter;

        private List<StoryChapter> _earlier;

        public StoryContentTests(string assetName)
        {
            _assetName = assetName;
        }

        [SetUp]
        public void SetUp()
        {
            _chapter = Load(_assetName);
            _earlier = new List<StoryChapter>();
            string[] chaptersInOrder = ChaptersInOrder;
            foreach (string name in chaptersInOrder)
            {
                if (!(name == _assetName))
                {
                    _earlier.Add(Load(name));
                    continue;
                }
                break;
            }
        }

        [Test]
        public void Scenes_RunDayByDay_FromTheDayAfterTheLastChapter_AndTheLastOneEndsTheChapter()
        {
            int firstDay = 1;
            if (_earlier.Count > 0)
            {
                IReadOnlyList<StoryBeat> beats = _earlier[_earlier.Count - 1].Beats;
                firstDay = beats[beats.Count - 1].day + 1;
            }
            Assert.GreaterOrEqual(_chapter.Beats.Count, 5);
            for (int i = 0; i < _chapter.Beats.Count; i++)
            {
                StoryBeat beat = _chapter.Beats[i];
                Assert.IsNotNull(beat.graph, "Scene " + beat.beatId + " has no dialogue");
                Assert.AreEqual(firstDay + i, beat.day, $"Scene {beat.beatId} should play on day {firstDay + i}");
                Assert.AreEqual(i == _chapter.Beats.Count - 1, beat.endsChapter, "Only the last scene ends the chapter (" + beat.beatId + ")");
            }
        }

        [Test]
        public void AStartDate_MovesTheCalendarForward_NeverBack()
        {
            DateTime calendar = new DateTime(2004, 5, 1);
            int day = 1;
            foreach (StoryChapter chapter in new List<StoryChapter>(_earlier) { _chapter })
            {
                int first = chapter.Beats[0].day;
                calendar = calendar.AddDays(first - day);
                day = first;
                if (chapter.TryGetStartDate(out var starts))
                {
                    if (chapter == _chapter)
                    {
                        Assert.Greater(starts, calendar, _chapter.ChapterId + " would move the calendar back (or nowhere)");
                    }
                    calendar = ((starts > calendar) ? starts : calendar);
                }
            }
        }

        [Test]
        public void YearsLater_KubaUnderstandsEveryEnglishWord()
        {
            if (!_chapter.TryGetStartDate(out var _))
            {
                Assert.Pass(_chapter.ChapterId + " is told in Kuba's first weeks - the barrier is the point");
            }
            foreach (StoryBeat beat in _chapter.Beats)
            {
                VocabularyTracker known = new VocabularyTracker(beat.graph.FindNode(beat.graph.StartNodeId).teachVocabularyWords);
                foreach (DialogueNode node in beat.graph.Nodes)
                {
                    foreach (DialogueLine line in node.lines)
                    {
                        if (line.language == DialogueLanguage.English)
                        {
                            Assert.AreEqual(line.text, WordBlurFormatter.Format(line.text, known), beat.graph.name + "/" + node.nodeId);
                        }
                    }
                }
            }
        }

        [Test]
        public void SceneIds_AreNotUsedByAnyOtherChapter()
        {
            HashSet<string> mine = new HashSet<string>();
            foreach (StoryBeat beat in _chapter.Beats)
            {
                Assert.IsTrue(mine.Add(beat.beatId), beat.beatId + " appears twice in " + _chapter.ChapterId);
            }
            string[] chaptersInOrder = ChaptersInOrder;
            foreach (string name in chaptersInOrder)
            {
                StoryChapter other = Load(name);
                if (other == _chapter)
                {
                    continue;
                }
                Assert.AreNotEqual(_chapter.ChapterId, other.ChapterId, name + " has the same chapter id");
                foreach (StoryBeat beat2 in other.Beats)
                {
                    Assert.IsFalse(mine.Contains(beat2.beatId), beat2.beatId + " is used by both " + _chapter.ChapterId + " and " + other.ChapterId);
                }
            }
        }

        [Test]
        public void EveryLink_LeadsToANode_AndEveryNodeIsReachable()
        {
            foreach (StoryBeat beat in _chapter.Beats)
            {
                DialogueGraph graph = beat.graph;
                Assert.IsNotNull(graph.FindNode(graph.StartNodeId), graph.name + ": no start node '" + graph.StartNodeId + "'");
                HashSet<string> reached = new HashSet<string>();
                Queue<string> queue = new Queue<string>();
                queue.Enqueue(graph.StartNodeId);
                while (queue.Count > 0)
                {
                    string id = queue.Dequeue();
                    if (!reached.Add(id))
                    {
                        continue;
                    }
                    foreach (string target in Targets(graph.FindNode(id)))
                    {
                        Assert.IsNotNull(graph.FindNode(target), graph.name + "/" + id + " leads to a missing node '" + target + "'");
                        queue.Enqueue(target);
                    }
                }
                foreach (DialogueNode node in graph.Nodes)
                {
                    Assert.IsTrue(reached.Contains(node.nodeId), graph.name + "/" + node.nodeId + " can never be reached");
                }
            }
        }

        [Test]
        public void EveryPath_EndsTheScene_WithoutLooping()
        {
            foreach (StoryBeat beat in _chapter.Beats)
            {
                AssertNoCycle(beat.graph, beat.graph.StartNodeId, new List<string>());
            }
        }

        [Test]
        public void EverySpeaker_HasAName_AndNoLineIsEmpty()
        {
            foreach (StoryBeat beat in _chapter.Beats)
            {
                foreach (DialogueNode node in beat.graph.Nodes)
                {
                    foreach (DialogueLine line in node.lines)
                    {
                        Assert.IsFalse(string.IsNullOrWhiteSpace(line.text), beat.graph.name + "/" + node.nodeId + " has an empty line");
                        Assert.IsTrue(line.speakerId == "narrator" || SpeakerNames.DisplayName(line.speakerId) != line.speakerId, beat.graph.name + "/" + node.nodeId + ": speaker '" + line.speakerId + "' has no display name");
                    }
                }
            }
        }

        [Test]
        public void EverySpeaker_HasAPortrait()
        {
            foreach (StoryBeat beat in _chapter.Beats)
            {
                foreach (DialogueNode node in beat.graph.Nodes)
                {
                    foreach (DialogueLine line in node.lines)
                    {
                        string id = SpeakerNames.PortraitId(line.speakerId);
                        if (id != null)
                        {
                            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Portraits/Portrait_" + id + ".png"), beat.graph.name + "/" + node.nodeId + ": '" + line.speakerId + "' has no portrait sprite (Tools/Blender/portraits.py, then Run All)");
                        }
                    }
                }
            }
        }

        [Test]
        public void Money_IsOnlyTakenRightAfterAChoice()
        {
            foreach (StoryBeat beat in _chapter.Beats)
            {
                DialogueGraph graph = beat.graph;
                HashSet<string> chosen = new HashSet<string>();
                HashSet<string> stepped = new HashSet<string> { graph.StartNodeId };
                foreach (DialogueNode node in graph.Nodes)
                {
                    foreach (DialogueChoice choice in node.choices)
                    {
                        chosen.Add(choice.targetNodeId);
                    }
                    stepped.Add(node.nextNodeId);
                    stepped.Add(node.branchNodeId);
                }
                foreach (DialogueNode node2 in graph.Nodes)
                {
                    if (node2.outcome.moneyEuros < 0f)
                    {
                        Assert.IsTrue(chosen.Contains(node2.nodeId) && !stepped.Contains(node2.nodeId), graph.name + "/" + node2.nodeId + " takes money without a choice that leads straight to it");
                    }
                }
            }
        }

        [Test]
        public void EveryChoice_HasAFreeOption_ThatIsAlwaysOffered()
        {
            foreach (StoryBeat beat in _chapter.Beats)
            {
                foreach (DialogueNode node in beat.graph.Nodes)
                {
                    if (node.choices.Count != 0)
                    {
                        Assert.IsTrue(node.choices.Exists((DialogueChoice c) => string.IsNullOrEmpty(c.requiredFlag) && DialogueSession.CostOf(beat.graph, c) <= 0f), beat.graph.name + "/" + node.nodeId + ": every option costs money or needs a flag");
                        Assert.LessOrEqual(node.choices.Count, 3, beat.graph.name + "/" + node.nodeId + ": the dialogue box has three choice slots");
                    }
                }
            }
        }

        [Test]
        public void UnlockedSlang_IsInTheDictionary()
        {
            HashSet<string> known = new HashSet<string>();
            string[] array = AssetDatabase.FindAssets("t:SlangEntry", new string[1] { "Assets/_Project/ScriptableObjects" });
            foreach (string guid in array)
            {
                known.Add(AssetDatabase.LoadAssetAtPath<SlangEntry>(AssetDatabase.GUIDToAssetPath(guid)).Id);
            }
            foreach (StoryBeat beat in _chapter.Beats)
            {
                foreach (DialogueNode node in beat.graph.Nodes)
                {
                    foreach (string slangId in node.unlockSlangIds)
                    {
                        Assert.IsTrue(known.Contains(slangId), beat.graph.name + "/" + node.nodeId + " unlocks unknown slang '" + slangId + "'");
                    }
                }
            }
        }

        [Test]
        public void FlagsTheStoryAsksFor_AreSetEarlier()
        {
            HashSet<string> setSoFar = new HashSet<string>();
            foreach (StoryChapter earlier in _earlier)
            {
                setSoFar.Add(earlier.CompletedFlag);
                foreach (StoryBeat beat2 in earlier.Beats)
                {
                    foreach (DialogueNode node in beat2.graph.Nodes)
                    {
                        setSoFar.UnionWith(node.setFlags);
                    }
                }
            }
            foreach (StoryBeat beat in _chapter.Beats)
            {
                List<string> asked = new List<string>();
                foreach (DialogueNode node2 in beat.graph.Nodes)
                {
                    foreach (DialogueChoice choice in node2.choices)
                    {
                        if (!string.IsNullOrEmpty(choice.requiredFlag))
                        {
                            asked.Add(choice.requiredFlag);
                        }
                    }
                    if (!string.IsNullOrEmpty(node2.branchIfFlag))
                    {
                        asked.Add(node2.branchIfFlag);
                    }
                }
                if (!string.IsNullOrEmpty(beat.requiredFlag))
                {
                    asked.Add(beat.requiredFlag);
                }
                foreach (string flag in asked)
                {
                    Assert.IsTrue(setSoFar.Contains(flag), beat.beatId + " depends on '" + flag + "', which no earlier scene sets");
                }
                foreach (DialogueNode node3 in beat.graph.Nodes)
                {
                    setSoFar.UnionWith(node3.setFlags);
                }
            }
        }

        private static StoryChapter Load(string assetName)
        {
            StoryChapter storyChapter = AssetDatabase.LoadAssetAtPath<StoryChapter>("Assets/_Project/ScriptableObjects/" + assetName);
            Assert.IsNotNull(storyChapter, assetName + " has not been generated (Zmywak > Setup > Run All)");
            return storyChapter;
        }

        private static IEnumerable<string> Targets(DialogueNode node)
        {
            foreach (DialogueChoice choice in node.choices)
            {
                yield return choice.targetNodeId;
            }
            if (node.choices.Count == 0)
            {
                if (!string.IsNullOrEmpty(node.branchIfFlag))
                {
                    yield return node.branchNodeId;
                }
                if (!string.IsNullOrEmpty(node.nextNodeId))
                {
                    yield return node.nextNodeId;
                }
            }
        }

        private static void AssertNoCycle(DialogueGraph graph, string nodeId, List<string> path)
        {
            Assert.IsFalse(path.Contains(nodeId), graph.name + " loops: " + string.Join(" > ", path) + " > " + nodeId);
            path.Add(nodeId);
            foreach (string target in Targets(graph.FindNode(nodeId)))
            {
                AssertNoCycle(graph, target, path);
            }
            path.RemoveAt(path.Count - 1);
        }
    }
}
