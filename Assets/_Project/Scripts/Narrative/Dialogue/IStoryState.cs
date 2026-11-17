using Zmywak.Narrative.LanguageBarrier;

namespace Zmywak.Narrative.Dialogue
{
    /// <summary>What a dialogue reads and changes as it plays: the words Kuba knows, the slang he has picked up, and story flags.</summary>
    public interface IStoryState
    {
        VocabularyTracker Vocabulary { get; }
        bool HasFlag(string flag);
        void SetFlag(string flag);
        void LearnWord(string word);
        void UnlockSlang(string slangId);
    }
}
