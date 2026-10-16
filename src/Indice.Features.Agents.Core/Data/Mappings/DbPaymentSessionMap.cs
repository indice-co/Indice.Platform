using Indice.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Agents.Core.Data.Mappings;

/// <summary>EF Core configuration for <see cref="DbPaymentSession"/>.</summary>
public class DbPaymentSessionMap : IEntityTypeConfiguration<DbPaymentSession>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<DbPaymentSession> builder) {
        builder.ToTable("PaymentSession", "dex");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).HasMaxLength(TextSizePresets.M128).IsRequired();
        builder.Property(x => x.PaymentIntentId).HasMaxLength(TextSizePresets.M128).IsRequired();
        builder.Property(x => x.ClientSecret).HasMaxLength(TextSizePresets.M256);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(TextSizePresets.S32);
        builder.Property(x => x.FirstCaptureAmount).HasPrecision(18, 2);
        builder.Property(x => x.CapturedAmount).HasPrecision(18, 2);
        builder.Property(x => x.Currency).HasMaxLength(TextSizePresets.S16);
        builder.Property(x => x.FailureReason).HasMaxLength(TextSizePresets.M512);
        builder.HasIndex(x => x.PaymentIntentId).IsUnique();
        builder.HasIndex(x => new { x.ConversationId, x.CreatedAt });
    }
}
