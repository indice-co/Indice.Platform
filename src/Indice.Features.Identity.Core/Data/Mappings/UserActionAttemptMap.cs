using Indice.Configuration;
using Indice.Features.Identity.Core.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Identity.Core.Data.Mappings;

/// <summary>Entity Framework mapping for type <see cref="UserActionAttempt"/>.</summary>
/// <typeparam name="TUser">The type of user.</typeparam>
internal class UserActionAttemptMap<TUser> : IEntityTypeConfiguration<UserActionAttempt> where TUser : User
{
    /// <summary>Configure Entity Framework mapping for type <see cref="UserActionAttempt"/>.</summary>
    /// <param name="builder"></param>
    public void Configure(EntityTypeBuilder<UserActionAttempt> builder) {
        builder.ToTable(nameof(UserActionAttempt), "auth");

        builder.HasKey(x => new { x.UserId, x.PurposeKey });

        builder.Property(x => x.UserId)
               .IsRequired();

        builder.Property(x => x.PurposeKey)
               .IsRequired()
               .HasMaxLength(TextSizePresets.M256);

        builder.Property(x => x.Count)
               .IsRequired();

        builder.Property(x => x.WindowEnd)
               .IsRequired();

        builder.Property(x => x.LastAttemptDate)
               .IsRequired();

        builder.HasIndex(x => x.WindowEnd);

        builder.HasOne<TUser>()
               .WithMany()
               .HasForeignKey(x => x.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
