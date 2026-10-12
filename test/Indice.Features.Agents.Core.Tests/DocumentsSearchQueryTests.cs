using Indice.Features.Agents.Core.Data;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Agents.Core.Tests;

public class DocumentsSearchQueryTests(ITestOutputHelper output)
{
    [Fact]
    public void SearchQuery_GeneratesTopKInsideSubquery() {
        var options = new DbContextOptionsBuilder<AgentsDbContext>()
            .UseSqlServer("Server=.;Database=x;Trusted_Connection=True;")
            .Options;
        using var db = new AgentsDbContext(options);

        var sqlVector = new SqlVector<float>(new float[1536]);
        var maxDistance = 0.5;
        var category = "cat";
        var language = "en";
        var topK = 5;

        var candidates = db.Set<DbChunk>().AsNoTracking()
            .Where(c => c.Category == null || c.Category == category)
            .Where(c => c.Language == null || c.Language == language);
        var ranked = candidates
            .Select(c => new { c.Id, Distance = EF.Functions.VectorDistance("cosine", c.Embedding, sqlVector) })
            .Where(x => x.Distance <= maxDistance)
            .OrderBy(x => x.Distance)
            .Take(topK)
            .Distinct();
        var query = ranked
            .Join(db.Set<DbChunk>(), r => r.Id, c => c.Id, (r, c) => new { r.Distance, Chunk = c })
            .OrderBy(x => x.Distance)
            .Select(x => new {
                x.Chunk.Id,
                x.Chunk.DocumentId,
                Title = x.Chunk.Title ?? string.Empty,
                x.Chunk.HeadingPath,
                x.Chunk.Content,
                x.Chunk.TokenCount,
                DocumentTitle = x.Chunk.Document.Title,
                DocumentSource = x.Chunk.Document.Source,
                DocumentIsPrivate = x.Chunk.Document.IsPrivate,
                DocumentContentHash = x.Chunk.Document.ContentHash,
                BlobContentType = x.Chunk.Document.Blob == null ? "application/markdown" : x.Chunk.Document.Blob.ContentType,
                BlobLength = x.Chunk.Document.Blob == null ? -1L : x.Chunk.Document.Blob.ContentLength,
                BlobFileName = x.Chunk.Document.Blob == null ? x.Chunk.Document.Title : x.Chunk.Document.Blob.FileName,
                x.Distance,
            });

        // Not enumerated: inspect `query` DebugView or `sql` in the debugger.
        var sql = query.ToQueryString();
        output.WriteLine(sql);

        Assert.Contains("SELECT TOP(@p)", sql);
        Assert.True(sql.IndexOf("TOP(@p)") > sql.IndexOf("FROM ("), "TOP must be inside the derived table");
        Assert.Contains("VECTOR_DISTANCE('cosine'", sql);
        Assert.True(sql.IndexOf("@category") < sql.IndexOf(") AS [c1]"), "Category filter must be inside the subquery");
        Assert.True(sql.IndexOf("@language") < sql.IndexOf(") AS [c1]"), "Language filter must be inside the subquery");
        Assert.DoesNotContain("[Data]", sql);
    }
}