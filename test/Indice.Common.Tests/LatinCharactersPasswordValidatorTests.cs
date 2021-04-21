using System.Collections.Generic;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity;
using Indice.AspNetCore.Identity.Entities;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Indice.Common.Tests
{
    public class LatinCharactersPasswordValidatorTests
    {
        private readonly IConfiguration _configuration;

        public LatinCharactersPasswordValidatorTests() {
            var inMemorySettings = new Dictionary<string, string> {
                {$"IdentityOptions:Password:{nameof(LatinLettersOnlyPasswordValidator.AllowUnicodeCharacters)}", bool.FalseString}
            };
            _configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        }

        [Theory]
        [InlineData("k1#λ")]
        [InlineData("K1$Λ")]
        [InlineData("K1$ e")]
        public async Task CheckInvalidPasswords(string password) {
            var validator = new LatinLettersOnlyPasswordValidator<User>(new IdentityMessageDescriber(), _configuration);
            var identityResult = await validator.ValidateAsync(null, new User(), password);
            Assert.False(identityResult.Succeeded);
        }

        [Theory]
        [InlineData(@"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789~!@#$%^&*()_+-={}[]:"";',./<>?")]
        public async Task CheckValidPasswords(string password) {
            var validator = new LatinLettersOnlyPasswordValidator<User>(new IdentityMessageDescriber(), _configuration);
            var identityResult = await validator.ValidateAsync(null, new User(), password);
            Assert.True(identityResult.Succeeded);
        }
    }
}
