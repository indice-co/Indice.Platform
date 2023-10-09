namespace Indice.Features.Identity.SignInLogs.Models;

/// <summary>Sign in log enricher type</summary>
[Flags]
public enum SignInLogEnricherRunType : byte
{
    /// <summary>Nothing specified</summary>
    Default = 1,
    /// <summary>Synchronously with http request</summary>
    Synchronous = 2,
    /// <summary>Asynchronously out of http context</summary>
    Asynchronous = 4
}
