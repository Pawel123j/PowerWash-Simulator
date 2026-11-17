using NUnit.Framework;
using UnityEngine;
using Zmywak.Core.Data;
using Zmywak.Meta.Economy;

namespace Zmywak.Tests
{
    public class ExchangeRateServiceTests
    {
        private ExchangeRateTableAsset _table;

        [SetUp]
        public void SetUp()
        {
            _table = ScriptableObject.CreateInstance<ExchangeRateTableAsset>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_table);
        }

        [Test]
        public void ConvertEurToPln_UsesThatYearsRate()
        {
            var service = new ExchangeRateService(_table);

            Assert.AreEqual(20f * 4.5294f, service.ConvertEurToPln(20f, 2004), 1e-3f);
            Assert.AreEqual(20f * 3.5129f, service.ConvertEurToPln(20f, 2008), 1e-3f);
        }

        [Test]
        public void SameEuros_BuyFewerZlotyWhenTheZlotyStrengthens()
        {
            var service = new ExchangeRateService(_table);

            Assert.Greater(service.ConvertEurToPln(100f, 2004), service.ConvertEurToPln(100f, 2008));
        }

        [Test]
        public void MissingTable_FallsBackToFlatRate()
        {
            var service = new ExchangeRateService(null);

            Assert.AreEqual(4f, service.GetRateForYear(2006), 1e-4f);
            Assert.AreEqual(40f, service.ConvertEurToPln(10f, 2006), 1e-4f);
        }

        [Test]
        public void ZeroEuros_ConvertsToZero()
        {
            var service = new ExchangeRateService(_table);

            Assert.AreEqual(0f, service.ConvertEurToPln(0f, 2005), 1e-6f);
        }
    }
}
