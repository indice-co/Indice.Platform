namespace Indice.AspNetCore.Features.Recaptcha;

/// <summary>Configuration options for reCAPTCHA.</summary>
public class RecaptchaOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Recaptcha";

    /// <summary>The captcha provider. Defaults to <see cref="CaptchaProviderType.None"/>.</summary>
    public CaptchaProviderType Provider { get; set; } = CaptchaProviderType.None;

    /// <summary>The reCAPTCHA v3 site key.</summary>
    public string? SiteKey { get; set; }

    /// <summary>The reCAPTCHA v3 secret key.</summary>
    public string? SecretKey { get; set; }

    /// <summary>The reCAPTCHA v2 site key (optional, falls back to SiteKey if not provided).</summary>
    public string? SiteKeyV2 { get; set; }

    /// <summary>The reCAPTCHA v2 secret key (optional, falls back to SecretKey if not provided).</summary>
    public string? SecretKeyV2 { get; set; }

    /// <summary>The minimum score threshold for v3 (0.0 to 1.0). Default is 0.5.</summary>
    public decimal ScoreThreshold { get; set; } = 0.5m;

    /// <summary>Gets the effective v2 site key.</summary>
    public string? EffectiveSiteKeyV2 => string.IsNullOrWhiteSpace(SiteKeyV2) ? SiteKey : SiteKeyV2;

    /// <summary>Gets the effective v2 secret key.</summary>
    public string? EffectiveSecretKeyV2 => string.IsNullOrWhiteSpace(SecretKeyV2) ? SecretKey : SecretKeyV2;

    /// <summary>Whether to show the reCAPTCHA widget on the login page. Default is true.</summary>
    public bool EnabledInLoginPage { get; set; } = true;
}

/// <summary>Supported captcha providers.</summary>
public enum CaptchaProviderType
{
    /// <summary>No provider configured.</summary>
    None = 0,

    /// <summary>ReCAPTCHA provider.</summary>
    Recaptcha = 1,

    /// <summary>hCaptcha provider.</summary>
    HCaptcha = 2
}