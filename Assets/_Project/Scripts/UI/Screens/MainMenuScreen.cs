using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zmywak.Core.Audio;
using Zmywak.Core.Data;
using Zmywak.Core.GameState;
using Zmywak.Core.GameState.States;
using Zmywak.Core.Save;

namespace Zmywak.UI.Screens
{
    /// <summary>
    /// The main menu. With a life in progress the first thing on it is CONTINUE - the save last played, with where it
    /// stands (the day, the date, the place) - and under it a new game, the three save slots and the settings. A first-time
    /// player sees no slots to choose from: "Nowa gra" starts in the first empty slot (and only when all three are taken
    /// does the list come up, to make room). The slots list says where each life stands, plays one, or deletes one with a
    /// second tap to be sure. Back with nothing open asks whether to leave the game.
    /// </summary>
    public class MainMenuScreen : MonoBehaviour
    {
        [SerializeField] private Button continueButton;
        [SerializeField] private TMP_Text continueDetails;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject slotsPanel;
        [SerializeField] private TMP_Text slotsNote;
        [SerializeField] private Button[] slotButtons = new Button[0];
        [SerializeField] private Button[] deleteButtons = new Button[0];
        [SerializeField] private Button closeSlotsButton;
        [SerializeField] private GameObject quitPanel;
        [SerializeField] private Button quitYesButton;
        [SerializeField] private Button quitNoButton;

        private int _confirmingDelete = -1;
        private Vector2? _newGameHome;
        private Vector2? _settingsHome;

        /// <summary>The save CONTINUE plays (-1 when there is none).</summary>
        public int ContinueSlot { get; private set; } = -1;

        private void OnEnable()
        {
            continueButton?.onClick.AddListener(OnContinueClicked);
            newGameButton?.onClick.AddListener(OnNewGameClicked);
            loadButton?.onClick.AddListener(OpenSlots);
            settingsButton?.onClick.AddListener(OpenSettings);
            closeSlotsButton?.onClick.AddListener(CloseSlots);
            quitYesButton?.onClick.AddListener(Quit);
            quitNoButton?.onClick.AddListener(CloseQuit);
            for (int i = 0; i < slotButtons.Length; i++)
            {
                int slot = i;
                slotButtons[i]?.onClick.AddListener(() => Play(slot));
            }

            for (int i = 0; i < deleteButtons.Length; i++)
            {
                int slot = i;
                deleteButtons[i]?.onClick.AddListener(() => Delete(slot));
            }

            slotsPanel?.SetActive(false);
            quitPanel?.SetActive(false);
            settingsPanel?.SetActive(false);
            Refresh();
        }

        private void OnDisable()
        {
            continueButton?.onClick.RemoveListener(OnContinueClicked);
            newGameButton?.onClick.RemoveListener(OnNewGameClicked);
            loadButton?.onClick.RemoveListener(OpenSlots);
            settingsButton?.onClick.RemoveListener(OpenSettings);
            closeSlotsButton?.onClick.RemoveListener(CloseSlots);
            quitYesButton?.onClick.RemoveListener(Quit);
            quitNoButton?.onClick.RemoveListener(CloseQuit);
            foreach (Button button in slotButtons)
            {
                button?.onClick.RemoveAllListeners();
            }

            foreach (Button button in deleteButtons)
            {
                button?.onClick.RemoveAllListeners();
            }
        }

        /// <summary>Shows CONTINUE for the save last played (or any save there is), and the slots only once there is one.</summary>
        private void Refresh()
        {
            ContinueSlot = SaveSystem.HasReadableSave(SaveSystem.Slot) ? SaveSystem.Slot : FirstSlot(withSave: true);
            bool any = ContinueSlot >= 0;
            continueButton?.gameObject.SetActive(any);
            loadButton?.gameObject.SetActive(any);
            if (continueDetails != null)
            {
                continueDetails.text = any ? Where(SaveSystem.Peek(ContinueSlot)) : string.Empty;
            }

            SetLabel(newGameButton, any ? "Nowa gra" : "Rozpocznij grę");

            // A first start: the one way in takes CONTINUE's place, big and green.
            if (newGameButton != null && continueButton != null)
            {
                var rect = (RectTransform)newGameButton.transform;
                _newGameHome ??= rect.anchoredPosition;
                rect.anchoredPosition = any ? _newGameHome.Value : ((RectTransform)continueButton.transform).anchoredPosition;
                if (newGameButton.targetGraphic != null)
                {
                    newGameButton.targetGraphic.color = any ? new Color(0.22f, 0.3f, 0.38f) : new Color(0.16f, 0.52f, 0.37f);
                }

                // ...and the settings close up under it, instead of waiting at the bottom past two empty places.
                if (settingsButton != null)
                {
                    var settingsRect = (RectTransform)settingsButton.transform;
                    _settingsHome ??= settingsRect.anchoredPosition;
                    settingsRect.anchoredPosition = any ? _settingsHome.Value : _newGameHome.Value;
                }
            }
        }

        private static int FirstSlot(bool withSave)
        {
            for (int slot = 0; slot < SaveSystem.SlotCount; slot++)
            {
                if (SaveSystem.HasReadableSave(slot) == withSave)
                {
                    return slot;
                }
            }

            return -1;
        }

        private void OnContinueClicked()
        {
            if (ContinueSlot >= 0)
            {
                Play(ContinueSlot);
            }
        }

        /// <summary>A new life in the first empty slot - or, all three taken, the list to make room in.</summary>
        private void OnNewGameClicked()
        {
            int empty = FirstSlot(withSave: false);
            if (empty >= 0)
            {
                Play(empty);
                return;
            }

            OpenSlots();
            if (slotsNote != null)
            {
                slotsNote.text = "Wszystkie trzy zapisy są zajęte. Usuń jeden, żeby zacząć nową grę.";
            }
        }

        public void OpenSlots()
        {
            _confirmingDelete = -1;
            RefreshSlots();
            if (slotsNote != null)
            {
                slotsNote.text = "Każdy zapis to osobne życie Kuby.";
            }

            slotsPanel?.SetActive(true);
        }

        private void CloseSlots()
        {
            slotsPanel?.SetActive(false);
            Refresh();
        }

        private void OpenSettings()
        {
            settingsPanel?.SetActive(true);
        }

        /// <summary>Back on the menu with nothing open: "leave the game?"</summary>
        public void AskToQuit()
        {
            quitPanel?.SetActive(true);
        }

        private void CloseQuit()
        {
            quitPanel?.SetActive(false);
        }

        private void Quit()
        {
            quitPanel?.SetActive(false);
            Application.Quit(); // everything is saved as it happens; nothing is lost here
        }

        /// <summary>Plays a slot: starts the game over from it and goes to the hub.</summary>
        public void Play(int slot)
        {
            SaveSystem.Slot = slot;
            StartFromAnyScene.RestartInto(typeof(MetaHubState), this);
        }

        /// <summary>Deletes a slot on the second tap (the first asks "Na pewno?").</summary>
        public void Delete(int slot)
        {
            if (_confirmingDelete != slot)
            {
                _confirmingDelete = slot;
                RefreshSlots();
                GameAudio.Play(GameSound.UiError, 0.4f);
                return;
            }

            SaveSystem.DeleteSlot(slot);
            _confirmingDelete = -1;
            RefreshSlots();
            Refresh();
        }

        private void RefreshSlots()
        {
            for (int slot = 0; slot < slotButtons.Length; slot++)
            {
                SaveData data = SaveSystem.Peek(slot);
                SetLabel(slotButtons[slot], Summary(slot, data));
                if (slot < deleteButtons.Length && deleteButtons[slot] != null)
                {
                    deleteButtons[slot].gameObject.SetActive(data != null);
                    SetLabel(deleteButtons[slot], _confirmingDelete == slot ? "Na pewno?" : "Usuń");
                }
            }
        }

        /// <summary>A slot on the menu: where that life stands, or that it is empty.</summary>
        public static string Summary(int slot, SaveData data)
        {
            string head = $"Zapis {slot + 1}";
            if (data == null)
            {
                return $"{head}\n<size=70%>pusty - nowa gra</size>";
            }

            var place = (Place)data.place;
            return $"{head} - dzień {Mathf.Max(1, data.dayNumber)}, {Date(data)}\n" +
                   $"<size=70%>{CityMap.Title(place)} · {PlaceMoney.Format(data.walletEuros, place)} · {Chapter(data)}</size>";
        }

        /// <summary>Under CONTINUE: the day, the date, the place and the chapter of the save.</summary>
        public static string Where(SaveData data)
        {
            if (data == null)
            {
                return string.Empty;
            }

            return $"Dzień {Mathf.Max(1, data.dayNumber)} · {Date(data)} · {CityMap.Title((Place)data.place)} · {Chapter(data)}";
        }

        private static string Date(SaveData data) =>
            System.DateTime.TryParse(data.currentDateIso, CultureInfo.InvariantCulture, DateTimeStyles.None, out System.DateTime parsed)
                ? parsed.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)
                : "?";

        private static string Chapter(SaveData data)
        {
            int told = 0;
            for (int chapter = 1; chapter <= 6; chapter++)
            {
                if (data.storyFlags != null && data.storyFlags.Contains($"ch{chapter}_complete"))
                {
                    told = chapter;
                }
            }

            return told >= 6 ? "po historii" : told == 5 ? "epilog" : $"rozdział {told + 1}";
        }

        private static void SetLabel(Button button, string text)
        {
            TMP_Text label = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
            if (label != null)
            {
                label.text = text;
            }
        }
    }
}
