using Indice.Configuration;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Extensions;
using Indice.Features.Cases.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Cases.Data.Config
{
    internal class DbCaseApprovalConfiguration : IEntityTypeConfiguration<DbCaseApproval>
    {
        public void Configure(EntityTypeBuilder<DbCaseApproval> builder) {
            builder
                .ToTable("CaseApproval");
            builder
                .HasKey(p => p.Id);
            builder
                .Property(p => p.Action)
                .HasMaxLength(TextSizePresets.S64)
                .HasConversion(
                    valueToSave => valueToSave.ToString(),
                    valueRetrieved => (Approval)Enum.Parse(typeof(Approval), valueRetrieved))
                .IsRequired();
            builder
                .OwnsOneAudit(p => p.CreatedBy, required: true);
            builder
                .Property(p => p.Reason)
                .HasMaxLength(TextSizePresets.M128);
        }
    }
}