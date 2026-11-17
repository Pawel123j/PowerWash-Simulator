using NUnit.Framework;
using UnityEngine;
using Zmywak.Core.Data;

namespace Zmywak.Tests
{
    public class ExchangeRateTableTests
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
        public void ExactYear_ReturnsThatYearsRate()
        {
            Assert.AreEqual(4.5294f, _table.GetRateForYear(2004), 1e-4f, "NBP, 2004");
            Assert.AreEqual(3.5129f, _table.GetRateForYear(2008), 1e-4f, "NBP, 2008");
            Assert.AreEqual(2.7716f, _table.GetAudRateForYear(2010), 1e-4f, "NBP, the Australian dollar in 2010");
        }

        [Test]
        public void OutOfRangeYear_FallsBackToNearestPoint()
        {
            Assert.AreEqual(4.5294f, _table.GetRateForYear(1999), 1e-4f);
            Assert.AreEqual(4.1852f, _table.GetRateForYear(2030), 1e-4f);
        }
    }
}
