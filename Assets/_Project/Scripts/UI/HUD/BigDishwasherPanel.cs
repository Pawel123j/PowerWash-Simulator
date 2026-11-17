using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zmywak.Kitchen.Dishwasher;
using Zmywak.Kitchen.Shifts;

namespace Zmywak.UI.HUD
{
    /// <summary>
    /// The big dishwasher's controls. A button above the toolbar, in reach of the thumb, puts the dirty dish in the
    /// sink straight into the machine - shown only while there is one the machine can take. A card beside the
    /// machine says how full it is, how long it still has to go, or that it is done, with the one button that
    /// matters then: start it, or unload it.
    /// </summary>
    public class BigDishwasherPanel : MonoBehaviour
    {
        [SerializeField] private BigDishwasherController machine;
        [SerializeField] private ShiftController shiftController;
        [SerializeField] private Button loadButton;
        [SerializeField] private TMP_Text loadLabel;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button actionButton;
        [SerializeField] private TMP_Text actionLabel;

        private const float FullHeight = 200f;
        private const float StatusOnlyHeight = 60f;

        private int _shownKey = -1;

        private static readonly Color StartColour = new Color(0.17f, 0.42f, 0.72f);
        private static readonly Color UnloadColour = new Color(0.16f, 0.6f, 0.36f);

        public Button LoadButton => loadButton;
        public Button ActionButton => actionButton;

        private void OnEnable()
        {
            loadButton?.onClick.AddListener(OnLoadClicked);
            actionButton?.onClick.AddListener(OnActionClicked);
        }

        private void OnDisable()
        {
            loadButton?.onClick.RemoveListener(OnLoadClicked);
            actionButton?.onClick.RemoveListener(OnActionClicked);
        }

        private void Update()
        {
            if (machine == null)
            {
                return;
            }

            bool open = shiftController == null || !shiftController.IsShiftOver;
            bool done = machine.IsDone;
            bool running = machine.IsRunning;

            if (loadButton != null)
            {
                bool loadable = open && machine.LoadableDish != null;
                if (loadButton.gameObject.activeSelf != loadable)
                {
                    loadButton.gameObject.SetActive(loadable);
                }
            }

            // The lines are built only when what they say changes (a dish in, a second off the clock), not every frame.
            int seconds = running ? Mathf.CeilToInt((float)machine.Remaining.TotalSeconds) : 0;
            int key = (((seconds * 64 + machine.Count) * 64 + machine.Capacity) * 2 + (done ? 1 : 0)) * 4 + (running ? 2 : 0) + (machine.CanStart ? 1 : 0);
            if (key != _shownKey)
            {
                _shownKey = key;
                if (loadLabel != null)
                {
                    loadLabel.text = $"Do dużej zmywarki ({machine.Count}/{machine.Capacity})";
                }

                if (statusText != null)
                {
                    statusText.text = done ? $"<b>Gotowe!</b> Czyste: {machine.Count}"
                        : running ? $"Myje się · <b>{Clock(machine.Remaining)}</b>"
                        : machine.CanStart ? $"Duża zmywarka <b>{machine.Count}/{machine.Capacity}</b> · {machine.Cycle.TotalMinutes:0} min"
                        : $"Duża zmywarka <b>{machine.Count}/{machine.Capacity}</b>";
                }
            }

            if (actionButton != null)
            {
                bool action = open && (done || machine.CanStart);
                if (actionButton.gameObject.activeSelf != action)
                {
                    actionButton.gameObject.SetActive(action);
                }

                // Without its button the card is just the status line, not an empty dark box over the counter.
                var card = (RectTransform)transform;
                float height = action ? FullHeight : StatusOnlyHeight;
                if (!Mathf.Approximately(card.sizeDelta.y, height))
                {
                    card.sizeDelta = new Vector2(card.sizeDelta.x, height);
                }

                if (action)
                {
                    if (actionLabel != null)
                    {
                        actionLabel.text = done ? "Wypakuj" : "Uruchom";
                    }

                    var image = actionButton.GetComponent<Image>();
                    if (image != null)
                    {
                        image.color = done ? UnloadColour : StartColour;
                    }
                }
            }
        }

        /// <summary>Minutes and seconds left, rounded up (a cycle shows 30:00 as it starts, not 29:59).</summary>
        private static string Clock(TimeSpan remaining)
        {
            int seconds = Mathf.CeilToInt((float)remaining.TotalSeconds);
            return $"{seconds / 60:00}:{seconds % 60:00}";
        }

        private void OnLoadClicked()
        {
            machine?.TryLoadFromSink();
        }

        private void OnActionClicked()
        {
            if (machine == null || (shiftController != null && shiftController.IsShiftOver))
            {
                return;
            }

            if (machine.IsDone)
            {
                machine.TryUnload();
            }
            else
            {
                machine.TryStart();
            }
        }
    }
}
