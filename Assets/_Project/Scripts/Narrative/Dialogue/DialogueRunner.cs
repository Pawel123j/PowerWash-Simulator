using System;
using System.Collections.Generic;
using UnityEngine;
using Zmywak.Core.Data;
using Zmywak.Core.Services;
using Zmywak.Narrative.Progress;

namespace Zmywak.Narrative.Dialogue
{
    /// <summary>
    /// Scene-side host for a <see cref="DialogueSession"/>: plays a graph against the persistent narrative
    /// progress (vocabulary, slang, story flags) and relays what happens to the dialogue UI and to whoever
    /// applies story outcomes to Kuba's life.
    /// </summary>
    public class DialogueRunner : MonoBehaviour
    {
        private DialogueSession _session;
        private NarrativeProgressController _narrativeProgress;
        private Action _retrySave;
        private float _retryAfter;

        public event Action<string, string> OnLineShown; // speakerId, displayText
        public event Action<IReadOnlyList<string>> OnChoicesShown;
        public event Action<StoryOutcome> OnOutcome;
        public event Action OnDialogueEnded;
        public event Action OnSaveFailed;
        public bool HasSaveFailure => _retrySave != null;
        public string CurrentNodeId => _session?.CurrentNodeId;

        public bool IsPlaying => _session != null && !_session.IsOver;

        /// <summary>Whether Kuba can spend this many euros now - set by whoever knows his wallet (the hub's
        /// <c>StoryBeatTrigger</c>) before a dialogue starts. Unset, every choice can be paid for.</summary>
        public Func<float, bool> CanSpend { get; set; }

        private void Awake()
        {
            ServiceLocator.TryGet(out _narrativeProgress);
        }

        public void StartDialogue(DialogueGraph graph, IDialoguePersistence persistence = null)
        {
            if (graph == null)
            {
                return;
            }

            if (_narrativeProgress == null)
            {
                ServiceLocator.TryGet(out _narrativeProgress);
            }

            _retrySave = null;
            _session = new DialogueSession(graph, _narrativeProgress, persistence) { CanSpend = CanSpend };
            _session.LineShown += (speaker, text) => OnLineShown?.Invoke(speaker, text);
            _session.ChoicesShown += choices => OnChoicesShown?.Invoke(choices);
            _session.OutcomeApplied += outcome => OnOutcome?.Invoke(outcome);
            _session.Ended += () => OnDialogueEnded?.Invoke();
            // A graph can start with a routing node, which may itself need a write.
            Run(_session.Start);
        }

        public void Advance()
        {
            if (HasSaveFailure) RetrySave();
            else if (_session != null) Run(_session.Advance);
        }

        public void SelectChoice(int index)
        {
            if (HasSaveFailure) RetrySave();
            else if (_session != null) Run(() => _session.Choose(index));
        }

        public void RetrySave()
        {
            if (_retrySave == null || Time.unscaledTime < _retryAfter) return;
            Run(_retrySave);
        }

        private void Run(Action action)
        {
            _retrySave = null;
            void Failed()
            {
                // If a pure routing node failed after Choose/Start already advanced, resume that node.
                _retrySave = _session.CurrentNodeId != null && _session.CurrentNodeId != _nodeBeforeAction
                    ? _session.Advance : action;
                _retryAfter = Time.unscaledTime + 0.5f;
                OnSaveFailed?.Invoke();
            }
            _nodeBeforeAction = _session.CurrentNodeId;
            _session.SaveFailed += Failed;
            try { action(); }
            finally { _session.SaveFailed -= Failed; }
        }

        private string _nodeBeforeAction;

        /// <summary>Whether the choice at this index (of those last shown) can be taken - false if Kuba cannot afford it.</summary>
        public bool IsChoiceAvailable(int index)
        {
            return _session != null && _session.IsChoiceAvailable(index);
        }
    }
}
