using System;
using UnityEngine;

namespace Zmywak.Kitchen.Rack
{
    /// <summary>A spot on the rack: the block of cells a dish covers, and whether it lies turned a quarter.</summary>
    public readonly struct RackPlacement : IEquatable<RackPlacement>
    {
        public readonly int Column;
        public readonly int Row;
        public readonly int Width;
        public readonly int Depth;
        public readonly bool Turned;

        public RackPlacement(int column, int row, int width, int depth, bool turned)
        {
            Column = column;
            Row = row;
            Width = width;
            Depth = depth;
            Turned = turned;
        }

        /// <summary>Centre of the block, in cells from the rack's front-left corner.</summary>
        public Vector2 Center => new Vector2(Column + Width * 0.5f, Row + Depth * 0.5f);

        public int Cells => Width * Depth;

        public bool Covers(int column, int row) =>
            column >= Column && column < Column + Width && row >= Row && row < Row + Depth;

        public bool Equals(RackPlacement other) =>
            Column == other.Column && Row == other.Row && Width == other.Width && Depth == other.Depth && Turned == other.Turned;

        public override bool Equals(object obj) => obj is RackPlacement other && Equals(other);

        public override int GetHashCode() => ((Column * 31 + Row) * 31 + Width) * 31 + Depth * 2 + (Turned ? 1 : 0);

        public override string ToString() => $"({Column},{Row}) {Width}x{Depth}{(Turned ? " turned" : string.Empty)}";
    }

    /// <summary>
    /// The dish rack as a grid of cells. A dish covers a block of cells matching its size (a glass one cell,
    /// a plate two by two, a pan with its handle four by two) and may lie turned a quarter to fit a gap, so
    /// packing it tight gets more into one dishwasher cycle. Plain logic with no scene objects, so the
    /// packing rules are testable. Columns run left to right (+x), rows front to back (+z).
    /// </summary>
    public sealed class RackLayout
    {
        private readonly int[] _owners; // 0 = free

        public RackLayout(int columns, int rows)
        {
            if (columns <= 0 || rows <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(columns), "A rack needs at least one cell.");
            }

            Columns = columns;
            Rows = rows;
            _owners = new int[columns * rows];
        }

        public int Columns { get; }
        public int Rows { get; }
        public int CellCount => _owners.Length;
        public int OccupiedCells { get; private set; }
        public float FillRatio => OccupiedCells / (float)CellCount;

        /// <summary>Footprint as it lies on the rack: width and depth swap when the dish is turned.</summary>
        public static Vector2Int Oriented(Vector2Int footprint, bool turned) =>
            turned ? new Vector2Int(footprint.y, footprint.x) : footprint;

        /// <summary>
        /// The body offset once turned. The dish turns by -90 degrees about the vertical, which takes its
        /// local +x (the pan's handle) to +z: towards the back of the rack.
        /// </summary>
        public static Vector2 OrientedOffset(Vector2 bodyOffset, bool turned) =>
            turned ? new Vector2(-bodyOffset.y, bodyOffset.x) : bodyOffset;

        /// <summary>Who holds this cell (0 = nobody).</summary>
        public int OwnerAt(int column, int row) => _owners[row * Columns + column];

        public bool CanPlace(RackPlacement placement)
        {
            if (placement.Column < 0 || placement.Row < 0 || placement.Width <= 0 || placement.Depth <= 0 ||
                placement.Column + placement.Width > Columns || placement.Row + placement.Depth > Rows)
            {
                return false;
            }

            for (int row = placement.Row; row < placement.Row + placement.Depth; row++)
            {
                for (int column = placement.Column; column < placement.Column + placement.Width; column++)
                {
                    if (OwnerAt(column, row) != 0)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public void Place(int owner, RackPlacement placement)
        {
            if (owner == 0)
            {
                throw new ArgumentException("Owner 0 means an empty cell.", nameof(owner));
            }

            if (!CanPlace(placement))
            {
                throw new InvalidOperationException($"No room at {placement}.");
            }

            for (int row = placement.Row; row < placement.Row + placement.Depth; row++)
            {
                for (int column = placement.Column; column < placement.Column + placement.Width; column++)
                {
                    _owners[row * Columns + column] = owner;
                }
            }

            OccupiedCells += placement.Cells;
        }

        public bool Remove(int owner)
        {
            bool removed = false;
            for (int i = 0; i < _owners.Length; i++)
            {
                if (_owners[i] == owner && owner != 0)
                {
                    _owners[i] = 0;
                    OccupiedCells--;
                    removed = true;
                }
            }

            return removed;
        }

        public void Clear()
        {
            Array.Clear(_owners, 0, _owners.Length);
            OccupiedCells = 0;
        }

        /// <summary>
        /// Where a dish let go with its body at <paramref name="bodyPoint"/> (in cells) would land: the free
        /// placement, either way round, whose body position is closest - but no further than
        /// <paramref name="maxDistance"/> cells. Null if nothing fits that close.
        /// </summary>
        public RackPlacement? FindNear(Vector2 bodyPoint, Vector2Int footprint, Vector2 bodyOffset, float maxDistance)
        {
            RackPlacement? best = null;
            float bestDistance = maxDistance;
            foreach (bool turned in Orientations(footprint))
            {
                Vector2Int size = Oriented(footprint, turned);
                Vector2 offset = OrientedOffset(bodyOffset, turned);
                for (int row = 0; row + size.y <= Rows; row++)
                {
                    for (int column = 0; column + size.x <= Columns; column++)
                    {
                        var candidate = new RackPlacement(column, row, size.x, size.y, turned);
                        float distance = Vector2.Distance(candidate.Center + offset, bodyPoint);
                        if (distance < bestDistance - 1e-4f && CanPlace(candidate))
                        {
                            bestDistance = distance;
                            best = candidate;
                        }
                    }
                }
            }

            return best;
        }

        /// <summary>The first free placement scanning front-left to back-right, unturned before turned.</summary>
        public RackPlacement? FindFirstFree(Vector2Int footprint)
        {
            foreach (bool turned in Orientations(footprint))
            {
                Vector2Int size = Oriented(footprint, turned);
                for (int row = 0; row + size.y <= Rows; row++)
                {
                    for (int column = 0; column + size.x <= Columns; column++)
                    {
                        var candidate = new RackPlacement(column, row, size.x, size.y, turned);
                        if (CanPlace(candidate))
                        {
                            return candidate;
                        }
                    }
                }
            }

            return null;
        }

        public bool HasRoomFor(Vector2Int footprint) => FindFirstFree(footprint).HasValue;

        /// <summary>
        /// Where a dish packs in best, for the quick flick onto the rack: the free placement that touches the most - other
        /// dishes and the rack's own edges, all round it - so the load fills from the corners in and leaves the open space
        /// in one piece for the big things (front-left first among equals). Null when it no longer fits.
        /// </summary>
        public RackPlacement? FindBest(Vector2Int footprint)
        {
            RackPlacement? best = null;
            int bestContact = -1;
            foreach (bool turned in Orientations(footprint))
            {
                Vector2Int size = Oriented(footprint, turned);
                for (int row = 0; row + size.y <= Rows; row++)
                {
                    for (int column = 0; column + size.x <= Columns; column++)
                    {
                        var candidate = new RackPlacement(column, row, size.x, size.y, turned);
                        if (!CanPlace(candidate))
                        {
                            continue;
                        }

                        int contact = Contact(candidate);
                        if (contact > bestContact)
                        {
                            bestContact = contact;
                            best = candidate;
                        }
                    }
                }
            }

            return best;
        }

        /// <summary>How many cell edges round a placement touch a dish or the rack's border.</summary>
        private int Contact(RackPlacement p)
        {
            int contact = 0;
            for (int column = p.Column; column < p.Column + p.Width; column++)
            {
                contact += Blocked(column, p.Row - 1) ? 1 : 0;
                contact += Blocked(column, p.Row + p.Depth) ? 1 : 0;
            }

            for (int row = p.Row; row < p.Row + p.Depth; row++)
            {
                contact += Blocked(p.Column - 1, row) ? 1 : 0;
                contact += Blocked(p.Column + p.Width, row) ? 1 : 0;
            }

            return contact;
        }

        private bool Blocked(int column, int row) =>
            column < 0 || row < 0 || column >= Columns || row >= Rows || OwnerAt(column, row) != 0;

        private static bool[] Orientations(Vector2Int footprint) =>
            footprint.x == footprint.y ? new[] { false } : new[] { false, true };
    }
}
