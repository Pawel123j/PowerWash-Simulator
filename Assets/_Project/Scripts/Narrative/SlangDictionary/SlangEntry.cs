using UnityEngine;

namespace Zmywak.Narrative.SlangDictionary
{
    /// <summary>One collectible piece of Irish slang, unlocked by a dialogue node.</summary>
    [CreateAssetMenu(menuName = "Zmywak/Slang Entry", fileName = "Slang_")]
    public class SlangEntry : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string phrase;
        [TextArea] [SerializeField] private string meaning;

        public string Id => id;
        public string Phrase => phrase;
        public string Meaning => meaning;
    }
}
