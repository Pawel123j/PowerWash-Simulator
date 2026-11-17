using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zmywak.Core.Data
{
    [Serializable]
    public struct ExchangeRatePoint
    {
        public int year;

        /// <summary>How many PLN one EUR bought, as a yearly average.</summary>
        public float plnPerEur;

        /// <summary>How many PLN one Australian dollar bought, as a yearly average.</summary>
        public float plnPerAud;
    }

    /// <summary>
    /// The złoty against the euro and the Australian dollar, 2004-2012: yearly averages of the National Bank of Poland's
    /// daily mid rates (table A, api.nbp.pl - 252 to 256 quotation days a year). The złoty strong after accession and
    /// stronger still in the boom, then the 2008 crash taking it down a fifth.
    /// </summary>
    [CreateAssetMenu(menuName = "Zmywak/Exchange Rate Table", fileName = "ExchangeRateTable")]
    public class ExchangeRateTableAsset : ScriptableObject
    {
        /// <summary>NBP table A, yearly averages of the daily mid rates.</summary>
        public static readonly ExchangeRatePoint[] NbpYearlyAverages =
        {
            new ExchangeRatePoint { year = 2004, plnPerEur = 4.5294f, plnPerAud = 2.6851f },
            new ExchangeRatePoint { year = 2005, plnPerEur = 4.0231f, plnPerAud = 2.4654f },
            new ExchangeRatePoint { year = 2006, plnPerEur = 3.8960f, plnPerAud = 2.3366f },
            new ExchangeRatePoint { year = 2007, plnPerEur = 3.7843f, plnPerAud = 2.3157f },
            new ExchangeRatePoint { year = 2008, plnPerEur = 3.5129f, plnPerAud = 2.0219f },
            new ExchangeRatePoint { year = 2009, plnPerEur = 4.3282f, plnPerAud = 2.4466f },
            new ExchangeRatePoint { year = 2010, plnPerEur = 3.9939f, plnPerAud = 2.7716f },
            new ExchangeRatePoint { year = 2011, plnPerEur = 4.1196f, plnPerAud = 3.0549f },
            new ExchangeRatePoint { year = 2012, plnPerEur = 4.1852f, plnPerAud = 3.3728f }
        };

        [SerializeField] private List<ExchangeRatePoint> points = new List<ExchangeRatePoint>(NbpYearlyAverages);

        /// <summary>Replaces the table (the setup writes <see cref="NbpYearlyAverages"/> into the asset).</summary>
        public void SetPoints(IEnumerable<ExchangeRatePoint> rates)
        {
            points = new List<ExchangeRatePoint>(rates);
        }

        /// <summary>Złoty to the euro that year (the nearest year's, outside the table).</summary>
        public float GetRateForYear(int year) => Nearest(year).plnPerEur;

        /// <summary>Złoty to the Australian dollar that year (the nearest year's, outside the table).</summary>
        public float GetAudRateForYear(int year) => Nearest(year).plnPerAud;

        private ExchangeRatePoint Nearest(int year)
        {
            if (points.Count == 0)
            {
                return new ExchangeRatePoint { year = year, plnPerEur = 4f, plnPerAud = 4f / 1.44f };
            }

            ExchangeRatePoint best = points[0];
            int bestDistance = Mathf.Abs(points[0].year - year);
            foreach (ExchangeRatePoint point in points)
            {
                int distance = Mathf.Abs(point.year - year);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = point;
                }
            }

            return best;
        }
    }
}
