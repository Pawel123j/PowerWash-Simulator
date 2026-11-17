using UnityEngine;
using UnityEngine.UI;
using Zmywak.Core.Data;
using Zmywak.Kitchen.Tools;
using Zmywak.UI.Common;

namespace Zmywak.UI.HUD
{
    /// <summary>
    /// One button in the tool bar; selects its tool on click and shows whether it is the one in hand (accent colour,
    /// slightly larger). When the dirt in the sink wants this tool and another is in hand, it breathes gently - and nudges
    /// harder while the wrong one is being scrubbed with - so the right tool is found without reading. Not on the bar until
    /// its day (the first days add the scraper and the spray later); held back while the guided first plate teaches another.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ToolButton : MonoBehaviour, IOwnScale
    {
        [SerializeField] private ToolDefinition tool;
        [SerializeField] private Image background;
        [SerializeField] private Color normalColor = new Color(0.2f, 0.23f, 0.27f);
        [SerializeField] private Color selectedColor = new Color(0.16f, 0.52f, 0.37f);
        [SerializeField] private float selectedScale = 1.06f;

        private static readonly Color HintColor = new Color(0.95f, 0.72f, 0.2f);

        private Button _button;
        private float _hint;  // 0-1, eases in and out
        private float _press;
        private float _nudgeTime = -10f;

        public ToolDefinition Tool => tool;

        /// <summary>This button is the one the dirt wants (it is breathing).</summary>
        public bool IsSuggested { get; private set; }

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClicked);
        }

        private void OnClicked()
        {
            if (ActiveToolSelector.Instance != null)
            {
                ActiveToolSelector.Instance.SelectTool(tool);
            }
        }

        /// <summary>A quick shake: "this one!" (the HUD calls it when the wrong tool has been rubbing for a while).</summary>
        public void Nudge()
        {
            _nudgeTime = Time.unscaledTime;
        }

        private void Update()
        {
            ActiveToolSelector selector = ActiveToolSelector.Instance;
            bool selected = selector != null && selector.CurrentTool == tool;
            IsSuggested = selector != null && selector.Suggested == tool;
            if (_button != null)
            {
                _button.interactable = selector == null || selector.CanPick(tool);
            }

            float dt = Time.unscaledDeltaTime;
            _hint = Mathf.MoveTowards(_hint, IsSuggested ? 1f : 0f, dt * 4f);
            _press = Mathf.MoveTowards(_press, 0f, dt * 6f);
            float breathe = _hint * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f));
            // Written only when they change: a button at rest touches nothing (each write rebuilds the HUD's canvas).
            if (background != null)
            {
                Color baseColor = selected ? selectedColor : normalColor;
                Color color = Color.Lerp(baseColor, HintColor, breathe * 0.75f);
                if (background.color != color)
                {
                    background.color = color;
                }
            }

            float scale = (selected ? selectedScale : 1f) + breathe * 0.07f;
            float since = Time.unscaledTime - _nudgeTime;
            float shake = since < 0.4f ? Mathf.Sin(since * 50f) * (1f - since / 0.4f) * 9f : 0f;
            Vector3 scaled = Vector3.one * scale;
            if (transform.localScale != scaled)
            {
                transform.localScale = scaled;
            }

            Quaternion turned = Quaternion.Euler(0f, 0f, shake);
            if (transform.localRotation != turned)
            {
                transform.localRotation = turned;
            }
        }
    }
}
