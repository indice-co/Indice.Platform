using System.Text;

namespace Indice.Features.Identity.UI.Models;

/// <summary>View model for the MFA onboarding recovery codes page.</summary>
public class RecoveryCodesViewModel
{
    /// <summary>The one-time recovery codes generated after enabling the authenticator app.</summary>
    public string[] RecoveryCodes { get; set; } = [];
    /// <summary>The user name the codes belong to. Used for the downloaded file header.</summary>
    public string? UserName { get; set; }
    /// <summary>The return URL to continue to once the user acknowledges the codes.</summary>
    public string? ReturnUrl { get; set; }

    /// <summary>
    /// Generates a string representation of Recovery codes. Ready to be displayed or downloaded.
    /// </summary>
    /// <param name="applicationName">The name of the application to include in the header.</param>
    /// <param name="headerText">Optional header text to display before the codes.</param>
    /// <returns>A string representation of the recovery codes.</returns>
    public string ToString(string? applicationName, string? headerText) {
        var builder = new StringBuilder();
        if (!string.IsNullOrEmpty(applicationName)) {
            builder.AppendLine(applicationName);
        }
        builder.AppendLine(headerText ?? "Recovery Codes:");
        builder.AppendLine(DateTime.UtcNow.ToLongDateString());
        builder.AppendLine("----------");
        builder.AppendLine(string.Join(Environment.NewLine, RecoveryCodes));
        builder.AppendLine("----------");
        return builder.ToString();
    }

    /// <summary>
    /// Generates a string representation of Recovery codes. Ready to be displayed or downloaded.
    /// </summary>
    /// <returns>A string representation of the recovery codes.</returns>
    public override string ToString() => ToString(null, null);
}
