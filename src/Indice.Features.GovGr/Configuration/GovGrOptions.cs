namespace Indice.Features.GovGr.Configuration;

/// <summary>The Kyc settings.</summary>
public class GovGrOptions
{
    /// <summary>The name of the AppSetting</summary>
    public const string Name = "GovGr";
    
    /// <summary>UseMockServices</summary>
    public bool UseMockServices { get; set; }
    /// <summary>KYC service options</summary>
    public KycOptions Kyc { get; set; } = new KycOptions();
    /// <summary>Wallet service options</summary>
    public WalletOptions Wallet { get; set; } = new WalletOptions();
    /// <summary>Documents service options</summary>
    public DocumentsOptions Documents { get; set; } = new DocumentsOptions();
    /// <summary>Business Registry service options</summary>
    public BusinessRegistryOptions BusinessRegistry { get; set; } = new BusinessRegistryOptions();
    /// <summary>Bancapp options</summary>
    public BancappOptions Bancapp { get; set; } = new BancappOptions();


    /// <summary>KYC service options</summary>
    public class KycOptions
    {
        /// <summary>Represents the environment. Valid options are <em>production</em>, <em>staging</em>, <em>development</em> &amp; <em>mock</em>. Defaults to <b>production</b>. </summary>
        public string Environment { get; set; }
        /// <summary>ClientId</summary>
        public string ClientId { get; set; }

        /// <summary>ClientSecret</summary>
        public string ClientSecret { get; set; }

        /// <summary>RedirectUri the callback url that ends the flow. This is different per client</summary>
        public string RedirectUri { get; set; }

        /// <summary>Check if in production</summary>
        public bool IsProduction => string.IsNullOrEmpty(Environment) || "Production".Equals(Environment, System.StringComparison.OrdinalIgnoreCase);
        /// <summary>Check if in staging/stage</summary>
        public bool IsStaging => "Staging".Equals(Environment, System.StringComparison.OrdinalIgnoreCase) || "Stage".Equals(Environment, System.StringComparison.OrdinalIgnoreCase);
        /// <summary>Check if in development/demo</summary>
        public bool IsDevelopment => "Development".Equals(Environment, System.StringComparison.OrdinalIgnoreCase) || "demo".Equals(Environment, System.StringComparison.OrdinalIgnoreCase);
        /// <summary>Check if in development/demo</summary>
        public bool IsMock => "mock".Equals(Environment, System.StringComparison.OrdinalIgnoreCase);
    }


    /// <summary>Wallet settings</summary>
    public class WalletOptions
    {
        /// <summary>Uses sandbox url and test data.</summary>
        public bool Sandbox { get; set; }
        /// <summary>The service token. Api key</summary>
        public string Token { get; set; }
    }

    /// <summary>Documents settings</summary>

    public class DocumentsOptions
    {

        /// <summary>Uses sandbox url and test data.</summary>
        public string ServiceName { get; set; }
        /// <summary>The service token. Api key</summary>
        public string Token { get; set; }
    }

    /// <summary>Business Registry settings</summary>
    public class BusinessRegistryOptions
    {
        /// <summary>Indicates whether the BusinessRegistry feature is enabled. Default value is false.</summary>
        public bool Enabled { get; set; } 
        /// <summary>BaseAddress</summary>
        public string BaseAddress { get; set; } = "https://www1.gsis.gr:443/webtax2/wsgsis/RgWsPublic/RgWsPublicPort";
        /// <summary>Username</summary>
        public string Username { get; set; }
        /// <summary>Password</summary>
        public string Password { get; set; }
        /// <summary>Callers Fiscal Code</summary>
        public string CallersFiscalCode { get; set; }
    }

    /// <summary>Bancapp settings</summary>
    public class BancappOptions
    {
        /// <summary>Represents the environment. Valid options are <em>production</em>, <em>staging</em>, <em>development</em> &amp; <em>mock</em>.Defaults to <b>stage</b>. </summary>
        public string Environment { get; set; }
        
        /// <summary>Username. Used to authenticate to Bancapp API.</summary>
        public string Username { get; set; }
        
        /// <summary>Password. Used to authenticate to Bancapp API.</summary>
        public string Password { get; set; }
        
        /// <summary>The ropc-client-id. Used to authenticate to Bancapp API.</summary>
        public string ClientId { get; set; }

        /// <summary>Check if in production</summary>
        public bool IsProduction => "Production".Equals(Environment, System.StringComparison.OrdinalIgnoreCase);

        /// <summary>Check if in staging/stage. This is the <b>default</b> option.</summary>
        public bool IsStaging => string.IsNullOrEmpty(Environment) ||
                                 "Staging".Equals(Environment, System.StringComparison.OrdinalIgnoreCase) ||
                                 "Stage".Equals(Environment, System.StringComparison.OrdinalIgnoreCase);

        /// <summary>Check if in development/demo</summary>
        public bool IsMock => "mock".Equals(Environment, System.StringComparison.OrdinalIgnoreCase);
        
        
    }
}