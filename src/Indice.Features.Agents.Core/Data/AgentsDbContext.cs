using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Agents.Core.Data;

/// <summary>EF Core database context for Dex chat history and document storage.</summary>
public class AgentsDbContext : DbContext
{
    /// <summary>Constructs a new <see cref="AgentsDbContext"/> with the supplied options.</summary>
    public AgentsDbContext(DbContextOptions<AgentsDbContext> options) : base(options) { }

    /// <summary>Ingested source documents.</summary>
    public DbSet<DbDocument> Documents => Set<DbDocument>();

    /// <summary>Optional binary payloads and file metadata for ingested documents.</summary>
    public DbSet<DbBlob> DocumentBlobs => Set<DbBlob>();

    /// <summary>Per-document content chunks with their dense embeddings.</summary>
    public DbSet<DbChunk> Chunks => Set<DbChunk>();

    /// <summary>Chat sessions.</summary>
    public DbSet<DbSession> Sessions => Set<DbSession>();

    /// <summary>Individual chat session messages.</summary>
    public DbSet<DbMessage> SessionMessages => Set<DbMessage>();

    /// <summary>Application-local user profiles (augmenting the IdP).</summary>
    public DbSet<DbProfile> Profiles => Set<DbProfile>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("dex");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AgentsDbContext).Assembly);
    }
}
