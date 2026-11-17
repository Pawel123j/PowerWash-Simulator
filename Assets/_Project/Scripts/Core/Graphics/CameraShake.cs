using UnityEngine;
using Zmywak.Core.Settings;

namespace Zmywak.Core.Graphics
{
    /// <summary>
    /// A short, small shake of the camera - a plate smashing, the machine banging to a stop - that dies away in a fraction
    /// of a second. Subtle on purpose (a few millimetres at the counter), and off with the screen-effects setting.
    /// It only ever adds an offset to wherever the camera is and takes it back the next frame.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public class CameraShake : MonoBehaviour
    {
        private Vector3 _applied;
        private float _strength;
        private float _remaining;
        private float _duration = 0.3f;
        private float _seed;

        /// <summary>The camera's shaker, added on first use.</summary>
        public static CameraShake On(Camera camera)
        {
            if (camera == null)
            {
                return null;
            }

            return camera.TryGetComponent(out CameraShake shake) ? shake : camera.gameObject.AddComponent<CameraShake>();
        }

        /// <summary>Shakes by up to <paramref name="strength"/> scene units for <paramref name="seconds"/> (the stronger of
        /// two shakes wins).</summary>
        public void Shake(float strength, float seconds)
        {
            if (!AccessibilitySettings.ScreenEffects)
            {
                return;
            }

            if (strength >= _strength * (_remaining / Mathf.Max(0.01f, _duration)))
            {
                _strength = strength;
                _duration = Mathf.Max(0.05f, seconds);
                _remaining = _duration;
                _seed = Random.value * 100f;
            }
        }

        public bool IsShaking => _remaining > 0f;

        private void LateUpdate()
        {
            transform.localPosition -= _applied;
            _applied = Vector3.zero;
            if (_remaining <= 0f)
            {
                return;
            }

            _remaining -= Time.unscaledDeltaTime;
            float fade = Mathf.Clamp01(_remaining / _duration);
            float t = Time.unscaledTime * 38f + _seed;
            _applied = new Vector3(Mathf.PerlinNoise(t, 0f) - 0.5f, Mathf.PerlinNoise(0f, t) - 0.5f, 0f) * (2f * _strength * fade * fade);
            _applied = transform.localRotation * _applied; // across the view, not along it
            transform.localPosition += _applied;
        }

        private void OnDisable()
        {
            transform.localPosition -= _applied;
            _applied = Vector3.zero;
            _remaining = 0f;
        }
    }
}
