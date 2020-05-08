using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Models;
using Indice.AspNetCore.Identity.Services;
using Xunit;

namespace Indice.Common.Tests
{
    public class LatinCharactersPasswordValidatorTests
    {
        [Theory]
        [InlineData("k1#λ")]
        [InlineData("K1$Λ")]
        [InlineData("K1$ e")]
        public async Task CheckInvalidPasswords(string password) {
            var validator = new LatinCharactersPasswordValidator<User>();
            var identityResult = await validator.ValidateAsync(null, new User(), password);
            Assert.False(identityResult.Succeeded);
        }

        [Theory]
        [InlineData(@"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789~!@#$%^&*()_+-={}[]:"";',./<>?")]
        public async Task CheckValidPasswords(string password) {
            var validator = new LatinCharactersPasswordValidator<User>();
            var identityResult = await validator.ValidateAsync(null, new User(), password);
            Assert.True(identityResult.Succeeded);
        }
    }
}
