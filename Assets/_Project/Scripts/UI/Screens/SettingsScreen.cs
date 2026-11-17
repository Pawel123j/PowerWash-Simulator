using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zmywak.Core.Audio;
using Zmywak.Core.Graphics;
using Zmywak.Core.Services;
using Zmywak.Core.Settings;

namespace Zmywak.UI.Screens
{
    /// <summary>
    /// The settings, on two tabs. Sound and screen: all sound on or off, the overall, music and effects levels, the text
    /// size, the screen's effects (shake, flashes), the colour-blind dirt palette. The game: vibration, tilting for glasses,
    /// relaxed mode, the easier sink (the right tool picked by itself), the dishwasher starting itself on a full rack and the
    /// graphics (automatic - the phone's class, then what runs smoothly - or a tier kept as it is; a tap moves to the next).
    /// Everything is saved as it changes. The same card is in the menu, the evening and the kitchen's pause.
    /// </summary>
    public class SettingsScreen : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Slider textScaleSlider;
        [SerializeField] private TMP_Text textScaleValueText;
        [SerializeField] private Toggle colorBlindToggle;
        [SerializeField] private Toggle hapticsToggle;
        [SerializeField] private Toggle soundToggle;
        [SerializeField] private Toggle tiltToggle;
        [SerializeField] private Toggle relaxedModeToggle;
        [SerializeField] private Button closeButton;

        [Header("Sound levels")]
        [SerializeField] private Slider masterSlider;
        [SerializeField] private TMP_Text masterValueText;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private TMP_Text musicValueText;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private TMP_Text sfxValueText;

        [Header("Easier and quieter")]
        [SerializeField] private Toggle screenEffectsToggle;
        [SerializeField] private Toggle toolAssistToggle;
        [SerializeField] private Toggle autoDishwasherToggle;
        [SerializeField] private Button qualityButton;

        [Header("Tabs")]
        [SerializeField] private Button soundTabButton;
        [SerializeField] private Button gameTabButton;
        [SerializeField] private GameObject soundTab;
        [SerializeField] private GameObject gameTab;

        private static readonly Color TabOn = new Color(0.16f, 0.52f, 0.37f);
        private static readonly Color TabOff = new Color(0.22f, 0.3f, 0.38f);

        private float _nextTick;

        private void OnEnable()
        {
            LoadIntoControls();
            ShowTab(true);

            textScaleSlider?.onValueChanged.AddListener(OnTextScaleChanged);
            colorBlindToggle?.onValueChanged.AddListener(OnColorBlindChanged);
            hapticsToggle?.onValueChanged.AddListener(OnHapticsChanged);
            soundToggle?.onValueChanged.AddListener(OnSoundChanged);
            tiltToggle?.onValueChanged.AddListener(OnTiltChanged);
            relaxedModeToggle?.onValueChanged.AddListener(OnRelaxedModeChanged);
            masterSlider?.onValueChanged.AddListener(OnMasterChanged);
            musicSlider?.onValueChanged.AddListener(OnMusicChanged);
            sfxSlider?.onValueChanged.AddListener(OnSfxChanged);
            screenEffectsToggle?.onValueChanged.AddListener(OnScreenEffectsChanged);
            toolAssistToggle?.onValueChanged.AddListener(OnToolAssistChanged);
            autoDishwasherToggle?.onValueChanged.AddListener(OnAutoDishwasherChanged);
            qualityButton?.onClick.AddListener(OnQualityClicked);
            soundTabButton?.onClick.AddListener(OnSoundTab);
            gameTabButton?.onClick.AddListener(OnGameTab);
            closeButton?.onClick.AddListener(OnCloseClicked);
        }

        private void OnDisable()
        {
            AccessibilitySettings.Flush(); // a level still waiting to be written goes to the disk as the card closes
            textScaleSlider?.onValueChanged.RemoveListener(OnTextScaleChanged);
            colorBlindToggle?.onValueChanged.RemoveListener(OnColorBlindChanged);
            hapticsToggle?.onValueChanged.RemoveListener(OnHapticsChanged);
            soundToggle?.onValueChanged.RemoveListener(OnSoundChanged);
            tiltToggle?.onValueChanged.RemoveListener(OnTiltChanged);
            relaxedModeToggle?.onValueChanged.RemoveListener(OnRelaxedModeChanged);
            masterSlider?.onValueChanged.RemoveListener(OnMasterChanged);
            musicSlider?.onValueChanged.RemoveListener(OnMusicChanged);
            sfxSlider?.onValueChanged.RemoveListener(OnSfxChanged);
            screenEffectsToggle?.onValueChanged.RemoveListener(OnScreenEffectsChanged);
            toolAssistToggle?.onValueChanged.RemoveListener(OnToolAssistChanged);
            autoDishwasherToggle?.onValueChanged.RemoveListener(OnAutoDishwasherChanged);
            qualityButton?.onClick.RemoveListener(OnQualityClicked);
            soundTabButton?.onClick.RemoveListener(OnSoundTab);
            gameTabButton?.onClick.RemoveListener(OnGameTab);
            closeButton?.onClick.RemoveListener(OnCloseClicked);
        }

        private void LoadIntoControls()
        {
            textScaleSlider?.SetValueWithoutNotify(AccessibilitySettings.TextScale);
            UpdateTextScaleLabel(AccessibilitySettings.TextScale);

            colorBlindToggle?.SetIsOnWithoutNotify(AccessibilitySettings.ColorBlindMode);
            hapticsToggle?.SetIsOnWithoutNotify(AccessibilitySettings.HapticsEnabled);
            soundToggle?.SetIsOnWithoutNotify(AccessibilitySettings.SoundEnabled);
            tiltToggle?.SetIsOnWithoutNotify(AccessibilitySettings.TiltEnabled);
            relaxedModeToggle?.SetIsOnWithoutNotify(AccessibilitySettings.RelaxedMode);
            screenEffectsToggle?.SetIsOnWithoutNotify(AccessibilitySettings.ScreenEffects);
            toolAssistToggle?.SetIsOnWithoutNotify(AccessibilitySettings.ToolAssist);
            autoDishwasherToggle?.SetIsOnWithoutNotify(AccessibilitySettings.AutoDishwasher);
            UpdateQualityLabel();
            SetLevel(masterSlider, masterValueText, AccessibilitySettings.MasterVolume);
            SetLevel(musicSlider, musicValueText, AccessibilitySettings.MusicVolume);
            SetLevel(sfxSlider, sfxValueText, AccessibilitySettings.SfxVolume);
        }

        private static void SetLevel(Slider slider, TMP_Text label, float value)
        {
            slider?.SetValueWithoutNotify(value);
            if (label != null)
            {
                label.text = $"{Mathf.RoundToInt(value * 100f)}%";
            }
        }

        /// <summary>Opens a tab: sound and screen (true), or the game (false).</summary>
        public void ShowTab(bool sound)
        {
            soundTab?.SetActive(sound);
            gameTab?.SetActive(!sound);
            Tint(soundTabButton, sound);
            Tint(gameTabButton, !sound);
        }

        private static void Tint(Button button, bool on)
        {
            if (button != null && button.targetGraphic != null)
            {
                button.targetGraphic.color = on ? TabOn : TabOff;
            }
        }

        private void OnSoundTab() => ShowTab(true);

        private void OnGameTab() => ShowTab(false);

        private void OnTextScaleChanged(float value)
        {
            AccessibilitySettings.TextScale = value;
            UpdateTextScaleLabel(value);
        }

        private void UpdateTextScaleLabel(float value)
        {
            if (textScaleValueText != null)
            {
                textScaleValueText.text = $"{value:0.0}x";
            }
        }

        private void OnMasterChanged(float value)
        {
            AccessibilitySettings.MasterVolume = value;
            SetLevel(null, masterValueText, value);
            Tick();
        }

        private void OnMusicChanged(float value)
        {
            AccessibilitySettings.MusicVolume = value;
            SetLevel(null, musicValueText, value);
        }

        private void OnSfxChanged(float value)
        {
            AccessibilitySettings.SfxVolume = value;
            SetLevel(null, sfxValueText, value);
            Tick();
        }

        /// <summary>A tick at the new level as a slider moves, so the level is heard while it is set (a few a second).</summary>
        private void Tick()
        {
            if (Time.unscaledTime >= _nextTick)
            {
                _nextTick = Time.unscaledTime + 0.12f;
                GameAudio.Play(GameSound.UiTick, 0.8f);
            }
        }

        private void OnColorBlindChanged(bool value) => AccessibilitySettings.ColorBlindMode = value;

        private void OnHapticsChanged(bool value) => AccessibilitySettings.HapticsEnabled = value;

        private void OnSoundChanged(bool value) => AccessibilitySettings.SoundEnabled = value;

        private void OnTiltChanged(bool value) => AccessibilitySettings.TiltEnabled = value;

        private void OnRelaxedModeChanged(bool value) => AccessibilitySettings.RelaxedMode = value;

        private void OnScreenEffectsChanged(bool value) => AccessibilitySettings.ScreenEffects = value;

        private void OnToolAssistChanged(bool value) => AccessibilitySettings.ToolAssist = value;

        private void OnAutoDishwasherChanged(bool value) => AccessibilitySettings.AutoDishwasher = value;

        /// <summary>Automatic, low, medium, high, and round again. Takes effect at once (the next dish is drawn at its sharpness).</summary>
        private void OnQualityClicked()
        {
            int next = AccessibilitySettings.QualityChoice >= (int)QualityTier.High ? -1 : AccessibilitySettings.QualityChoice + 1;
            if (ServiceLocator.TryGet(out QualityManager quality))
            {
                quality.Choose(next);
            }
            else
            {
                AccessibilitySettings.QualityChoice = next;
            }

            UpdateQualityLabel();
        }

        private void UpdateQualityLabel()
        {
            TMP_Text label = qualityButton != null ? qualityButton.GetComponentInChildren<TMP_Text>(true) : null;
            if (label == null)
            {
                return;
            }

            int choice = AccessibilitySettings.QualityChoice;
            label.text = choice < 0
                ? $"Grafika: automatyczna (teraz {TierName(QualityManager.ActiveTier)})"
                : $"Grafika: {TierName((QualityTier)choice)}";
        }

        private static string TierName(QualityTier tier) => tier == QualityTier.Low ? "niska" : tier == QualityTier.Medium ? "średnia" : "wysoka";

        private void OnCloseClicked()
        {
            panelRoot?.SetActive(false);
        }
    }
}
