using Indice.Configuration;
using Indice.Features.Messages.Core.Data.Models;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Messages.Core.Data.Mappings
{
    /// <summary>
    /// Configuration for <see cref="DbCampaign"/> entity.
    /// </summary>
    public class DbCampaignMap : IEntityTypeConfiguration<DbCampaign>
    {
        /// <summary>
        /// Creates a new instance of <see cref="DbCampaignMap"/>.
        /// </summary>
        /// <param name="schemaName">The schema name.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public DbCampaignMap(string schemaName) {
            SchemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
        }

        private string SchemaName { get; }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<DbCampaign> builder) {
            // Configure table name.
            builder.ToTable("Campaign", SchemaName);
            // Configure primary key.
            builder.HasKey(x => x.Id);
            // Configure properties.
            builder.Property(x => x.Id);
            builder.Property(x => x.Title).HasMaxLength(TextSizePresets.M128).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.Published).IsRequired();
            builder.OwnsOne(x => x.ActivePeriod).Property(x => x.From).HasColumnName(nameof(Period.From));
            builder.OwnsOne(x => x.ActivePeriod).Property(x => x.To).HasColumnName(nameof(Period.To));
            builder.Property(x => x.IsGlobal).IsRequired();
            builder.Property(x => x.Data).HasJsonConversion();
            builder.Property(x => x.Content).HasJsonConversion();
            // Owned properties
            builder.OwnsOne(x => x.ActionLink, actionLinkBuilder => {
                actionLinkBuilder.Property(x => x.Text).HasMaxLength(TextSizePresets.M128).HasColumnName("ActionText");
                actionLinkBuilder.Property(x => x.Href).HasMaxLength(TextSizePresets.L1024).HasColumnName("ActionHref");
            });
            // Configure relationships.
            builder.HasOne(x => x.Attachment).WithMany().HasForeignKey(x => x.AttachmentId);
            builder.HasOne(x => x.Type).WithMany().HasForeignKey(x => x.TypeId);
            builder.HasOne(x => x.DistributionList).WithMany().HasForeignKey(x => x.DistributionListId);
        }
    }
}
