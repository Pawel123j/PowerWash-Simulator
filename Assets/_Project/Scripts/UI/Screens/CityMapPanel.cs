using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zmywak.Core.Data;
using Zmywak.Core.Services;
using Zmywak.Meta.Progress;

namespace Zmywak.UI.Screens
{
    /// <summary>
    /// The hub's map: a card with a painted map of wherever Kuba lives (Dublin, Kraków, the camp in the Pilbara) and
    /// pins on the places of his life (CityMap). A pin for something the hub does - work, the pub, a call home, money
    /// home - does it, as the hub's own button would (<see cref="PinTapped"/>); a landmark only shows its name.
    /// </summary>
    public class CityMapPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text title;
        [SerializeField] private Image map;
        [SerializeField] private RectTransform pins;
        [SerializeField] private Sprite dublin;
        [SerializeField] private Sprite krakow;
        [SerializeField] private Sprite pilbara;
        [SerializeField] private Sprite pinSprite;

        // A pin's touch target: 48 dp on a dense 5-inch phone, as every control in the game.
        private const float TouchSize = 130f;

        private readonly List<GameObject> _pins = new List<GameObject>();

        /// <summary>A pin with an action was tapped (the map has closed).</summary>
        public event Action<MapAction> PinTapped;

        public bool IsOpen => panel != null && panel.activeSelf;

        /// <summary>The pins on show, with their buttons (none until the map is opened).</summary>
        public IReadOnlyList<GameObject> PinObjects => _pins;

        private void OnEnable()
        {
            openButton?.onClick.AddListener(Open);
            closeButton?.onClick.AddListener(Close);
        }

        private void OnDisable()
        {
            openButton?.onClick.RemoveListener(Open);
            closeButton?.onClick.RemoveListener(Close);
        }

        public void Open()
        {
            Place place = ServiceLocator.TryGet(out MetaProgressController progress) ? progress.Place : Place.Dublin;
            if (title != null)
            {
                title.text = CityMap.Title(place);
            }

            if (map != null)
            {
                map.sprite = place == Place.Krakow ? krakow : place == Place.Pilbara ? pilbara : dublin;
            }

            foreach (GameObject old in _pins)
            {
                Destroy(old);
            }

            _pins.Clear();
            foreach (MapPin pin in CityMap.Pins(place))
            {
                _pins.Add(CreatePin(pin));
            }

            panel?.SetActive(true);
        }

        public void Close()
        {
            panel?.SetActive(false);
        }

        /// <summary>A tap on a pin, as from the screen (tests use it).</summary>
        public void Tap(MapAction action)
        {
            if (action == MapAction.None)
            {
                return;
            }

            Close();
            PinTapped?.Invoke(action);
        }

        private GameObject CreatePin(MapPin pin)
        {
            // A touch target the size of a fingertip (the game's 48 dp), the visible dot small in the middle of it.
            var go = new GameObject($"Pin_{pin.Label}", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(pins, false);
            rect.anchorMin = rect.anchorMax = new Vector2(pin.U, pin.V);
            rect.sizeDelta = new Vector2(TouchSize, TouchSize);
            rect.anchoredPosition = Vector2.zero;
            var hitArea = go.GetComponent<Image>();
            hitArea.color = new Color(1f, 1f, 1f, 0f);
            hitArea.raycastTarget = pin.Action != MapAction.None;

            var dotGo = new GameObject("Dot", typeof(RectTransform), typeof(Image));
            var dotRect = (RectTransform)dotGo.transform;
            dotRect.SetParent(rect, false);
            dotRect.sizeDelta = new Vector2(44f, 44f);
            var dot = dotGo.GetComponent<Image>();
            dot.sprite = pinSprite;
            dot.color = Colour(pin.Action);
            dot.raycastTarget = false;

            // The name beside it, on a dark tab so it reads over any part of the map.
            var tab = new GameObject("Label", typeof(RectTransform), typeof(Image));
            var tabRect = (RectTransform)tab.transform;
            tabRect.SetParent(rect, false);
            tabRect.anchorMin = tabRect.anchorMax = new Vector2(0.5f, 0.5f);
            tabRect.pivot = new Vector2(0.5f, 1f);
            tabRect.anchoredPosition = new Vector2(0f, -26f); // just under the dot
            tab.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.1f, 0.85f);
            tab.GetComponent<Image>().raycastTarget = false;
            var label = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            label.rectTransform.SetParent(tabRect, false);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(8f, 2f);
            label.rectTransform.offsetMax = new Vector2(-8f, -2f);
            label.text = pin.Label;
            label.fontSize = 22f;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.color = pin.Action == MapAction.None ? new Color(0.8f, 0.8f, 0.78f) : Color.white;
            label.raycastTarget = false;
            tabRect.sizeDelta = new Vector2(label.GetPreferredValues(pin.Label).x + 20f, 34f);

            if (pin.Action != MapAction.None)
            {
                // The whole pin and its name are the tap target.
                var hit = go.AddComponent<Button>();
                hit.targetGraphic = dot;
                MapAction action = pin.Action;
                hit.onClick.AddListener(() => Tap(action));
                tab.GetComponent<Image>().raycastTarget = true;
            }

            return go;
        }

        private static Color Colour(MapAction action)
        {
            switch (action)
            {
                case MapAction.Work: return new Color(0.16f, 0.62f, 0.32f);
                case MapAction.Home: return new Color(0.95f, 0.75f, 0.3f);
                case MapAction.Pub: return new Color(0.8f, 0.45f, 0.15f);
                case MapAction.CallHome: return new Color(0.3f, 0.55f, 0.9f);
                case MapAction.SendMoney: return new Color(0.75f, 0.35f, 0.75f);
                default: return new Color(0.45f, 0.45f, 0.48f);
            }
        }
    }
}
