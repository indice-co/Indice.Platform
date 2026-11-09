namespace Indice.Features.ActivityLogs.Models;

/// <summary>activity log enricher type</summary>
[Flags]
public enum ActivityLogEnricherRunType : byte
{
    /// <summary>Nothing specified</summary>
    Default = 1,
    /// <summary>Synchronously with http request</summary>
    Synchronous = 2,
    /// <summary>Asynchronously out of http context</summary>
    Asynchronous = 4
}
