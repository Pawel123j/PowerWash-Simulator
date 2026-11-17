using UnityEngine;
using UnityEngine.UI;

namespace Zmywak.UI.Common
{
    /// <summary>
    /// Sets how full a bar is. A Filled image stretches its whole sprite, which squashes the rounded
    /// ends of a pill-shaped bar; a Sliced fill keeps its rounded caps and is sized through its anchors
    /// instead. Handles both, so a bar can use either.
    /// </summary>
    public static class FillBar
    {
        public static void Set(Image fill, float value01, bool vertical = false)
        {
            if (fill == null)
            {
                return;
            }

            value01 = Mathf.Clamp01(value01);
            if (fill.type == Image.Type.Filled)
            {
                fill.fillAmount = value01;
                return;
            }

            // Only what changed is written: every write to a RectTransform rebuilds the canvas it is on, and bars are set
            // every frame by whoever slides them.
            RectTransform rect = fill.rectTransform;
            if (rect.anchorMin != Vector2.zero)
            {
                rect.anchorMin = Vector2.zero;
            }

            Vector2 max = vertical ? new Vector2(1f, value01) : new Vector2(value01, 1f);
            if (rect.anchorMax != max)
            {
                rect.anchorMax = max;
            }

            // Hidden when empty - a zero-size sliced sprite still draws its corner caps.
            bool shows = value01 > 0.001f;
            if (fill.enabled != shows)
            {
                fill.enabled = shows;
            }
        }

        public static float Get(Image fill, bool vertical = false)
        {
            if (fill == null)
            {
                return 0f;
            }

            if (fill.type == Image.Type.Filled)
            {
                return fill.fillAmount;
            }

            return vertical ? fill.rectTransform.anchorMax.y : fill.rectTransform.anchorMax.x;
        }
    }
}
