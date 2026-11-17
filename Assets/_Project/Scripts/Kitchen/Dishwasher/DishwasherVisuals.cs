using UnityEngine;
using Zmywak.Core.Diagnostics;

namespace Zmywak.Kitchen.Dishwasher
{
    /// <summary>
    /// What the dishwasher shows on its front panel: a status light (green = ready, pulsing amber =
    /// washing, bright green flash = done, hard red blink = broken down) and a progress bar filling up over the cycle. Emissive colours
    /// are pushed above 1 so the bloom picks them up as a glow.
    /// </summary>
    public class DishwasherVisuals : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private DishwasherController dishwasher;
        [SerializeField] private Renderer statusLight;
        [SerializeField] private Transform progressFill;
        [SerializeField] private float progressFullWidth = 1f;
        [SerializeField] private Color idleColor = new Color(0.2f, 0.9f, 0.35f);
        [SerializeField] private Color runningColor = new Color(1f, 0.6f, 0.1f);
        [SerializeField] private Color brokenColor = new Color(1f, 0.12f, 0.08f);
        [SerializeField] private float doneFlashSeconds = 1.5f;

        private MaterialPropertyBlock _block;
        private float _doneFlashRemaining;

        private void OnEnable()
        {
            using var _startup = StartupTimeline.Measure("DishwasherVisuals.OnEnable");
            _block = new MaterialPropertyBlock();
            if (dishwasher != null)
            {
                dishwasher.OnCycleCompleted += OnCycleCompleted;
            }
        }

        private void OnDisable()
        {
            if (dishwasher != null)
            {
                dishwasher.OnCycleCompleted -= OnCycleCompleted;
            }
        }

        private void OnCycleCompleted(System.Collections.Generic.IReadOnlyList<Zmywak.Core.Data.DishDefinition> washed)
        {
            _doneFlashRemaining = doneFlashSeconds;
        }

        private void Update()
        {
            if (dishwasher == null)
            {
                return;
            }

            if (_doneFlashRemaining > 0f)
            {
                _doneFlashRemaining -= Time.deltaTime;
            }

            Color light;
            float glow;
            if (dishwasher.IsBroken)
            {
                light = brokenColor;
                glow = Mathf.Repeat(Time.time * 2.5f, 1f) < 0.5f ? 4f : 0.3f;
            }
            else if (dishwasher.IsRunning)
            {
                light = runningColor;
                glow = 1.5f + Mathf.Sin(Time.time * 5f) * 0.8f;
            }
            else if (_doneFlashRemaining > 0f)
            {
                light = idleColor;
                glow = 3f + Mathf.Sin(Time.time * 18f) * 1.5f;
            }
            else
            {
                light = idleColor;
                glow = 1.2f;
            }

            if (statusLight != null)
            {
                statusLight.GetPropertyBlock(_block);
                _block.SetColor(BaseColorId, light);
                _block.SetColor(EmissionColorId, light * glow);
                statusLight.SetPropertyBlock(_block);
            }

            if (progressFill != null)
            {
                float progress = dishwasher.IsRunning ? dishwasher.Progress01 : 0f;
                Vector3 scale = progressFill.localScale;
                scale.x = Mathf.Max(0.0001f, progress * progressFullWidth);
                progressFill.localScale = scale;
                // Grows from the left end of the slot.
                Vector3 position = progressFill.localPosition;
                position.x = -progressFullWidth * 0.5f + scale.x * 0.5f;
                progressFill.localPosition = position;
            }
        }
    }
}
