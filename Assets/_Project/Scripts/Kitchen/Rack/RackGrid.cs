using System.Collections.Generic;
using UnityEngine;
using Zmywak.Core.Data;
using Zmywak.Kitchen.Dishes;

namespace Zmywak.Kitchen.Rack
{
    /// <summary>
    /// The dish rack in the scene: a grid of cells (see <see cref="RackLayout"/>) that clean dishes are
    /// dragged onto. Each dish covers as many cells as it needs and turns a quarter to fit a gap when that
    /// lands closer to the finger. When the dishwasher starts, the whole load goes in and the rack is free
    /// for the next one. The transform sits at the rack's centre.
    /// </summary>
    public class RackGrid : MonoBehaviour
    {
        /// <summary>Share of the cells a load needs to count as a full rack (extends Flow, pays a bonus).</summary>
        public const float FullLoadFill = 0.75f;

        [SerializeField] private int columns = 6;
        [SerializeField] private int rows = 4;
        [SerializeField] private float cellSize = 0.45f;
        [Tooltip("How far from the dish (in cells) a free spot may be and still catch it when let go.")]
        [SerializeField] private float snapDistanceCells = 1.3f;

        private readonly List<RackCell> _cells = new List<RackCell>();
        private readonly List<DishController> _dishes = new List<DishController>();
        private RackLayout _layout;
        private int _nextOwner = 1; // cells remember which dish covers them by this number

        public int Columns => columns;
        public int Rows => rows;
        public float CellSize => cellSize;
        public int CellCount => Layout.CellCount;
        public int OccupiedCells => Layout.OccupiedCells;
        public float FillRatio => Layout.FillRatio;
        public int DishCount => _dishes.Count;
        public IReadOnlyList<DishController> Dishes => _dishes;

        /// <summary>Cells a load has to cover to count as a full rack.</summary>
        public int FullLoadCells => Mathf.CeilToInt(CellCount * FullLoadFill - 1e-4f);

        public bool IsFullLoad => OccupiedCells >= FullLoadCells;

        private RackLayout Layout => _layout ??= new RackLayout(columns, rows);

        private void Awake()
        {
            GetComponentsInChildren(_cells);
        }

        public bool TryPlace(DishController dish, Vector3 worldPos)
        {
            ClearPreview();
            RackPlacement? placement = FindPlacement(dish.Definition, worldPos);
            if (!placement.HasValue)
            {
                return false;
            }

            Place(dish, placement.Value);
            return true;
        }

        /// <summary>
        /// The quick way onto the rack (a flick up from the sink): the dish flies from where it was let go into the spot it
        /// packs into best (<see cref="RackLayout.FindBest"/>). False when nothing fits - it goes back to the sink.
        /// </summary>
        public bool TryPlaceBest(DishController dish)
        {
            ClearPreview();
            RackPlacement? placement = Layout.FindBest(dish.Definition.RackFootprint);
            if (!placement.HasValue)
            {
                return false;
            }

            Vector3 from = dish.transform.position;
            Quaternion turn = dish.transform.rotation;
            Vector3 scale = dish.transform.localScale;
            Place(dish, placement.Value);
            dish.FlyFrom(from, turn, scale);
            return true;
        }

        /// <summary>Where the best spot for a kind of dish is now (null when it no longer fits) - for the tutorial's arrow.</summary>
        public Vector3? BestSpotFor(DishDefinition definition)
        {
            RackPlacement? placement = Layout.FindBest(definition.RackFootprint);
            return placement.HasValue ? BodyWorldPosition(definition, placement.Value) : (Vector3?)null;
        }

        private void Place(DishController dish, RackPlacement placement)
        {
            Layout.Place(_nextOwner++, placement);
            _dishes.Add(dish);
            // Resting on the wires, not sunk to its middle in the tray: that hid a plate's well and a pan's floor
            // under the tray, and racked dishes looked like empty rings.
            Vector3 resting = BodyWorldPosition(dish.Definition, placement) + Vector3.up * (dish.Definition.PlaceholderScale.y + WireTop);
            dish.transform.SetPositionAndRotation(resting, RotationFor(placement));
            dish.ConfirmRacked();
        }

        /// <summary>Height of the rack's wires above its centre (KitchenArt.BuildRackFrame), where a dish's bottom rests.</summary>
        private const float WireTop = 0.075f;

        /// <summary>Where a dish of this kind let go at <paramref name="worldPos"/> would land, if anywhere.</summary>
        public RackPlacement? FindPlacement(DishDefinition definition, Vector3 worldPos) =>
            Layout.FindNear(WorldToCells(worldPos), definition.RackFootprint, definition.RackBodyOffset, snapDistanceCells);

        /// <summary>Any free spot for a dish of this kind (front-left first), or null when it no longer fits.</summary>
        public RackPlacement? FindFreePlacement(DishDefinition definition) => Layout.FindFirstFree(definition.RackFootprint);

        public bool HasRoomFor(DishDefinition definition) => Layout.HasRoomFor(definition.RackFootprint);

        /// <summary>World position of the dish body when it lies at <paramref name="placement"/>.</summary>
        public Vector3 BodyWorldPosition(DishDefinition definition, RackPlacement placement) =>
            CellsToWorld(placement.Center + RackLayout.OrientedOffset(definition.RackBodyOffset, placement.Turned));

        public static Quaternion RotationFor(RackPlacement placement) =>
            placement.Turned ? Quaternion.Euler(0f, -90f, 0f) : Quaternion.identity;

        public Vector3 CellCenter(int column, int row) => CellsToWorld(new Vector2(column + 0.5f, row + 0.5f));

        /// <summary>While a dish is being dragged: light up exactly the cells letting go would put it on.</summary>
        public RackPlacement? PreviewDrop(DishController dish, Vector3 worldPos)
        {
            RackPlacement? target = FindPlacement(dish.Definition, worldPos);
            foreach (RackCell cell in _cells)
            {
                cell.SetHighlighted(target.HasValue && target.Value.Covers(cell.Column, cell.Row));
            }

            return target;
        }

        public void ClearPreview()
        {
            foreach (RackCell cell in _cells)
            {
                cell.SetHighlighted(false);
            }
        }

        /// <summary>Hands every racked dish over (to the dishwasher) and empties the rack for the next load.</summary>
        public List<DishController> TakeAll()
        {
            var load = new List<DishController>(_dishes);
            _dishes.Clear();
            Layout.Clear();
            ClearPreview();
            return load;
        }

        private Vector2 WorldToCells(Vector3 world)
        {
            Vector3 local = transform.InverseTransformPoint(world);
            return new Vector2(local.x / cellSize + columns * 0.5f, local.z / cellSize + rows * 0.5f);
        }

        private Vector3 CellsToWorld(Vector2 cells)
        {
            return transform.TransformPoint(new Vector3((cells.x - columns * 0.5f) * cellSize, 0f, (cells.y - rows * 0.5f) * cellSize));
        }
    }
}
