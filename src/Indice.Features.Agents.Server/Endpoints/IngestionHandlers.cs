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
        if (ValidateUpload(request) is { } problem) {
            return problem;
        }

        var filename = request.ActualSourceFile?.FileName ?? request.MarkdownSourceFile.FileName;
        if (!string.IsNullOrWhiteSpace(request.ActualSourceUrl) && Uri.TryCreate(request.ActualSourceUrl, UriKind.Absolute, out var actualSourceUri)) { 
            filename = Path.GetFileName(actualSourceUri.AbsolutePath);
        }
        var report = await pipeline.IngestAsync(new IngestRequest {
            OpenMarkdownSourceStream = () => request.MarkdownSourceFile.OpenReadStream(),
            OpenActualSourceStream = request.ActualSourceFile is not null ? () => request.ActualSourceFile.OpenReadStream() : null,
            Source = !string.IsNullOrWhiteSpace(request.ActualSourceUrl) ? request.ActualSourceUrl :
                      $"local://{filename}",
            FileName = filename,
            ContentType = !string.IsNullOrWhiteSpace(request.ActualSourceUrl) ? FileExtensions.GetMimeType(Path.GetExtension(filename)) : request.ActualSourceFile?.ContentType ?? request.MarkdownSourceFile.ContentType,
            ContentLength = !string.IsNullOrWhiteSpace(request.ActualSourceUrl) ? -1 : request.ActualSourceFile?.Length ?? request.MarkdownSourceFile.Length,     
            Category = request.Category,
            Language = request.Language,
            IsPrivate = request.IsPrivate ?? false

        }, cancellationToken);
        return TypedResults.Ok(report);
    }

    private static ValidationProblem? ValidateUpload(DocumentIngestRequest request) {
        var errors = ValidationErrors.Create();
        if (request.MarkdownSourceFile is null || request.MarkdownSourceFile.Length == 0) {
            errors.AddError(nameof(request.MarkdownSourceFile), "Markdown source file is required.");
        }
        if (request.MarkdownSourceFile is not null && !request.MarkdownSourceFile.FileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase)) {
            errors.AddError(nameof(request.MarkdownSourceFile), "Only .md files are supported.");
        }
        if (request.ActualSourceUrl is not null && !Uri.IsWellFormedUriString(request.ActualSourceUrl, UriKind.Absolute)) {
            errors.AddError(nameof(request.ActualSourceUrl), "Invalid URL format.");
        }
        if (request.ActualSourceFile is not null && request.ActualSourceUrl is not null) {
            errors.AddError(nameof(request.ActualSourceFile), "Either provide an actual source file or a source URL, not both.");
        }
        if (request.ActualSourceFile is not null && request.ActualSourceFile.Length == 0) {
            errors.AddError(nameof(request.ActualSourceFile), "The actual source file was provided but is empty.");
        }
        return errors.Count > 0 ? TypedResults.ValidationProblem(errors) : null;
    }

    public static async Task<Results<NoContent, ValidationProblem>> Clear(IDocumentsService store, CancellationToken cancellationToken) {
        await store.ClearAsync();
        return TypedResults.NoContent();
    }
}
