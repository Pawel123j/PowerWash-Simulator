using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Zmywak.UI.Common
{
    /// <summary>
    /// The Android back gesture (the Input System reports it as the Escape key) closes the topmost open
    /// panel of a screen - list them top-first - the way players expect back to behave. With nothing open it does what the
    /// screen says back means there (<see cref="onNothingOpen"/>: the main menu asks whether to leave the game, the evening
    /// opens its menu) - never nothing, and never anything that loses progress without asking.
    /// </summary>
    public class BackClosesPanels : MonoBehaviour
    {
        [Tooltip("Panels in stacking order, topmost first; back closes the first one that is open.")]
        [SerializeField] private GameObject[] panels = new GameObject[0];

        [Tooltip("What back does when no panel is open (empty: nothing).")]
        [SerializeField] private UnityEvent onNothingOpen = new UnityEvent();

        /// <summary>The screen's own back (for the setup to wire).</summary>
        public UnityEvent OnNothingOpenEvent => onNothingOpen;

        /// <summary>While set and false, back does nothing (a story scene playing over the hub has its own way back).</summary>
        public System.Func<bool> Allowed { get; set; }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Back();
            }
        }

        /// <summary>The back gesture: the topmost panel closes, or - with none open - the screen's own back.</summary>
        public void Back()
        {
            if (Allowed != null && !Allowed())
            {
                return;
            }

            if (!CloseTopmost())
            {
                onNothingOpen?.Invoke();
            }
        }

        /// <summary>Closes the topmost open panel; false if none was open.</summary>
        public bool CloseTopmost()
        {
            foreach (GameObject panel in panels)
            {
                if (panel != null && panel.activeInHierarchy)
                {
                    panel.SetActive(false);
                    Zmywak.Core.Audio.GameAudio.Play(Zmywak.Core.Audio.GameSound.UiBack, 0.5f);
                    return true;
                }
            }

            return false;
        }

        /// <summary>Whether any of the panels is open.</summary>
        public bool AnyOpen
        {
            get
            {
                foreach (GameObject panel in panels)
                {
                    if (panel != null && panel.activeInHierarchy)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
