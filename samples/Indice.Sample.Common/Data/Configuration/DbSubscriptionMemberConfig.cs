using Indice.Configuration;
using Indice.Sample.Common.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Sample.Common.Data.Configuration
{
    public class DbSubscriptionMemberConfig : IEntityTypeConfiguration<DbSubscriptionMember>
    {
        public void Configure(EntityTypeBuilder<DbSubscriptionMember> builder) {
            // Configure table name.
            builder.ToTable("SubscriptionMember");
            // Configure primary key. 
            builder.HasKey(x => x.Id);
            // Configure columns.
            builder.Property(x => x.Email).HasMaxLength(TextSizePresets.M256);
            // Configure indexes.
            builder.HasIndex(x => new { x.SubscriptionId, x.MemberId }).IsUnique(true);
            // Configure relationships.
            builder.HasOne(x => x.Subscription).WithMany().HasForeignKey(x => x.SubscriptionId);
        }
    }
}
