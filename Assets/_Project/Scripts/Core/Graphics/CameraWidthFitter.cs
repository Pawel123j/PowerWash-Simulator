using UnityEngine;

namespace Zmywak.Core.Graphics
{
    /// <summary>
    /// Keeps a minimum world-space width visible regardless of screen aspect. A camera's size (orthographic) or
    /// field of view (perspective) only fixes the vertical extent, so on a tall portrait phone (9:19.5 and
    /// narrower) a fixed one crops the sides of the play area; this grows it just enough to keep
    /// <see cref="minHalfWidth"/> on screen, and never shrinks it below <see cref="minOrthographicSize"/>. For a
    /// perspective camera both are measured at <see cref="focusDistance"/>, where the counter is.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraWidthFitter : MonoBehaviour
    {
        [SerializeField] private float minHalfWidth = 2.8f;
        [SerializeField] private float minOrthographicSize = 5f;
        [Tooltip("Perspective only: how far in front of the camera the extents above are measured.")]
        [SerializeField] private float focusDistance = 10f;

        private Camera _camera;

        private void Awake()
        {
            Fit();
        }

        private void LateUpdate()
        {
            Fit();
        }

        /// <summary>Resizes the view for the camera's current aspect now, rather than at the end of the frame
        /// (for a render into a texture of another shape).</summary>
        public void Fit()
        {
            if (_camera == null)
            {
                _camera = GetComponent<Camera>();
            }

            if (_camera == null || _camera.aspect <= 0f)
            {
                return;
            }

            float halfHeight = Mathf.Max(minOrthographicSize, minHalfWidth / _camera.aspect);
            if (_camera.orthographic)
            {
                _camera.orthographicSize = halfHeight;
            }
            else
            {
                _camera.fieldOfView = 2f * Mathf.Atan(halfHeight / focusDistance) * Mathf.Rad2Deg;
            }
        }
    }
}
