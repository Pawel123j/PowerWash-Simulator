using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zmywak.Core.GameState;

namespace Zmywak.UI.Common
{
    /// <summary>
    /// The screen between the big scenes (<see cref="SceneLoader"/>): it fades in over the old scene, shows one short line
    /// (<see cref="LoadingTips"/>) on a dark card with a thin progress bar, and fades out over the new one. Built in code
    /// and kept for the whole run - it belongs to no scene. While it covers the screen it takes every touch.
    /// </summary>
    public class LoadingScreen : MonoBehaviour, ILoadingView, IKeepAcrossRestart
    {
        private const float FadeInSeconds = 0.18f;
        private const float FadeOutSeconds = 0.3f;

        private CanvasGroup _group;
        private TextMeshProUGUI _heading;
        private TextMeshProUGUI _text;
        private RectTransform _barFill;
        private RectTransform _card;
        private float _alpha;
        private bool _wanted;
        private float _progress;
        private float _shownProgress;

        public bool IsCovering => _wanted && _alpha >= 0.999f;

        /// <summary>Makes the loading screen (once) and gives it to the scene loader.</summary>
        public static LoadingScreen Install()
        {
            if (SceneLoader.View is LoadingScreen existing && existing != null)
            {
                return existing;
            }

            var go = new GameObject("LoadingScreen", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster),
                typeof(CanvasGroup));
            DontDestroyOnLoad(go);
            LoadingScreen screen = go.AddComponent<LoadingScreen>();
            screen.Build();
            SceneLoader.View = screen;
            return screen;
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(SceneLoader.View, this))
            {
                SceneLoader.View = null;
            }
        }

        private void Build()
        {
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // over every scene's own canvas
            var scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            _group = GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;

            Image backdrop = NewImage("Backdrop", transform, new Color(0.055f, 0.06f, 0.07f, 1f), null);
            Stretch(backdrop.rectTransform);
            backdrop.raycastTarget = true;

            // A faint warm glow, like the lamp in Kuba's room.
            Image glow = NewImage("Glow", transform, new Color(1f, 0.72f, 0.38f, 0.07f), RuntimeSprites.Glow);
            glow.raycastTarget = false;
            Place(glow.rectTransform, new Vector2(0.5f, 0.62f), new Vector2(1400f, 1400f), Vector2.zero);

            TextMeshProUGUI title = NewText("Title", transform, "ZMYWAK", 64, FontStyles.Bold, new Color(1f, 1f, 1f, 0.85f));
            title.characterSpacing = 10f;
            Place(title.rectTransform, new Vector2(0.5f, 0.72f), new Vector2(900f, 100f), Vector2.zero);

            Image card = NewImage("Card", transform, new Color(1f, 1f, 1f, 0.06f), RuntimeSprites.Rounded);
            card.type = Image.Type.Sliced;
            _card = card.rectTransform;
            Place(_card, new Vector2(0.5f, 0.5f), new Vector2(900f, 300f), Vector2.zero);

            _heading = NewText("Heading", _card, string.Empty, 32, FontStyles.Bold, new Color(1f, 0.82f, 0.35f));
            Place(_heading.rectTransform, new Vector2(0.5f, 1f), new Vector2(820f, 60f), new Vector2(0f, -50f));
            _text = NewText("Tip", _card, string.Empty, 36, FontStyles.Normal, new Color(0.9f, 0.93f, 0.95f));
            _text.enableAutoSizing = true;
            _text.fontSizeMin = 24;
            _text.fontSizeMax = 36;
            Place(_text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(820f, 170f), new Vector2(0f, -20f));

            Image barBack = NewImage("Bar", transform, new Color(1f, 1f, 1f, 0.1f), RuntimeSprites.Rounded);
            barBack.type = Image.Type.Sliced;
            Place(barBack.rectTransform, new Vector2(0.5f, 0.33f), new Vector2(560f, 14f), Vector2.zero);
            Image fill = NewImage("Fill", barBack.rectTransform, new Color(0.3f, 0.75f, 0.5f), RuntimeSprites.Rounded);
            fill.type = Image.Type.Sliced;
            _barFill = fill.rectTransform;
            _barFill.anchorMin = new Vector2(0f, 0f);
            _barFill.anchorMax = new Vector2(0f, 1f);
            _barFill.pivot = new Vector2(0f, 0.5f);
            _barFill.sizeDelta = new Vector2(0f, 0f);
            gameObject.SetActive(true);
        }

        public void Show(string nextScene)
        {
            LoadingTips.Tip tip = LoadingTips.For(nextScene);
            _heading.text = tip.Heading;
            _text.text = tip.Text;
            _progress = 0f;
            _shownProgress = 0f;
            _wanted = true;
            _group.blocksRaycasts = true;
        }

        public void SetProgress(float progress01)
        {
            _progress = Mathf.Clamp01(progress01);
        }

        public void Hide()
        {
            _wanted = false;
        }

        private void Update()
        {
            if (!_wanted && _alpha <= 0f)
            {
                return; // hidden: nothing to draw, nothing to rebuild
            }

            float dt = Time.unscaledDeltaTime;
            _alpha = Mathf.MoveTowards(_alpha, _wanted ? 1f : 0f, dt / (_wanted ? FadeInSeconds : FadeOutSeconds));
            _group.alpha = _alpha * _alpha * (3f - 2f * _alpha);
            // Takes every touch from the moment it is asked for (a second tap on the way out must not land) until it has gone.
            _group.blocksRaycasts = _wanted || _alpha > 0.01f;
            _shownProgress = Mathf.MoveTowards(_shownProgress, _progress, dt * 2.5f);
            if (_barFill != null)
            {
                float width = ((RectTransform)_barFill.parent).rect.width;
                _barFill.sizeDelta = new Vector2(width * _shownProgress, 0f);
            }

            if (_card != null)
            {
                // The card drifts up a little as it appears.
                _card.anchoredPosition = new Vector2(0f, -24f * (1f - _alpha));
            }
        }

        private static Image NewImage(string name, Transform parent, Color color, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            return image;
        }

        private static TextMeshProUGUI NewText(string name, Transform parent, string text, float size, FontStyles style, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void Place(RectTransform rt, Vector2 anchor, Vector2 size, Vector2 position)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = position;
        }
    }
}
