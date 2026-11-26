using Indice.Configuration;
using Indice.Features.Cases.Core.Data.Models;
using Indice.Features.Cases.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Cases.Core.Data.Mappings;

internal class DbCoownsCaseConfiguration : IEntityTypeConfiguration<DbCoownsCase>
{
    /// <summary>
    /// Configures the entity type for <see cref="DbCoownsCase"/>.
    /// </summary>
    /// <param name="builder"></param>
    public void Configure(EntityTypeBuilder<DbCoownsCase> builder) {
        builder
            .ToTable("CaseCoOwner");
        builder
            .HasKey(x => x.Id);
        builder
            .HasOne(x => x.Case)
            .WithMany(x => x.CoOwners)
            .HasForeignKey(x => x.CaseId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.OwnsOne(
                p => p.CoOwner,
                actionBuilder => {
                    var prefix = nameof(DbCoownsCase.CoOwner);
                    actionBuilder
                        .Property(p => p.UserId)
                        .HasColumnName($"{prefix}{nameof(DbCoownsCase.CoOwner.UserId)}")
                        .HasMaxLength(TextSizePresets.S64)
                        .IsRequired();
                    actionBuilder
                        .Property(p => p.Reference)
                        .HasColumnName($"{prefix}{nameof(DbCoownsCase.CoOwner.Reference)}")
                        .HasMaxLength(TextSizePresets.M128);
                    actionBuilder
                        .Property(p => p.FirstName)
                        .HasColumnName($"{prefix}{nameof(DbCoownsCase.CoOwner.FirstName)}")
                        .HasMaxLength(TextSizePresets.M128);
                    actionBuilder
                        .Property(p => p.LastName)
                        .HasColumnName($"{prefix}{nameof(DbCoownsCase.CoOwner.LastName)}")
                        .HasMaxLength(TextSizePresets.M128);
                    actionBuilder
                        .Property(p => p.Tin)
                        .HasColumnName($"{prefix}{nameof(DbCoownsCase.CoOwner.Tin)}")
                        .HasMaxLength(TextSizePresets.S32);
                    actionBuilder
                        .Ignore(p => p.FullName);
                    actionBuilder
                        .Property(p => p.Permission)
                        .HasColumnName($"{prefix}{nameof(DbCoownsCase.CoOwner.Permission)}");
                    actionBuilder
                        .OwnsOneAudit(p => p.AddedBy, required: true, prefix: nameof(CoOwnerContactMeta.AddedBy));
                    // Serves the "cases where I am a co-owner" lookup.
                    // The unique index on (CaseId, CoOwnerUserId) spans the entity and its owned type, which the model cannot express. It ships with the database scripts.
                    actionBuilder
                        .HasIndex(p => p.UserId);
                });
        builder
            .Navigation(x => x.CoOwner)
            .IsRequired();
    }
}
