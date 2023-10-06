using Indice.Globalization;
using Xunit;

namespace Indice.Common.Tests;

public class PhoneNumberTests
{
    public PhoneNumberTests() {

    }

    [Trait("Tag", "PhoneNumber")]
    [Theory]
    [InlineData("+1 (0115) 123 4567")]
    [InlineData("+30 6972233445")]
    [InlineData("+30 (697) 2233445")]
    [InlineData("+30 210-6985955")]
    [InlineData("+44 (0141) 345 9822")]
    [InlineData("+1 800-444-4444")]
    public void ByTwoLetterISOCodeValid_Test(string number) {
        var phone = PhoneNumber.Parse(number);
        Assert.True(true);
    }
}
