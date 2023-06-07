namespace Indice.Features.Identity.SignInLogs.Models;

/// <summary>Signin log enricher type</summary>
[Flags]
public enum SignInLogEnricherRunType : byte
{
    /// <summary>Nothing specified</summary>
    Default = 1,
    /// <summary>Synchronously with http request</summary>
    Synchronous = 2,
    /// <summary>Assynchronously out of http context</summary>
    Asynchronous = 4
}
