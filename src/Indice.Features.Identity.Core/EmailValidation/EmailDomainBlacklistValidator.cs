using System.Net.Mail;
using Indice.Features.Identity.Core.Configuration;
using Indice.Features.Identity.Core.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

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
        if (await IsBlacklistedAsync(user.Email)) {
            return IdentityResult.Failed((manager?.ErrorDescriber ?? new ExtendedIdentityErrorDescriber()).InvalidEmail(user.Email));
        }
        return IdentityResult.Success;
    }

    /// <summary>
    /// Checks all configured providers to determine whether the domain is blacklisted.
    /// </summary>
    /// <param name="email">The email address to check.</param>
    /// <param name="cancellationToken">Indicates that the operation should be cancelled.</param>
    /// <returns>True if the domain is blacklisted; otherwise false.</returns>
    private async Task<bool> IsBlacklistedAsync(string? email, CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(email)) {
            return true;
        }

        if (!TryGetDomain(email, out var domain)) {
            return true;
        }

        using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var tasks = _providers.Select(x => x.IsDomainBlacklistedAsync(domain, linkedTokenSource.Token)).ToList();

        while (tasks.Count > 0 && !linkedTokenSource.IsCancellationRequested) {
            var finishedTask = await Task.WhenAny(tasks);
            tasks.Remove(finishedTask);

            if (finishedTask.IsCanceled || finishedTask.IsFaulted) {
                // Fail-open for this provider: ignore canceled/faulted checks and continue with the rest.
                continue;
            }

            if (finishedTask.Result) {
                linkedTokenSource.Cancel();
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Attempts to extract the domain part from an email address using proper RFC 5322 validation.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <param name="domain">The extracted domain in lowercase.</param>
    /// <returns>True if extraction was successful and email is valid; otherwise false.</returns>
    private static bool TryGetDomain(string email, out string domain) {
        domain = string.Empty;
        try {
            var mailAddress = new MailAddress(email);
            domain = mailAddress.Host.ToLowerInvariant();
            return true;
        } catch {
            return false;
        }
    }
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
/// A provider that retrieves a list of blacklisted email domains from configuration
/// via <see cref="EmailBlacklistOptions"/>, using the <see cref="EmailBlacklistOptions.Domain"/> setting.
/// This is typically bound from a configuration section named <c>Blacklist</c>.
/// </summary>
public class ConfigEmailDomainBlacklistProvider : IEmailDomainBlacklistProvider
{
    private readonly HashSet<string> _blacklist;
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigEmailDomainBlacklistProvider"/> class.
    /// </summary>
    /// <param name="options">The options containing the email blacklist configuration.</param> 
    public ConfigEmailDomainBlacklistProvider(IOptions<EmailBlacklistOptions> options) {
        var list = options.Value.Domain?
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            ?? Array.Empty<string>();
        _blacklist = new HashSet<string>(list, StringComparer.OrdinalIgnoreCase);
    }
    /// <inheritdoc/>
    public Task<bool> IsDomainBlacklistedAsync(string domain, CancellationToken cancellationToken = default) =>
        Task.FromResult(_blacklist.Contains(domain));
}

/// <summary>
/// A provider that loads blacklisted email domains from the EmailValidation/email_blocklist.conf file.
/// </summary>
public class FileEmailDomainBlacklistProvider : IEmailDomainBlacklistProvider
{
    private readonly HashSet<string> _blacklist;
    /// <summary>
    /// Initializes a new instance of the <see cref="FileEmailDomainBlacklistProvider"/> class using the specified blacklist file.    
    /// </summary>
    public FileEmailDomainBlacklistProvider() {
        var assembly = typeof(FileEmailDomainBlacklistProvider).Assembly;
        var resourceName = "Indice.Features.Identity.Core.EmailValidation.email_blocklist.conf";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) {
            throw new InvalidOperationException($"Embedded resource not found: {resourceName}");
        }

        using var reader = new StreamReader(stream);
        _blacklist = [.. reader.ReadToEnd()
            .Split('\n')
            .Where(x => !string.IsNullOrWhiteSpace(x) && !x.StartsWith("#"))
            .Select(x => x.Trim().ToLowerInvariant())];
    }
    /// <inheritdoc/>
    public Task<bool> IsDomainBlacklistedAsync(string domain, CancellationToken cancellationToken = default) =>
        Task.FromResult(_blacklist.Contains(domain));
}
