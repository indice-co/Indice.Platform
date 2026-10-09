using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Features.Recaptcha;

/// <summary>Implementation of the reCAPTCHA validation service.</summary>
public class HCaptchaService : IRecaptchaService
{
    private const string HCaptchaVerifyUrl = "https://api.hcaptcha.com/siteverify";

    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly RecaptchaOptions _options;
    private readonly ILogger<HCaptchaService> _logger;

    /// <summary>Creates a new instance of <see cref="HCaptchaService"/>.</summary>
    public HCaptchaService(
        IHttpClientFactory httpClientFactory,
        IOptions<RecaptchaOptions> options,
        ILogger<HCaptchaService> logger) {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public CaptchaProviderType Provider => CaptchaProviderType.HCaptcha;

    /// <inheritdoc/>
    public bool IsEnabled => _options.Provider == CaptchaProviderType.HCaptcha
                          && !string.IsNullOrWhiteSpace(_options.SiteKey)
                          && !string.IsNullOrWhiteSpace(_options.SecretKey);
    /// <inheritdoc/>
    public bool IsEnabledInLogin => _options.EnabledInLoginPage;
    /// <inheritdoc/>
    public decimal ScoreThreshold => _options.ScoreThreshold;

    /// <inheritdoc/>
    public string? SiteKey => _options.SiteKey;

    /// <inheritdoc/>
    public string? SiteKeyV2 => _options.EffectiveSiteKeyV2;

    /// <inheritdoc/>
    public async Task<RecaptchaValidationResult> ValidateAsync(string? token, string? version = "HCaptcha", string? remoteIp = null, CancellationToken cancellationToken = default) {
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
        var secretKey = _options.SecretKey;
        var verifyUrl = HCaptchaVerifyUrl;

        if (string.IsNullOrWhiteSpace(secretKey) || string.IsNullOrWhiteSpace(verifyUrl)) {
            _logger.LogWarning("reCAPTCHA validation failed - provider not configured correctly.");
            return new RecaptchaValidationResult { Success = false, Score = 0.0m };
        }

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
            using var content = new FormUrlEncodedContent(formData);
            var response = await httpClient.PostAsync(
                verifyUrl,
                content,
                cancellationToken
            );

            var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<HCaptchaResponse>(jsonResponse, JsonOptions);

            if (result is null) {
                _logger.LogError("Failed to deserialize reCAPTCHA response: {Response}", jsonResponse);
                return new RecaptchaValidationResult { Success = false, Score = 0.0m };
            }

            var score = (decimal)result.Score;

            if (!result.Success) {
                _logger.LogWarning("hCAPTCHA validation failed. Error codes: {ErrorCodes}",
                    string.Join(", ", result.ErrorCodes ?? []));
            }

            return new RecaptchaValidationResult {
                Success = result.Success,
                Score = score,
                RequiresV2Fallback = false,
                ErrorCodes = result.ErrorCodes?.ToList(),
                Action = result.Action
            };
        } catch (HttpRequestException ex) {
            _logger.LogError(ex, "HTTP error occurred during hCAPTCHA validation.");
            return new RecaptchaValidationResult { Success = false, Score = 0.0m };
        } catch (TaskCanceledException ex) {
            _logger.LogError(ex, "hCAPTCHA validation request timed out or was canceled.");
            return new RecaptchaValidationResult { Success = false, Score = 0.0m };
        } catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken) {
            _logger.LogWarning(ex, "hCAPTCHA validation was canceled by the caller.");
            return new RecaptchaValidationResult { Success = false, Score = 0.0m };
        } catch (JsonException ex) {
            _logger.LogError(ex, "Failed to parse hCAPTCHA validation response.");
            return new RecaptchaValidationResult { Success = false, Score = 0.0m };
        }
    }

    private sealed class HCaptchaResponse
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
