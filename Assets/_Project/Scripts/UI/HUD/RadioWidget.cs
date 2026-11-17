using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zmywak.Kitchen.Radio;

namespace Zmywak.UI.HUD
{
    /// <summary>
    /// The kitchen radio on screen: a little portable on the wall with its speaker grille and a lit dial window -
    /// the station on top, the programme or the day's headline crawling along underneath. A tap turns the dial
    /// (<see cref="KitchenRadio.NextStation"/>). Off, the window goes dark.
    /// </summary>
    public class RadioWidget : MonoBehaviour
    {
        private const float CrawlSpeed = 70f; // canvas units a second
        private const float CrawlGap = 120f;

        [SerializeField] private KitchenRadio radio;
        [SerializeField] private Button button;
        [SerializeField] private Image dial;
        [SerializeField] private TMP_Text stationText;
        [SerializeField] private RectTransform captionViewport;
        [SerializeField] private TMP_Text captionText;
        [SerializeField] private Image onLight;

        private static readonly Color DialLit = new Color(0.98f, 0.82f, 0.46f);
        private static readonly Color DialDark = new Color(0.3f, 0.27f, 0.22f);

        private float _crawl;

        private void OnEnable()
        {
            button?.onClick.AddListener(OnTapped);
            if (radio != null)
            {
                radio.Changed += Refresh;
            }

            Refresh();
        }

        private void OnDisable()
        {
            button?.onClick.RemoveListener(OnTapped);
            if (radio != null)
            {
                radio.Changed -= Refresh;
            }
        }

        private void OnTapped()
        {
            radio?.NextStation();
        }

        private void Refresh()
        {
            if (radio == null)
            {
                return;
            }

            bool on = radio.Station != RadioStation.Off;
            if (dial != null)
            {
                dial.color = on ? DialLit : DialDark;
            }

            if (onLight != null)
            {
                onLight.color = on ? new Color(1f, 0.35f, 0.2f) : new Color(0.25f, 0.1f, 0.08f);
            }

            if (stationText != null)
            {
                stationText.text = RadioStations.Label(radio.Station, radio.Place);
            }

            if (captionText != null)
            {
                captionText.text = radio.Caption;
                _crawl = 0f;
            }
        }

        private void Update()
        {
            if (captionText == null || captionViewport == null)
            {
                return;
            }

            // A caption wider than the window crawls along it, like the display on a car radio; a short one sits still.
            float width = captionText.preferredWidth;
            float window = captionViewport.rect.width;
            RectTransform rect = captionText.rectTransform;
            if (width <= window)
            {
                rect.anchoredPosition = new Vector2(0f, rect.anchoredPosition.y);
                return;
            }

            // In from a third of the way along, out past the left edge, a gap, and round again.
            float lead = window * 0.3f;
            _crawl = (_crawl + Time.unscaledDeltaTime * CrawlSpeed) % (lead + width + CrawlGap);
            rect.anchoredPosition = new Vector2(lead - _crawl, rect.anchoredPosition.y);
        }
    }
}
