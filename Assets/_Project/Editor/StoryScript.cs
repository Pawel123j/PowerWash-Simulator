using System.Collections.Generic;
using UnityEditor;
using Zmywak.Core.Data;
using Zmywak.Narrative.Dialogue;
using Zmywak.Narrative.Story;

namespace Zmywak.EditorTools
{
    /// <summary>
    /// How the chapters are written: a dialogue graph as plain code - nodes, their lines (narration, Kuba's thoughts,
    /// Polish and English speech), the words and slang a node teaches, flags, outcomes, choices and where they lead -
    /// saved as DialogueGraph assets and gathered into a chapter's beats, one a day.
    /// </summary>
    internal static class StoryScript
    {
        /// <summary>Kuba's inner voice.</summary>
        public const string Kuba = "kuba_thought";

        public static StoryBeat Beat(string id, int day, DialogueGraph graph, bool endsChapter = false)
        {
            return new StoryBeat { beatId = id, day = day, graph = graph, endsChapter = endsChapter };
        }

        /// <param name="fluent">Years into Dublin, Kuba understands what is said to him: the scene opens with a node that
        /// teaches every English word its lines use, so none is blurred (chapters set years after his arrival).</param>
        public static DialogueGraph SaveGraph(string folder, string name, Graph builder, bool fluent = false)
        {
            DialogueGraph graph = VerticalSliceSetup.CreateOrLoad<DialogueGraph>($"{folder}/Dialogue_{name}.asset");
            var nodes = new List<DialogueNode>(builder.Nodes);
            string start = builder.StartNodeId;
            if (fluent)
            {
                var understood = new DialogueNode { nodeId = "fluent", nextNodeId = start, teachVocabularyWords = EnglishWords(builder.Nodes) };
                nodes.Insert(0, understood);
                start = understood.nodeId;
            }

            graph.SetContent(start, nodes);
            EditorUtility.SetDirty(graph);
            return graph;
        }

        /// <summary>Every English word spoken in the nodes, cut out the way WordBlurFormatter cuts them (letters at
        /// both ends, anything inside - "don't", "sous-chef" - kept), lower case, each once.</summary>
        private static List<string> EnglishWords(IEnumerable<DialogueNode> nodes)
        {
            var words = new List<string>();
            var seen = new HashSet<string>();
            foreach (DialogueNode node in nodes)
            {
                foreach (DialogueLine line in node.lines)
                {
                    if (line.language != DialogueLanguage.English)
                    {
                        continue;
                    }

                    foreach (string token in line.text.Split(' '))
                    {
                        int from = 0;
                        int to = token.Length;
                        while (from < to && !char.IsLetter(token[from]))
                        {
                            from++;
                        }

                        while (to > from && !char.IsLetter(token[to - 1]))
                        {
                            to--;
                        }

                        string word = token.Substring(from, to - from).Trim().ToLowerInvariant();
                        if (word.Length > 0 && seen.Add(word))
                        {
                            words.Add(word);
                        }
                    }
                }
            }

            return words;
        }

        /// <summary>One scene's graph, node by node.</summary>
        public sealed class Graph
        {
            public Graph(string startNodeId)
            {
                StartNodeId = startNodeId;
            }

            public string StartNodeId { get; }
            public List<DialogueNode> Nodes { get; } = new List<DialogueNode>();

            public Node Node(string id)
            {
                var node = new DialogueNode { nodeId = id };
                Nodes.Add(node);
                return new Node(node);
            }
        }

        public sealed class Node
        {
            private readonly DialogueNode _node;

            public Node(DialogueNode node)
            {
                _node = node;
            }

            public Node Narrate(string text) => Line(SpeakerNames.Narrator, DialogueLanguage.Polish, text);
            public Node Think(string text) => Line(Kuba, DialogueLanguage.Polish, text);
            public Node Pl(string speaker, string text) => Line(speaker, DialogueLanguage.Polish, text);
            public Node En(string speaker, string text) => Line(speaker, DialogueLanguage.English, text);

            public Node Teach(params string[] words)
            {
                _node.teachVocabularyWords.AddRange(words);
                return this;
            }

            public Node Slang(params string[] slangIds)
            {
                _node.unlockSlangIds.AddRange(slangIds);
                return this;
            }

            public Node Flag(params string[] flags)
            {
                _node.setFlags.AddRange(flags);
                return this;
            }

            public Node Outcome(float money = 0f, float energy = 0f, float homesickness = 0f, float integration = 0f)
            {
                _node.outcome = new StoryOutcome { moneyEuros = money, energy = energy, homesickness = homesickness, integration = integration };
                return this;
            }

            public Node Choice(string text, string target, string requiredFlag = null)
            {
                _node.choices.Add(new DialogueChoice { choiceText = text, targetNodeId = target, requiredFlag = requiredFlag });
                return this;
            }

            public Node BranchIf(string flag, string target)
            {
                _node.branchIfFlag = flag;
                _node.branchNodeId = target;
                return this;
            }

            public Node Next(string nodeId)
            {
                _node.nextNodeId = nodeId;
                return this;
            }

            private Node Line(string speaker, DialogueLanguage language, string text)
            {
                _node.lines.Add(new DialogueLine { speakerId = speaker, language = language, text = text });
                return this;
            }
        }
    }
}
