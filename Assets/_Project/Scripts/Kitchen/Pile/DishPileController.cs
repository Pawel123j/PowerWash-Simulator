using System;
using System.Collections.Generic;
using UnityEngine;
using Zmywak.Core.Data;
using Zmywak.Core.Settings;
using Zmywak.Kitchen.Dishes;
using Zmywak.Kitchen.Feedback;

namespace Zmywak.Kitchen.Pile
{
    /// <summary>
    /// Watches every unclaimed (still physics-driven) dish. When the pile grows too tall it gives way - physically:
    /// the dishes at its top are shoved outward and tumble off, and whatever then hits something hard enough breaks
    /// (glass and china, see <see cref="DishPhysics"/>; steel only rings). Breakages, here or off the counter, are
    /// docked from the wage.
    /// Height = highest point of any dish at rest in the pile; dishes still falling in are ignored
    /// (new ones spawn above the pile and would otherwise trigger a collapse on arrival).
    /// </summary>
    public class DishPileController : MonoBehaviour
    {
        [SerializeField] private float collapseHeight = 1.4f;
        [SerializeField] private float collapseCooldownSeconds = 3f;

        [Tooltip("How far below the top a dish may sit and still slide off when the pile gives way.")]
        [SerializeField] private float collapseBand = 0.45f;

        [Tooltip("How hard the top of the pile is shoved outward when it gives way (scene units a second).")]
        [SerializeField] private float collapsePush = 2.4f;

        private const float FloorY = -1.5f; // well below the counter top (y = 0)
        private const float CollapseLift = 1.3f; // a heaving pile throws its top up a little as well as out
        private const float CollapseSpin = 5f;

        private readonly List<DishController> _pileDishes = new List<DishController>();
        private float _cooldownRemaining;

        /// <summary>Dishes broken - by a hard knock in the pile, or off the counter onto the floor.</summary>
        public event Action<int> OnDishesBroken;

        /// <summary>The pile gave way: how many dishes slid off its top.</summary>
        public event Action<int> OnCollapsed;

        public float CollapseHeight => collapseHeight;

        /// <summary>How close the pile is to giving way, 0-1.</summary>
        public float Fullness => collapseHeight > 0f ? Mathf.Clamp01(CurrentHeight / collapseHeight) : 0f;

        /// <summary>
        /// A friend from the crew takes dishes off the top of the pile (to his own sink): up to <paramref name="count"/> of the
        /// highest settled ones, lifted away towards <paramref name="towards"/>. Returns the dishes taken.
        /// </summary>
        public List<DishDefinition> TakeFromTop(int count, Vector3 towards)
        {
            var taken = new List<DishDefinition>();
            _pileDishes.RemoveAll(dish => dish == null || dish.IsClaimed);
            var settled = _pileDishes.FindAll(dish => dish.IsSettledInPile);
            settled.Sort((a, b) => b.TopY.CompareTo(a.TopY));
            for (int i = 0; i < settled.Count && taken.Count < count; i++)
            {
                DishController dish = settled[i];
                dish.Shattered -= OnShattered;
                _pileDishes.Remove(dish);
                taken.Add(dish.Definition);
                dish.SendInto(towards);
            }

            return taken;
        }

        /// <summary>Top of the pile: highest point of any dish at rest in it (0 when empty).</summary>
        public float CurrentHeight
        {
            get
            {
                float height = 0f;
                foreach (DishController dish in _pileDishes)
                {
                    if (dish != null && dish.IsSettledInPile)
                    {
                        height = Mathf.Max(height, dish.TopY);
                    }
                }

                return height;
            }
        }

        public void Register(DishController dish)
        {
            _pileDishes.Add(dish);
            dish.Shattered += OnShattered;
        }

        private void OnShattered(DishController dish)
        {
            dish.Shattered -= OnShattered;
            _pileDishes.Remove(dish);
            OnDishesBroken?.Invoke(1);
        }

        private void Update()
        {
            _pileDishes.RemoveAll(dish => dish == null || dish.IsClaimed);
            HandleDishesOffTheCounter();

            if (AccessibilitySettings.RelaxedMode)
            {
                return;
            }

            if (_cooldownRemaining > 0f)
            {
                _cooldownRemaining -= Time.deltaTime;
                return;
            }

            if (_pileDishes.Count == 0)
            {
                return;
            }

            if (CurrentHeight >= collapseHeight)
            {
                Collapse();
            }
        }

        /// <summary>
        /// A dish that slides or rolls off the edge of the counter used to fall forever and silently vanish.
        /// Glass and china smash on the floor (a breakage); a steel pan or pot clangs and goes back on the pile to be
        /// washed again - as does anything in relaxed mode (this object sits on the spawn spot).
        /// </summary>
        private void HandleDishesOffTheCounter()
        {
            int smashed = 0;
            for (int i = _pileDishes.Count - 1; i >= 0; i--)
            {
                DishController dish = _pileDishes[i];
                if (dish.transform.position.y > FloorY)
                {
                    continue;
                }

                // Shards burst - or the clang rings - where it went over the edge, not deep below the counter.
                Vector3 edge = dish.transform.position;
                edge.y = 0f;
                if (AccessibilitySettings.RelaxedMode || !DishPhysics.CanBreak(dish.Definition.Material))
                {
                    if (!AccessibilitySettings.RelaxedMode)
                    {
                        KitchenFeedbackEvents.RaiseImpact(edge, 6f, dish.Definition.Material);
                    }

                    dish.ReturnToPile(transform.position + Vector3.up * 1.5f);
                    continue;
                }

                _pileDishes.RemoveAt(i);
                dish.Shattered -= OnShattered;
                KitchenFeedbackEvents.RaiseBroken(edge);
                Destroy(dish.gameObject);
                smashed++;
            }

            if (smashed > 0)
            {
                OnDishesBroken?.Invoke(smashed);
            }
        }

        /// <summary>
        /// The pile gives way at the top: every settled dish within <see cref="collapseBand"/> of the top is shoved
        /// outward from the pile's middle (and a little up, with a tumble), and physics decides the rest - what lands
        /// hard on the counter breaks, what lands on softer heap or is made of steel survives.
        /// </summary>
        private void Collapse()
        {
            float top = CurrentHeight;
            Vector3 middle = transform.position;
            int shoved = 0;
            foreach (DishController dish in _pileDishes)
            {
                if (dish == null || !dish.IsSettledInPile || dish.TopY < top - collapseBand)
                {
                    continue;
                }

                Vector3 away = dish.transform.position - middle;
                away.y = 0f;
                if (away.sqrMagnitude < 0.0025f)
                {
                    Vector2 any = UnityEngine.Random.insideUnitCircle.normalized; // dead centre: falls whichever way
                    away = new Vector3(any.x, 0f, any.y);
                }

                away.Normalize();
                Vector3 spin = new Vector3(-away.z, 0f, away.x) * CollapseSpin + UnityEngine.Random.insideUnitSphere * 1.5f;
                dish.Knock(away * collapsePush * UnityEngine.Random.Range(0.85f, 1.15f) + Vector3.up * CollapseLift, spin);
                shoved++;
            }

            _cooldownRemaining = collapseCooldownSeconds;
            OnCollapsed?.Invoke(shoved);
        }
    }
}
