using UnityEngine;
using Zmywak.Core.Services;
using Zmywak.Meta.Progress;

namespace Zmywak.UI.Screens
{
    /// <summary>
    /// Kuba's room behind the menu and the hub, as it is tonight: his own room, or - evicted after weeks in arrears
    /// (LifeLedger.InHostel) - a hostel dormitory, a metal bunk with someone's rucksack on top, lockers, a second bunk
    /// along the wall. Built by RoomArt; this only shows the one he sleeps in, and follows a change during the evening.
    /// </summary>
    public class RoomDressing : MonoBehaviour
    {
        [Tooltip("His own room's things that a dormitory has no place for: the pine bed, the photo from home.")]
        [SerializeField] private GameObject[] home = new GameObject[0];
        [Tooltip("The dormitory: bunks and lockers (not static: lit by the light probes, not the lightmaps).")]
        [SerializeField] private GameObject hostel;

        private MetaProgressController _progress;

        /// <summary>Whether the dormitory is what is on show.</summary>
        public bool ShowsHostel => hostel != null && hostel.activeSelf;

        private void Start()
        {
            if (ServiceLocator.TryGet(out _progress))
            {
                _progress.OnProgressChanged += Refresh;
            }

            Refresh();
        }

        private void OnDestroy()
        {
            if (_progress != null)
            {
                _progress.OnProgressChanged -= Refresh;
            }
        }

        public void Refresh()
        {
            bool inHostel = _progress != null && _progress.Ledger.InHostel;
            foreach (GameObject item in home)
            {
                if (item != null)
                {
                    item.SetActive(!inHostel);
                }
            }

            if (hostel != null)
            {
                hostel.SetActive(inHostel);
            }
        }
    }
}
