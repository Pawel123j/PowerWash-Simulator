using UnityEngine;
using Zmywak.Core.Data;
using Zmywak.Core.Services;
using Zmywak.Core.Diagnostics;

namespace Zmywak.Kitchen.Feedback
{
    /// <summary>
    /// The kitchen as it is where Kuba works: Declan's white tiles in Dublin, the cream ones of a hotel kitchen by
    /// Kraków's market square, the grey steel panels of a mining camp's kitchen in the Pilbara - the wall tinted on
    /// its renderer (a property block: the shared material and the lightmaps stay as they are).
    /// </summary>
    public class KitchenPlaceDressing : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Renderer[] walls = new Renderer[0];

        /// <summary>The place the kitchen is dressed for.</summary>
        public Place Place { get; private set; } = Place.Dublin;

        private void Start()
        {
            using var _startup = StartupTimeline.Measure("KitchenPlaceDressing.Start");
            Dress(ServiceLocator.TryGet(out IPlayerCondition condition) ? condition.Place : Place.Dublin);
        }

        public void Dress(Place place)
        {
            Place = place;
            if (place == Place.Dublin)
            {
                return; // the scene as built
            }

            Color tint = place == Place.Krakow ? new Color(0.98f, 0.9f, 0.74f) : new Color(0.7f, 0.75f, 0.8f);
            var block = new MaterialPropertyBlock();
            foreach (Renderer wall in walls)
            {
                if (wall == null)
                {
                    continue;
                }

                wall.GetPropertyBlock(block);
                block.SetColor(BaseColorId, tint);
                wall.SetPropertyBlock(block);
            }
        }
    }
}
