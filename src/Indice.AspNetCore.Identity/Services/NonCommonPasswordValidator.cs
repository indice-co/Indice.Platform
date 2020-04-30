using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Indice.AspNetCore.Identity.Services
{
    /// <inheritdoc/>
    public class NonCommonPasswordValidator : NonCommonPasswordValidator<User>
    {
        /// <inheritdoc/>
        public NonCommonPasswordValidator(IEnumerable<IPasswordBlacklistProvider> providers) : base(providers) { }
    }

    /// <summary>
    /// A validator that checks if the user's password is a very common one and as a result easy to guess.
    /// </summary>
    /// <typeparam name="TUser">The type of user instance.</typeparam>
    public class NonCommonPasswordValidator<TUser> : IPasswordValidator<TUser> where TUser : User
    {
        private readonly HashSet<string> _commonPasswords;
        /// <summary>
        /// The code used when describing the <see cref="IdentityError"/>.
        /// </summary>
        public static string ErrorDescriber = "PasswordIsBlacklisted";

        /// <summary>
        /// Creates a new instance of <see cref="NonCommonPasswordValidator"/>.
        /// </summary>
        /// <param name="providers">The list of <see cref="IPasswordBlacklistProvider"/> providers to use.</param>
        public NonCommonPasswordValidator(IEnumerable<IPasswordBlacklistProvider> providers) {
            _commonPasswords = new HashSet<string>();
            foreach (var provider in providers) {
                AddPasswordSet(provider.Blacklist);
            }
        }

        /// <inheritdoc/>
        public Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user, string password) {
            var result = IdentityResult.Success;
            if (_commonPasswords.Contains(password)) {
                result = IdentityResult.Failed(new IdentityError {
                    Code = ErrorDescriber,
                    Description = "Your password is very common to use."
                });
            }
            return Task.FromResult(result);
        }

        private void AddPasswordSet(HashSet<string> additionalPasswords) {
            foreach (var password in additionalPasswords) {
                if (!_commonPasswords.Contains(password)) {
                    _commonPasswords.Add(password);
                }
            }
        }
    }

    /// <summary>
    /// Must implement one or more of these in order to enrich the list of available blacklisted passwords.
    /// </summary>
    public interface IPasswordBlacklistProvider
    {
        /// <summary>
        /// Gets a list containing passwords to blacklist.
        /// </summary>
        /// <returns></returns>
        HashSet<string> Blacklist { get; }
    }

    /// <summary>
    /// A provider for <see cref="NonCommonPasswordValidator"/> that contains a hardcoded list of blacklisted passwords that the user cannot use.
    /// </summary>
    public class DefaultPasswordBlacklistProvider : IPasswordBlacklistProvider
    {
        /// <inheritdoc/>
        public HashSet<string> Blacklist { get; } = new HashSet<string> {
            "12345", "123456", "123456789", "test1", "password", "12345678", "zinch", "g_czechout", "asdf", "qwerty", "1234567890", "1234567", "Aa123456.", "iloveyou", "1234", "abc123", "111111",
            "123123", "dubsmash", "test", "princess", "qwertyuiop", "sunshine", "BvtTest123", "11111", "letmein", "football", "admin", "welcome", "monkey", "login", "starwars", "dragon", "passw0rd",
            "master", "hello", "freedom", "whatever", "qazwsx", "trustno1", "654321", "jordan23", "harley", "password1", "666666", "!@#$%^&*", "charlie", "aa123456", "donald", "google", "facebook"
        };
    }

    /// <summary>
    /// A provider for <see cref="NonCommonPasswordValidator"/> that gets a list of blacklisted passwords from either 'IdentityOptions:Password:Blacklist' or 'PasswordOptions:Blacklist' option.
    /// </summary>
    public class ConfigPasswordBlacklistProvider : IPasswordBlacklistProvider
    {
        private readonly HashSet<string> _blacklist;

        /// <inheritdoc/>
        public ConfigPasswordBlacklistProvider(IConfiguration configuration) {
            var list = configuration.GetSection($"{nameof(IdentityOptions)}:{nameof(IdentityOptions.Password)}").GetValue<string[]>(nameof(Blacklist)) ??
                       configuration.GetSection($"{nameof(PasswordOptions)}").GetValue<string[]>(nameof(Blacklist)) ??
                       Array.Empty<string>();
            _blacklist = new HashSet<string>(list);
        }

        /// <inheritdoc/>
        public HashSet<string> Blacklist => _blacklist;
    }
}
