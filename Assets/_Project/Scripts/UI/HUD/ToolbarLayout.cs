using UnityEngine;
using Zmywak.Kitchen.Tools;

namespace Zmywak.UI.HUD
{
    /// <summary>
    /// Keeps the tool bar tidy while tools come in over the first days: only the tools already on the bar are shown,
    /// side by side and centred, each in the slot size the full bar of five has - so a button never jumps in size, and the
    /// thumb finds the same tools in the same order as the bar fills up.
    /// </summary>
    public class ToolbarLayout : MonoBehaviour
    {
        [SerializeField] private int slots = 5;

        private int _shown = -1;

        private void LateUpdate()
        {
            ActiveToolSelector selector = ActiveToolSelector.Instance;
            int shown = 0;
            foreach (Transform child in transform)
            {
                var button = child.GetComponent<ToolButton>();
                bool on = button == null || selector == null || selector.IsOnTheBar(button.Tool);
                if (child.gameObject.activeSelf != on)
                {
                    child.gameObject.SetActive(on);
                }

                shown += on ? 1 : 0;
            }

            if (shown == _shown)
            {
                return;
            }

            _shown = shown;
            var rect = (RectTransform)transform;
            float slot = rect.rect.width / Mathf.Max(1, slots);
            float start = (rect.rect.width - slot * shown) * 0.5f;
            int index = 0;
            foreach (Transform child in transform)
            {
                if (!child.gameObject.activeSelf)
                {
                    continue;
                }

                var childRect = (RectTransform)child;
                childRect.anchoredPosition = new Vector2(start + index * slot + 10f, childRect.anchoredPosition.y);
                index++;
            }
        }
    }
}
