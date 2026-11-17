using Zmywak.Core.Save;

namespace Zmywak.Narrative.Dialogue
{
    /// <summary>Commits a finished node and its destination together, before the session advances.
    /// A null finished node records a choice only. A null next node completes the entire beat.
    /// Failure must leave live story/economy state unchanged so the same input can be retried.</summary>
    public interface IDialoguePersistence
    {
        DialogueCheckpoint Checkpoint { get; }
        bool IsComplete { get; }
        bool TryCommit(DialogueNode finishedNode, string nextNodeId, bool atChoices);
    }
}
