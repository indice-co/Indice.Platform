namespace Indice.Hosting.Diagnostics;

/// <summary>The names of the diagnostics the workers publish.</summary>
public static class WorkerDiagnostics
{
    /// <summary>
    /// The name of the <see cref="System.Diagnostics.Metrics.Meter"/> of the workers. Subscribe to it with OpenTelemetry
    /// (<c>metrics.AddMeter(WorkerDiagnostics.MeterName)</c>) or watch it with <c>dotnet-counters monitor --counters Indice.Hosting</c>.
    /// </summary>
    public const string MeterName = "Indice.Hosting";
}
