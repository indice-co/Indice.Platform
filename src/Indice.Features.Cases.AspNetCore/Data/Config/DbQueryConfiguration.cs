using Indice.Configuration;
using Indice.Features.Cases.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Cases.Data.Config
{
    internal class DbQueryConfiguration : IEntityTypeConfiguration<DbQuery>
    {
        public void Configure(EntityTypeBuilder<DbQuery> builder) {
            builder
                .ToTable("Query");
            builder
                .HasKey(p => p.Id);
            builder
                .Property(p => p.UserId)
                .IsRequired()
                .HasMaxLength(TextSizePresets.M128);
            builder
                .Property(p => p.FriendlyName)
                .IsRequired()
                .HasMaxLength(TextSizePresets.M128);
            builder
                .Property(c => c.Parameters)
                .IsRequired();

        }
    }
}
