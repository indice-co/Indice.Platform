using Indice.Features.Cases.Core.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Cases.Core.Data.Mappings;

internal class DbCommentConfiguration : IEntityTypeConfiguration<DbComment>
{
    public void Configure(EntityTypeBuilder<DbComment> builder) {
        builder
            .ToTable("Comment");
        builder
            .HasKey(p => p.Id);
        builder
            .OwnsOneAudit(p => p.CreatedBy, required: true);
        builder
            .Property(p => p.Text)
            .IsRequired(false);
    }
}