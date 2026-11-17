using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zmywak.Core.Data;
using Zmywak.Core.Haptics;
using Zmywak.Core.Settings;
using Zmywak.Kitchen.Feedback;
using Zmywak.Kitchen.Input;
using Zmywak.Kitchen.Prep;
using Zmywak.Kitchen.Shifts;
using Zmywak.UI.Common;

namespace Zmywak.UI.HUD
{
    /// <summary>
    /// The board before the shift (<see cref="PrepRound"/>), on a card over the kitchen when the shift opens with prep:
    /// a side view of the vegetable riding under the knife, a green mark three millimetres past the blade, the claw
    /// on top with a blue plaster when it got nicked, and the cut cubes heaping up to the left. Any tap on the card
    /// brings the knife down. The kitchen behind waits - no clock, no dishes - until "Do zlewu".
    /// </summary>
    public class PrepBoardPanel : MonoBehaviour
    {
        private const float PixelsPerMm = 8f;
        private const float KnifeX = -160f;           // play-area units, from its centre
        private const float VegetableHeight = 120f;
        private const float KnifeStroke = 0.12f;      // seconds, down and back up
        private const float KnifeDrop = 130f;          // from just above the vegetable down to the board
        private const float BoardTopY = -20f;
        private const float LabelSeconds = 0.6f;
        private const int MaxHeapedCubes = 70;

        private static readonly Color[] VegetableColors =
        {
            new Color(0.93f, 0.86f, 0.9f),  // onion
            new Color(0.95f, 0.52f, 0.12f), // carrot
            new Color(0.58f, 0.78f, 0.36f)  // celery
        };

        [SerializeField] private ShiftController shiftController;
        [SerializeField] private DishInputRouter inputRouter;
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text statsText;
        [SerializeField] private TMP_Text cutLabel;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private RectTransform playArea;
        [SerializeField] private RectTransform vegetable;
        [SerializeField] private Image vegetableImage;
        [SerializeField] private RectTransform knife;
        [SerializeField] private RectTransform claw;
        [SerializeField] private GameObject plaster;
        [SerializeField] private Image cubeTemplate;
        [SerializeField] private Image flash;
        [SerializeField] private Image timeFill;
        [SerializeField] private GameObject roundView;
        [SerializeField] private GameObject resultView;
        [SerializeField] private Button continueButton;

        private readonly List<Image> _cubes = new List<Image>();
        private PrepRound _round;
        private AudioSource _audio;
        private bool _opened;
        private bool _wasPressed;
        private float _knifeT = 1f;
        private float _labelT;
        private float _flashT;
        private Vector2 _knifeHome;

        public bool IsOpen => root != null && root.activeSelf;

        /// <summary>The prep under way (or done, waiting for "Do zlewu"); null when the card is closed.</summary>
        public PrepRound Round => IsOpen ? _round : null;

        private void Awake()
        {
            continueButton?.onClick.AddListener(Continue);
            if (cubeTemplate != null)
            {
                cubeTemplate.gameObject.SetActive(false);
            }

            if (knife != null)
            {
                _knifeHome = knife.anchoredPosition;
            }

            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            continueButton?.onClick.RemoveListener(Continue);
        }

        private void Open()
        {
            _opened = true;
            _round = new PrepRound(Fatigue.ScrubStrength(shiftController.EnergyAtStart));
            _round.Sliced += OnSliced;
            _wasPressed = Pointer.current != null && Pointer.current.press.isPressed;
            root.SetActive(true);
            roundView?.SetActive(true);
            resultView?.SetActive(false);
            inputRouter?.AddBlocker(this);
            Refresh();
        }

        /// <summary>Brings the knife down, as a tap does. False when it could not (a plaster on, the knife still
        /// coming up, the prep done).</summary>
        public bool TryCut()
        {
            return _round != null && !_round.IsFinished && _round.Cut().HasValue;
        }

        /// <summary>"Do zlewu": the prep is paid and the shift begins.</summary>
        public void Continue()
        {
            if (!IsOpen || _round == null || !_round.IsFinished)
            {
                return;
            }

            root.SetActive(false);
            inputRouter?.RemoveBlocker(this);
            shiftController.FinishPrep(_round.BonusEuros);
        }

        private void Update()
        {
            if (!_opened && shiftController != null && shiftController.AwaitingPrep && root != null)
            {
                Open();
            }

            if (!IsOpen || _round == null)
            {
                return;
            }

            float dt = Time.deltaTime; // the pause menu stops the board too
            bool wasFinished = _round.IsFinished;
            _round.Tick(dt);

            Pointer pointer = Pointer.current;
            bool pressed = pointer != null && pointer.press.isPressed;
            bool began = pressed && !_wasPressed;
            _wasPressed = pressed;
            if (began && !_round.IsFinished && dt > 0f)
            {
                TryCut();
            }

            _knifeT = Mathf.Min(1f, _knifeT + dt / KnifeStroke);
            _labelT = Mathf.Max(0f, _labelT - dt);
            _flashT = Mathf.Max(0f, _flashT - dt);
            if (!wasFinished && _round.IsFinished)
            {
                ShowResult();
            }

            Refresh();
        }

        private void OnSliced(PrepCut cut, float mm)
        {
            _knifeT = 0f;
            _labelT = LabelSeconds;
            switch (cut)
            {
                case PrepCut.Finger:
                    _flashT = 0.35f;
                    SetLabel("Au! Palce!", new Color(1f, 0.35f, 0.3f));
                    Play(ProceduralSounds.Sound.Chop, 1f, 0.6f);
                    Pulse(HapticPulse.Heavy);
                    return;
                case PrepCut.Even:
                    SetLabel("Równo", new Color(0.55f, 0.95f, 0.55f));
                    break;
                case PrepCut.Thick:
                    SetLabel("Za grubo", new Color(1f, 0.8f, 0.35f));
                    break;
                case PrepCut.Thin:
                    SetLabel("Za cienko", new Color(1f, 0.8f, 0.35f));
                    break;
                case PrepCut.Chunk:
                    SetLabel("Kawał!", new Color(1f, 0.55f, 0.3f));
                    break;
            }

            Play(ProceduralSounds.Sound.Chop, cut == PrepCut.Chunk ? 0.5f : 0.9f, cut == PrepCut.Even ? 1f : 0.9f);
            Pulse(HapticPulse.Light);
            AddCube(mm);
        }

        private void AddCube(float mm)
        {
            if (cubeTemplate == null)
            {
                return;
            }

            Image cube = Instantiate(cubeTemplate, cubeTemplate.transform.parent);
            cube.gameObject.SetActive(true);
            // A little paler or darker each - skin side, heart side - so the heap reads as cubes, not a blob.
            float shade = Random.Range(0.8f, 1.05f);
            Color colour = VegetableColors[Mathf.Min(_round.VegetableIndex, VegetableColors.Length - 1)];
            cube.color = new Color(Mathf.Min(1f, colour.r * shade), Mathf.Min(1f, colour.g * shade), Mathf.Min(1f, colour.b * shade), 1f);
            RectTransform rect = cube.rectTransform;
            rect.sizeDelta = new Vector2(Mathf.Max(8f, mm * PixelsPerMm), VegetableHeight * Random.Range(0.18f, 0.32f));
            // Swept left off the blade into a heap that grows up and out.
            float spread = Mathf.Min(150f, 70f + _cubes.Count * 2f);
            rect.anchoredPosition = new Vector2(KnifeX - 150f + Random.Range(-spread, spread * 0.4f),
                BoardTopY + VegetableHeight * 0.15f + Random.Range(0f, Mathf.Min(70f, _cubes.Count * 1.2f)));
            rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-35f, 35f));
            _cubes.Add(cube);
            if (_cubes.Count > MaxHeapedCubes)
            {
                Destroy(_cubes[0].gameObject);
                _cubes.RemoveAt(0);
            }
        }

        private void Refresh()
        {
            if (_round == null)
            {
                return;
            }

            if (vegetable != null && !_round.IsFinished)
            {
                // The vegetable's cut end has been pushed FedMm past the blade; the rest lies to the right under the claw.
                vegetable.gameObject.SetActive(true);
                vegetable.anchoredPosition = new Vector2(KnifeX - _round.FedMm * PixelsPerMm, vegetable.anchoredPosition.y);
                vegetable.sizeDelta = new Vector2(Mathf.Max(1f, _round.LeftMm * PixelsPerMm), VegetableHeight);
                vegetableImage.color = VegetableColors[Mathf.Min(_round.VegetableIndex, VegetableColors.Length - 1)];
            }
            else if (vegetable != null)
            {
                vegetable.gameObject.SetActive(false);
            }

            if (knife != null)
            {
                float stroke = Mathf.Sin(Mathf.Clamp01(_knifeT) * Mathf.PI); // down and back up
                knife.anchoredPosition = _knifeHome + Vector2.down * KnifeDrop * stroke;
            }

            plaster?.SetActive(_round.Fingers > 0);
            if (claw != null)
            {
                // The claw flinches back while the plaster goes on.
                claw.localRotation = Quaternion.Euler(0f, 0f, _round.PlasterRemaining > 0f ? -18f : 0f);
            }

            if (flash != null)
            {
                flash.enabled = _flashT > 0f;
                flash.color = new Color(1f, 0.15f, 0.1f, _flashT * 0.9f);
            }

            if (cutLabel != null)
            {
                cutLabel.alpha = Mathf.Clamp01(_labelT / LabelSeconds * 2f);
            }

            if (titleText != null && !_round.IsFinished)
            {
                string name = _round.VegetableName;
                titleText.text = $"{char.ToUpper(name[0])}{name.Substring(1)} {_round.VegetableIndex + 1}/{_round.VegetableCount}" +
                                 (_round.PlasterRemaining > 0f ? "  -  plaster..." : string.Empty);
            }

            if (statsText != null)
            {
                statsText.text = $"Równe kostki: {_round.Even}   Nierówne: {_round.Thick + _round.Thin + _round.Chunks}   " +
                                 $"Plastry na palcach: {_round.Fingers}";
            }

            FillBar.Set(timeFill, 1f - _round.Elapsed / PrepRound.DurationSeconds);
        }

        private void ShowResult()
        {
            roundView?.SetActive(false);
            resultView?.SetActive(true);
            if (titleText != null)
            {
                titleText.text = "Deska gotowa";
            }

            if (resultText != null)
            {
                resultText.text = $"Równe kostki: {_round.Even} z {_round.Slices} ({_round.Evenness:P0})\n" +
                                  $"Zrobione: {_round.Completion:P0}\n" +
                                  $"Plastry na palcach: {_round.Fingers}\n" +
                                  $"Premia za deskę: +{PlaceMoney.Format(_round.BonusEuros * PlaceMoney.SmallSums(shiftController.Place), shiftController.Place)}\n" +
                                  $"<size=80%><i>{_round.Verdict}</i></size>";
            }
        }

        private void SetLabel(string text, Color color)
        {
            if (cutLabel != null)
            {
                cutLabel.text = text;
                cutLabel.color = color;
            }
        }

        private void Play(ProceduralSounds.Sound sound, float volume, float pitch)
        {
            float gain = AccessibilitySettings.SfxGain;
            if (_audio == null || gain <= 0f)
            {
                return;
            }

            _audio.pitch = pitch * Random.Range(0.94f, 1.06f);
            _audio.PlayOneShot(KitchenSounds.Clip(sound), volume * gain);
        }

        private static void Pulse(HapticPulse strength) => HapticsService.Pulse(strength); // it checks the setting itself
    }
}
