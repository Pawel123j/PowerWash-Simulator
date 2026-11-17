using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zmywak.Core.Audio;

namespace Zmywak.UI.Common
{
    /// <summary>The kind of sound a control makes when touched.</summary>
    public enum UiRole
    {
        Click,
        Confirm,
        Back,
        Toggle,
        Silent
    }

    /// <summary>A control that scales itself (ToolButton): the press does not touch its scale.</summary>
    public interface IOwnScale
    {
    }

    /// <summary>
    /// How a control answers a finger: it sinks a little while pressed and springs back with a small overshoot, and it
    /// makes its sound - a click, a confirming chime for the main way forward, a softer one for going back, a toggle's
    /// snap - or, touched while it cannot be used, a soft "no" (the screen usually says why). Added to every control by
    /// <see cref="UiSounds"/>; the role is guessed from the control's name unless set.
    /// </summary>
    public class UiFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerClickHandler
    {
        private const float PressedScale = 0.95f;
        private const float PressSeconds = 0.06f;
        private const float ReleaseSeconds = 0.22f;

        [SerializeField] private UiRole role = UiRole.Click;
        [SerializeField] private bool roleSet;

        private Selectable _selectable;
        private Vector3 _restScale = Vector3.one;
        private float _press; // 0 at rest, 1 pressed
        private bool _pressed;
        private float _releaseTime = -1f;
        private bool _ownScale;

        public UiRole Role
        {
            get => role;
            set
            {
                role = value;
                roleSet = true;
            }
        }

        private void Awake()
        {
            _selectable = GetComponent<Selectable>();
            _ownScale = GetComponent<IOwnScale>() != null;
            if (!roleSet)
            {
                role = Guess(gameObject.name, _selectable);
            }
        }

        /// <summary>What a control's name says it does.</summary>
        public static UiRole Guess(string name, Selectable selectable)
        {
            if (selectable is Toggle)
            {
                return UiRole.Toggle;
            }

            if (selectable is Slider || selectable is Scrollbar)
            {
                return UiRole.Silent; // a slider ticks as it moves (SettingsScreen), not on the touch
            }

            string n = name ?? string.Empty;
            if (n.Contains("Close") || n.Contains("Back") || n.Contains("Resume") || n.Contains("Cancel") || n.Contains("No"))
            {
                return UiRole.Back;
            }

            if (n.Contains("GoToWork") || n.Contains("NewGame") || n.Contains("MenuContinue") || n.Contains("Confirm") || n.Contains("Yes") ||
                n.StartsWith("Slot") || n.Contains("StartDishwasher"))
            {
                return UiRole.Confirm;
            }

            return UiRole.Click;
        }

        private bool Usable => _selectable == null || _selectable.IsInteractable();

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!Usable)
            {
                return;
            }

            if (!_pressed && _press <= 0f)
            {
                _restScale = transform.localScale;
            }

            _pressed = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Release();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Release();
        }

        private void Release()
        {
            if (_pressed)
            {
                _pressed = false;
                _releaseTime = Time.unscaledTime;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!Usable)
            {
                GameAudio.Play(GameSound.UiError, 0.5f);
                return;
            }

            switch (role)
            {
                case UiRole.Confirm:
                    GameAudio.Play(GameSound.UiConfirm, 0.7f);
                    break;
                case UiRole.Back:
                    GameAudio.Play(GameSound.UiBack, 0.6f);
                    break;
                case UiRole.Toggle:
                    GameAudio.Play(GameSound.UiToggle, 0.7f);
                    break;
                case UiRole.Click:
                    GameAudio.Play(GameSound.UiClick, 0.6f, Random.Range(0.97f, 1.03f));
                    break;
            }
        }

        private void Update()
        {
            if (_ownScale)
            {
                return;
            }

            if (_pressed)
            {
                _press = Mathf.MoveTowards(_press, 1f, Time.unscaledDeltaTime / PressSeconds);
                transform.localScale = _restScale * Mathf.Lerp(1f, PressedScale, _press);
            }
            else if (_releaseTime >= 0f)
            {
                // Back up with a little overshoot, then settle.
                float t = Mathf.Clamp01((Time.unscaledTime - _releaseTime) / ReleaseSeconds);
                float from = Mathf.Lerp(1f, PressedScale, _press);
                float spring = 1f + Mathf.Sin(t * Mathf.PI) * 0.03f * (1f - t);
                transform.localScale = _restScale * Mathf.Lerp(from, 1f, 1f - (1f - t) * (1f - t)) * spring;
                if (t >= 1f)
                {
                    transform.localScale = _restScale;
                    _releaseTime = -1f;
                    _press = 0f;
                }
            }
        }

        private void OnDisable()
        {
            if (_pressed || _releaseTime >= 0f)
            {
                transform.localScale = _restScale;
            }

            _pressed = false;
            _releaseTime = -1f;
            _press = 0f;
        }
    }
}
