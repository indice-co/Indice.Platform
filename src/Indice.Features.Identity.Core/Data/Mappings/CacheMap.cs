using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Identity.Core.Data.Mappings;

internal class CacheMap : IEntityTypeConfiguration<CacheEntry>
{
    public void Configure(EntityTypeBuilder<CacheEntry> builder) {
        builder.ToTable("Cache", "auth");
        builder.HasKey(e => e.Id);
        builder.Property(x => x.Id).HasMaxLength(499);

        builder.HasIndex(e => e.ExpiresAtTime).HasDatabaseName("Index_ExpiresAtTime");
    }
}

/// <summary>Distributed cache backing store entity type. This should work fine with postgress as well as SqlServer packages.</summary>
/// <remarks>Works with the Microsoft.Extensions.Caching.SqlServer and Postgress packages.</remarks>
internal class CacheEntry
{
    public string Id { get; set; } = null!;

    public byte[] Value { get; set; } = null!;

    public DateTimeOffset ExpiresAtTime { get; set; }
    public long? SlidingExpirationInSeconds { get; set; }
    public DateTimeOffset? AbsoluteExpiration { get; set; }
}