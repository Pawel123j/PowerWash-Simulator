using System;

namespace Zmywak.Core.Data
{
    /// <summary>
    /// What a moment in the story does to Kuba's life: money gained or lost, and nudges to his stats.
    /// Lives in Core so the story (Narrative) can describe it and the week (Meta) can apply it without the
    /// two modules knowing about each other.
    /// </summary>
    [Serializable]
    public struct StoryOutcome
    {
        public float moneyEuros;
        public float energy;
        public float homesickness;
        public float integration;

        public bool IsEmpty => moneyEuros == 0f && energy == 0f && homesickness == 0f && integration == 0f;
    }
}
