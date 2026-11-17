using UnityEngine;

namespace Zmywak.Core.Graphics
{
    /// <summary>
    /// Slides a material's textures slowly - ripples on the sink's water, foam drifting on it - with a gentle
    /// sway so it never looks like a conveyor belt. Works on the renderer's own copy of the material (the
    /// shared asset stays untouched) and costs one property write a frame.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class TextureScroller : MonoBehaviour
    {
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

        [SerializeField] private Vector2 speed = new Vector2(0.02f, 0.01f);
        [SerializeField] private float swayAmount = 0.01f;
        [SerializeField] private float swaySeconds = 7f;

        private Material _material;
        private Vector2 _offset;

        /// <summary>How fast the texture slides (UV per second) - the window's weather speeds the rain up or slows it.</summary>
        public Vector2 Speed
        {
            get => speed;
            set => speed = value;
        }

        private void Awake()
        {
            _material = GetComponent<Renderer>().material;
        }

        private void OnDestroy()
        {
            if (_material != null)
            {
                Destroy(_material);
            }
        }

        private void Update()
        {
            _offset += speed * Time.deltaTime;
            float phase = Time.time * Mathf.PI * 2f / Mathf.Max(0.1f, swaySeconds);
            var sway = new Vector2(Mathf.Sin(phase), Mathf.Cos(phase * 0.7f)) * swayAmount;
            _material.SetTextureOffset(BaseMapId, new Vector2(Mathf.Repeat(_offset.x, 1f), Mathf.Repeat(_offset.y, 1f)) + sway);
        }
    }
}
