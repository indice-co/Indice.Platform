using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Cases.Data.Config;

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