using Indice.Configuration;
using Indice.Features.Cases.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Cases.Data.Config
{
    internal class DbCommentConfiguration : IEntityTypeConfiguration<DbComment>
    {
        public void Configure(EntityTypeBuilder<DbComment> builder) {
            builder
                .ToTable("Comment");
            builder
                .HasKey(p => p.Id);
            builder
                .OwnsOne(
                    p => p.CreatedBy,
                    actionBuilder => {
                        var prefix = nameof(DbComment.CreatedBy);
                        actionBuilder
                            .Property(p => p.Id)
                            .HasColumnName($"{prefix}{nameof(DbComment.CreatedBy.Id)}")
                            .HasMaxLength(TextSizePresets.S64);
                        actionBuilder
                            .Property(p => p.Email)
                            .HasColumnName($"{prefix}{nameof(DbComment.CreatedBy.Email)}")
                            .HasMaxLength(TextSizePresets.M128);
                        actionBuilder
                            .Property(p => p.Name)
                            .HasColumnName($"{prefix}{nameof(DbComment.CreatedBy.Name)}")
                            .HasMaxLength(TextSizePresets.M128);
                        actionBuilder
                            .Property(p => p.When)
                            .HasColumnName($"{prefix}{nameof(DbComment.CreatedBy.When)}");
                    });
        }
    }
}