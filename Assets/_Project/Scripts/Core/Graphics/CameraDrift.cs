using UnityEngine;

namespace Zmywak.Core.Graphics
{
    /// <summary>
    /// A slow, breathing sway for a camera looking at a still scene (the main menu's view of Kuba's room): a
    /// degree or two of drift and a centimetre of rise and fall, on long, unrelated periods so it never
    /// visibly repeats - the difference between a photograph and a place.
    /// </summary>
    public class CameraDrift : MonoBehaviour
    {
        [SerializeField] private float yawDegrees = 1.6f;
        [SerializeField] private float pitchDegrees = 0.8f;
        [SerializeField] private float bobMetres = 0.012f;

        private Vector3 _position;
        private Quaternion _rotation;

        private void Awake()
        {
            _position = transform.localPosition;
            _rotation = transform.localRotation;
        }

        private void LateUpdate()
        {
            float t = Time.unscaledTime;
            float yaw = Mathf.Sin(t * 0.13f) * yawDegrees + Mathf.Sin(t * 0.037f + 1.3f) * yawDegrees * 0.5f;
            float pitch = Mathf.Sin(t * 0.09f + 0.7f) * pitchDegrees;
            transform.localRotation = _rotation * Quaternion.Euler(pitch, yaw, 0f);
            transform.localPosition = _position + Vector3.up * (Mathf.Sin(t * 0.21f) * bobMetres);
        }
    }
}
