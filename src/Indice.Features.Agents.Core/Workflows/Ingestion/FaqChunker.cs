using System.Security.Cryptography;
using System.Text;

namespace Indice.Features.Agents.Core.Workflows.Ingestion;

/// <summary>Parses a Markdown FAQ document into chunks.</summary>
public static class FaqChunker
{
    /// <summary>
    /// Walks <paramref name="body"/> line by line, recognizing ATX <c># </c> as a category boundary and
    /// <c>## </c> as a question boundary. Body until the next <c>##</c>/<c>#</c>/EOF is the answer.
    /// Returns the first <c>#</c> (document-level category) and one chunk per Q&amp;A pair.
    /// </summary>
    public static (string? FirstCategory, IReadOnlyList<DocumentChunk> Chunks) ParseFaq(string body) {
        var chunks = new List<DocumentChunk>();
        string? firstCategory = null;
        string? currentCategory = null;
        string? pendingQuestion = null;
        var pendingAnswer = new StringBuilder();
        var chunkIndex = 0;

        foreach (var line in body.Split(['\n', '\r'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)) {

            if (line.StartsWith("## ", StringComparison.Ordinal)) {
                Flush();
                pendingQuestion = line[3..].TrimStart();
                continue;
            }
            if (line.StartsWith("# ", StringComparison.Ordinal)) {
                Flush();
                currentCategory = line[2..].TrimStart();
                firstCategory ??= currentCategory;
                continue;
            }
            // Lines before any `##` are silently discarded.
            if (pendingQuestion is null) {
                continue;
            }
            if (pendingAnswer.Length > 0) {
                pendingAnswer.Append('\n');
            }
            pendingAnswer.Append(line);
        }
        Flush();

        return (firstCategory, chunks);

        void Flush() {
            if (string.IsNullOrWhiteSpace(pendingQuestion)) {
                pendingQuestion = null;
                pendingAnswer.Clear();
                return;
            }
            var answer = pendingAnswer.ToString().Trim();
            if (answer.Length == 0) {
                pendingQuestion = null;
                pendingAnswer.Clear();
                return;
            }
            var embedded = $"Q: {pendingQuestion}\nA: {answer}";
            var headingPath = string.IsNullOrEmpty(currentCategory)
                ? pendingQuestion!
                : $"{currentCategory} > {pendingQuestion}";
            chunks.Add(new DocumentChunk {
                ChunkIndex = chunkIndex++,
                Content = embedded,
                ContentHash = Sha256Hex(embedded),
                HeadingPath = headingPath,
                Title = pendingQuestion,
                Category = currentCategory,
                TokenCount = 0,
            });
            pendingQuestion = null;
            pendingAnswer.Clear();
        }
    }
    
    /// <summary>
    /// Computes the SHA-256 hash of the given <paramref name="input"/> and returns it as a hexadecimal string.
    /// </summary>
    public static string Sha256Hex(string input) {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}
