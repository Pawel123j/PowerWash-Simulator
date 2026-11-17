using System;
using UnityEngine;

namespace Zmywak.Kitchen.Prep
{
    /// <summary>What one press of the knife did.</summary>
    public enum PrepCut
    {
        Even,   // within a millimetre of three
        Thick,  // late: more than four millimetres
        Thin,   // early: under two
        Chunk,  // nobody cut, and the vegetable ran on past the blade
        Finger  // far too early: the knife came down before the claw had moved back
    }

    /// <summary>
    /// The board before a shift: brunoise, three millimetres, as Declan showed and Gavin times. The guiding hand pushes
    /// the vegetable under the blade at a steady pace and every cut takes whatever has passed it. On the beat - a cut
    /// every half second - the slices come out even; late, thick; early, thin; and far too early the knife comes down
    /// on the claw: a nicked finger, a blue plaster and a pause. With nobody cutting, the vegetable runs on past the
    /// blade in a chunk. A tired hand pushes unevenly, so the beat wanders. Pure and testable: PrepBoardPanel draws it
    /// and takes the taps.
    /// </summary>
    public sealed class PrepRound
    {
        public const float TargetSliceMm = 3f;
        public const float ToleranceMm = 1f;
        public const float FingerBelowMm = 1.2f;
        public const float ChunkAtMm = 9f;
        public const float FeedMmPerSecond = 6f;
        public const float KnifeLiftSeconds = 0.1f; // a second tap while the knife is still coming up does nothing
        public const float PlasterSeconds = 2.5f;
        public const float DurationSeconds = 40f;
        public const float MaxBonusEuros = 6f;
        public const float FingerCostEuros = 1f;

        /// <summary>The morning's veg: an onion, a carrot and a stick of celery, in millimetres of board.</summary>
        public static readonly (string name, float lengthMm)[] DefaultVegetables =
        {
            ("cebula", 54f), ("marchew", 72f), ("seler", 60f)
        };

        private readonly (string name, float lengthMm)[] _vegetables;
        private readonly float _wobble;
        private readonly float _totalMm;
        private float _doneMm;
        private float _sinceCut = float.PositiveInfinity;
        private bool _timeUp;

        /// <param name="steadiness">1 for a rested hand; tiredness (Fatigue.ScrubStrength, down to 0.7) makes the push
        /// uneven, so the beat wanders.</param>
        public PrepRound(float steadiness = 1f, (string name, float lengthMm)[] vegetables = null)
        {
            _vegetables = vegetables ?? DefaultVegetables;
            _wobble = Mathf.Clamp01(1f - steadiness) * 1.2f;
            foreach ((string _, float length) in _vegetables)
            {
                _totalMm += length;
            }

            LeftMm = _vegetables.Length > 0 ? _vegetables[0].lengthMm : 0f;
        }

        /// <summary>A slice came off (or a finger was nicked): what, and how thick.</summary>
        public event Action<PrepCut, float> Sliced;

        public int VegetableIndex { get; private set; }
        public int VegetableCount => _vegetables.Length;
        public string VegetableName => VegetableIndex < _vegetables.Length ? _vegetables[VegetableIndex].name : string.Empty;
        public float VegetableLengthMm => VegetableIndex < _vegetables.Length ? _vegetables[VegetableIndex].lengthMm : 0f;

        /// <summary>Of the vegetable on the board, still to the right of the blade (including what has just passed it).</summary>
        public float LeftMm { get; private set; }

        /// <summary>Pushed past the blade since the last cut - what the next cut will take.</summary>
        public float FedMm { get; private set; }

        public float Elapsed { get; private set; }
        public float PlasterRemaining { get; private set; }
        public int Even { get; private set; }
        public int Thick { get; private set; }
        public int Thin { get; private set; }
        public int Chunks { get; private set; }
        public int Fingers { get; private set; }
        public int Slices => Even + Thick + Thin + Chunks;
        public bool IsFinished => _timeUp || VegetableIndex >= _vegetables.Length;

        /// <summary>Share of the veg that has gone under the knife (1 when all of it is prepped).</summary>
        public float Completion => _totalMm > 0f ? Mathf.Clamp01(_doneMm / _totalMm) : 1f;

        /// <summary>Share of the slices that came out even.</summary>
        public float Evenness => Slices > 0 ? Even / (float)Slices : 0f;

        /// <summary>Paid on top of the shift: up to 6 EUR for even, finished prep, less 1 EUR a plaster - to the half euro
        /// (Dublin's money; elsewhere the kitchen pays as many of the place's small sums, PlaceMoney.SmallSums).</summary>
        public float BonusEuros => Mathf.Max(0f, Mathf.Round((MaxBonusEuros * Evenness * Completion - Fingers * FingerCostEuros) * 2f) / 2f);

        /// <summary>What the kitchen says about it.</summary>
        public string Verdict
        {
            get
            {
                if (Completion < 0.6f)
                {
                    return "Gavin: „Moja babcia kroi szybciej, a nie żyje od dziewięćdziesiątego ósmego.”";
                }

                if (Evenness >= 0.85f && Completion >= 0.99f && Fingers == 0)
                {
                    return "Declan: „Trzy milimetry. Każdy.”";
                }

                if (Fingers > 0 && Evenness < 0.6f)
                {
                    return "Gavin: „Niebieski plaster. Żeby było go widać w zupie, a nie u gościa.”";
                }

                return Evenness >= 0.6f ? "Gavin: „Grand. Nie świetnie. Ale grand.”" : "Declan: „Nierówno. Jutro jeszcze raz.”";
            }
        }

        public void Tick(float dt)
        {
            if (IsFinished || dt <= 0f)
            {
                return;
            }

            Elapsed += dt;
            _sinceCut += dt;
            if (Elapsed >= DurationSeconds)
            {
                _timeUp = true;
                return;
            }

            if (PlasterRemaining > 0f)
            {
                PlasterRemaining = Mathf.Max(0f, PlasterRemaining - dt);
                return;
            }

            float speed = FeedMmPerSecond * (1f + _wobble * Mathf.Sin(Elapsed * 2.3f));
            FedMm = Mathf.Min(FedMm + speed * dt, LeftMm);
            if (FedMm >= ChunkAtMm)
            {
                Record(PrepCut.Chunk, FedMm);
            }
            else if (FedMm >= LeftMm - 0.001f)
            {
                // The heel has passed the blade - it goes in the stock pot, not the soup.
                _doneMm += LeftMm;
                NextVegetable();
            }
        }

        /// <summary>The knife comes down. Returns what it did, or null when it could not (a plaster on, the knife still
        /// coming up, nothing on the board).</summary>
        public PrepCut? Cut()
        {
            if (IsFinished || PlasterRemaining > 0f || _sinceCut < KnifeLiftSeconds)
            {
                return null;
            }

            _sinceCut = 0f;
            if (LeftMm <= FingerBelowMm)
            {
                // Only a sliver of the end is left: the knife sweeps it off with the heel, no harm done.
                _doneMm += LeftMm;
                NextVegetable();
                return null;
            }

            float slice = FedMm;
            if (slice < FingerBelowMm)
            {
                Fingers++;
                PlasterRemaining = PlasterSeconds;
                Sliced?.Invoke(PrepCut.Finger, slice);
                return PrepCut.Finger;
            }

            PrepCut kind = Mathf.Abs(slice - TargetSliceMm) <= ToleranceMm ? PrepCut.Even
                : slice > TargetSliceMm ? PrepCut.Thick : PrepCut.Thin;
            Record(kind, slice);
            return kind;
        }

        private void Record(PrepCut kind, float slice)
        {
            switch (kind)
            {
                case PrepCut.Even: Even++; break;
                case PrepCut.Thick: Thick++; break;
                case PrepCut.Thin: Thin++; break;
                case PrepCut.Chunk: Chunks++; break;
            }

            LeftMm -= slice;
            _doneMm += slice;
            FedMm = 0f;
            Sliced?.Invoke(kind, slice);
            if (LeftMm <= 0.001f)
            {
                NextVegetable();
            }
        }

        private void NextVegetable()
        {
            VegetableIndex++;
            FedMm = 0f;
            LeftMm = VegetableIndex < _vegetables.Length ? _vegetables[VegetableIndex].lengthMm : 0f;
        }
    }
}
