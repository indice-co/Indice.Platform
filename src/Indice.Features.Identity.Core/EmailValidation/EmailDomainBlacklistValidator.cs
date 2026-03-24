using Indice.Features.Identity.Core.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Indice.Features.Identity.Core.EmailValidation;

/// <inheritdoc/>
public class EmailDomainBlacklistValidator : EmailDomainBlacklistValidator<User>
{
    /// <inheritdoc/>
    public EmailDomainBlacklistValidator(IEnumerable<IEmailDomainBlacklistProvider> providers) : base(providers) { }
}

/// <summary>
/// Validates that the user's email domain is not included in any configured blacklist.
/// </summary>
/// <typeparam name="TUser">The type of user instance.</typeparam>
/// <remarks>
/// Creates a new instance of <see cref="EmailDomainBlacklistValidator{TUser}"/>.
public class EmailDomainBlacklistValidator<TUser> : IUserValidator<TUser> where TUser : User
{
    private readonly IEnumerable<IEmailDomainBlacklistProvider> _providers;

    /// <summary>Creates a new instance of <see cref="EmailDomainBlacklistValidator"/>.</summary>
    /// <param name="providers">The list of <see cref="IEmailDomainBlacklistProvider"/> providers to use.</param>
    public EmailDomainBlacklistValidator(IEnumerable<IEmailDomainBlacklistProvider> providers) {
        _providers = providers ?? throw new ArgumentNullException(nameof(providers));
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user) {
        var email = user.Email;
        var result = IdentityResult.Success;
        if (string.IsNullOrWhiteSpace(email) || !TryGetDomain(email, out var domain) || await IsBlacklistedAsync(domain)) {
            return IdentityResult.Failed(
                (manager?.ErrorDescriber ?? new ExtendedIdentityErrorDescriber())
                .InvalidEmail(email)
            );
        }
        return result;
    }

    /// <summary>
    /// Attempts to extract the domain part from an email address.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <param name="domain">The extracted domain.</param>
    /// <returns>True if extraction was successful; otherwise false.</returns>
    private static bool TryGetDomain(string email, out string domain) {
        domain = string.Empty;
        var parts = email.Split('@');
        if (parts.Length != 2) {
            return false;
        }
        domain = parts[1].ToLowerInvariant();
        return true;
    }

    /// <summary>
    /// Checks all configured providers to determine whether the domain is blacklisted.
    /// </summary>
    /// <param name="domain">The email domain to check.</param>
    /// <param name="cancellationToken">Indicates that the operation should be cancelled.</param>
    /// <returns>True if the domain is blacklisted; otherwise false.</returns>
    public async Task<bool> IsBlacklistedAsync(string domain, CancellationToken cancellationToken = default) {
        var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var tasks = _providers.Select(x => x.ContainsAsync(domain, linkedTokenSource.Token)).ToList();

        while (tasks.Count > 0 && !linkedTokenSource.IsCancellationRequested) {
            var finishedTask = await Task.WhenAny(tasks);
            if (finishedTask.Result) {
                linkedTokenSource.Cancel();
                return true;
            }
            tasks.Remove(finishedTask);
        }

        return false;
    }

    /// <summary>
    /// Represents a provider that supplies blacklisted email domains.
    /// </summary>
    public interface IEmailDomainBlacklistProvider
    {
        /// <summary>
        /// Checks whether the specified domain is blacklisted.
        /// </summary>
        /// <param name="domain">The email domain to check.</param>
        /// <param name="cancellationToken">Indicates that the operation should be cancelled.</param>
        /// <returns>True if the domain is blacklisted; otherwise false.</returns>
        Task<bool> ContainsAsync(string domain, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// A provider that contains a predefined (hard-coded) list of blacklisted email domains.
    /// </summary>
    public class DefaultEmailDomainBlacklistProvider : IEmailDomainBlacklistProvider
    {
        /// <summary>Gets a set containing domains to blacklist.</summary>
        protected HashSet<string> Blacklist { get; } = new(StringComparer.OrdinalIgnoreCase) {
        };

        /// <inheritdoc/>
        public Task<bool> ContainsAsync(string domain, CancellationToken cancellationToken = default) =>
            Task.FromResult(Blacklist.Contains(domain));
    }

    /// <summary>
    /// A provider that retrieves a list of blacklisted email domains from configuration.
    /// The list can be defined under:
    /// 'IdentityOptions:Email:DomainBlacklist' or 'EmailDomainBlacklist'.
    /// </summary>
    public class ConfigEmailDomainBlacklistProvider : IEmailDomainBlacklistProvider
    {
        /// <summary>Gets a set containing domains to blacklist.</summary>
        protected HashSet<string> Blacklist { get; }

        /// <inheritdoc/>
        public ConfigEmailDomainBlacklistProvider(IConfiguration configuration) {
            var list =
                configuration.GetSection($"IdentityOptions:Email:DomainBlacklist").Get<string[]>() ??
                configuration.GetSection("EmailDomainBlacklist").Get<string[]>() ??
                [];

            Blacklist = new HashSet<string>(list, StringComparer.OrdinalIgnoreCase);
        }

        /// <inheritdoc/>
        public Task<bool> ContainsAsync(string domain, CancellationToken cancellationToken = default) =>
            Task.FromResult(Blacklist.Contains(domain));
    }

    /// <summary>
    /// A provider that loads blacklisted email domains from a file.
    /// Each non-empty line that does not start with '#' is treated as a domain entry.
    /// Lines are trimmed and normalized to lower-case for comparison.
    /// </summary>
    public class FileEmailDomainBlacklistProvider : IEmailDomainBlacklistProvider
    {
        private readonly HashSet<string> _blacklist;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileEmailDomainBlacklistProvider"/> class.
        /// </summary>
        /// <param name="filePath">The path to the file containing the blacklisted domains.</param>
        public FileEmailDomainBlacklistProvider(string filePath) {
            _blacklist = File.ReadAllLines(filePath)
                .Where(x => !string.IsNullOrWhiteSpace(x) && !x.StartsWith("#"))
                .Select(x => x.Trim().ToLowerInvariant())
                .ToHashSet();
        }

        /// <inheritdoc/>
        public Task<bool> ContainsAsync(string domain, CancellationToken cancellationToken = default) =>
            Task.FromResult(_blacklist.Contains(domain));
    }
}