using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zmywak.Kitchen.Dishes;
using Zmywak.Kitchen.Shifts;

namespace Zmywak.UI.HUD
{
    /// <summary>
    /// Soaking in the sink: one button over the big dishwasher's, in reach of the thumb. With a scraped dish in hand
    /// it lowers it into the water to soak and frees the hands for the next one; while one soaks under the dish being
    /// washed it says how far it has got, and once the hands are free again it takes it back out to be scrubbed - the
    /// dirt much softer for the wait. (Touching the soaking dish takes it out too.)
    /// </summary>
    public class SoakPanel : MonoBehaviour
    {
        private enum Mode
        {
            Hidden,
            PutIn,
            Waiting,
            TakeOut
        }

        [SerializeField] private WashStation washStation;
        [SerializeField] private ShiftController shiftController;
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;

        private static readonly Color PutInColour = new Color(0.18f, 0.38f, 0.6f);
        private static readonly Color WaitingColour = new Color(0.2f, 0.23f, 0.27f);
        private static readonly Color TakeOutColour = new Color(0.16f, 0.6f, 0.36f);

        public Button Button => button;

        private Image _image;
        private int _shownKey = -1;

        private void OnEnable()
        {
            button?.onClick.AddListener(OnClicked);
        }

        private void OnDisable()
        {
            button?.onClick.RemoveListener(OnClicked);
        }

        private void Update()
        {
            if (button == null || washStation == null)
            {
                return;
            }

            Mode mode = Decide();
            bool shown = mode != Mode.Hidden;
            if (button.gameObject.activeSelf != shown)
            {
                button.gameObject.SetActive(shown);
            }

            if (!shown)
            {
                return;
            }

            DishController soaking = washStation.Soaking;
            int percent = soaking != null ? Mathf.FloorToInt(soaking.SoakProgress01 * 100f) : 0;
            // Rebuilt only when the mode or the percentage changes - not a new string every frame.
            int key = (int)mode * 1000 + percent;
            if (key == _shownKey)
            {
                return;
            }

            _shownKey = key;
            if (label != null)
            {
                label.text = mode == Mode.PutIn ? "Odstaw do moczenia"
                    : mode == Mode.Waiting ? $"Moczy się · {percent}%"
                    : percent >= 100 ? "Wyjmij - namoczone" : $"Wyjmij z wody ({percent}%)";
            }

            button.interactable = mode != Mode.Waiting;
            if (_image == null)
            {
                _image = button.GetComponent<Image>();
            }

            if (_image != null)
            {
                _image.color = mode == Mode.PutIn ? PutInColour : mode == Mode.Waiting ? WaitingColour : TakeOutColour;
            }
        }

        private Mode Decide()
        {
            if (shiftController != null && shiftController.IsShiftOver)
            {
                return Mode.Hidden;
            }

            DishController soaking = washStation.Soaking;
            DishController held = washStation.Occupant;
            if (soaking != null)
            {
                return held == null && !soaking.IsMoving ? Mode.TakeOut : Mode.Waiting;
            }

            return held != null && held.CanSoak ? Mode.PutIn : Mode.Hidden;
        }

        private void OnClicked()
        {
            switch (Decide())
            {
                case Mode.PutIn:
                    washStation.Occupant.TrySoak();
                    break;
                case Mode.TakeOut:
                    washStation.Soaking.TryTakeOutOfSoak();
                    break;
            }
        }
    }
}
