using UnityEngine;

namespace Zmywak.Core.Settings
{
    /// <summary>
    /// Colorblind mode swaps dirt-type tints for a palette chosen to stay distinguishable under
    /// protanopia/deuteranopia (blue vs. orange vs. near-black, reddish purple, bluish green), instead of the
    /// default brown/red/black/beige which lean on a red-green distinction. Ties directly into a real gameplay
    /// need (telling the dirt types apart) rather than a generic, unverifiable post-process trick.
    /// </summary>
    public static class ColorBlindPalette
    {
        public static Color Adjust(Color original, string dirtTypeId)
        {
            if (!AccessibilitySettings.ColorBlindMode)
            {
                return original;
            }

            switch (dirtTypeId)
            {
                case "grease":
                    return new Color(0.10f, 0.35f, 0.75f); // blue
                case "dried_sauce":
                    return new Color(0.90f, 0.50f, 0.05f); // orange
                case "burnt_food":
                    return Color.black;
                // Two more from the same colour-blind-safe set (Okabe-Ito) for the bowl's and the mug's dirt:
                // a beige porridge on a white bowl and a brown coffee are close for everyone, let alone without red-green.
                case "dried_porridge":
                    return new Color(0.80f, 0.47f, 0.65f); // reddish purple
                case "coffee":
                    return new Color(0.00f, 0.62f, 0.45f); // bluish green
                case "baked_cheese":
                    return new Color(0.94f, 0.89f, 0.26f); // yellow, against the roaster's navy
                default:
                    return original;
            }
        }
    }
}
