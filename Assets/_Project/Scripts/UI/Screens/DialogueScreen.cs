using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zmywak.Core.Audio;
using Zmywak.Narrative.Dialogue;

namespace Zmywak.UI.Screens
{
    /// <summary>
    /// Presents whatever <see cref="DialogueRunner"/> is currently doing: a line, typed out as it is said, or a choice - with
    /// the speaker's portrait above the box (Kuba's while he decides, nobody's while the narrator speaks), which rises and
    /// fades in when the speaker changes. A tap anywhere on the box while a line is being typed shows all of it at once; the
    /// next tap goes on. The box is only as tall as what it holds (a line, or a line and the choices), comes up from the
    /// bottom when a scene starts, and keeps a history of the scene's lines for whoever tapped through one too fast.
    /// Blurred English stays blurred: the typing counts letters, not the tags that blur them.
    /// </summary>
    public class DialogueScreen : MonoBehaviour, IPointerClickHandler
    {
        /// <summary>A speaker's portrait (see <see cref="SpeakerNames.PortraitId"/>).</summary>
        [Serializable]
        public struct SpeakerPortrait
        {
            public string speakerId;
            public Sprite sprite;
        }

        private const float PortraitFadeSeconds = 0.18f;
        private const float PortraitRise = 28f;
        private const float CharactersPerSecond = 48f;
        private const int HistoryLength = 60;

        [SerializeField] private DialogueRunner runner;
        [SerializeField] private TMP_Text speakerText;
        [Tooltip("The name tag behind the speaker's name - hidden with it when the narrator speaks.")]
        [SerializeField] private GameObject speakerTag;
        [SerializeField] private TMP_Text lineText;
        [SerializeField] private Button continueButton;
        [SerializeField] private GameObject choicesPanel;
        [SerializeField] private Button[] choiceButtons;
        [SerializeField] private TMP_Text[] choiceButtonTexts;
        [Tooltip("Shows the speaker's portrait; behind the box, so the bust's lower edge is hidden.")]
        [SerializeField] private Image portrait;
        [SerializeField] private List<SpeakerPortrait> portraits = new List<SpeakerPortrait>();

        [Header("The box")]
        [SerializeField] private RectTransform dialogueBox;
        [SerializeField] private CanvasGroup boxGroup;
        [SerializeField] private float lineHeight = 560f;
        [SerializeField] private float choiceHeight = 860f;
        [SerializeField] private Button historyButton;
        [SerializeField] private GameObject historyPanel;
        [SerializeField] private TMP_Text historyText;
        [SerializeField] private Button closeHistoryButton;

        private Dictionary<string, Sprite> _portraitById;
        private Vector2 _portraitHome;
        private float _portraitAlpha;
        private bool _portraitWanted;
        private float _boxHeight;
        private float _boxWanted;
        private float _boxIn;
        private float _typed;
        private int _total;
        private int _lastBlip;
        private bool _showingChoices;
        private float _choicesShownAt;
        private string _speaker;
        private string _lineBeforeSaveError;

        /// <summary>
        /// How long new choices ignore taps. A player tapping quickly through the lines must not pick a choice with the
        /// tap meant for the next line: on the emulator the last twenty euro went to the agent that way, unread.
        /// </summary>
        public const float ChoiceTapGuardSeconds = 0.4f;
        private readonly List<string> _history = new List<string>();

        /// <summary>The portrait showing (or fading in) now, or null - for tests.</summary>
        public Sprite ShownPortrait => _portraitWanted && portrait != null ? portrait.sprite : null;

        /// <summary>A line is still being typed out.</summary>
        public bool IsTyping => lineText != null && !_showingChoices && _typed < _total;

        /// <summary>The scene's lines so far, as the history shows them.</summary>
        public IReadOnlyList<string> History => _history;

        private void Awake()
        {
            if (portrait != null)
            {
                _portraitHome = portrait.rectTransform.anchoredPosition;
            }
        }

        private void OnEnable()
        {
            HidePortraitAtOnce();
            _history.Clear();
            _lineBeforeSaveError = null;
            _boxIn = 0f;
            _boxWanted = lineHeight;
            _boxHeight = 0f;
            historyPanel?.SetActive(false);
            if (runner == null)
            {
                return;
            }

            runner.OnLineShown += ShowLine;
            runner.OnChoicesShown += ShowChoices;
            runner.OnSaveFailed += ShowSaveFailure;
            continueButton?.onClick.AddListener(OnContinueClicked);
            historyButton?.onClick.AddListener(OpenHistory);
            closeHistoryButton?.onClick.AddListener(CloseHistory);

            for (int i = 0; i < choiceButtons.Length; i++)
            {
                int index = i; // capture for the closure
                choiceButtons[i].onClick.AddListener(() => OnChoiceClicked(index));
            }
        }

        private void OnDisable()
        {
            if (runner == null)
            {
                return;
            }

            runner.OnLineShown -= ShowLine;
            runner.OnChoicesShown -= ShowChoices;
            runner.OnSaveFailed -= ShowSaveFailure;
            continueButton?.onClick.RemoveListener(OnContinueClicked);
            historyButton?.onClick.RemoveListener(OpenHistory);
            closeHistoryButton?.onClick.RemoveListener(CloseHistory);

            foreach (Button button in choiceButtons)
            {
                button.onClick.RemoveAllListeners();
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (portrait != null)
            {
                _portraitAlpha = Mathf.MoveTowards(_portraitAlpha, _portraitWanted ? 1f : 0f, dt / PortraitFadeSeconds);
                ApplyPortraitFade();
            }

            // The box comes up from the bottom as the scene starts, and grows or shrinks to what it holds.
            _boxIn = Mathf.MoveTowards(_boxIn, 1f, dt / 0.25f);
            if (boxGroup != null && boxGroup.alpha != _boxIn)
            {
                boxGroup.alpha = _boxIn;
            }

            // Written only while it moves: a write to the box rebuilds the canvas, and a scene is mostly the player reading.
            if (dialogueBox != null)
            {
                _boxHeight = _boxHeight <= 0f || Mathf.Abs(_boxHeight - _boxWanted) < 0.5f
                    ? _boxWanted
                    : Mathf.Lerp(_boxHeight, _boxWanted, 1f - Mathf.Exp(-14f * dt));
                var size = new Vector2(dialogueBox.sizeDelta.x, _boxHeight);
                if (dialogueBox.sizeDelta != size)
                {
                    dialogueBox.sizeDelta = size;
                }

                var position = new Vector2(dialogueBox.anchoredPosition.x, 60f - 120f * (1f - EaseOut(_boxIn)));
                if (dialogueBox.anchoredPosition != position)
                {
                    dialogueBox.anchoredPosition = position;
                }
            }

            if (IsTyping)
            {
                _typed = Mathf.Min(_total, _typed + dt * CharactersPerSecond);
                int shown = Mathf.FloorToInt(_typed);
                lineText.maxVisibleCharacters = shown;
                if (shown - _lastBlip >= 3)
                {
                    _lastBlip = shown;
                    GameAudio.Play(GameSound.DialogueBlip, 0.18f, Blip(_speaker));
                }
            }
        }

        private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);

        /// <summary>Each voice blips at its own pitch, a little.</summary>
        private static float Blip(string speaker)
        {
            int hash = string.IsNullOrEmpty(speaker) ? 0 : speaker.GetHashCode() & 0xff;
            return 0.85f + hash / 255f * 0.35f;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (historyPanel != null && historyPanel.activeSelf)
            {
                return;
            }

            if (!_showingChoices)
            {
                OnContinueClicked();
            }
        }

        private void ShowPortrait(string speakerId)
        {
            if (portrait == null)
            {
                return;
            }

            if (_portraitById == null)
            {
                _portraitById = new Dictionary<string, Sprite>();
                foreach (SpeakerPortrait entry in portraits)
                {
                    if (!string.IsNullOrEmpty(entry.speakerId) && entry.sprite != null)
                    {
                        _portraitById[entry.speakerId] = entry.sprite;
                    }
                }
            }

            string id = SpeakerNames.PortraitId(speakerId);
            Sprite sprite = id != null && _portraitById.TryGetValue(id, out Sprite found) ? found : null;
            if (sprite == null)
            {
                _portraitWanted = false; // the narrator, or someone without a portrait: the last one fades away
                return;
            }

            if (sprite != portrait.sprite || !_portraitWanted)
            {
                portrait.sprite = sprite;
                _portraitAlpha = 0f; // a new face rises in rather than swapping under the old one
            }

            _portraitWanted = true;
            ApplyPortraitFade();
        }

        private void HidePortraitAtOnce()
        {
            _portraitWanted = false;
            _portraitAlpha = 0f;
            ApplyPortraitFade();
        }

        private void ApplyPortraitFade()
        {
            if (portrait == null)
            {
                return;
            }

            bool shows = _portraitAlpha > 0.001f;
            if (portrait.enabled != shows)
            {
                portrait.enabled = shows;
            }

            Color c = portrait.color;
            if (c.a != _portraitAlpha)
            {
                c.a = _portraitAlpha;
                portrait.color = c;
            }

            float eased = 1f - (1f - _portraitAlpha) * (1f - _portraitAlpha);
            // The bust stands on the box's top edge, wherever the box has grown to.
            float boxTop = dialogueBox != null ? dialogueBox.anchoredPosition.y + dialogueBox.sizeDelta.y : _portraitHome.y + 110f;
            float homeY = dialogueBox != null ? boxTop - 110f : _portraitHome.y;
            var position = new Vector2(_portraitHome.x, homeY - PortraitRise * (1f - eased));
            if (portrait.rectTransform.anchoredPosition != position)
            {
                portrait.rectTransform.anchoredPosition = position;
            }
        }

        private void ShowLine(string speakerId, string text)
        {
            _lineBeforeSaveError = null;
            _speaker = speakerId;
            _showingChoices = false;
            _boxWanted = lineHeight;
            ShowPortrait(speakerId);
            string speaker = SpeakerNames.DisplayName(speakerId);
            if (speakerText != null)
            {
                speakerText.text = speaker;
                (speakerTag != null ? speakerTag : speakerText.gameObject).SetActive(speaker.Length > 0); // the narrator has no name
            }

            if (lineText != null)
            {
                lineText.text = text;
                // Narration reads as narration, not as someone talking.
                lineText.fontStyle = speakerId == SpeakerNames.Narrator ? FontStyles.Italic : FontStyles.Normal;
                lineText.ForceMeshUpdate();
                _total = lineText.textInfo.characterCount;
                _typed = 0f;
                _lastBlip = 0;
                lineText.maxVisibleCharacters = 0;
            }

            AddToHistory(speaker, text);
            choicesPanel?.SetActive(false);
            continueButton?.gameObject.SetActive(true);
            historyButton?.gameObject.SetActive(_history.Count > 1);
        }

        private void AddToHistory(string speaker, string text)
        {
            _history.Add(string.IsNullOrEmpty(speaker) ? $"<i>{text}</i>" : $"<b>{speaker}:</b> {text}");
            if (_history.Count > HistoryLength)
            {
                _history.RemoveAt(0);
            }
        }

        private void ShowChoices(IReadOnlyList<string> choices)
        {
            if (_lineBeforeSaveError != null && lineText != null)
            {
                lineText.text = _lineBeforeSaveError;
                _lineBeforeSaveError = null;
            }
            _showingChoices = true;
            _choicesShownAt = Time.unscaledTime;
            _boxWanted = choiceHeight;
            if (lineText != null)
            {
                lineText.maxVisibleCharacters = 99999; // the line that led here stays readable over the choices
            }

            // The choice is Kuba's: his face, and his name over it - not the name of whoever spoke last, which put "Declan"
            // over Kuba's face. That line stays, now said in its own words by its speaker: "Declan: ...".
            string asked = SpeakerNames.DisplayName(_speaker) ?? string.Empty;
            bool kubaAsked = SpeakerNames.PortraitId(_speaker) == SpeakerNames.PortraitId("kuba_thought");
            const string askedColor = "<color=#AEB6BF>";
            if (lineText != null && asked.Length > 0 && !kubaAsked && !lineText.text.StartsWith(askedColor))
            {
                lineText.text = $"{askedColor}{asked}:</color> {lineText.text}";
            }

            ShowPortrait("kuba_thought");
            if (speakerText != null)
            {
                speakerText.text = SpeakerNames.DisplayName("kuba_thought");
                (speakerTag != null ? speakerTag : speakerText.gameObject).SetActive(true);
            }

            continueButton?.gameObject.SetActive(false);
            historyButton?.gameObject.SetActive(false); // the choices take the bottom of the box
            choicesPanel?.SetActive(true);

            for (int i = 0; i < choiceButtons.Length; i++)
            {
                bool active = i < choices.Count;
                choiceButtons[i].gameObject.SetActive(active);
                if (!active)
                {
                    continue;
                }

                // A choice Kuba cannot pay for without touching the rent stays in sight, greyed out.
                bool affordable = runner.IsChoiceAvailable(i);
                choiceButtons[i].interactable = affordable;
                if (i < choiceButtonTexts.Length && choiceButtonTexts[i] != null)
                {
                    choiceButtonTexts[i].text = affordable ? choices[i] : $"{choices[i]} <i>(nie stać cię - to pieniądze na rachunki)</i>";
                }
            }
        }

        private int _lastContinueFrame = -1;

        private void ShowSaveFailure()
        {
            _showingChoices = false;
            _typed = _total;
            if (lineText != null)
            {
                _lineBeforeSaveError ??= lineText.text;
                lineText.text = "Nie udało się zapisać rozmowy. Wybór czeka na zapis. Zwolnij miejsce lub przywróć dostęp do plików i naciśnij Dalej, aby spróbować ponownie. Zamknięcie gry cofnie rozmowę do ostatniego zapisu.";
                lineText.maxVisibleCharacters = 99999;
            }
            choicesPanel?.SetActive(false);
            continueButton?.gameObject.SetActive(true);
            historyButton?.gameObject.SetActive(false);
        }

        /// <summary>A tap: the first shows the whole line being typed, the next goes on (one per frame: a tap that reaches
        /// both the box and its button must not skip a line).</summary>
        public void OnContinueClicked()
        {
            if (Time.frameCount == _lastContinueFrame)
            {
                return;
            }

            _lastContinueFrame = Time.frameCount;
            if (IsTyping)
            {
                _typed = _total;
                lineText.maxVisibleCharacters = 99999;
                return;
            }

            GameAudio.Play(GameSound.PageTurn, 0.25f, UnityEngine.Random.Range(0.95f, 1.08f));
            runner.Advance();
        }

        private void OnChoiceClicked(int index)
        {
            if (Time.unscaledTime - _choicesShownAt < ChoiceTapGuardSeconds)
            {
                return;
            }

            if (index < choiceButtonTexts.Length && choiceButtonTexts[index] != null)
            {
                AddToHistory("Kuba", $"<color=#FFD866>» {choiceButtonTexts[index].text}</color>");
            }

            runner.SelectChoice(index);
        }

        private void OpenHistory()
        {
            if (historyPanel == null || historyText == null)
            {
                return;
            }

            var text = new StringBuilder();
            foreach (string line in _history)
            {
                text.AppendLine(line);
                text.AppendLine();
            }

            historyText.text = _history.Count > 0 ? text.ToString() : "Jeszcze nic nie padło.";
            historyPanel.SetActive(true);
        }

        private void CloseHistory()
        {
            historyPanel?.SetActive(false);
        }
    }
}
