using Indice.Extensions;
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
        if (ValidateUpload(request.ContentMarkdown) is { } problem) {
            return problem;
        }
        
        var report = await pipeline.IngestAsync(new IngestRequest {
            OpenContentStream = () => request.ContentMarkdown.OpenReadStream(),
            OpenSourceStream = request.SourceFile is not null ? () => request.SourceFile.OpenReadStream() : null,
            Source = !string.IsNullOrWhiteSpace(request.SourceUrl) ? request.SourceUrl :
                      $"local://{request.SourceFile?.FileName ?? request.ContentMarkdown.FileName}",
            FileName = !string.IsNullOrWhiteSpace(request.SourceUrl) ? Path.GetFileName(request.SourceUrl) : request.SourceFile?.FileName ?? request.ContentMarkdown.FileName,
            ContentType = !string.IsNullOrWhiteSpace(request.SourceUrl) ? FileExtensions.GetMimeType( Path.GetExtension(request.SourceUrl)) : request.SourceFile?.ContentType ?? request.ContentMarkdown.ContentType,
            ContentLength = !string.IsNullOrWhiteSpace(request.SourceUrl) ? -1 : request.SourceFile?.Length ?? request.ContentMarkdown.Length,     
            Category = request.Category,
            Language = request.Language
        }, cancellationToken);
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
