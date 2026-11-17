using Zmywak.Core.Data;

namespace Zmywak.Meta.Economy
{
    /// <summary>
    /// Money between the currencies of Kuba's life, at the National Bank of Poland's yearly averages
    /// (<see cref="ExchangeRateTableAsset"/>): euros and Australian dollars to złoty (what a transfer home arrives as),
    /// and between any of them and the money of wherever he lives (<see cref="Place"/>). Without a table (the tests):
    /// 4 złoty to the euro, 1.44 dollars to the euro.
    /// </summary>
    public class ExchangeRateService
    {
        private const float FallbackPlnPerEur = 4f;
        private const float FallbackAudPerEur = 1.44f;

        private readonly ExchangeRateTableAsset _table;

        public ExchangeRateService(ExchangeRateTableAsset table)
        {
            _table = table;
        }

        /// <summary>How many złoty a euro bought that year.</summary>
        public float GetRateForYear(int year)
        {
            return _table != null ? _table.GetRateForYear(year) : FallbackPlnPerEur;
        }

        public float ConvertEurToPln(float eurAmount, int year)
        {
            return eurAmount * GetRateForYear(year);
        }

        /// <summary>How many Australian dollars a euro bought that year (through the złoty, as NBP quotes both).</summary>
        public float AudPerEurIn(int year)
        {
            return _table != null ? _table.GetRateForYear(year) / _table.GetAudRateForYear(year) : FallbackAudPerEur;
        }

        /// <summary>How much of a place's money a euro bought that year.</summary>
        public float PerEuro(Place place, int year)
        {
            switch (place)
            {
                case Place.Krakow:
                    return GetRateForYear(year);
                case Place.Pilbara:
                    return AudPerEurIn(year);
                default:
                    return 1f;
            }
        }

        /// <summary>A sum in euros, in a place's money.</summary>
        public float FromEuros(float euros, Place place, int year) => euros * PerEuro(place, year);

        /// <summary>A sum in a place's money, in euros.</summary>
        public float ToEuros(float amount, Place place, int year) => amount / PerEuro(place, year);

        /// <summary>What a sum in a place's money arrives home as, in złoty.</summary>
        public float ToPln(float amount, Place place, int year) => ToEuros(amount, place, year) * GetRateForYear(year);
    }
}
