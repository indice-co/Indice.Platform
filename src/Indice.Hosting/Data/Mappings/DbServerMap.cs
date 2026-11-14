using Indice.Hosting.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Hosting.Data;

/// <summary>EF Core configuration for <see cref="DbServer"/> entity.</summary>
public sealed class DbServerMap : IEntityTypeConfiguration<DbServer>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DbServer> builder) {
        // Configure table name.
        builder.ToTable("Server", "work");
        // Configure primary key.
        builder.HasKey(x => x.Id);
        // Configure indexes.
        // Stale servers are pruned by their last heartbeat.
        builder.HasIndex(x => x.LastHeartbeat);
        // Configure properties.
        builder.Property(x => x.Id).HasMaxLength(256);
        builder.Property(x => x.MachineName).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ApplicationName).HasMaxLength(256);
        builder.Property(x => x.Version).HasMaxLength(128);
    }
}
