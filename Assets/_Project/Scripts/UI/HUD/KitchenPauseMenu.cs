using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zmywak.Core.GameState;
using Zmywak.Core.GameState.States;
using Zmywak.Core.Services;
using Zmywak.Kitchen.Input;
using Zmywak.Kitchen.Shifts;

namespace Zmywak.UI.HUD
{
    /// <summary>
    /// The shift's pause: the ⏸ button, the Android back gesture (the Input System reports it as the Escape
    /// key) and any interruption - a phone call, switching apps - stop the clock, the physics and the sound
    /// until the player taps "back to the sink". The card has the settings, clocking out, and the way to the main menu -
    /// which asks first, and clocks out for what is done (the pay and the day are kept: nothing is lost by leaving).
    /// Back closes whatever card is on top (the repair, the settings, the question) before it resumes.
    /// </summary>
    public class KitchenPauseMenu : MonoBehaviour
    {
        [SerializeField] private ShiftController shiftController;
        [SerializeField] private DishInputRouter inputRouter;
        [SerializeField] private DishwasherRepairPanel repairPanel;
        [SerializeField] private GameObject root;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button endShiftButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Button menuButton;
        [SerializeField] private GameObject menuConfirm;
        [SerializeField] private Button menuYesButton;
        [SerializeField] private Button menuNoButton;

        private float _timeScaleBeforePause = 1f;

        public bool IsPaused { get; private set; }

        /// <summary>
        /// Whether the app going to the background (a call, switching apps) pauses the shift: always on a phone, never in
        /// the Editor, where focus comes and goes with every click outside the Game view (and never exists in batch-mode
        /// test runs). Tests set it to check what the phone does, and back to null afterwards.
        /// </summary>
        public static bool? PauseOnInterruptionOverride { get; set; }

        private static bool PausesOnInterruption => PauseOnInterruptionOverride ?? !Application.isEditor;

        private void Awake()
        {
            pauseButton?.onClick.AddListener(Pause);
            resumeButton?.onClick.AddListener(Resume);
            endShiftButton?.onClick.AddListener(EndShiftEarly);
            settingsButton?.onClick.AddListener(OpenSettings);
            menuButton?.onClick.AddListener(AskForMenu);
            menuYesButton?.onClick.AddListener(GoToMenu);
            menuNoButton?.onClick.AddListener(CloseMenuConfirm);
            if (root != null)
            {
                root.SetActive(false);
            }

            settingsPanel?.SetActive(false);
            menuConfirm?.SetActive(false);
        }

        private void OnDestroy()
        {
            pauseButton?.onClick.RemoveListener(Pause);
            resumeButton?.onClick.RemoveListener(Resume);
            endShiftButton?.onClick.RemoveListener(EndShiftEarly);
            settingsButton?.onClick.RemoveListener(OpenSettings);
            menuButton?.onClick.RemoveListener(AskForMenu);
            menuYesButton?.onClick.RemoveListener(GoToMenu);
            menuNoButton?.onClick.RemoveListener(CloseMenuConfirm);
            if (IsPaused)
            {
                // Leaving the scene while paused must not leave the whole game frozen and silent.
                Time.timeScale = 1f;
                AudioListener.pause = false;
            }
        }

        private void Update()
        {
            bool shiftRunning = shiftController != null && !shiftController.IsShiftOver;
            if (pauseButton != null)
            {
                pauseButton.gameObject.SetActive(shiftRunning);
            }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                OnBack();
            }
        }

        /// <summary>The system back gesture: close the topmost card, else toggle the pause.</summary>
        public void OnBack()
        {
            if (settingsPanel != null && settingsPanel.activeInHierarchy)
            {
                settingsPanel.SetActive(false);
            }
            else if (menuConfirm != null && menuConfirm.activeInHierarchy)
            {
                menuConfirm.SetActive(false);
            }
            else if (repairPanel != null && repairPanel.IsOpen && !IsPaused)
            {
                repairPanel.Close();
            }
            else if (IsPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }

        // A phone call or switching apps (see PauseOnInterruptionOverride for the Editor). Coming back does not resume:
        // the card stays until the player taps "back to the sink", so the clock never runs under a surprised thumb.
        private void OnApplicationPause(bool paused)
        {
            if (paused && PausesOnInterruption)
            {
                Pause();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && PausesOnInterruption)
            {
                Pause();
            }
        }

        public void Pause()
        {
            if (IsPaused || shiftController == null || shiftController.IsShiftOver)
            {
                return;
            }

            IsPaused = true;
            _timeScaleBeforePause = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            inputRouter?.AddBlocker(this);
            root?.SetActive(true);
        }

        public void Resume()
        {
            if (!IsPaused)
            {
                return;
            }

            IsPaused = false;
            Time.timeScale = _timeScaleBeforePause;
            AudioListener.pause = false;
            inputRouter?.RemoveBlocker(this);
            root?.SetActive(false);
            settingsPanel?.SetActive(false);
            menuConfirm?.SetActive(false);
        }

        private void OpenSettings()
        {
            settingsPanel?.SetActive(true);
        }

        private void AskForMenu()
        {
            menuConfirm?.SetActive(true);
        }

        private void CloseMenuConfirm()
        {
            menuConfirm?.SetActive(false);
        }

        /// <summary>To the main menu: the shift is clocked out for what is done (paid and saved like any other), then the menu.</summary>
        public void GoToMenu()
        {
            Resume();
            shiftController?.EndShiftNow();
            if (ServiceLocator.TryGet(out GameStateMachine machine))
            {
                machine.ChangeState<MainMenuState>();
            }
        }

        private void EndShiftEarly()
        {
            Resume();
            shiftController?.EndShiftNow();
        }
    }
}
