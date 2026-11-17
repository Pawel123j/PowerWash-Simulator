using UnityEngine;

namespace Zmywak.Kitchen.Dishes
{
    /// <summary>
    /// The sink: one dish at a time is lifted out of the pile and held here, enlarged, while it is scraped,
    /// rinsed and scrubbed - so it can't be buried under other dishes mid-wash and is big enough on a phone
    /// screen to rub with a finger. The spot is freed once the dish is racked (or destroyed).
    /// One more dish can lie soaking on the bottom, in the water under the held spot: it frees the hands for the
    /// next dish, and comes back up only once they are free again - so nothing ever covers it when it is wanted.
    /// </summary>
    public class WashStation : MonoBehaviour
    {
        [SerializeField] private float heldHeight = 0.8f;
        [SerializeField] private float heldScaleMultiplier = 2.2f;
        [Tooltip("How far below the station the basin's floor is (where a soaking dish lies).")]
        [SerializeField] private float basinFloorDepth = 0.4f;
        [Tooltip("How far below the station the water's surface is.")]
        [SerializeField] private float waterDepth = 0.14f;

        public DishController Occupant { get; private set; }
        public Vector3 HeldPosition => transform.position + Vector3.up * heldHeight;
        public float HeldScaleMultiplier => heldScaleMultiplier;

        /// <summary>The dish lying in the water to soak, if one is.</summary>
        public DishController Soaking { get; private set; }

        /// <summary>The middle of the basin's floor.</summary>
        public Vector3 BasinFloor => transform.position + Vector3.down * basinFloorDepth;

        public float WaterLevelY => transform.position.y - waterDepth;

        public bool TryOccupy(DishController dish)
        {
            if (Occupant != null && Occupant != dish)
            {
                return false;
            }

            Occupant = dish;
            return true;
        }

        public void Release(DishController dish)
        {
            if (Occupant == dish)
            {
                Occupant = null;
            }
        }

        /// <summary>The held dish goes down into the water: its hands-spot is free from now on.</summary>
        public bool TrySoak(DishController dish)
        {
            if (Soaking != null && Soaking != dish)
            {
                return false;
            }

            Soaking = dish;
            Release(dish);
            return true;
        }

        public void ReleaseSoak(DishController dish)
        {
            if (Soaking == dish)
            {
                Soaking = null;
            }
        }
    }
}
