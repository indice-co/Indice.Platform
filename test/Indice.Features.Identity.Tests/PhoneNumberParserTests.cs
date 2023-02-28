using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PhoneNumbers;
using Xunit;

namespace Indice.Features.Identity.Tests;

public class PhoneNumberParserTests
{
    [InlineData("GB", "+441142726444")]
    [InlineData("GB", "+44 1142726444")]
    [InlineData("GR", "+306973022211")]
    [Theory]
    public void PhonumberIsParsedCorrectly_Test(string countryCode, string phoneNumber) {
        // 357 22 xxxxxx
        //  +357
        // Format: 2x - xxxxxx; 9x - xxxxxx
        PhoneNumber number;
        Assert.True(PhoneNumber.TryParse(phoneNumber, out number));
        Assert.Equal(countryCode, number.Country.Iso3166Code);
        Assert.True(PhoneNumber.TryParse(phoneNumber, countryCode, out number));
        Assert.Equal(countryCode, number.Country.Iso3166Code);
    }
}
