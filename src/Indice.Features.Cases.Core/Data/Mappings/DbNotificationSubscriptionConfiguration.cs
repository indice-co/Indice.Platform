using Indice.Configuration;
using Indice.Features.Cases.Core.Data.Models;
using Indice.Features.Cases.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Cases.Core.Data.Mappings;

internal class DbNotificationSubscriptionConfiguration : IEntityTypeConfiguration<DbNotificationSubscription>
{
    public void Configure(EntityTypeBuilder<DbNotificationSubscription> builder) {
        builder
            .ToTable("NotificationSubscription");

        builder
            .OwnsOne(p => p.Subscriber, subscriberBuilder => {
                subscriberBuilder
                .Property(p => p.Email)
                .HasColumnName($"{nameof(Subscriber.Email)}")
                .HasMaxLength(TextSizePresets.M128)
                .IsRequired(true);

                subscriberBuilder
                .Property(p => p.GroupId)
                .HasColumnName($"{nameof(Subscriber.GroupId)}")
                .HasMaxLength(TextSizePresets.M128)
                .IsRequired(false);

                subscriberBuilder.HasIndex(p => p.Email);
            });
    }
}
