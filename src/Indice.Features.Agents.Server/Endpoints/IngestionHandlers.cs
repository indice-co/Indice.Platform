using System.Globalization;
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
        if (!string.IsNullOrWhiteSpace(request.ActualSourceUrl) && !Uri.IsWellFormedUriString(request.ActualSourceUrl, UriKind.Absolute)) {
            errors.AddError(nameof(request.ActualSourceUrl), "Invalid URL format.");
        }
        if (request.ActualSourceFile is not null && !string.IsNullOrWhiteSpace(request.ActualSourceUrl)) {
            errors.AddError(nameof(request.ActualSourceFile), "Either provide an actual source file or a source URL, not both.");
        }
        if (request.ActualSourceFile is not null && request.ActualSourceFile.Length == 0) {
            errors.AddError(nameof(request.ActualSourceFile), "The actual source file was provided but is empty.");
        }
        if (!string.IsNullOrWhiteSpace(request.Language) && !IsValidCulture(request.Language)) {
            errors.AddError(nameof(request.Language), "Invalid or unsupported language.");
        }
        return errors.Count > 0 ? TypedResults.ValidationProblem(errors) : null;
    }

    /// <summary>
    /// Checks if the provided culture name is valid.
    /// </summary>
    /// <param name="cultureName">Culture code (e.g., "en-US", "fr-FR").</param>
    /// <returns>True if valid, false otherwise.</returns>
    private static bool IsValidCulture(string cultureName) {
        if (string.IsNullOrWhiteSpace(cultureName))
            return false;

        try {
            // Attempt to get the CultureInfo object
            CultureInfo.GetCultureInfo(cultureName);
            return true;
        } catch (CultureNotFoundException) {
            return false;
        }
    }

    public static async Task<Results<NoContent, ValidationProblem>> Clear(IDocumentsService store, CancellationToken cancellationToken) {
        await store.ClearAsync();
        return TypedResults.NoContent();
    }
}
