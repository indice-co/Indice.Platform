using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Features.Recaptcha;

/// <summary>Service for validating Google reCAPTCHA tokens.</summary>
/// <remarks>
/// This service handles both v3 (invisible, score-based) and v2 (checkbox) validation.
/// Typical flow:
/// 1. v3 token is generated invisibly and sent to /RecaptchaValidate for score check
/// 2. If score >= threshold: Token is re-validated during form POST (security best practice)
/// 3. If score &lt; threshold: v2 checkbox shown, user completes it, token validated during form POST
/// </remarks>
public interface IRecaptchaService
{
    /// <summary>Validates a reCAPTCHA token and returns the result.</summary>
    /// <param name="token">The reCAPTCHA token to validate.</param>
    /// <param name="version">The reCAPTCHA version (v2 or v3). Default is v3.</param>
    /// <param name="remoteIp">The user's IP address (optional but recommended for better bot detection).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The reCAPTCHA validation result.</returns>
    Task<RecaptchaValidationResult> ValidateAsync(string? token, string? version = "v3", string? remoteIp = null, CancellationToken cancellationToken = default);

    /// <summary>Checks if reCAPTCHA is enabled.</summary>
    bool IsEnabled { get; }
    /// <summary>Checks if reCAPTCHA is enabled in login form.</summary>
    bool IsEnabledInLogin { get; }

    /// <summary>Gets the configured score threshold.</summary>
    decimal ScoreThreshold { get; }

    /// <summary>Gets the site key for v3.</summary>
    string? SiteKey { get; }

    /// <summary>Gets the site key for v2.</summary>
    string? SiteKeyV2 { get; }
}

/// <summary>Implementation of the reCAPTCHA validation service.</summary>
public class RecaptchaService : IRecaptchaService
{
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly RecaptchaOptions _options;
    private readonly ILogger<RecaptchaService> _logger;

    /// <summary>Creates a new instance of <see cref="RecaptchaService"/>.</summary>
    public RecaptchaService(
        IHttpClientFactory httpClientFactory,
        IOptions<RecaptchaOptions> options,
        ILogger<RecaptchaService> logger) {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public bool IsEnabled => !string.IsNullOrWhiteSpace(_options.SiteKey) && !string.IsNullOrWhiteSpace(_options.SecretKey);
    /// <inheritdoc/>
    public bool IsEnabledInLogin => _options.EnabledInLoginPage;
    /// <inheritdoc/>
    public decimal ScoreThreshold => _options.ScoreThreshold;

    /// <inheritdoc/>
    public string? SiteKey => _options.SiteKey;

    /// <inheritdoc/>
    public string? SiteKeyV2 => _options.EffectiveSiteKeyV2;

    /// <inheritdoc/>
    public async Task<RecaptchaValidationResult> ValidateAsync(string? token, string? version = "v3", string? remoteIp = null, CancellationToken cancellationToken = default) {
        if (!IsEnabled) {
            _logger.LogDebug("reCAPTCHA validation skipped - not configured.");
            return new RecaptchaValidationResult { Success = true, Score = 1.0m };
        }

        if (string.IsNullOrWhiteSpace(token)) {
            _logger.LogWarning("reCAPTCHA validation failed - no token provided.");
            return new RecaptchaValidationResult {
                Success = false,
                Score = 0.0m,
                ErrorCodes = ["missing-input-response"]
            };
        }

        // Determine which version and use appropriate secret key
        var isV2 = string.Equals(version, "v2", StringComparison.OrdinalIgnoreCase);
        var secretKey = isV2 ? _options.EffectiveSecretKeyV2 : _options.SecretKey;

        try {
            var httpClient = _httpClientFactory.CreateClient();
            var formData = new Dictionary<string, string>
            {
                { "secret", secretKey! },
                { "response", token }
            };

            if (!string.IsNullOrWhiteSpace(remoteIp)) {
                formData["remoteip"] = remoteIp;
            }
            var content = new FormUrlEncodedContent(formData);
            var response = await httpClient.PostAsync(
                "https://www.google.com/recaptcha/api/siteverify",
                content,
                cancellationToken
            );

            var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<GoogleRecaptchaResponse>(jsonResponse, JsonOptions);

            if (result is null) {
                _logger.LogError("Failed to deserialize reCAPTCHA response: {Response}", jsonResponse);
                return new RecaptchaValidationResult { Success = false, Score = 0.0m };
            }

            // Calculate score: v2 returns binary success/fail (1.0 or 0.0), v3 returns score 0.0-1.0
            var score = isV2 ? (result.Success ? 1.0m : 0.0m) : (decimal)result.Score;

            // v3 requires v2 fallback if score is below configured threshold
            var requiresV2Fallback = !isV2 && result.Success && score < _options.ScoreThreshold;

            if (!result.Success) {
                _logger.LogWarning("reCAPTCHA validation failed. Error codes: {ErrorCodes}",
                    string.Join(", ", result.ErrorCodes ?? []));
            } else if (requiresV2Fallback) {
                _logger.LogInformation("reCAPTCHA v3 score {Score} below threshold {Threshold}, requiring v2 fallback.",
                    score, _options.ScoreThreshold);
            }

            return new RecaptchaValidationResult {
                Success = result.Success,
                Score = score,
                RequiresV2Fallback = requiresV2Fallback,
                ErrorCodes = result.ErrorCodes?.ToList(),
                Action = result.Action
            };
        } catch (HttpRequestException ex) { 
            _logger.LogError(ex, "HTTP error occurred during reCAPTCHA validation.");
            return new RecaptchaValidationResult { Success = false, Score = 0.0m };
        } catch (TaskCanceledException ex) {
            _logger.LogError(ex, "reCAPTCHA validation request timed out or was canceled.");
            return new RecaptchaValidationResult { Success = false, Score = 0.0m };
        } catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken) {
            _logger.LogWarning(ex, "reCAPTCHA validation was canceled by the caller.");
            return new RecaptchaValidationResult { Success = false, Score = 0.0m };
        } catch (JsonException ex) {
            _logger.LogError(ex, "Failed to parse reCAPTCHA validation response.");
            return new RecaptchaValidationResult { Success = false, Score = 0.0m };
        }
    }

    private sealed class GoogleRecaptchaResponse
    {
        public bool Success { get; set; }
        public double Score { get; set; }
        public string? Action { get; set; }

        [JsonPropertyName("challenge_ts")]
        public DateTime? ChallengeTs { get; set; }

        public string? Hostname { get; set; }

        [JsonPropertyName("error-codes")]
        public string[]? ErrorCodes { get; set; }
    }
}

/// <summary>Result of reCAPTCHA validation.</summary>
public class RecaptchaValidationResult
{
    /// <summary>Whether the validation was successful.</summary>
    public bool Success { get; set; }

    /// <summary>
    /// The reCAPTCHA score (0.0 to 1.0).
    /// - For v3: Actual score from Google (e.g., 0.3, 0.7, 0.9)
    /// - For v2: Binary result - 1.0 if user passed challenge, 0.0 if failed
    /// </summary>
    public decimal Score { get; set; }

    /// <summary>
    /// Whether the v3 score is below the configured threshold, requiring v2 fallback.
    /// - Always false for v2 (v2 is the final fallback)
    /// - For v3: true if Success=true but Score &lt; ScoreThreshold
    /// </summary>
    public bool RequiresV2Fallback { get; set; }

    /// <summary>Error codes if validation failed (from Google's API response).</summary>
    public List<string>? ErrorCodes { get; set; }

    /// <summary>The action name returned by v3 and is related to the user form action (e.g., 'login', 'register'). Null for v2.</summary>
    public string? Action { get; set; }
}