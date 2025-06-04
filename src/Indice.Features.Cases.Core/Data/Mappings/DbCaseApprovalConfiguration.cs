using Indice.Configuration;
using Indice.Features.Cases.Core.Data.Models;
using Indice.Features.Cases.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Cases.Core.Data.Mappings;

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