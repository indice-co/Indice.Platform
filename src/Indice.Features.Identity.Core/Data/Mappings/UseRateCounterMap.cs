using Indice.Configuration;
using Indice.Features.Identity.Core.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Identity.Core.Data.Mappings;

/// <summary>Entity Framework mapping for type <see cref="UseRateCounter"/>.</summary>
/// <typeparam name="TUser">The type of user.</typeparam>
internal class UseRateCounterMap<TUser> : IEntityTypeConfiguration<UseRateCounter> where TUser : User
{
    /// <summary>Configure Entity Framework mapping for type <see cref="UseRateCounter"/>.</summary>
    /// <param name="builder"></param>
    public void Configure(EntityTypeBuilder<UseRateCounter> builder) {
        builder.ToTable(nameof(UseRateCounter), "auth");

        builder.HasKey(x => new { x.UserId, x.PurposeKey });

        builder.Property(x => x.UserId)
               .IsRequired();

        builder.Property(x => x.PurposeKey)
               .IsRequired()
               .HasMaxLength(TextSizePresets.M256);

        builder.Property(x => x.Count)
               .IsRequired();

        builder.Property(x => x.ResetDate)
               .IsRequired();

        builder.Property(x => x.LastUpdate)
               .IsRequired();

        builder.HasIndex(x => x.ResetDate);

        builder.HasOne<TUser>()
               .WithMany()
               .HasForeignKey(x => x.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
