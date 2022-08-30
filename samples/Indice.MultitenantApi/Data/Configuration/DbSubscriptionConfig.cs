using Indice.Configuration;
using Indice.MultitenantApi.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.MultitenantApi.Data.Configuration
{
    public class DbSubscriptionConfig : IEntityTypeConfiguration<DbSubscription>
    {
        public void Configure(EntityTypeBuilder<DbSubscription> builder) {
            // Configure table name.
            builder.ToTable("Subscription");
            // Configure primary key.
            builder.HasKey(x => x.Id);
            // Configure columns.
            builder.HasAlternateKey(x => x.Alias).HasName($"AK_Alias");
            builder.Property(x => x.Alias).HasMaxLength(TextSizePresets.M128).HasDefaultValueSql("(CONVERT([VARCHAR](36), NEWID()))");
        }
    }
}
