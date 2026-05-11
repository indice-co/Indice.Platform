using System.Net;
using FubarDev.FtpServer;
using FubarDev.FtpServer.AccountManagement.Anonymous;
using FubarDev.FtpServer.FileSystem.DotNet;
using Indice.Features.FtpServer;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="IFtpServerBuilder"/>.
/// </summary>
public static class FtpServerFeatureExtensions
{
    /// <summary>
    /// Runs the FTP server as a hosted service.
    /// </summary>
    /// <param name="builder">The server builder used to configure the FTP server.</param>
    /// <param name="configureAction">The configure action</param>
    /// <returns>The builder for further configuration</returns>
    public static IFtpServerBuilder RunAsHostedService(this IFtpServerBuilder builder, Action<FtpServerOptions>? configureAction = null) {
        if (configureAction is not null) {
            builder.Services.Configure(configureAction);
        }
        builder.Services.AddHostedService<FtpServerHostedService>();
        return builder;
    }

    /// <summary>
    /// Uses the .NET file system API.
    /// </summary>
    /// <param name="builder">The server builder used to configure the FTP server.</param>
    /// <param name="configureAction">The configure action</param>
    /// <returns>The builder for further configuration</returns>
    /// <returns></returns>
    public static IFtpServerBuilder UseDotNetFileSystem(this IFtpServerBuilder builder, Action<DotNetFileSystemOptions> configureAction) {
        builder.Services.Configure(configureAction);
        builder.UseDotNetFileSystem();
        return builder;
    }

    /// <summary>
    /// Enables anonymous authentication for the FTP server and allows configuration of anonymous authentication
    /// options.
    /// </summary>
    /// <remarks>Call this method to allow clients to connect using anonymous credentials. Use the <paramref
    /// name="configureAction"/> parameter to specify additional options, such as whether any password is accepted for
    /// anonymous users.</remarks>
    /// <param name="builder">The FTP server builder to configure. Cannot be null.</param>
    /// <param name="configureAction">An action to configure the anonymous authentication options. Cannot be null.</param>
    /// <returns>The same instance of <see cref="IFtpServerBuilder"/> to allow for method chaining.</returns>
    public static IFtpServerBuilder EnableAnonymousAuthentication(this IFtpServerBuilder builder, Action<FtpServerAnonymousAuthenticationOptions> configureAction) {
        var options = new FtpServerAnonymousAuthenticationOptions(builder.Services);
        configureAction(options);
        if (options.AllowAnyPassword) {
            builder.Services.AddSingleton<IAnonymousPasswordValidator, NotEmptyPasswordValidation>(sp => new NotEmptyPasswordValidation(options.MinimumPasswordLength));
        }
        builder.EnableAnonymousAuthentication();
        return builder;
    }

    /// <summary>
    /// Enables passive address resolution for the FTP server and allows configuration of passive mode options.
    /// </summary>
    /// <remarks>This method configures the FTP server to resolve passive addresses and enables promiscuous
    /// passive mode. Use this extension method to customize how the server handles passive connections, especially in
    /// environments with complex network configurations.</remarks>
    /// <param name="builder">The FTP server builder to configure.</param>
    /// <param name="configureAction">An action to configure the passive mode options. This action is invoked to customize settings related to passive
    /// address resolution.</param>
    /// <returns>The same instance of <see cref="IFtpServerBuilder"/> to allow for method chaining.</returns>
    public static IFtpServerBuilder UsePassiveAddressResolution(this IFtpServerBuilder builder, Action<SimplePasvOptions>? configureAction = null) {
        //https://github.com/FubarDevelopment/FtpServer/issues/140
        builder.Services.Configure(configureAction ?? new Action<SimplePasvOptions>((options) => {
            options.PasvMinPort = 49152;
            options.PasvMaxPort = 49153;
            options.PublicAddress = null;
        }));
        builder.Services.Configure<PasvCommandOptions>(options => {
            options.PromiscuousPasv = true;
        }); 
        return builder;
    }
}

/// <summary>
/// Options for configuring anonymous authentication for the FTP server.
/// </summary>
public class FtpServerAnonymousAuthenticationOptions
{
    internal FtpServerAnonymousAuthenticationOptions(IServiceCollection services) {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }
    internal IServiceCollection Services { get; }
    /// <summary>
    /// Gets or sets a value indicating whether any password is accepted during authentication.
    /// </summary>
    public bool AllowAnyPassword { get; set; }

    /// <summary>
    /// Gets or sets the minimum length for passwords during authentication. This option is used to enforce a minimum length requirement for passwords when <see cref="AllowAnyPassword"/> is set to true. If a password does not meet the specified minimum length, authentication will fail.
    /// </summary>
    public int MinimumPasswordLength { get; set; } = 3;

    /// <summary>
    /// Configures a custom password validator for anonymous authentication. The specified validator will be used to 
    /// validate passwords for anonymous users.
    /// </summary>
    /// <typeparam name="TAnonymousPasswordValidator">The type of the custom password validator.</typeparam>
    /// <returns>The same instance of <see cref="FtpServerAnonymousAuthenticationOptions"/> to allow for method chaining.</returns>
    public FtpServerAnonymousAuthenticationOptions WithPasswordValidator<TAnonymousPasswordValidator>() where TAnonymousPasswordValidator : class, IAnonymousPasswordValidator {
        Services.AddSingleton<IAnonymousPasswordValidator, TAnonymousPasswordValidator>();
        return this;
    }
}

/// <summary>
/// Performs no validation but guards against empty passwords.
/// </summary>
public class NotEmptyPasswordValidation : IAnonymousPasswordValidator
{
    private readonly int _minimumLength;

    /// <summary>
    ///  Initializes a new instance of the NotEmptyPasswordValidation class with the specified minimum password length.
    /// </summary>
    /// <param name="minimumLength">The minimum number of characters required for a valid password. Must be greater than zero.</param>
    public NotEmptyPasswordValidation(int minimumLength) {
        _minimumLength = minimumLength;
    }

    /// <inheritdoc/>
    public bool IsValid(string password) {
        if (password.IndexOfAny(['/', '\\']) != -1) {
            return false;
        }
        if (string.IsNullOrWhiteSpace(password)) {
            return false;
        }
        if (password.Length < _minimumLength) {
            return false;
        }
        return true;
    }
}