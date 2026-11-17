using System;
using System.Collections.Generic;
using Zmywak.Core.Data;
using Zmywak.Narrative.LanguageBarrier;

namespace Zmywak.Narrative.Dialogue
{
    /// <summary>
    /// Walks one <see cref="DialogueGraph"/>: shows each line (unknown English words blurred through the
    /// story state's vocabulary), then offers the node's choices - only those whose required flag is set -
    /// or moves on, taking a flag branch if the node has one. A node's rewards, flags and life outcome apply
    /// once its own lines are done. A choice that costs money (<see cref="CostOf"/>) is offered but cannot be
    /// taken while Kuba cannot spare it (<see cref="CanSpend"/>). Plain C#, so the rules are testable without a scene.
    /// </summary>
    public sealed class DialogueSession
    {
        private readonly DialogueGraph _graph;
        private readonly IStoryState _state;
        private readonly IDialoguePersistence _persistence;
        private readonly List<DialogueChoice> _offered = new List<DialogueChoice>();
        private readonly List<bool> _available = new List<bool>();
        private DialogueNode _node;
        private int _lineIndex;

        public DialogueSession(DialogueGraph graph, IStoryState state, IDialoguePersistence persistence = null)
        {
            _graph = graph;
            _state = state;
            _persistence = persistence;
        }

        public event Action<string, string> LineShown; // speakerId, display text
        public event Action<IReadOnlyList<string>> ChoicesShown;
        public event Action<StoryOutcome> OutcomeApplied;
        public event Action Ended;
        public event Action SaveFailed;

        /// <summary>Whether Kuba can spend this many euros now. Unset, every choice can be paid for.</summary>
        public Func<float, bool> CanSpend { get; set; }

        public bool IsOver { get; private set; }

        /// <summary>The node being played (null once over) - for tests and debugging.</summary>
        public string CurrentNodeId => _node?.nodeId;

        /// <summary>What taking a choice costs: the money the node it leads to takes away (0 if it gives or takes none).</summary>
        public static float CostOf(DialogueGraph graph, DialogueChoice choice)
        {
            DialogueNode target = graph != null && choice != null ? graph.FindNode(choice.targetNodeId) : null;
            return target != null && target.outcome.moneyEuros < 0f ? -target.outcome.moneyEuros : 0f;
        }

        /// <summary>Whether the choice at this index (of those last shown) can be taken - false if Kuba cannot afford it.</summary>
        public bool IsChoiceAvailable(int index)
        {
            return index >= 0 && index < _available.Count && _available[index];
        }

        public void Start()
        {
            if (_persistence?.IsComplete == true)
            {
                End();
                return;
            }

            var checkpoint = _persistence?.Checkpoint;
            if (checkpoint != null && _graph?.FindNode(checkpoint.nodeId) != null)
            {
                if (checkpoint.atChoices)
                {
                    _node = _graph.FindNode(checkpoint.nodeId);
                    _lineIndex = _node.lines.Count - 1;
                    if (_lineIndex >= 0) ShowLine();
                    OfferChoices();
                }
                else
                {
                    GoTo(checkpoint.nodeId);
                }
                return;
            }

            GoTo(_graph != null ? _graph.StartNodeId : null);
        }

        /// <summary>Next line; after the node's last line, its choices or the next node.</summary>
        public void Advance()
        {
            if (_node == null || _offered.Count > 0)
            {
                return; // over, or waiting for a choice
            }

            if (_lineIndex + 1 < _node.lines.Count)
            {
                _lineIndex++;
                ShowLine();
                return;
            }

            bool hasChoices = _node.choices.Exists(choice => HasFlagAfterNode(choice.requiredFlag));
            bool branch = !string.IsNullOrEmpty(_node.branchIfFlag) && HasFlagAfterNode(_node.branchIfFlag);
            string next = hasChoices ? _node.nodeId : branch ? _node.branchNodeId : _node.nextNodeId;
            if (!Commit(_node, next, hasChoices)) return;

            if (hasChoices)
                OfferChoices();
            else
                GoTo(next);
        }

        private bool HasFlagAfterNode(string flag) => string.IsNullOrEmpty(flag) ||
            (_state != null && (_state.HasFlag(flag) || _node.setFlags.Contains(flag)));

        private void OfferChoices()
        {
            _offered.Clear();
            _available.Clear();
            foreach (DialogueChoice choice in _node.choices)
            {
                if (string.IsNullOrEmpty(choice.requiredFlag) || (_state != null && _state.HasFlag(choice.requiredFlag)))
                {
                    _offered.Add(choice);
                }
            }

            if (_offered.Count > 0)
            {
                var texts = new List<string>(_offered.Count);
                bool anyAvailable = false;
                foreach (DialogueChoice choice in _offered)
                {
                    texts.Add(choice.choiceText);
                    float cost = CostOf(_graph, choice);
                    bool available = cost <= 0f || CanSpend == null || CanSpend(cost);
                    _available.Add(available);
                    anyAvailable |= available;
                }

                if (!anyAvailable)
                {
                    // Every way on costs money Kuba does not have: a scene must never strand him (StoryContentTests
                    // asks every choice for a free option), so let him through rather than lock the game.
                    for (int i = 0; i < _available.Count; i++)
                    {
                        _available[i] = true;
                    }
                }

                ChoicesShown?.Invoke(texts);
            }
        }

        /// <summary>Pick one of the choices last shown (index into that list, not the node's full list). A choice Kuba
        /// cannot afford is ignored.</summary>
        public void Choose(int index)
        {
            if (!IsChoiceAvailable(index))
            {
                return;
            }

            string target = _offered[index].targetNodeId;
            if (!Commit(null, target, false)) return;
            _offered.Clear();
            _available.Clear();
            GoTo(target);
        }

        private bool Commit(DialogueNode node, string next, bool atChoices)
        {
            if (_persistence != null)
            {
                if (_persistence.TryCommit(node, next, atChoices)) return true;
                SaveFailed?.Invoke();
                return false;
            }

            if (node != null) FinishNode(node);
            return true;
        }

        private void GoTo(string nodeId)
        {
            _node = _graph != null && !string.IsNullOrEmpty(nodeId) ? _graph.FindNode(nodeId) : null;
            _lineIndex = 0;
            _offered.Clear();
            _available.Clear();

            if (_node == null)
            {
                End();
                return;
            }

            if (_node.lines.Count == 0)
            {
                // A pure routing node (a flag branch, a reward): nothing to show, act on it straight away.
                _lineIndex = -1;
                Advance();
                return;
            }

            ShowLine();
        }

        private void ShowLine()
        {
            DialogueLine line = _node.lines[_lineIndex];
            string display = line.text;
            if (line.language == DialogueLanguage.English && _state?.Vocabulary != null)
            {
                display = WordBlurFormatter.Format(line.text, _state.Vocabulary);
            }

            LineShown?.Invoke(line.speakerId, display);
        }

        private void FinishNode(DialogueNode node)
        {
            if (_state != null)
            {
                foreach (string word in node.teachVocabularyWords)
                {
                    _state.LearnWord(word);
                }

                foreach (string slangId in node.unlockSlangIds)
                {
                    _state.UnlockSlang(slangId);
                }

                foreach (string flag in node.setFlags)
                {
                    _state.SetFlag(flag);
                }
            }

            if (!node.outcome.IsEmpty)
            {
                OutcomeApplied?.Invoke(node.outcome);
            }
        }

        private void End()
        {
            if (IsOver)
            {
                return;
            }

            IsOver = true;
            _node = null;
            Ended?.Invoke();
        }
    }
}
