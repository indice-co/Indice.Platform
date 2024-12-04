using Indice.Configuration;
using Indice.Features.Cases.Core.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Cases.Core.Data.Mappings;

internal class DbNotificationSubscriptionConfiguration : IEntityTypeConfiguration<DbNotificationSubscription>
{
    public void Configure(EntityTypeBuilder<DbNotificationSubscription> builder) {
        builder
            .ToTable("NotificationSubscription");
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
