using Indice.Hosting.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Hosting.Data;

/// <summary>EF Core configuration for <see cref="DbLock"/> entity.</summary>
public sealed class DbLockMap : IEntityTypeConfiguration<DbLock>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DbLock> builder) {
        // Configure table name.
        builder.ToTable("Lock", "work");
        // Configure primary key.
        builder.HasKey(x => x.Name);
        // Configure index.
        builder.HasIndex(x => x.Id);
        // Configure properties.
        builder.Property(x => x.Name).HasMaxLength(256);
        // Configure properties.
        builder.Property(x => x.Duration);
    }
}
