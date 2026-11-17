using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Zmywak.Core.Services;
using Zmywak.Core.Settings;
using Zmywak.Core.Diagnostics;

namespace Zmywak.Core.Graphics
{
    public enum QualityTier
    {
        Low,
        Medium,
        High
    }

    /// <summary>
    /// Low/Medium/High profiles. The game starts on the tier the player picked in the settings, or - on automatic - the tier
    /// it settled on last time, or for a first start the one the device's class suggests (<see cref="DeviceProfile"/>); a
    /// <see cref="QualityGovernor"/> then adjusts it in the kitchen (median frame time per window, two slow windows to step
    /// down, retries upwards with a growing wait, never above the device's class) and the tier it settles on is remembered.
    /// The kitchen runs at its tier's frame rate (60, Low 30); the menu, the evening and a paused shift - a still room, text
    /// to read - run at 30, so they do not warm the phone like the sink. A 120 Hz screen simply shows those rates.
    /// Mutates the project's ACTIVE URP asset at runtime instead of swapping between separate pipeline assets.
    /// </summary>
    public class QualityManager : MonoBehaviour
    {
        public const string KitchenScene = "Kitchen_Shift";

        /// <summary>The frame rate outside the kitchen's play.</summary>
        public const int CalmFrameRate = 30;

        [SerializeField] private QualityTier startingTier = QualityTier.High;
        [SerializeField] private bool autoAdjust = true;

        private QualityGovernor _governor;
        private QualityTier _ceiling;
        private bool _inKitchen;
        private int _appliedFrameRate = -1;

        public QualityTier CurrentTier { get; private set; }

        /// <summary>The tier in force (High until a manager has set one) - for whoever sizes things by it (the dirt masks).</summary>
        public static QualityTier ActiveTier { get; private set; } = QualityTier.High;

        /// <summary>The settings' choice: automatic (the device and the governor decide) or one tier, kept as it is.</summary>
        public static bool Automatic => AccessibilitySettings.QualityChoice < 0;

        private void Awake()
        {
            using var _startup = StartupTimeline.Measure("QualityManager.Awake");
            DontDestroyOnLoad(gameObject);
            ServiceLocator.Register(this);
            _ceiling = Application.isMobilePlatform ? DeviceProfile.ForThisDevice() : startingTier;
            QualityTier start = StartTier();
            _governor = new QualityGovernor(start, Automatic ? _ceiling : start);
            SetTier(start);
            SceneManager.activeSceneChanged += OnSceneChanged;
            _inKitchen = SceneManager.GetActiveScene().name == KitchenScene;
        }

        private QualityTier StartTier()
        {
            int choice = AccessibilitySettings.QualityChoice;
            if (choice >= 0)
            {
                return (QualityTier)Mathf.Clamp(choice, 0, 2);
            }

            int remembered = AccessibilitySettings.LastQualityTier;
            return remembered >= 0 ? (QualityTier)Mathf.Min(remembered, (int)_ceiling) : _ceiling;
        }

        private void OnDestroy()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
            ServiceLocator.Unregister<QualityManager>();
        }

        private void OnSceneChanged(Scene from, Scene to)
        {
            _inKitchen = to.name == KitchenScene;
            _governor.Reset(CurrentTier); // a new scene's first seconds (loading, warming up) are not judged against the old ones
        }

        private void Update()
        {
            bool playing = _inKitchen && Time.timeScale > 0f;
            ApplyFrameRate(playing ? QualityGovernor.TargetFrameRate(CurrentTier) : CalmFrameRate);
            if (autoAdjust && Automatic && playing && _governor.Frame(Time.unscaledDeltaTime))
            {
                SetTier(_governor.Tier);
                AccessibilitySettings.LastQualityTier = (int)_governor.Tier; // start there next time
            }
        }

        private void ApplyFrameRate(int rate)
        {
            if (rate != _appliedFrameRate)
            {
                _appliedFrameRate = rate;
                Application.targetFrameRate = rate;
            }
        }

        /// <summary>Sets the tier from outside (a test); automatic adjustment carries on from it.</summary>
        public void ApplyTier(QualityTier tier)
        {
            _governor?.Reset(tier);
            SetTier(tier);
        }

        /// <summary>The settings' choice: -1 automatic, else a tier kept as it is (the governor stops adjusting it).</summary>
        public void Choose(int choice)
        {
            AccessibilitySettings.QualityChoice = choice;
            QualityTier tier = StartTier();
            _governor = new QualityGovernor(tier, Automatic ? _ceiling : tier);
            SetTier(tier);
        }

        private void SetTier(QualityTier tier)
        {
            CurrentTier = tier;
            ActiveTier = tier;
            _appliedFrameRate = -1; // the rate follows the tier

            UniversalRenderPipelineAsset pipeline = UniversalRenderPipeline.asset;
            if (pipeline == null)
            {
                return;
            }

            // Ambient occlusion (contact darkness under and between things) only where there is GPU to spare.
            foreach (ScriptableRendererData data in pipeline.rendererDataList)
            {
                if (data == null)
                {
                    continue;
                }

                foreach (ScriptableRendererFeature feature in data.rendererFeatures)
                {
                    if (feature is ScreenSpaceAmbientOcclusion)
                    {
                        feature.SetActive(tier == QualityTier.High);
                    }
                }
            }

            switch (tier)
            {
                // Shadow distances fit the kitchen: its long-lens camera stands ~23.5 m from the counter's
                // middle and everything lies within ~6 m of that; URP's default 50 m spread the shadow map over
                // empty space. Low stops just past the rack. (The room behind the menu and hub is small; URP
                // clamps the distance to its camera's far plane anyway.)
                case QualityTier.Low:
                    pipeline.renderScale = 0.75f;
                    pipeline.shadowDistance = 26f;
                    pipeline.msaaSampleCount = 1;
                    pipeline.shadowCascadeCount = 1;
                    break;

                case QualityTier.Medium:
                    pipeline.renderScale = 0.9f;
                    pipeline.shadowDistance = 28f;
                    pipeline.msaaSampleCount = 2;
                    pipeline.shadowCascadeCount = 1;
                    break;

                case QualityTier.High:
                    pipeline.renderScale = 1f;
                    pipeline.shadowDistance = 30f;
                    pipeline.msaaSampleCount = 4;
                    pipeline.shadowCascadeCount = 1;
                    break;
            }
        }
    }
}
