using System;
using System.Collections.Generic;
using System.Text;
using Indice.Globalization;
using Xunit;

namespace Indice.Common.Tests
{
    public class CurrencyInfoTests
    {
        public CurrencyInfoTests() {

        }

        [Trait("Tag", "Currency")]
        [Theory]
        [InlineData("EUR")]
        [InlineData("USD")]
        [InlineData("GBP")]
        [InlineData("CAD")]
        [InlineData("CHF")]
        public void ByISOCodeValid_Test(string iso) {
            if (iso?.Length > 3) {
                Assert.Throws<ArgumentOutOfRangeException>(() => CurrencyInfo.GetByISOSymbol(iso));
            } else {
                var currency = CurrencyInfo.GetByISOSymbol(iso);
                Assert.NotNull(currency);
            }
        }

        [Trait("Tag", "Currency")]
        [Theory]
        [InlineData("DOLLAR")]
        [InlineData("XDSFSDFS")]
        [InlineData("eur")]
        public void ByISOCodeInvalid_Test(string iso) {
            Assert.Throws<ArgumentOutOfRangeException>(() => CurrencyInfo.GetByISOSymbol(iso));
        }
    }
}
