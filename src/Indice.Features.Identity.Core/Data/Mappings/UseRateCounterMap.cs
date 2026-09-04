using Indice.Configuration;
using Indice.Features.Identity.Core.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Identity.Core.Data.Mappings;

/// <summary>Entity Framework mapping for type <see cref="UserRateCounter"/>.</summary>
/// <typeparam name="TUser">The type of user.</typeparam>
internal class UseRateCounterMap<TUser> : IEntityTypeConfiguration<UserRateCounter> where TUser : User
{
    /// <summary>Configure Entity Framework mapping for type <see cref="UserRateCounter"/>.</summary>
    /// <param name="builder"></param>
    public void Configure(EntityTypeBuilder<UserRateCounter> builder) {
        builder.ToTable(nameof(UserRateCounter), "auth");

        builder.HasKey(x => new { x.UserId, x.ActionName });

        builder.Property(x => x.UserId)
               .IsRequired();

        builder.Property(x => x.ActionName)
               .IsRequired()
               .HasMaxLength(TextSizePresets.M256);

        builder.Property(x => x.Count)
               .IsRequired();

        builder.Property(x => x.ResetDate)
               .IsRequired();

        builder.Property(x => x.LastUpdate)
               .IsRequired();

        builder.Property<byte[]>("RowVersion")
               .IsRowVersion();

        builder.HasIndex(x => x.ResetDate);

        builder.HasOne<TUser>()
               .WithMany()
               .HasForeignKey(x => x.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
