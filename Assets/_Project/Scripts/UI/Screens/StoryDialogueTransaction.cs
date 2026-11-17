using System;
using System.Collections.Generic;
using Zmywak.Core.Save;
using Zmywak.Meta.Progress;
using Zmywak.Narrative.Dialogue;
using Zmywak.Narrative.Progress;

namespace Zmywak.UI.Screens
{
    /// <summary>The hub joins narrative and economy without introducing a dependency between those assemblies.
    /// One write includes rewards, learned words, flags, the selected route and completion of the beat/chapter.</summary>
    public sealed class StoryDialogueTransaction : IDialoguePersistence
    {
        private readonly string _beatId;
        private readonly string _completedFlag;
        private readonly MetaProgressController _meta;
        private readonly NarrativeProgressController _narrative;
        private readonly int _generation;

        public DialogueCheckpoint Checkpoint { get; private set; }
        public bool IsComplete => _narrative.HasSeenBeat(_beatId);

        public StoryDialogueTransaction(string beatId, string completedFlag,
            MetaProgressController meta, NarrativeProgressController narrative)
        {
            if (string.IsNullOrEmpty(beatId)) throw new ArgumentException("A stable beat ID is required.", nameof(beatId));
            _beatId = beatId;
            _completedFlag = completedFlag;
            _meta = meta;
            _narrative = narrative;
            _generation = SaveSystem.LifeGeneration;
            var checkpoint = SaveSystem.Load()?.dialogueCheckpoint;
            Checkpoint = checkpoint?.beatId == beatId ? checkpoint : null;
        }

        public bool TryCommit(DialogueNode finishedNode, string nextNodeId, bool atChoices)
        {
            if (_generation != SaveSystem.LifeGeneration || _meta == null || _narrative == null ||
                !_meta.CanSaveCurrentLife || !_narrative.CanSaveCurrentLife) return false;

            SaveData data = SaveSystem.Load() ?? new SaveData();
            data.appliedDialogueNodes ??= new List<string>();
            // Length prefix avoids collisions even if authored IDs contain punctuation.
            string key = finishedNode == null ? null : $"{_beatId.Length}:{_beatId}{finishedNode.nodeId}";
            bool apply = key != null && !data.appliedDialogueNodes.Contains(key);
            _meta.PrepareStorySnapshot(data, apply ? finishedNode.outcome : default);
            _narrative.WriteSnapshot(data);
            if (apply)
            {
                foreach (string word in finishedNode.teachVocabularyWords)
                    if (!string.IsNullOrWhiteSpace(word)) Add(data.knownVocabulary, word.Trim().ToLowerInvariant());
                foreach (string slang in finishedNode.unlockSlangIds) Add(data.unlockedSlangIds, slang);
                foreach (string flag in finishedNode.setFlags) Add(data.storyFlags, flag);
                data.appliedDialogueNodes.Add(key);
            }

            data.dialogueCheckpoint = string.IsNullOrEmpty(nextNodeId) ? null : new DialogueCheckpoint
            {
                beatId = _beatId, nodeId = nextNodeId, atChoices = atChoices
            };
            if (data.dialogueCheckpoint == null)
            {
                Add(data.seenStoryBeats, _beatId);
                Add(data.storyFlags, _completedFlag);
                data.hasSeenIntro |= _beatId == NarrativeProgressController.ArrivalBeatId;
            }

            if (!SaveSystem.Save(data)) return false;

            Checkpoint = data.dialogueCheckpoint;
            // Both models must be updated before any observer can query or autosave them.
            _narrative.AcceptSnapshot(data, notify: false);
            _meta.AcceptStorySnapshot(data, apply && !finishedNode.outcome.IsEmpty);
            _narrative.NotifySnapshotAccepted();
            return true;
        }

        private static void Add(List<string> list, string value)
        {
            if (!string.IsNullOrEmpty(value) && !list.Contains(value)) list.Add(value);
        }
    }
}
