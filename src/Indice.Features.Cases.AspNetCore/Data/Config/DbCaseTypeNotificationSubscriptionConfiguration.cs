using Indice.Configuration;
using Indice.Features.Cases.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Cases.Data.Config
{
    internal class DbCaseTypeNotificationSubscriptionConfiguration : IEntityTypeConfiguration<DbCaseTypeNotificationSubscription>
    {
        public void Configure(EntityTypeBuilder<DbCaseTypeNotificationSubscription> builder) {
            builder
                .ToTable("CaseTypeNotificationSubscription");
            builder
                .HasIndex(p => p.Email);
            builder
                .Property(p => p.GroupId)
                .HasMaxLength(TextSizePresets.M128);
            builder
               .Property(p => p.Email)
               .HasMaxLength(TextSizePresets.M128);
        }
    }
}
