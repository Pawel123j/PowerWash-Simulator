using UnityEngine;

namespace Zmywak.Core.Graphics
{
    /// <summary>
    /// Now and then a car goes by in the street below: its headlights come in through the window and slide across
    /// the ceiling, then fade. Moves this light from <see cref="from"/> to <see cref="to"/> (local positions) while
    /// keeping it aimed at <see cref="aimAt"/> shifted along with it, brightening and dimming over the pass, with a
    /// random wait between cars. Pure decoration: switched off between passes, so it costs nothing then.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class LightSweep : MonoBehaviour
    {
        [SerializeField] private Vector3 from = new Vector3(-1.5f, 1f, 2.6f);
        [SerializeField] private Vector3 to = new Vector3(2f, 1f, 2.6f);
        [Tooltip("Where the light points when it is at 'from'; the target slides with it.")]
        [SerializeField] private Vector3 aimAt = new Vector3(-1f, 2.6f, 0f);
        [SerializeField] private float passSeconds = 3.2f;
        [SerializeField] private Vector2 waitSeconds = new Vector2(7f, 16f);
        [SerializeField] private float peakIntensity = 1.6f;

        private Light _light;
        private float _wait;
        private float _progress = -1f; // -1 while waiting for the next car

        /// <summary>How far the current car has got, 0-1, or -1 between cars.</summary>
        public float Progress => _progress;

        private void Awake()
        {
            _light = GetComponent<Light>();
            _light.enabled = false;
            _wait = Random.Range(2f, 5f); // the first one soon after the scene opens
        }

        private void Update()
        {
            if (_progress < 0f)
            {
                _wait -= Time.deltaTime;
                if (_wait > 0f)
                {
                    return;
                }

                _progress = 0f;
                _light.enabled = true;
            }

            _progress += Time.deltaTime / Mathf.Max(0.1f, passSeconds);
            if (_progress >= 1f)
            {
                _progress = -1f;
                _wait = Random.Range(waitSeconds.x, waitSeconds.y);
                _light.enabled = false;
                return;
            }

            // Eased across, brightest in the middle of the pass.
            float t = _progress * _progress * (3f - 2f * _progress);
            Vector3 offset = (to - from) * t;
            transform.localPosition = from + offset;
            transform.LookAt(transform.parent != null ? transform.parent.TransformPoint(aimAt + offset) : aimAt + offset);
            _light.intensity = peakIntensity * Mathf.Sin(_progress * Mathf.PI);
        }
    }
}
