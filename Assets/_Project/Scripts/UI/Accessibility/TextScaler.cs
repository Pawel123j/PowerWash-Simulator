using TMPro;
using UnityEngine;
using Zmywak.Core.Settings;

namespace Zmywak.UI.Accessibility
{
    /// <summary>
    /// Multiplies a TMP text's own base font size by the player's accessibility text-scale preference.
    /// Attach to individual texts (starting with the highest-value ones - dialogue, hub readouts, the kitchen's
    /// counters) rather than scaling a whole Canvas, so layouts don't need to be re-verified project-wide in one pass.
    /// An auto-sized text has its ceiling scaled instead of its size (auto-sizing would overwrite that): it grows
    /// as far as its box holds and never spills out of it - which is how the scene setup builds scaled texts.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class TextScaler : MonoBehaviour
    {
        private TMP_Text _text;
        private float _baseFontSize;
        private float _baseMin;
        private float _baseMax;
        private float _appliedScale = -1f;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
            _baseFontSize = _text.fontSize;
            _baseMin = _text.fontSizeMin;
            _baseMax = _text.fontSizeMax;
        }

        private void OnEnable()
        {
            Apply();
            AccessibilitySettings.Changed += Apply; // the settings say when the size changes: no per-frame polling
        }

        private void OnDisable()
        {
            AccessibilitySettings.Changed -= Apply;
        }

        private void Apply()
        {
            float scale = AccessibilitySettings.TextScale;
            if (Mathf.Approximately(scale, _appliedScale))
            {
                return;
            }

            _appliedScale = scale;
            if (_text.enableAutoSizing)
            {
                _text.fontSizeMax = _baseMax * scale;
                _text.fontSizeMin = Mathf.Min(_baseMin, _text.fontSizeMax); // smaller text may go below the old floor
            }
            else
            {
                _text.fontSize = _baseFontSize * scale;
            }
        }
    }
}
