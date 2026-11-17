using UnityEngine;
using Zmywak.Core.Audio;

namespace Zmywak.UI.Common
{
    /// <summary>
    /// A card coming up over the screen: the dim behind it fades in while the card itself grows from a little smaller and
    /// slides up into place, with a soft whoosh - quick (a fifth of a second), so it reads as the card arriving and never
    /// holds the player up. On every panel the setup makes (settings, the slang list, the map, the menus).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class PanelTransition : MonoBehaviour
    {
        private const float Seconds = 0.2f;

        [Tooltip("The card that grows in (the first child called Card when not set).")]
        [SerializeField] private RectTransform card;
        [SerializeField] private bool playSound = true;

        private CanvasGroup _group;
        private float _t = 1f;
        private Vector2 _cardHome;
        private bool _homeKnown;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            if (card == null)
            {
                Transform found = transform.Find("Card");
                card = found as RectTransform;
            }
        }

        private void OnEnable()
        {
            if (card != null && !_homeKnown)
            {
                _cardHome = card.anchoredPosition;
                _homeKnown = true;
            }

            _t = 0f;
            Apply();
            if (playSound)
            {
                GameAudio.Play(GameSound.UiOpen, 0.35f);
            }
        }

        private void OnDisable()
        {
            _t = 1f;
            Apply();
        }

        private void Update()
        {
            if (_t >= 1f)
            {
                return;
            }

            _t = Mathf.Min(1f, _t + Time.unscaledDeltaTime / Seconds);
            Apply();
        }

        private void Apply()
        {
            float eased = 1f - (1f - _t) * (1f - _t) * (1f - _t);
            if (_group != null)
            {
                _group.alpha = Mathf.Clamp01(_t * 1.6f);
            }

            if (card != null)
            {
                card.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, eased);
                if (_homeKnown)
                {
                    card.anchoredPosition = _cardHome + new Vector2(0f, -30f * (1f - eased));
                }
            }
        }
    }
}
