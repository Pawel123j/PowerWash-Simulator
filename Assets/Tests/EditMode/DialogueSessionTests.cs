using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Zmywak.Core.Data;
using Zmywak.Narrative.Dialogue;
using Zmywak.Narrative.LanguageBarrier;
using Zmywak.Narrative.Story;

namespace Zmywak.Tests
{
    public class DialogueSessionTests
    {
        private readonly List<DialogueGraph> _graphs = new List<DialogueGraph>();

        [TearDown]
        public void TearDown()
        {
            foreach (DialogueGraph graph in _graphs)
            {
                Object.DestroyImmediate(graph);
            }

            _graphs.Clear();
        }

        [Test]
        public void EnglishIsBlurred_UntilTheNodeThatTeachesItIsDone()
        {
            DialogueGraph graph = Graph("a",
                Node("a", next: "b", teach: new[] { "kitchen" }, lines: Line("declan", "Welcome to the kitchen.", english: true)),
                Node("b", lines: Line("declan", "The kitchen.", english: true)));
            var state = new FakeState();
            var shown = new List<string>();
            var session = new DialogueSession(graph, state);
            session.LineShown += (_, text) => shown.Add(text);

            session.Start();
            session.Advance();

            Assert.AreEqual("_______ to the _______.", shown[0], "Unknown words are blanked while first said");
            Assert.AreEqual("The kitchen.", shown[1], "...and readable once taught");
        }

        [Test]
        public void ChoicesNeedingAFlag_AreOnlyOfferedOnceItIsSet()
        {
            DialogueGraph graph = Graph("a",
                Node("a", lines: Line("kuba_thought", "?"), choices: new[]
                {
                    Choice("Plain", "plain"),
                    Choice("Secret", "secret", requiredFlag: "knows_secret")
                }),
                Node("plain", lines: Line("x", "plain")),
                Node("secret", lines: Line("x", "secret")));

            var without = new FakeState();
            IReadOnlyList<string> offered = null;
            var session = new DialogueSession(graph, without);
            session.ChoicesShown += choices => offered = choices;
            session.Start();
            session.Advance();
            CollectionAssert.AreEqual(new[] { "Plain" }, offered);

            var with = new FakeState();
            with.SetFlag("knows_secret");
            var lines = new List<string>();
            session = new DialogueSession(graph, with);
            session.ChoicesShown += choices => offered = choices;
            session.LineShown += (_, text) => lines.Add(text);
            session.Start();
            session.Advance();
            CollectionAssert.AreEqual(new[] { "Plain", "Secret" }, offered);

            session.Choose(1); // index into what was offered
            Assert.AreEqual("secret", lines[lines.Count - 1]);
        }

        [Test]
        public void AChoiceKubaCannotAfford_IsShown_ButCannotBeTaken()
        {
            DialogueGraph graph = Graph("a",
                Node("a", lines: Line("kuba_thought", "?"), choices: new[]
                {
                    Choice("Round for everyone", "round"),
                    Choice("Admit it", "honest")
                }),
                Node("round", lines: new[] { Line("x", "round") }, outcome: new StoryOutcome { moneyEuros = -35f }),
                Node("honest", lines: Line("x", "honest")));
            Assert.AreEqual(35f, DialogueSession.CostOf(graph, graph.FindNode("a").choices[0]));
            Assert.AreEqual(0f, DialogueSession.CostOf(graph, graph.FindNode("a").choices[1]));

            float spare = 20f;
            IReadOnlyList<string> offered = null;
            var lines = new List<string>();
            var session = new DialogueSession(graph, new FakeState()) { CanSpend = euros => euros <= spare };
            session.ChoicesShown += choices => offered = choices;
            session.LineShown += (_, text) => lines.Add(text);
            session.Start();
            session.Advance();

            CollectionAssert.AreEqual(new[] { "Round for everyone", "Admit it" }, offered, "An unaffordable choice should stay in sight");
            Assert.IsFalse(session.IsChoiceAvailable(0));
            Assert.IsTrue(session.IsChoiceAvailable(1));
            session.Choose(0);
            Assert.AreEqual("a", session.CurrentNodeId, "Took a choice Kuba cannot pay for");
            session.Choose(1);
            Assert.AreEqual("honest", lines[lines.Count - 1]);

            spare = 50f;
            session = new DialogueSession(graph, new FakeState()) { CanSpend = euros => euros <= spare };
            session.Start();
            session.Advance();
            Assert.IsTrue(session.IsChoiceAvailable(0), "With the money there, the round can be bought");
        }

        [Test]
        public void WhenEveryChoiceCostsTooMuch_TheSceneStillGoesOn()
        {
            DialogueGraph graph = Graph("a",
                Node("a", lines: Line("kuba_thought", "?"), choices: new[] { Choice("Pay", "pay") }),
                Node("pay", lines: new[] { Line("x", "paid") }, outcome: new StoryOutcome { moneyEuros = -10f }));
            var session = new DialogueSession(graph, new FakeState()) { CanSpend = _ => false };
            session.Start();
            session.Advance();

            Assert.IsTrue(session.IsChoiceAvailable(0), "A scene with no affordable way on would lock the game");
        }

        [Test]
        public void ANode_SetsItsFlags_AndReportsItsOutcome_OnceItsLinesAreDone()
        {
            DialogueGraph graph = Graph("a",
                Node("a", lines: new[] { Line("x", "one"), Line("x", "two") }, flags: new[] { "paid" },
                    outcome: new StoryOutcome { moneyEuros = -20f, homesickness = 5f }));
            var state = new FakeState();
            var outcomes = new List<StoryOutcome>();
            var session = new DialogueSession(graph, state);
            session.OutcomeApplied += outcomes.Add;

            session.Start();
            Assert.IsFalse(state.HasFlag("paid"), "Applied before the node was even read");
            session.Advance();
            Assert.IsFalse(state.HasFlag("paid"));
            session.Advance();

            Assert.IsTrue(state.HasFlag("paid"));
            Assert.AreEqual(1, outcomes.Count);
            Assert.AreEqual(-20f, outcomes[0].moneyEuros);
            Assert.IsTrue(session.IsOver);
        }

        [Test]
        public void AFlagBranch_TakesTheOtherPath_WhenTheFlagIsSet()
        {
            DialogueGraph graph = Graph("a",
                Node("a", lines: Line("x", "start"), branchIfFlag: "fake_bill", branchTo: "fake", next: "honest"),
                Node("honest", lines: Line("x", "honest")),
                Node("fake", lines: Line("x", "fake")));

            Assert.AreEqual("honest", PlayToEnd(graph, new FakeState()));

            var cheated = new FakeState();
            cheated.SetFlag("fake_bill");
            Assert.AreEqual("fake", PlayToEnd(graph, cheated));
        }

        [Test]
        public void ANodeWithoutLines_JustRoutesOn()
        {
            DialogueGraph graph = Graph("router",
                Node("router", next: "b", flags: new[] { "passed" }),
                Node("b", lines: Line("x", "b")));
            var state = new FakeState();

            Assert.AreEqual("b", PlayToEnd(graph, state));
            Assert.IsTrue(state.HasFlag("passed"));
        }

        [Test]
        public void Ending_IsReportedOnce()
        {
            DialogueGraph graph = Graph("a", Node("a", lines: Line("x", "only")));
            int ended = 0;
            var session = new DialogueSession(graph, new FakeState());
            session.Ended += () => ended++;

            session.Start();
            session.Advance();
            session.Advance();
            session.Choose(0);

            Assert.AreEqual(1, ended);
        }

        [Test]
        public void Scheduler_PlaysScenesInOrder_OnTheirDay_AndWaitsForConditions()
        {
            DialogueGraph graph = Graph("a", Node("a", lines: Line("x", "x")));
            var beats = new List<StoryBeat>
            {
                new StoryBeat { beatId = "one", day = 1, graph = graph },
                new StoryBeat { beatId = "two", day = 2, graph = graph },
                new StoryBeat { beatId = "bonus", day = 2, graph = graph, requiredFlag = "friendly" },
                new StoryBeat { beatId = "three", day = 3, graph = graph }
            };
            var seen = new HashSet<string>();
            var flags = new HashSet<string>();
            StoryBeat Next(int day) => StoryBeatScheduler.NextBeat(beats, day, seen.Contains, flags.Contains);

            Assert.AreEqual("one", Next(1).beatId);
            seen.Add("one");
            Assert.IsNull(Next(1), "Day 2's scene played on day 1");
            Assert.AreEqual("two", Next(5).beatId, "A late player still gets the scenes, in order");
            seen.Add("two");
            Assert.AreEqual("three", Next(3).beatId, "A scene whose condition is not met is passed over");
            flags.Add("friendly");
            Assert.AreEqual("bonus", Next(3).beatId);
        }

        // ------------------------------------------------------------------

        private static string PlayToEnd(DialogueGraph graph, IStoryState state)
        {
            string last = null;
            var session = new DialogueSession(graph, state);
            session.LineShown += (_, text) => last = text;
            session.Start();
            for (int i = 0; i < 50 && !session.IsOver; i++)
            {
                session.Advance();
            }

            Assert.IsTrue(session.IsOver, "The dialogue never ended");
            return last;
        }

        private DialogueGraph Graph(string start, params DialogueNode[] nodes)
        {
            var graph = ScriptableObject.CreateInstance<DialogueGraph>();
            graph.SetContent(start, new List<DialogueNode>(nodes));
            _graphs.Add(graph);
            return graph;
        }

        private static DialogueNode Node(string id, string next = null, DialogueLine[] lines = null, string[] teach = null,
            string[] flags = null, StoryOutcome outcome = default, DialogueChoice[] choices = null, string branchIfFlag = null,
            string branchTo = null)
        {
            return new DialogueNode
            {
                nodeId = id,
                nextNodeId = next,
                lines = new List<DialogueLine>(lines ?? new DialogueLine[0]),
                teachVocabularyWords = new List<string>(teach ?? new string[0]),
                setFlags = new List<string>(flags ?? new string[0]),
                outcome = outcome,
                choices = new List<DialogueChoice>(choices ?? new DialogueChoice[0]),
                branchIfFlag = branchIfFlag,
                branchNodeId = branchTo
            };
        }

        private static DialogueNode Node(string id, DialogueLine lines, string next = null, string[] teach = null,
            DialogueChoice[] choices = null, string branchIfFlag = null, string branchTo = null)
        {
            return Node(id, next, new[] { lines }, teach, null, default, choices, branchIfFlag, branchTo);
        }

        private static DialogueLine Line(string speaker, string text, bool english = false)
        {
            return new DialogueLine { speakerId = speaker, text = text, language = english ? DialogueLanguage.English : DialogueLanguage.Polish };
        }

        private static DialogueChoice Choice(string text, string target, string requiredFlag = null)
        {
            return new DialogueChoice { choiceText = text, targetNodeId = target, requiredFlag = requiredFlag };
        }

        private sealed class FakeState : IStoryState
        {
            private readonly HashSet<string> _flags = new HashSet<string>();

            public VocabularyTracker Vocabulary { get; } = new VocabularyTracker(new[] { "the", "to" });
            public bool HasFlag(string flag) => _flags.Contains(flag);
            public void SetFlag(string flag) => _flags.Add(flag);
            public void LearnWord(string word) => Vocabulary.Learn(word);
            public void UnlockSlang(string slangId) { }
        }
    }
}
