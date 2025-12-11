using Indice.Hosting.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Hosting.Data;

/// <summary>EF Core configuration for <see cref="DbQMessage"/> entity.</summary>
public sealed class DbQMessageMap : IEntityTypeConfiguration<DbQMessage>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DbQMessage> builder) {
        // Configure table name.
        builder.ToTable("QMessage", "work");
        // Configure primary key.
        builder.HasKey(x => x.Id);
        // Configure indexes.
        builder.HasIndex(x => x.QueueName);
        // Configure properties.
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}

/// <summary>
/// EF Core configuration for <see cref="DbQMessage"/> entity specific to PostgreSQL.
/// This configuration provides PostgreSQL-specific mappings for the <c>RowVersion</c> property,
/// including the use of the <c>bytea</c> column type and custom default value generation,
/// to ensure proper concurrency control in PostgreSQL environments.
/// </summary>
public sealed class DbQMessagePostgreSQLMap : IEntityTypeConfiguration<DbQMessage>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DbQMessage> builder) {
        builder.Property(x => x.RowVersion)
           .HasColumnName("RowVersion")
           .HasColumnType("bytea")
           .IsConcurrencyToken()
           .ValueGeneratedOnAddOrUpdate()
           .HasDefaultValueSql("gen_random_bytes(8)");
    }
}
