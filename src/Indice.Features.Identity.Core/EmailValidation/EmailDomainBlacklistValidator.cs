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
/// </remarks>
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

        if (string.IsNullOrWhiteSpace(email) ||
            !TryGetDomain(email, out var domain) ||
            await IsBlacklistedAsync(domain)) {
            return IdentityResult.Failed(
                (manager?.ErrorDescriber ?? new ExtendedIdentityErrorDescriber())
                .InvalidEmail(email)
            );
        }

        return IdentityResult.Success;
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
        var tasks = _providers.Select(x => x.IsDomainBlacklistedAsync(domain, linkedTokenSource.Token)).ToList();

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
        Task<bool> IsDomainBlacklistedAsync(string domain, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// A provider that retrieves a list of blacklisted email domains from configuration.
    /// The list can be defined under:
    /// 'IdentityOptions:Email:DomainBlacklist' or 'EmailDomainBlacklist'.
    /// </summary>
    public class ConfigEmailDomainBlacklistProvider : IEmailDomainBlacklistProvider
    {
        private readonly HashSet<string> _blacklist;

        /// <summary>
        /// Initializes a new instance of the ConfigEmailDomainBlacklistProvider class using the specified configuration
        /// source.
        /// </summary>
        /// <remarks>The constructor attempts to load the blacklist from the
        /// 'IdentityOptions:Email:DomainBlacklist' section first, then from 'EmailDomainBlacklist' if the first is not
        /// found. If neither section is present, an empty blacklist is used. Domain matching is
        /// case-insensitive.</remarks>
        /// <param name="configuration">The configuration source from which to retrieve the email domain blacklist settings. Cannot be null.</param>
        public ConfigEmailDomainBlacklistProvider(IConfiguration configuration) {
            var list =
                configuration.GetSection("IdentityOptions:Email:DomainBlacklist").Get<string[]>() ??
                configuration.GetSection("EmailDomainBlacklist").Get<string[]>() ??
                [];

            _blacklist = new HashSet<string>(list, StringComparer.OrdinalIgnoreCase);
        }
        /// <inheritdoc/>
        public Task<bool> IsDomainBlacklistedAsync(string domain, CancellationToken cancellationToken = default) =>
            Task.FromResult(_blacklist.Contains(domain));
    }

    /// <summary>
    /// A provider that loads blacklisted email domains from the disposable_email_blocklist.conf.txt file.
    /// </summary>
    public class FileEmailDomainBlacklistProvider : IEmailDomainBlacklistProvider
    {
        private readonly HashSet<string> _blacklist;
        /// <summary>
        /// Initializes a new instance of the FileEmailDomainBlacklistProvider class using the specified blacklist file.
        /// </summary>
        /// <remarks>The blacklist is loaded once during construction. Changes to the file after
        /// initialization are not reflected in the provider.</remarks>
        /// <param name="filePath">The path to the file containing the list of blacklisted email domains. Each line should represent a domain.
        /// Lines that are empty or start with '#' are ignored.</param>
        /// <exception cref="FileNotFoundException">Thrown if the file specified by filePath does not exist.</exception>
        public FileEmailDomainBlacklistProvider(string filePath) {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Blacklist file not found: {filePath}");

            _blacklist = File.ReadAllLines(filePath)
                .Where(x => !string.IsNullOrWhiteSpace(x) && !x.StartsWith("#"))
                .Select(x => x.Trim().ToLowerInvariant())
                .ToHashSet();
        }
        /// <inheritdoc/>
        public Task<bool> IsDomainBlacklistedAsync(string domain, CancellationToken cancellationToken = default) =>
            Task.FromResult(_blacklist.Contains(domain));
    }
}