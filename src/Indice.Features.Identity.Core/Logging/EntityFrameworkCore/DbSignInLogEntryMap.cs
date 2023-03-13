using Indice.Features.Identity.Core.Logging.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Identity.Core.Logging.EntityFrameworkCore;

/// <summary>Entity Framework Core mapping for type <see cref="DbSignInLogEntry"/>.</summary>
public class DbSignInLogEntryMap : IEntityTypeConfiguration<DbSignInLogEntry>
{
    /// <summary>Configure Entity Framework mapping for type <see cref="DbSignInLogEntry"/>.</summary>
    /// <param name="builder">Provides a simple API for configuring an <see cref="IMutableEntityType" />.</param>
    public void Configure(EntityTypeBuilder<DbSignInLogEntry> builder) {
        // Configure table name and schema.
        builder.ToTable("SignInLog", "dbo");
        // Configure primary key.
        builder.HasKey(x => x.Id);
        // Configure indexes.
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.SubjectId);
        // Configure properties.
        builder.Property(x => x.Succedded).IsRequired();
        builder.Property(x => x.ExtraData).HasJsonConversion();
    }
}
