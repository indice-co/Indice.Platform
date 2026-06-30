using System.Security.Cryptography;
using System.Text;
using Indice.Features.Agents.Core.Workflows;

namespace Indice.Features.Agents.Core.Tests;

public class FaqIngestionLoopTests
{
    [Fact]
    public void FaqIngestionLoopTest() {
        var chunks = FaqIngestionLoop(new MemoryStream(Encoding.UTF8.GetBytes(SAMPLE_FAQ_BODY)));
        Assert.Equal(5, chunks.Count);
    }
    [Fact]
    public void FaqIngestionLoopFromFileTest() {
        var chunks = FaqIngestionLoop(File.OpenRead(Path.Combine(Directory.GetCurrentDirectory(), "FAQ.md")));
        Assert.Equal(20, chunks.Count);
    }

    private List<DocumentChunk> FaqIngestionLoop(Stream stream) {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
        var body = reader.ReadToEnd();
        var chunks = new List<DocumentChunk>();
        string? firstCategory = null;
        string? currentCategory = null;
        string? pendingQuestion = null;
        var pendingAnswer = new StringBuilder();
        var chunkIndex = 0;
        foreach (var line in body.Split(['\n', '\r'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)) {

            if (line.StartsWith("## ", StringComparison.Ordinal)) {
                Flush();
                pendingQuestion = line[3..].TrimStart();
                continue;
            }
            if (line.StartsWith("# ", StringComparison.Ordinal)) {
                Flush();
                currentCategory = line[2..].TrimStart();
                firstCategory ??= currentCategory;
                continue;
            }
            // Lines before any `##` are silently discarded.
            if (pendingQuestion is null) {
                continue;
            }
            if (pendingAnswer.Length > 0) {
                pendingAnswer.Append('\n');
            }
            pendingAnswer.Append(line);
        }
        Flush();
        void Flush() {
            if (string.IsNullOrWhiteSpace(pendingQuestion)) {
                pendingQuestion = null;
                pendingAnswer.Clear();
                return;
            }
            var answer = pendingAnswer.ToString().Trim();
            if (answer.Length == 0) {
                pendingQuestion = null;
                pendingAnswer.Clear();
                return;
            }
            var embedded = $"Q: {pendingQuestion}\nA: {answer}";
            var headingPath = string.IsNullOrEmpty(currentCategory)
                ? pendingQuestion!
                : $"{currentCategory} > {pendingQuestion}";
            chunks.Add(new DocumentChunk {
                ChunkIndex = chunkIndex++,
                Content = embedded,
                ContentHash = Sha256Hex(embedded),
                HeadingPath = headingPath,
                Title = pendingQuestion,
                Category = currentCategory,
                TokenCount = 0,
            });
            pendingQuestion = null;
            pendingAnswer.Clear();
        }

        return chunks;
    }

    private static string Sha256Hex(string input) {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }

    public static readonly string SAMPLE_FAQ_BODY = """
        ## Question: How do branding, theming, localization, and multi-tenancy work?
        Answer: The UI is a Razor Class Library where every page is overrideable from the host project file-by-file (drop a file in to win, no fork). You can switch the entire UI framework between Bootstrap5 and Tailwind (full parallel page trees) via `[UIFramework]`. Per-client white-labelling uses `ClientThemeAttribute` + `ClientThemingService` + per-client `ClientThemeConfig`, and the `ClientUser` entity provides client-level RBAC (operators bound to specific OIDC clients). Localized resources ship for Greek, Spanish, Italian, Japanese, German, French, and Portuguese, with English as base; pre-built authorization policies (`BeAdmin`, `BeUsersReader`, `BeClientsWriter`) compose scope + role. One deployment can host many branded clients.

        ## Question: What is the AdminUI and how is it secured?
        Answer: The AdminUI is an Angular 20.3 operator SPA mounted at `/admin`, built with ngx-charts (sign-in dashboards and geo visualisation), ngx-datatable, jsonforms (dynamic forms), and CKEditor (legal text). It manages users, roles, clients, API resources, scopes, sign-in logs, activity logs, and runtime settings. Critically, it is itself an OIDC client (`idsrv-admin-ui`) of the system it manages, with scoped access (`identity`, `identity:clients`, `identity:users`, `identity:logs`) — so operators sign in through the same MFA, device-trust, and sign-in-log flows as end users. It is themeable via `wwwroot/css/admin-ui-overrides.css`.

        ## Question: How is the system configured, and why so many databases?
        Answer: Almost everything that can be a setting is a setting, organised into config sections: `IdentityOptions` (password, lockout, sign-in, claims, user, device policies), `IdentityServer` (endpoints, features, MFA, rate limiter, avatar, email, signing), `Recaptcha`, `Csp`, and delivery providers (`Email`/`Sms`/`Totp`/`PushNotifications`). `ConnectionStrings` defines five distinct databases — Identity, Configuration, Operational, SignInLogs, and ActivityLogs — to isolate blast radius and let you target tier-appropriate storage and SLAs. A runtime configuration API (`AppSettings`/`SettingHandlers`, enabled with `.AddDatabaseSettingEndpoints()`) lets operators tune password/MFA/phone/claim rules without a redeploy, with every change audited via ActivityLogs.

        ## Question: What password and lockout policy options are available?
        Answer: The password policy is a composable rule set: `RequiredLength`, `RequireDigit`/`RequireUppercase`/`RequireLowercase`/`RequireNonAlphanumeric`, `RequiredUniqueChars`, `PasswordHistoryLimit` (defeats trivial rotation), `PasswordExpirationPolicy` (`Never`, `NextLogin`, `Monthly`, `Quarterly`, `Semesterly`, `Annually`, `Biannually`), a `Blacklist`, and `MaxAllowedUserNameSubset` (rejects passwords containing a slice of the username). Lockout options include `AllowedForNewUsers`, `MaxFailedAccessAttempts` (default 5), and `DefaultLockoutTimeSpan` (default 5 minutes) — strict enough to slow online attacks, lenient enough not to permanently lock out distracted users.

        ## Question: Where does Indice IAM shine compared with Amazon Cognito and Azure AD B2C?
        Answer: On three axes especially. **Cost** — the software is free (MIT open source) with no per-MAU fees; you pay Indice for integration and support, and inactive users cost zero. **Sovereignty** — your database, your cloud, your key material, versus AWS-only or Azure-only with provider-managed keys. **Debuggability** — it's an open .NET stack you can step-through-debug (attach, set a breakpoint, reproduce a "cannot log in" report) rather than reading docs and filing a support ticket against a closed service. It also ships built-in capabilities others leave as DIY/custom: push-approval MFA, Viber OTP, device-trust with activation delay, the identity validation pipeline, impossible-travel detection, per-client theming, `ClientUser` RBAC, and a runtime configuration API.
        """;
}
