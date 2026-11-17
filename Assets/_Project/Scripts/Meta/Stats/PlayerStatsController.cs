using UnityEngine;

namespace Zmywak.Meta.Stats
{
    /// <summary>The three meta stats from the design doc, each clamped to 0-100. Plain C# so it is unit-testable.</summary>
    public class PlayerStatsController
    {
        public float Energy { get; private set; } = 100f;
        public float Homesickness { get; private set; } = 30f;
        public float Integration { get; private set; } = 10f;

        public void ChangeEnergy(float delta) => Energy = Mathf.Clamp(Energy + delta, 0f, 100f);
        public void ChangeHomesickness(float delta) => Homesickness = Mathf.Clamp(Homesickness + delta, 0f, 100f);
        public void ChangeIntegration(float delta) => Integration = Mathf.Clamp(Integration + delta, 0f, 100f);

        public void LoadFrom(float energy, float homesickness, float integration)
        {
            Energy = Mathf.Clamp(energy, 0f, 100f);
            Homesickness = Mathf.Clamp(homesickness, 0f, 100f);
            Integration = Mathf.Clamp(integration, 0f, 100f);
        }
    }
}
