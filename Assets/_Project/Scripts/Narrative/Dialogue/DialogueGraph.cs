using System;
using System.Collections.Generic;
using UnityEngine;
using Zmywak.Core.Data;

namespace Zmywak.Narrative.Dialogue
{
    public enum DialogueLanguage
    {
        Polish,
        English
    }

    /// <summary>One spoken/thought line. English lines get run through the word-blur filter at render time.</summary>
    [Serializable]
    public class DialogueLine
    {
        public string speakerId;
        public DialogueLanguage language;
        [TextArea] public string text;
    }

    /// <summary>A player-facing choice (always shown in Polish - it is Kuba's decision, not overheard speech).</summary>
    [Serializable]
    public class DialogueChoice
    {
        public string choiceText;
        public string targetNodeId;

        [Tooltip("Only offered once this story flag is set (e.g. an option Kuba's own earlier thoughts unlocked). Empty = always.")]
        public string requiredFlag;
    }

    /// <summary>
    /// One beat of dialogue: a handful of lines shown in sequence, then either a branching choice or an
    /// automatic jump to the next node. What a node teaches (vocabulary, slang), the story flags it sets and
    /// what it does to Kuba's life (<see cref="outcome"/>) apply once its lines are done - so a word is still
    /// blurred while it is being said for the first time.
    /// </summary>
    [Serializable]
    public class DialogueNode
    {
        public string nodeId;
        public List<DialogueLine> lines = new List<DialogueLine>();
        public List<string> teachVocabularyWords = new List<string>();
        public List<string> unlockSlangIds = new List<string>();
        public List<string> setFlags = new List<string>();
        public StoryOutcome outcome;
        public List<DialogueChoice> choices = new List<DialogueChoice>();

        [Tooltip("Without choices: go here next if this story flag is set...")]
        public string branchIfFlag;
        public string branchNodeId;

        [Tooltip("...and here otherwise (empty = the dialogue ends).")]
        public string nextNodeId;
    }

    /// <summary>A complete dialogue scene: a bag of nodes plus where to start. Authored as a ScriptableObject asset.</summary>
    [CreateAssetMenu(menuName = "Zmywak/Dialogue Graph", fileName = "Dialogue_")]
    public class DialogueGraph : ScriptableObject
    {
        [SerializeField] private string startNodeId;
        [SerializeField] private List<DialogueNode> nodes = new List<DialogueNode>();

        public string StartNodeId => startNodeId;
        public IReadOnlyList<DialogueNode> Nodes => nodes;

        public DialogueNode FindNode(string nodeId)
        {
            foreach (DialogueNode node in nodes)
            {
                if (node.nodeId == nodeId)
                {
                    return node;
                }
            }

            return null;
        }

        /// <summary>Replaces the whole scene - for content generators and tests; the game only reads graphs.</summary>
        public void SetContent(string start, List<DialogueNode> content)
        {
            startNodeId = start;
            nodes = content ?? new List<DialogueNode>();
        }
    }
}
