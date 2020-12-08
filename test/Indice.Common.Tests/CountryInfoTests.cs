using System;
using System.Collections.Generic;
using System.Text;
using Indice.Globalization;
using Xunit;

namespace Indice.Common.Tests
{
    public class CountryInfoTests
    {
        public CountryInfoTests() {

        }

        [Trait("Tag", "Country")]
        [Theory]
        [InlineData("GR")]
        [InlineData("AU")]
        [InlineData("GB")]
        [InlineData("Greece")]
        public void ByTwoLetterISOCodeValid_Test(string iso) {
            var country = CountryInfo.GetCountryByNameOrCode(iso);
            Assert.NotNull(country);
        }
    }
}
