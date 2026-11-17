using UnityEngine;
using Zmywak.Core.Diagnostics;

namespace Zmywak.Kitchen.Dishes
{
    /// <summary>
    /// GameObject.CreatePrimitive always assigns the legacy built-in Default-Material, which renders
    /// magenta under URP. Everything built in code goes through here instead. Materials are cloned from
    /// URP/Lit templates in Resources (see VerticalSliceSetup.EnsurePlaceholderMaterialTemplate) rather
    /// than built from Shader.Find: that returns null in a player build unless something else references
    /// the shader, and a keyword switched on at runtime may have had its shader variant stripped - the
    /// templates carry the keywords, so their variants are always built.
    /// </summary>
    public static class PlaceholderMaterial
    {
        public const string TemplateResourceName = "PlaceholderLit";
        /// <summary>URP/Lit reading smoothness from the base map's alpha: clean glaze shines, dirt is matte.</summary>
        public const string DishTemplateResourceName = "PlaceholderLitDish";
        /// <summary>Transparent URP/Lit for see-through glass: the base map's alpha is opacity, gloss stays constant.</summary>
        public const string GlassTemplateResourceName = "PlaceholderLitGlass";

        private static Material _template;
        private static Material _dishTemplate;
        private static Material _glassTemplate;
        private static bool _loggedMissingTemplate;

        public static Material Create(Color color, float smoothness = 0.5f, float metallic = 0f)
        {
            Material material = Clone(ref _template, TemplateResourceName);
            material.color = color;
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            return material;
        }

        /// <summary>A dish surface: pair it with a base map whose alpha is the gloss (1 = clean glaze).</summary>
        public static Material CreateDish(float smoothness, float metallic)
        {
            Material material = Clone(ref _dishTemplate, DishTemplateResourceName);
            material.color = Color.white;
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            return material;
        }

        /// <summary>A see-through glass: pair it with a base map whose alpha is opacity (low = clear glass).</summary>
        public static Material CreateGlass(float smoothness)
        {
            Material material = Clone(ref _glassTemplate, GlassTemplateResourceName);
            material.color = Color.white;
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", 0f);
            return material;
        }

        private static Material Clone(ref Material template, string resourceName)
        {
            if (template == null)
            {
                using var _load = StartupTimeline.Measure("assets: Resources.Load material template");
                template = Resources.Load<Material>(resourceName);
                if (template == null && !_loggedMissingTemplate)
                {
                    _loggedMissingTemplate = true;
                    Debug.LogError($"PlaceholderMaterial: Resources/{resourceName}.mat not found - objects will render " +
                                   "magenta. Run 'Zmywak/Setup/0c. Create Placeholder Material Template' in the Editor.");
                }
            }

            return template != null
                ? new Material(template)
                : new Material(Shader.Find("Hidden/InternalErrorShader"));
        }
    }
}
