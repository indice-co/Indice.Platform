using System.Collections.Generic;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Data.Models;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Indice.AspNetCore.Identity.Tests
{
    public class UnicodeCharactersPasswordValidatorTests
    {
        private readonly IConfiguration _configuration;

        public UnicodeCharactersPasswordValidatorTests() {
            var inMemorySettings = new Dictionary<string, string> {
                {$"IdentityOptions:Password:{nameof(UnicodeCharactersPasswordValidator.AllowUnicodeCharacters)}", bool.FalseString}
            };
            _configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        }

        [Theory]
        [InlineData("k1#λ")]
        [InlineData("K1$Λ")]
        [InlineData("K1$ e")]
        public async Task CheckInvalidPasswords(string password) {
            var validator = new UnicodeCharactersPasswordValidator<User>(new IdentityMessageDescriber(), _configuration);
            var identityResult = await validator.ValidateAsync(null, new User(), password);
            Assert.False(identityResult.Succeeded);
        }

        [Theory]
        [InlineData(@"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789~!@#$%^&*()_+-={}[]:"";',./<>?")]
        public async Task CheckValidPasswords(string password) {
            var validator = new UnicodeCharactersPasswordValidator<User>(new IdentityMessageDescriber(), _configuration);
            var identityResult = await validator.ValidateAsync(null, new User(), password);
            Assert.True(identityResult.Succeeded);
        }
    }
}
