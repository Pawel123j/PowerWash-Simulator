using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zmywak.Kitchen.Dishwasher;
using Zmywak.Kitchen.Input;
using Zmywak.UI.Common;

namespace Zmywak.UI.HUD
{
    /// <summary>
    /// The repair card for a broken dishwasher: a look inside the machine where the fault is fixed with a
    /// sink gesture - flick scraps off the filter, circle the seized spray arm loose, hold the reset button
    /// in. Touch is read straight from <see cref="Pointer.current"/>, like the dish input, and the kitchen
    /// behind stops taking touches while the card is up. The shift clock keeps running meanwhile.
    /// </summary>
    public class DishwasherRepairPanel : MonoBehaviour
    {
        private const float DoneLingerSeconds = 0.9f;
        private const float MinTurnRadius = 0.18f; // circling right over the hub is just jitter
        private const float ResetButtonRadius = 0.42f; // play-area units; matches the drawn button
        private const float ScrapFlySeconds = 0.35f;
        private const float ScrapFlySpeed = 4f; // play-area radii per second

        private static readonly Color[] ScrapColors =
        {
            new Color(0.45f, 0.27f, 0.12f), // gravy
            new Color(0.74f, 0.3f, 0.13f), // tomato
            new Color(0.93f, 0.82f, 0.45f), // pasta
            new Color(0.38f, 0.5f, 0.2f), // herbs
            new Color(0.6f, 0.45f, 0.3f) // bread
        };

        private static readonly Color ProgressStart = new Color(1f, 0.55f, 0.2f);
        private static readonly Color ProgressDone = new Color(0.35f, 0.85f, 0.45f);

        [SerializeField] private DishwasherController dishwasher;
        [SerializeField] private DishInputRouter inputRouter;
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text instructionText;
        [SerializeField] private Image progressFill;
        [SerializeField] private RectTransform playArea;
        [SerializeField] private Button closeButton;

        [Header("Clogged filter")]
        [SerializeField] private GameObject filterView;
        [SerializeField] private Image scrapTemplate;

        [Header("Stuck spray arm")]
        [SerializeField] private GameObject sprayArmView;
        [SerializeField] private RectTransform sprayArm;

        [Header("Tripped reset")]
        [SerializeField] private GameObject resetView;
        [SerializeField] private RectTransform resetButton;
        [SerializeField] private Image resetRing;

        private readonly List<Image> _scraps = new List<Image>();
        private readonly List<float> _scrapFlight = new List<float>(); // seconds flown, <0 = still on the filter
        private readonly List<Vector2> _scrapFlyDirection = new List<Vector2>();

        private DishwasherRepair _repair;
        private bool _wasPressed;
        private int _grabbedScrap = -1;
        private Vector2 _grabStart;
        private bool _hasAngle;
        private float _lastAngle;
        private float _doneRemaining;

        public bool IsOpen => root != null && root.activeSelf;

        /// <summary>The fault on the card, or null when closed.</summary>
        public DishwasherRepair Repair => IsOpen ? _repair : null;

        private void Awake()
        {
            closeButton?.onClick.AddListener(Close);
            if (scrapTemplate != null)
            {
                scrapTemplate.gameObject.SetActive(false);
            }

            if (root != null)
            {
                root.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            closeButton?.onClick.RemoveListener(Close);
        }

        public void Open()
        {
            if (dishwasher == null || !dishwasher.IsBroken || root == null)
            {
                return;
            }

            _repair = dishwasher.ActiveRepair;
            _doneRemaining = DoneLingerSeconds;
            _grabbedScrap = -1;
            _hasAngle = false;
            _wasPressed = Pointer.current != null && Pointer.current.press.isPressed; // no press "begins" mid-touch
            root.SetActive(true);
            inputRouter?.AddBlocker(this);

            filterView.SetActive(_repair.Fault == DishwasherFault.CloggedFilter);
            sprayArmView.SetActive(_repair.Fault == DishwasherFault.StuckSprayArm);
            resetView.SetActive(_repair.Fault == DishwasherFault.TrippedReset);
            if (_repair.Fault == DishwasherFault.CloggedFilter)
            {
                LayOutScraps((CloggedFilterRepair)_repair);
            }

            ShowFaultText();
            Refresh(0f);
        }

        public void Close()
        {
            if (root == null || !root.activeSelf)
            {
                return;
            }

            root.SetActive(false);
            _repair = null;
            inputRouter?.RemoveBlocker(this);
        }

        /// <summary>Screen position of a point inside the machine (unit disc, centre 0,0, rim at radius 1).</summary>
        public Vector2 PlayAreaToScreen(Vector2 point)
        {
            Vector3 world = playArea.TransformPoint(point * Radius);
            return RectTransformUtility.WorldToScreenPoint(EventCamera, world);
        }

        private float Radius => playArea.rect.width * 0.5f;

        private Camera EventCamera
        {
            get
            {
                Canvas canvas = playArea.GetComponentInParent<Canvas>();
                return canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            }
        }

        private void Update()
        {
            if (!IsOpen || _repair == null)
            {
                return;
            }

            float dt = Time.unscaledDeltaTime;
            if (_repair.IsDone)
            {
                _doneRemaining -= dt;
                Refresh(dt);
                if (_doneRemaining <= 0f)
                {
                    Close();
                }

                return;
            }

            Pointer pointer = Pointer.current;
            bool pressed = pointer != null && pointer.press.isPressed;
            Vector2 point = pointer != null ? ScreenToPlayArea(pointer.position.ReadValue()) : Vector2.zero;
            bool began = pressed && !_wasPressed;
            _wasPressed = pressed;

            switch (_repair)
            {
                case CloggedFilterRepair filter:
                    DriveFilter(filter, pressed, began, point);
                    break;
                case StuckSprayArmRepair arm:
                    DriveSprayArm(arm, pressed, point);
                    break;
                case TrippedResetRepair reset:
                    if (pressed && point.magnitude <= ResetButtonRadius)
                    {
                        reset.Hold(dt);
                    }
                    else
                    {
                        reset.Release(dt);
                    }

                    break;
            }

            if (_repair.IsDone)
            {
                titleText.text = "Naprawione!";
                instructionText.text = "Zmywarka rusza dalej.";
            }

            Refresh(dt);
        }

        private Vector2 ScreenToPlayArea(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(playArea, screen, EventCamera, out Vector2 local);
            return local / Radius;
        }

        private void DriveFilter(CloggedFilterRepair filter, bool pressed, bool began, Vector2 point)
        {
            if (began)
            {
                _grabbedScrap = filter.ScrapAt(point);
                _grabStart = point;
            }

            if (_grabbedScrap < 0)
            {
                return;
            }

            // Checked on the release frame too: a quick swipe may only cover the distance as the finger lifts.
            Vector2 drag = point - _grabStart;
            bool flicked = filter.Flick(_grabbedScrap, drag);
            if (!flicked && pressed)
            {
                // Held but not far enough yet: the scrap slides along under the finger.
                _scraps[_grabbedScrap].rectTransform.anchoredPosition = (filter.ScrapPosition(_grabbedScrap) + drag) * Radius;
                return;
            }

            if (flicked)
            {
                _scrapFlight[_grabbedScrap] = 0f;
                _scrapFlyDirection[_grabbedScrap] = drag.sqrMagnitude > 1e-6f ? drag.normalized : Vector2.up;
            }
            else
            {
                // Let go too early: it sticks back where it was.
                _scraps[_grabbedScrap].rectTransform.anchoredPosition = filter.ScrapPosition(_grabbedScrap) * Radius;
            }

            _grabbedScrap = -1;
        }

        private void DriveSprayArm(StuckSprayArmRepair arm, bool pressed, Vector2 point)
        {
            if (!pressed || point.magnitude < MinTurnRadius)
            {
                _hasAngle = false;
                return;
            }

            float angle = Mathf.Atan2(point.y, point.x) * Mathf.Rad2Deg;
            if (_hasAngle)
            {
                arm.Turn(Mathf.DeltaAngle(_lastAngle, angle));
            }

            _lastAngle = angle;
            _hasAngle = true;
        }

        private void Refresh(float dt)
        {
            float progress = _repair.Progress01;
            FillBar.Set(progressFill, progress);
            if (progressFill != null)
            {
                progressFill.color = Color.Lerp(ProgressStart, ProgressDone, progress);
            }

            switch (_repair)
            {
                case CloggedFilterRepair _:
                    AnimateFlyingScraps(dt);
                    break;
                case StuckSprayArmRepair arm:
                    if (arm.IsDone)
                    {
                        arm.Turn(-540f * dt); // runs free: a last spin of its own
                    }

                    sprayArm.localEulerAngles = new Vector3(0f, 0f, arm.ArmAngle);
                    break;
                case TrippedResetRepair reset:
                    resetRing.fillAmount = progress;
                    resetButton.localScale = Vector3.one * (reset.IsHeld || reset.IsDone ? 0.92f : 1f);
                    break;
            }
        }

        private void AnimateFlyingScraps(float dt)
        {
            for (int i = 0; i < _scraps.Count; i++)
            {
                if (_scrapFlight[i] < 0f || !_scraps[i].gameObject.activeSelf)
                {
                    continue;
                }

                _scrapFlight[i] += dt;
                float t = _scrapFlight[i] / ScrapFlySeconds;
                RectTransform rect = _scraps[i].rectTransform;
                rect.anchoredPosition += _scrapFlyDirection[i] * (ScrapFlySpeed * Radius * dt);
                rect.localScale = Vector3.one * Mathf.Lerp(1f, 0.6f, t);
                Color c = _scraps[i].color;
                c.a = 1f - t;
                _scraps[i].color = c;
                if (t >= 1f)
                {
                    _scraps[i].gameObject.SetActive(false);
                }
            }
        }

        private void LayOutScraps(CloggedFilterRepair filter)
        {
            while (_scraps.Count < filter.ScrapCount)
            {
                Image scrap = Instantiate(scrapTemplate, scrapTemplate.transform.parent);
                scrap.name = $"Scrap{_scraps.Count}";
                _scraps.Add(scrap);
                _scrapFlight.Add(-1f);
                _scrapFlyDirection.Add(Vector2.zero);
            }

            for (int i = 0; i < _scraps.Count; i++)
            {
                Image scrap = _scraps[i];
                // Reopening the card after closing it mid-repair: scraps already flicked off stay off.
                bool onFilter = i < filter.ScrapCount && !filter.IsCleared(i);
                scrap.gameObject.SetActive(onFilter);
                if (!onFilter)
                {
                    continue;
                }

                RectTransform rect = scrap.rectTransform;
                float diameter = filter.ScrapSize(i) * 2f * Radius;
                // Squashed and turned a little each, so they read as food rather than dots.
                rect.sizeDelta = new Vector2(diameter, diameter * (0.72f + 0.07f * (i % 4)));
                rect.localEulerAngles = new Vector3(0f, 0f, 37f * i);
                rect.anchoredPosition = filter.ScrapPosition(i) * Radius;
                rect.localScale = Vector3.one;
                scrap.color = ScrapColors[i % ScrapColors.Length];
                _scrapFlight[i] = -1f;
            }
        }

        private void ShowFaultText()
        {
            switch (_repair.Fault)
            {
                case DishwasherFault.CloggedFilter:
                    titleText.text = "Zapchany filtr";
                    instructionText.text = "Złap każdą resztkę na filtrze i odrzuć ją palcem.";
                    break;
                case DishwasherFault.StuckSprayArm:
                    titleText.text = "Zablokowane ramię spryskiwacza";
                    instructionText.text = "Kręć palcem wokół środka, aż ramię się rozrusza.";
                    break;
                case DishwasherFault.TrippedReset:
                    titleText.text = "Wywaliło bezpiecznik";
                    instructionText.text = "Przytrzymaj RESET, aż zaskoczy.";
                    break;
            }
        }
    }
}
