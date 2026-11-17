using UnityEngine;

namespace Zmywak.Core.Data
{
    /// <summary>A playable character. Only Kuba is used in the vertical slice, but the system supports more.</summary>
    [CreateAssetMenu(menuName = "Zmywak/Character Definition", fileName = "Character_")]
    public class CharacterDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private string originCountry;
        [Range(1, 5)] [SerializeField] private int englishLevel = 1;
        [SerializeField] private string specialSkillDescription;

        public string Id => id;
        public string DisplayName => displayName;
        public string OriginCountry => originCountry;
        public int EnglishLevel => englishLevel;
        public string SpecialSkillDescription => specialSkillDescription;
    }
}
