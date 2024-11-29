using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.PasswordValidation;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Indice.Features.Identity.Tests;

public class AllowedCharactersPasswordValidatorTests
{
    private readonly IConfiguration _configuration;
    private const string ALLOWED_CHARACTERS = "123abcAbc!#$";

    public AllowedCharactersPasswordValidatorTests() {
        var inMemorySettings = new Dictionary<string, string> {
            {$"IdentityOptions:Password:{nameof(AllowedCharactersPasswordValidator.AllowedCharacters)}", ALLOWED_CHARACTERS}
        };
        _configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
    }

    [Theory]
    [InlineData("1234Abc!")]
    [InlineData("123AbcD!")]
    [InlineData("123 Abc")]
    [InlineData("1234Abc(")]
    public async Task CheckInvalidPasswords(string password) {
        var validator = new AllowedCharactersPasswordValidator(_configuration, new IdentityMessageDescriber());
        var identityResult = await validator.ValidateAsync(null, new User(), password);
        Assert.False(identityResult.Succeeded);
    }

    [Theory]
    [InlineData("xxxxxxx")]
    [InlineData("123Abc!$")]
    [InlineData("123")]
    public async Task CheckValidPasswords(string password) {
        var validator = new AllowedCharactersPasswordValidator(_configuration, new IdentityMessageDescriber());
        var identityResult = await validator.ValidateAsync(null, new User(), password);
        Assert.True(identityResult.Succeeded);
    }
}
