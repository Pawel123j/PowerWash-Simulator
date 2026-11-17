using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zmywak.Core.Data;

namespace Zmywak.Kitchen.Dishes
{
    public enum WashStage
    {
        AwaitingScrape,
        AwaitingRinse,
        AwaitingScrub,
        ReadyForRack,
        Racked,
        Done
    }

    public class DirtLayerState
    {
        public DirtTypeDefinition Type;
        /// <summary>0 = fully clean, up to the layer's original coverage = untouched.</summary>
        public float Remaining;
        public float InitialCoverage;
    }

    /// <summary>Runtime wash state for one dish. Plain C# so it can be driven by both gameplay code and tests.</summary>
    public class DishInstance
    {
        private const float RequiredRinseSeconds = 1f;

        /// <summary>Brush radius in dish UV units (the dish surface is the UV disc of radius 0.5).</summary>
        public const float BrushRadius = 0.16f;
        /// <summary>Multiplier on tool power per texel under the brush centre. Tuning: seconds per dish.</summary>
        public const float BrushStrength = 5f;
        /// <summary>A layer this close to clean finishes itself - nobody enjoys hunting the last specks.</summary>
        public const float AutoFinishFraction = 0.05f;
        /// <summary>Seconds lying in the sink's water for the dirt to soften all it will.</summary>
        public const float FullSoakSeconds = 20f;
        /// <summary>What is left of every dirt's scrub difficulty after a full soak.</summary>
        public const float SoakedDifficulty = 0.35f;

        private readonly DirtCanvas _canvas;
        private readonly float[] _powers;
        private readonly float[] _difficulties;
        private readonly Dictionary<long, bool> _canTakeOff = new Dictionary<long, bool>(); // see CanStillTakeOff

        public DishDefinition Definition { get; }
        public WashStage Stage { get; private set; } = WashStage.AwaitingScrape;
        public IReadOnlyList<DirtLayerState> Layers { get; }
        public float RinseProgress { get; private set; }
        public float SoakSeconds { get; private set; }
        public DirtCanvas Canvas => _canvas;

        /// <summary>How far the soak has got, 0-1 (1 after <see cref="FullSoakSeconds"/>).</summary>
        public float Soak01 => Mathf.Clamp01(SoakSeconds / FullSoakSeconds);

        /// <summary>Multiplier on every dirt's scrub difficulty: 1 unsoaked, down to <see cref="SoakedDifficulty"/> soaked through.</summary>
        public float SoakSoftening => Mathf.Lerp(1f, SoakedDifficulty, Soak01);

        /// <summary>A dish whose dirt was made ahead (<see cref="DirtPrewarm"/>): the canvas must be for this kind of dish.</summary>
        public DishInstance(DishDefinition definition, DirtCanvas canvas) : this(definition, canvas, 0, true, canvas.Resolution)
        {
        }

        public DishInstance(DishDefinition definition, int seed = 0, bool circular = true, int resolution = DirtCanvas.DefaultResolution)
            : this(definition, null, seed, circular, resolution)
        {
        }

        private DishInstance(DishDefinition definition, DirtCanvas canvas, int seed, bool circular, int resolution)
        {
            Definition = definition;
            Layers = definition.DirtLayers
                .Select(layer => new DirtLayerState
                {
                    Type = layer.dirtType,
                    Remaining = Mathf.Clamp01(layer.coverage),
                    InitialCoverage = Mathf.Clamp01(layer.coverage)
                })
                .ToList();

            _canvas = canvas ?? new DirtCanvas(Layers.Select(l => l.InitialCoverage).ToList(), seed, circular, resolution,
                patterns: Layers.Select(l => l.Type != null ? l.Type.Pattern : DirtPattern.Splotches).ToList(),
                wallBand: circular ? DishMeshFactory.WallBand(definition.Shape) : null);
            _powers = new float[Layers.Count];
            _difficulties = new float[Layers.Count];
        }

        public float Cleanliness01 => DirtMaskMath.ComputeCleanliness01(Layers.Select(l => l.Remaining).ToList());

        /// <summary>A layer that only shows in the reflection (streaks on glass).</summary>
        public bool IsHidden(int layer) => Layers[layer].Type != null && Layers[layer].Type.ShowsOnlyInReflection;

        /// <summary>
        /// The visible dirt is off but streaks are left: the glass has to be held to the light and the streaks
        /// rubbed off where the reflection shows them.
        /// </summary>
        public bool NeedsInspection => Stage == WashStage.AwaitingScrub && !AnyDirtLeft(hidden: false) && AnyDirtLeft(hidden: true);

        /// <summary>
        /// Whether a tool works on the dirt being washed now - what still shows, or the streaks once only they are left. The
        /// shift scores scrubbing with it as careful work, scrubbing with anything else as the wrong tool (it still works, slowly).
        /// </summary>
        public bool ToolSuits(ToolDefinition tool)
        {
            if (tool == null)
            {
                return false;
            }

            bool streaksOnly = !AnyDirtLeft(hidden: false);
            for (int i = 0; i < Layers.Count; i++)
            {
                if (IsHidden(i) == streaksOnly && Layers[i].Remaining > 0f && Layers[i].Type != null && tool.IsEffectiveAgainst(Layers[i].Type) &&
                    CanStillTakeOff(i, tool))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Whether the tool can still thin the layer: a tool with a residue floor (the scraper, the spray) leaves a film it cannot
        /// go below, and once only that is left it is no longer the tool for the job. Asked every frame by the tool bar, so the
        /// answer is kept until a stroke moves some dirt.
        /// </summary>
        private bool CanStillTakeOff(int layer, ToolDefinition tool)
        {
            float floor = tool.ResidueFloor;
            if (floor <= 0f)
            {
                return true;
            }

            long key = layer * 100000L + Mathf.RoundToInt(floor * 10000f);
            if (!_canTakeOff.TryGetValue(key, out bool can))
            {
                can = _canvas.HasDirtAbove(layer, floor);
                _canTakeOff[key] = can;
            }

            return can;
        }

        private bool AnyDirtLeft(bool hidden)
        {
            for (int i = 0; i < Layers.Count; i++)
            {
                if (IsHidden(i) == hidden && Layers[i].Remaining > 0f)
                {
                    return true;
                }
            }

            return false;
        }

        public void Scrape()
        {
            if (Stage == WashStage.AwaitingScrape)
            {
                Stage = WashStage.AwaitingRinse;
            }
        }

        public void Rinse(float deltaTime)
        {
            if (Stage != WashStage.AwaitingRinse)
            {
                return;
            }

            RinseProgress += deltaTime;
            if (RinseProgress >= RequiredRinseSeconds)
            {
                Stage = WashStage.AwaitingScrub;
                if (Layers.All(l => l.Remaining <= 0f))
                {
                    Stage = WashStage.ReadyForRack; // nothing stuck on it - rinsing was enough
                }
            }
        }

        /// <summary>
        /// Lying in the sink's soapy water: it rinses the dish as the tap would, and the longer it soaks (up to
        /// <see cref="FullSoakSeconds"/>) the easier every dirt on it scrubs off. Only once the scraps are off it.
        /// </summary>
        public void Soak(float deltaTime)
        {
            if (Stage != WashStage.AwaitingRinse && Stage != WashStage.AwaitingScrub)
            {
                return;
            }

            Rinse(deltaTime);
            SoakSeconds = Mathf.Min(FullSoakSeconds, SoakSeconds + deltaTime);
        }

        /// <summary>
        /// Rubs the dish with <paramref name="tool"/> at one point of its surface (UV space). Only dirt under
        /// the brush comes off, at the rate the tool is effective against each layer. Returns true if any
        /// dirt was removed (the caller redraws the dish texture).
        /// </summary>
        public bool ScrubAt(ToolDefinition tool, Vector2 uv, float deltaTime)
        {
            return ScrubStroke(tool, uv, uv, deltaTime);
        }

        /// <summary>
        /// Rubs along the segment the finger moved this frame, stamping the brush at most half a radius
        /// apart and sharing the frame's time between the stamps. Without this a fast rub (more than a
        /// brush width per frame) would leave clean dots with dirty gaps between them.
        /// </summary>
        public bool ScrubStroke(ToolDefinition tool, Vector2 fromUv, Vector2 toUv, float deltaTime)
        {
            if (Stage != WashStage.AwaitingScrub || tool == null)
            {
                return false;
            }

            // Streaks form as the glass dries: while there is still visible dirt on it, there is nothing to
            // rub off yet - otherwise scrubbing the grease would wipe them away before anyone checked.
            bool streaksDormant = AnyDirtLeft(hidden: false);
            for (int i = 0; i < Layers.Count; i++)
            {
                DirtTypeDefinition type = Layers[i].Type;
                bool dormant = streaksDormant && IsHidden(i);
                _powers[i] = type != null && !dormant ? tool.PowerAgainst(type) * BrushStrength : 0f;
                _difficulties[i] = (type != null ? type.ScrubDifficulty : 1f) * SoakSoftening;
            }

            // A spray's jet is wider than a sponge; a scraper stops at a film (its residue floor) for another tool to finish.
            float radius = BrushRadius * tool.BrushRadiusScale;
            int stamps = Mathf.Clamp(Mathf.CeilToInt(Vector2.Distance(fromUv, toUv) / (radius * 0.5f)), 1, 32);
            bool changed = false;
            for (int s = 1; s <= stamps; s++)
            {
                Vector2 uv = Vector2.Lerp(fromUv, toUv, (float)s / stamps);
                changed |= _canvas.ScrubAt(uv, radius, _powers, _difficulties, deltaTime / stamps, tool.ResidueFloor);
            }

            if (!changed)
            {
                return false;
            }

            _canTakeOff.Clear(); // some dirt moved: what a tool with a residue floor can still do is asked afresh

            bool allClean = true;
            for (int i = 0; i < Layers.Count; i++)
            {
                float fraction = _canvas.RemainingFraction(i);
                if (fraction <= AutoFinishFraction && fraction > 0f)
                {
                    _canvas.ClearLayer(i);
                    fraction = 0f;
                }

                Layers[i].Remaining = Layers[i].InitialCoverage * fraction;
                if (Layers[i].Remaining > 0f)
                {
                    allClean = false;
                }
            }

            if (allClean)
            {
                Stage = WashStage.ReadyForRack;
            }

            return true;
        }

        public void MarkRacked()
        {
            if (Stage == WashStage.ReadyForRack)
            {
                Stage = WashStage.Racked;
            }
        }

        public void MarkDone()
        {
            if (Stage == WashStage.Racked)
            {
                Stage = WashStage.Done;
            }
        }
    }
}
