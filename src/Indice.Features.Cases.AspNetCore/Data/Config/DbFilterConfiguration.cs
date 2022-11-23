using Indice.Configuration;
using Indice.Features.Cases.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Cases.Data.Config
{
    internal class DbFilterConfiguration : IEntityTypeConfiguration<DbFilter>
    {
        public void Configure(EntityTypeBuilder<DbFilter> builder) {
            builder
                .ToTable("Filter");
            builder
                .HasKey(p => p.Id);
            builder
                .Property(p => p.UserId)
                .IsRequired()
                .HasMaxLength(TextSizePresets.M128);
            builder
                .Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(TextSizePresets.M128);
            builder
                .Property(c => c.QueryParameters)
                .IsRequired();

        }
    }
}
