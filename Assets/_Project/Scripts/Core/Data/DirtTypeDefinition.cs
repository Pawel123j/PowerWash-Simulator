using UnityEngine;

namespace Zmywak.Core.Data
{
    /// <summary>How a layer of dirt is laid out on a dish.</summary>
    public enum DirtPattern
    {
        /// <summary>Blotchy patches of crust or film.</summary>
        Splotches,

        /// <summary>Thin wavy smears, like water streaks drying on glass.</summary>
        Streaks
    }

    /// <summary>One kind of grime a dish can carry (grease, burnt food, dried sauce, ...).</summary>
    [CreateAssetMenu(menuName = "Zmywak/Dirt Type Definition", fileName = "DirtType_")]
    public class DirtTypeDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [Tooltip("Colour of this dirt on a dish; alpha is its opacity at full thickness (grease is a see-through film).")]
        [SerializeField] private Color placeholderDirtyColor = new Color(0.45f, 0.32f, 0.14f);
        [Tooltip("Higher values require more scrubbing progress to fully remove.")]
        [SerializeField] private float scrubDifficulty = 1f;
        [SerializeField] private DirtPattern pattern = DirtPattern.Splotches;
        [Tooltip("Invisible in ordinary light, and only forms once the other dirt is off (streaks as a glass dries): " +
                 "it shows up in the reflection when the dish is held to the light.")]
        [SerializeField] private bool showsOnlyInReflection;
        [Tooltip("How glossy it is at full thickness, 0-1: burnt crust is matte, a grease film shines.")]
        [Range(0f, 1f)] [SerializeField] private float gloss = 0.2f;
        [Tooltip("How much it stands off the dish, 0-1: burnt crust is thick and lumpy, a film is flat. Drawn as relief in the dish's normal map.")]
        [Range(0f, 1f)] [SerializeField] private float relief = 0.5f;

        public string Id => id;
        public string DisplayName => displayName;
        public Color PlaceholderDirtyColor => placeholderDirtyColor;
        public float ScrubDifficulty => Mathf.Max(0.01f, scrubDifficulty);
        public DirtPattern Pattern => pattern;
        public bool ShowsOnlyInReflection => showsOnlyInReflection;
        public float Gloss => gloss;
        public float Relief => relief;
    }
}
