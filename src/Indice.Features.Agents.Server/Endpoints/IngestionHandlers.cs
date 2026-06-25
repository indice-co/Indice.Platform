using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Models.Requests;
using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Core.Workflows;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Agents.Server.Endpoints;

/// <summary>Logic-free handlers for the IngestionApi.</summary>
internal static class IngestionHandlers
{
    /// <summary>Ingests an uploaded markdown document and returns an ingestion report.</summary>
    public static async Task<Results<Ok<IngestionReport>, ValidationProblem>> DocumentIngest( IIngestionPipeline pipeline,
        CancellationToken cancellationToken, [FromForm] DocumentIngestRequest request) {
        if (ValidateUpload(request.File) is { } problem) {
            return problem;
        }
        await using var stream = request.File.OpenReadStream();
        var report = await pipeline.IngestAsync(stream, request.File.FileName, request.Category, request.Language, cancellationToken);
        return TypedResults.Ok(report);
    }

    private static ValidationProblem? ValidateUpload(IFormFile file) {
        if (file is null || file.Length == 0) {
            return TypedResults.ValidationProblem(ValidationErrors.AddError("file", "File is empty."));
        }
        if (!file.FileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase)) {
            return TypedResults.ValidationProblem(ValidationErrors.AddError("file", "Only .md files are supported."));
        }
        return null;
    }

    public static async Task<Results<NoContent, ValidationProblem>> Clear(IDocumentsService store, CancellationToken cancellationToken) {
        await store.ClearAsync();
        return TypedResults.NoContent();
    }
}
