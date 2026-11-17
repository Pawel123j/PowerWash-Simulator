using System;
using System.Collections.Generic;
using System.Linq;

namespace Zmywak.Core.Data
{
    /// <summary>
    /// The order the dishes come in. First a fixed opening - what the first lesson teaches on - then the shift's mix dealt
    /// like a shuffled deck: every round holds each kind as many times as the mix has it (its weight), so the night's
    /// balance holds, in an order nobody can learn, and never more than <see cref="MaxRun"/> of a kind in a row. Unshuffled,
    /// it simply goes round the mix in its order (the kitchen on its own, for the tests). The same seed deals the same order,
    /// so a day plays the same however often it is loaded, and a test can pin it.
    /// </summary>
    public sealed class ShuffleBag<T> where T : class
    {
        /// <summary>The most of one kind in a row (a third plate in a row is moved further into the round).</summary>
        public const int MaxRun = 2;

        // A round that still runs long after the runs are broken up (its last places all one kind) is shuffled again.
        private const int MaxShuffles = 32;

        private readonly List<T> _opening;
        private readonly List<T> _deck;
        private readonly Func<T, string> _kind;
        private readonly bool _shuffle;
        private readonly bool _repeat;
        private readonly Random _random;
        private readonly List<T> _round = new List<T>();
        private int _openingIndex;
        private int _roundIndex;
        private bool _dealtOnce;
        private string _lastKind;
        private int _run;

        public ShuffleBag(IEnumerable<T> opening, IEnumerable<T> deck, Func<T, string> kind, int seed, bool shuffle = true, bool repeat = true)
        {
            _opening = opening != null ? opening.Where(item => item != null).ToList() : new List<T>();
            _deck = deck != null ? deck.Where(item => item != null).ToList() : new List<T>();
            _kind = kind;
            _shuffle = shuffle;
            _repeat = repeat;
            _random = new Random(seed);
        }

        /// <summary>How many items a full round deals (the opening not counted).</summary>
        public int RoundSize => _deck.Count;

        /// <summary>The next item, or null once a bag that does not repeat has dealt its round.</summary>
        public T Next()
        {
            if (_openingIndex < _opening.Count)
            {
                return Track(_opening[_openingIndex++]);
            }

            if (_roundIndex >= _round.Count && !Deal())
            {
                return null;
            }

            return Track(_round[_roundIndex++]);
        }

        private bool Deal()
        {
            if (_deck.Count == 0 || (_dealtOnce && !_repeat))
            {
                return false;
            }

            _round.Clear();
            _round.AddRange(_deck);
            _roundIndex = 0;
            _dealtOnce = true;
            for (int attempt = 0; _shuffle && attempt < MaxShuffles; attempt++)
            {
                for (int i = _round.Count - 1; i > 0; i--)
                {
                    int j = _random.Next(i + 1);
                    (_round[i], _round[j]) = (_round[j], _round[i]);
                }

                BreakRuns();
                if (!RunsLong())
                {
                    break;
                }
            }

            return true;
        }

        /// <summary>Whether the round, after what was dealt just before it, has more than <see cref="MaxRun"/> of a kind in a row.</summary>
        private bool RunsLong()
        {
            string kind = _lastKind;
            int run = _run;
            foreach (T item in _round)
            {
                string current = _kind(item);
                run = current == kind ? run + 1 : 1;
                kind = current;
                if (run > MaxRun)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Moves a kind that would come a third time in a row (counting what was dealt just before, across rounds) further
        /// into the round. Only a mix of nearly one kind can still run long: there is nothing else to put between.
        /// </summary>
        private void BreakRuns()
        {
            string kind = _lastKind;
            int run = _run;
            for (int i = 0; i < _round.Count; i++)
            {
                string current = _kind(_round[i]);
                if (current == kind && run >= MaxRun)
                {
                    int other = _round.FindIndex(i + 1, item => _kind(item) != current);
                    if (other >= 0)
                    {
                        (_round[i], _round[other]) = (_round[other], _round[i]);
                        current = _kind(_round[i]);
                    }
                }

                run = current == kind ? run + 1 : 1;
                kind = current;
            }
        }

        private T Track(T item)
        {
            string kind = _kind(item);
            _run = kind == _lastKind ? _run + 1 : 1;
            _lastKind = kind;
            return item;
        }
    }
}
