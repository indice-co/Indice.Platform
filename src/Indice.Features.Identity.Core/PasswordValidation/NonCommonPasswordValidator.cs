using Indice.Features.Identity.Core.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Indice.Features.Identity.Core.PasswordValidation;

/// <inheritdoc/>
public class NonCommonPasswordValidator : NonCommonPasswordValidator<DbUser>
{
    /// <inheritdoc/>
    public NonCommonPasswordValidator(IEnumerable<IPasswordBlacklistProvider> providers, IdentityMessageDescriber messageDescriber) : base(providers, messageDescriber) { }
}

/// <summary>A validator that checks if the user's password is a very common one and as a result easy to guess.</summary>
/// <typeparam name="TUser">The type of user instance.</typeparam>
public class NonCommonPasswordValidator<TUser> : IPasswordValidator<TUser> where TUser : DbUser
{
    private readonly IEnumerable<IPasswordBlacklistProvider> _providers;
    private readonly IdentityMessageDescriber _messageDescriber;
    /// <summary>The code used when describing the <see cref="IdentityError"/>.</summary>
    public static string ErrorDescriber = "PasswordIsBlacklisted";

    /// <summary>Creates a new instance of <see cref="NonCommonPasswordValidator"/>.</summary>
    /// <param name="providers">The list of <see cref="IPasswordBlacklistProvider"/> providers to use.</param>
    /// <param name="messageDescriber">Provides the various messages used throughout Indice packages.</param>
    public NonCommonPasswordValidator(IEnumerable<IPasswordBlacklistProvider> providers, IdentityMessageDescriber messageDescriber) {
        _providers = providers ?? throw new ArgumentNullException(nameof(providers));
        _messageDescriber = messageDescriber ?? throw new ArgumentNullException(nameof(messageDescriber));
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user, string password) {
        var result = IdentityResult.Success;
        if (string.IsNullOrWhiteSpace(password) || await IsBlacklistedAsync(password)) {
            result = IdentityResult.Failed(new IdentityError {
                Code = ErrorDescriber,
                Description = _messageDescriber.PasswordIsCommon
            });
        }
        return result;
    }

    /// <summary>Check all available providers for blacklisted passwords.</summary>
    /// <param name="password">The given password to check</param>
    /// <param name="cancellationToken">Indicates that the search process should no longer be continued.</param>
    /// <returns>True if blacklisted otherwise false if all is stellar.</returns>
    /// <remarks>https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/async/#await-tasks-efficiently</remarks>
    public async Task<bool> IsBlacklistedAsync(string password, CancellationToken cancellationToken = default) {
        var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var tasks = _providers.Select(x => x.ContainsAsync(password, linkedTokenSource.Token)).ToList();
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
}

/// <summary>Must implement one or more of these in order to enrich the list of available blacklisted passwords.</summary>
public interface IPasswordBlacklistProvider
{
    /// <summary>Checks to see if password is blacklisted.</summary>
    /// <param name="password">The given password to check</param>
    /// <param name="cancellationToken">Indicates that the search process should no longer be continued.</param>
    /// <returns>True if blacklisted otherwise false if all is stellar.</returns>
    Task<bool> ContainsAsync(string password, CancellationToken cancellationToken = default);
}

/// <summary>A provider for <see cref="NonCommonPasswordValidator"/> that contains a hard-coded list of blacklisted passwords that the user cannot use.</summary>
public class DefaultPasswordBlacklistProvider : IPasswordBlacklistProvider
{
    /// <summary>Gets a list containing passwords to blacklist.</summary>
    protected HashSet<string> Blacklist { get; } = new HashSet<string> {
        "12345", "123456", "123456789", "test1", "password", "12345678", "zinch", "g_czechout", "asdf", "qwerty", "1234567890", "1234567", "Aa123456.", "iloveyou", "1234", "abc123", "111111",
        "123123", "dubsmash", "test", "princess", "qwertyuiop", "sunshine", "BvtTest123", "11111", "letmein", "football", "admin", "welcome", "monkey", "login", "starwars", "dragon", "passw0rd",
        "master", "hello", "freedom", "whatever", "qazwsx", "trustno1", "654321", "jordan23", "harley", "password1", "666666", "!@#$%^&*", "charlie", "aa123456", "donald", "google", "facebook"
    };

    /// <inheritdoc/>
    public Task<bool> ContainsAsync(string password, CancellationToken cancellationToken = default) => Task.FromResult(Blacklist.Contains(password));
}

/// <summary>A provider for <see cref="NonCommonPasswordValidator"/> that gets a list of blacklisted passwords from either 'IdentityOptions:Password:Blacklist' or 'PasswordOptions:Blacklist' option.</summary>
public class ConfigPasswordBlacklistProvider : IPasswordBlacklistProvider
{

    /// <summary>Gets a list containing passwords to blacklist.</summary>
    protected HashSet<string> Blacklist { get; }

    /// <inheritdoc/>
    public ConfigPasswordBlacklistProvider(IConfiguration configuration) {
        var list = configuration.GetSection($"{nameof(IdentityOptions)}:{nameof(IdentityOptions.Password)}:{nameof(Blacklist)}").Get<string[]>() ??
                   configuration.GetSection($"{nameof(PasswordOptions)}").GetValue<string[]>(nameof(Blacklist)) ??
                   Array.Empty<string>();
        Blacklist = new HashSet<string>(list);
    }

    /// <inheritdoc/>
    public Task<bool> ContainsAsync(string password, CancellationToken cancellationToken = default) => Task.FromResult(Blacklist.Contains(password));
}
