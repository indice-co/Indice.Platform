namespace Indice.Features.Identity.UI.Telemetry;

/// <summary>
/// Represents factory used to generate Application Insights JavaScript snippet with dependency injection support.
/// </summary>
public interface ITelemetryJavaScriptSnippet
{
    /// <summary>
    /// Gets a JavaScript code snippet including the 'script' tag.
    /// </summary>
    /// <returns>JavaScript code snippet.</returns>
    string FullScript { get; }
    /// <summary>
    /// Gets a JavaScript code snippet not including the 'script' tag.
    /// </summary>
    /// <returns>JavaScript code snippet.</returns>
    string ScriptBody { get; }
}