using Indice.Features.Identity.Core.Logging.Models;

namespace Indice.Features.Identity.Core.Logging.Enrichers;

/// <summary></summary>
public interface ISignInLogEntryEnricher
{
    /// <summary></summary>
    /// <param name="logEntry"></param>
    Task Enrich(SignInLogEntry logEntry);
}
