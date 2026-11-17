using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Zmywak.UI.Common
{
    /// <summary>
    /// Gives every control on its canvas a voice and a feel (<see cref="UiFeedback"/>): the ones built with the scene and
    /// the ones made later (the map's pins, a card's buttons), looked for again every second while the canvas is up.
    /// </summary>
    public class UiSounds : MonoBehaviour
    {
        private const float RescanSeconds = 1f;

        private readonly List<Selectable> _found = new List<Selectable>();
        private float _nextScan;

        private void OnEnable()
        {
            Scan();
        }

        private void Update()
        {
            if (Time.unscaledTime >= _nextScan)
            {
                Scan();
            }
        }

        /// <summary>Hooks up whatever control has none yet.</summary>
        public void Scan()
        {
            _nextScan = Time.unscaledTime + RescanSeconds;
            GetComponentsInChildren(true, _found);
            foreach (Selectable control in _found)
            {
                if (control != null && control.GetComponent<UiFeedback>() == null)
                {
                    control.gameObject.AddComponent<UiFeedback>();
                }
            }
        }
    }
}
