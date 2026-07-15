using System.Text.Json;
using Indice.Configuration;
using Indice.Features.Agents.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Agents.Core.Data.Mappings;

/// <summary>EF Core configuration for <see cref="DbMessage"/>.</summary>
public class DbMessageMap : IEntityTypeConfiguration<DbMessage>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly ValueComparer<ChatMessageContent> ContentComparer = new(
        (left, right) => JsonSerializer.Serialize(left, JsonOptions) == JsonSerializer.Serialize(right, JsonOptions),
        value => JsonSerializer.Serialize(value, JsonOptions).GetHashCode(StringComparison.Ordinal),
        value => JsonSerializer.Deserialize<ChatMessageContent>(JsonSerializer.Serialize(value, JsonOptions), JsonOptions) ?? new ChatMessageContent());

    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<DbMessage> builder) {
        builder.ToTable("Message", "dex");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Role)
               .HasConversion(
                    role => role.ToString().ToLowerInvariant(),
                    value => Enum.Parse<ChatMessageRole>(value, ignoreCase: true))
               .HasMaxLength(TextSizePresets.S32)
               .IsRequired();

        //TODO: check whats going on here with this version. builder.ComplexProperty(b => b.Content, b => b.ToJson().IsRequired());
        builder.Property(x => x.Content)
               .HasConversion(
                    content => JsonSerializer.Serialize(content, JsonOptions),
                    json => JsonSerializer.Deserialize<ChatMessageContent>(json, JsonOptions) ?? new ChatMessageContent())
               .Metadata.SetValueComparer(ContentComparer);
        builder.Property(x => x.ModelUsed).HasMaxLength(TextSizePresets.M128);
        builder.HasIndex(x => new { x.SessionId, x.CreatedAt });
    }
}
