using System.Security.Claims;
using FubarDev.FtpServer.AccountManagement;
using Microsoft.Extensions.Options;

namespace Indice.Features.FtpServer;

/// <summary>
/// An implementation of <see cref="IMembershipProviderAsync"/> that validates user credentials against a predefined list of username-password pairs specified in the <see cref="FtpServerCredentialsOptions"/>. This provider is suitable for simple scenarios where user credentials are managed in-memory and does not require integration with external authentication systems.
/// </summary>
public class FtpServerCredentialsMembershipProvider : IMembershipProviderAsync
{
    private readonly IOptions<FtpServerCredentialsOptions> _options;
    /// <summary>
    /// Initializes a new instance of the FtpServerCredentialsMembershipProvider class using the specified options. 
    /// </summary>
    /// <param name="options">The options used to configure the FTP server credentials provider. Cannot be null.</param>
    public FtpServerCredentialsMembershipProvider(IOptions<FtpServerCredentialsOptions> options) {
        _options = options;
    }

    /// <inheritdoc />
    public Task LogOutAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default) {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<MemberValidationResult> ValidateUserAsync(string username, string password, CancellationToken cancellationToken) {
        var credential = await ValidateCredentialsAsync(username, password, cancellationToken);
        if (credential == null) {
            return new MemberValidationResult(MemberValidationStatus.InvalidLogin);
        }
        var user = credential.ToClaimsPrincipal("custom");

        return new MemberValidationResult(MemberValidationStatus.AuthenticatedUser, user);

    }

    /// <inheritdoc />
    public Task<MemberValidationResult> ValidateUserAsync(string username, string password) {
        return ValidateUserAsync(username, password, default);
    }

    /// <summary>
    /// Asynchronously validates the specified username and password against the configured credentials.
    /// </summary>
    /// <param name="username">The username to validate. Cannot be null.</param>
    /// <param name="password">The password to validate. Cannot be null.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="FtpSymmetricCredentials"/> if the
    /// credentials are valid; otherwise, <see langword="null"/>.</returns>
    protected virtual Task<FtpSymmetricCredentials?> ValidateCredentialsAsync(string username, string password, CancellationToken cancellationToken) {
        var credential = _options.Value.Credentials.Values.FirstOrDefault(c => c.Username == username && c.Password == password);
        return Task.FromResult(credential);
    }
}

/// <summary>
/// Represents configuration options for specifying a collection of FTP server credentials.
/// </summary>
/// <remarks>Use this class to provide one or more sets of credentials that can be used to authenticate with an
/// FTP server. This is typically used when configuring FTP client or server components that require credential
/// management.</remarks>
public class FtpServerCredentialsOptions
{
    /// <summary>
    /// Gets or sets the collection of FTP symmetric credentials, indexed by user name.
    /// </summary>
    /// <remarks>Each entry in the dictionary associates a user name with its corresponding FTP symmetric
    /// credentials. Modifying this collection affects authentication for FTP operations that use these
    /// credentials.</remarks>
    public Dictionary<string, FtpSymmetricCredentials> Credentials { get; set; } = new();
}

/// <summary>
/// Represents a set of symmetric credentials used for authenticating with an FTP server.
/// </summary>
/// <param name="Username">The user name to use when connecting to the FTP server. Cannot be null or empty.</param>
/// <param name="Password">The password associated with the specified user name. Cannot be null or empty.</param>
/// <param name="Roles">An optional comma-separated list of roles associated with the user. This can be used for role-based authorization when accessing FTP server resources. Can be null.</param>
public record FtpSymmetricCredentials(string Username, string Password, string? Roles = null) {

    /// <summary>
    /// Gets the roles associated with the user as an array of strings. 
    /// If the Roles property is null, empty, or consists only of whitespace, this method returns an array containing the username and a default "user" role. 
    /// Otherwise, it splits the Roles string by commas and trims any whitespace from each role before returning the array of roles.
    /// </summary>
    /// <returns>An array of strings representing the roles associated with the user.</returns>
    public string[] GetRoles() {
        if (string.IsNullOrWhiteSpace(Roles)) {
            return [Username, "user"];
        }
        return Roles.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Converts the current instance of <see cref="FtpSymmetricCredentials"/> to a <see cref="ClaimsPrincipal"/> that can be used for authentication and authorization purposes. 
    /// The resulting <see cref="ClaimsPrincipal"/> will contain claims for the user's name and roles based on the properties of the current instance. 
    /// The authentication type can be specified as an optional parameter, defaulting to "custom" if not provided.
    /// </summary>
    /// <param name="authenticationType"></param>
    /// <returns>The constructed principal</returns>
    public ClaimsPrincipal ToClaimsPrincipal(string authenticationType = "custom") => new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimsIdentity.DefaultNameClaimType, Username),
            .. GetRoles().Select(role => new Claim(ClaimsIdentity.DefaultRoleClaimType, role)),
        ],
        authenticationType));
}
