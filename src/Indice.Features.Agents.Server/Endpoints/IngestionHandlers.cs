using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Workflows;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Agents.Server.Endpoints;

/// <summary>Logic-free handlers for the IngestionApi.</summary>
internal static class IngestionHandlers
{
    /// <summary>POST /api/ingest/faq — multipart upload of a single FAQ-format Markdown file.</summary>
    public static async Task<Results<Ok<IngestionReport>, ValidationProblem>> UploadFaq(
        IFormFile file,
        IIngestionPipeline pipeline,
        CancellationToken cancellationToken,
        [FromForm] string? category = null,
        [FromForm] string? language = null) {
        if (ValidateUpload(file) is { } problem) {
            return problem;
        }
        await using var stream = file.OpenReadStream();
        var report = await pipeline.IngestAsync(stream, file.FileName, category, language, cancellationToken);
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
}
