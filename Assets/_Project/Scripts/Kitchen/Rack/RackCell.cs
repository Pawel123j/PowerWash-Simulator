using UnityEngine;

namespace Zmywak.Kitchen.Rack
{
    /// <summary>One cell of the dish rack: its grid position and the marker that lights up under a dragged dish.</summary>
    public class RackCell : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly Color HighlightColor = new Color(0.35f, 0.9f, 0.55f);

        [SerializeField] private int column;
        [SerializeField] private int row;

        private Renderer _marker;
        private MaterialPropertyBlock _block;
        private bool _highlighted;

        public int Column => column;
        public int Row => row;

        private void Awake()
        {
            _marker = GetComponentInChildren<Renderer>();
        }

        /// <summary>Tints the cell's marker while a dragged dish would cover it.</summary>
        public void SetHighlighted(bool highlighted)
        {
            if (_highlighted == highlighted || _marker == null)
            {
                return;
            }

            _highlighted = highlighted;
            _block ??= new MaterialPropertyBlock();
            if (highlighted)
            {
                _block.SetColor(BaseColorId, HighlightColor);
                _marker.SetPropertyBlock(_block);
            }
            else
            {
                _marker.SetPropertyBlock(null); // back to the shared cell material
            }
        }
    }
}
