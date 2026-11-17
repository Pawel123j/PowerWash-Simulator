using UnityEngine;
using Zmywak.Core.Graphics;
using Zmywak.Core.Services;

namespace Zmywak.Core.Diagnostics
{
    /// <summary>
    /// Small always-on FPS / frame-time / quality-tier readout for the on-device performance pass
    /// (Stage 6). Only draws in the Editor or a Development Build (<see cref="Debug.isDebugBuild"/>) -
    /// a real release build never shows it, so there's nothing to strip before publishing.
    /// </summary>
    public class DebugPerformanceOverlay : MonoBehaviour
    {
        private float _smoothedDeltaTime;
        private GUIStyle _style;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            _smoothedDeltaTime += (Time.unscaledDeltaTime - _smoothedDeltaTime) * 0.1f;
        }

        private void OnGUI()
        {
            if (!Debug.isDebugBuild && !Application.isEditor)
            {
                return;
            }

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 22,
                    alignment = TextAnchor.UpperLeft,
                    wordWrap = true
                };
                _style.normal.textColor = Color.white;
            }

            float fps = _smoothedDeltaTime > 0f ? 1f / _smoothedDeltaTime : 0f;
            string tier = ServiceLocator.TryGet(out QualityManager quality) ? quality.CurrentTier.ToString() : "?";

            string text = $"{fps:0} FPS  ({_smoothedDeltaTime * 1000f:0} ms)\nJakość: {tier}";
            GUI.Box(new Rect(10, 10, 280, 80), text, _style);
        }
    }
}
