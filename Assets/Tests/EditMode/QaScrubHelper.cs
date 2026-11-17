using UnityEngine;
using Zmywak.Core.Data;
using Zmywak.Kitchen.Dishes;

namespace Zmywak.Tests
{
    /// <summary>
    /// Scrubbing a dish for QA: a sweep that reaches every texel of the dish (rows 0.05 apart, edge to edge, so none sits near a
    /// brush's rim), and an exhaustion check that does not rely on the code under test - it reads the dirt itself with
    /// <see cref="DirtCanvas.DirtAt"/>. (The first PWS-020 probe swept rows only from 0.1 to 0.82 and so left a cap of thick dirt at
    /// the top of the dish untouched, and judged "nothing more to take off" from a single stroke in the middle.)
    /// </summary>
    internal static class QaScrubHelper
    {
        public const float ExhaustedSlack = 1e-3f;

        /// <summary>One sweep over the whole dish with the tool; whether any dirt moved.</summary>
        public static bool Pass(DishInstance dish, ToolDefinition tool)
        {
            bool changed = false;
            for (int row = 0; row <= 20; row++)
            {
                float y = row * 0.05f;
                changed |= dish.ScrubStroke(tool, new Vector2(0f, y), new Vector2(1f, y), 0.5f);
            }

            return changed;
        }

        /// <summary>The thickest dirt, on any layer the tool is effective against, anywhere on the dish (0 when none is left).</summary>
        public static float ThickestEffective(DishInstance dish, ToolDefinition tool)
        {
            float thickest = 0f;
            int n = dish.Canvas.Resolution;
            for (int layer = 0; layer < dish.Layers.Count; layer++)
            {
                DirtTypeDefinition type = dish.Layers[layer].Type;
                if (type == null || dish.Layers[layer].Remaining <= 0f || !tool.IsEffectiveAgainst(type))
                {
                    continue;
                }

                for (int y = 0; y < n; y++)
                {
                    for (int x = 0; x < n; x++)
                    {
                        float v = dish.Canvas.DirtAt(layer, new Vector2((x + 0.5f) / n, (y + 0.5f) / n));
                        if (v > thickest)
                        {
                            thickest = v;
                        }
                    }
                }
            }

            return thickest;
        }

        /// <summary>
        /// Sweeps until the tool has taken off everything it can: no dirt it is effective against is thicker than its residue floor
        /// (read independently of ToolSuits), or the dish came clean. False if that was not reached in <paramref name="maxPasses"/>.
        /// </summary>
        public static bool Exhaust(DishInstance dish, ToolDefinition tool, int maxPasses = 200)
        {
            for (int pass = 0; pass < maxPasses && dish.Stage == WashStage.AwaitingScrub; pass++)
            {
                if (ThickestEffective(dish, tool) <= tool.ResidueFloor + ExhaustedSlack)
                {
                    return true;
                }

                Pass(dish, tool);
            }

            return dish.Stage != WashStage.AwaitingScrub || ThickestEffective(dish, tool) <= tool.ResidueFloor + ExhaustedSlack;
        }
    }
}
