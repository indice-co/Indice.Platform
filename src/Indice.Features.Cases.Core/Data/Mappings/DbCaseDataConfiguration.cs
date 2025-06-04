using System.Text.Json;
using Indice.Features.Cases.Core.Data.Models;
using Indice.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Cases.Core.Data.Mappings;

internal class DbCaseDataConfiguration : IEntityTypeConfiguration<DbCaseData>
{
    public static readonly JsonSerializerOptions SerializerOptions = JsonSerializerOptionDefaults.GetDefaultSettings();

    public void Configure(EntityTypeBuilder<DbCaseData> builder) {
        builder
            .ToTable("CaseData");
        builder
            .HasKey(p => p.Id);
        builder
            .OwnsOneAudit(p => p.CreatedBy, required: true);
        builder
            .Property(c => c.Data)
            .HasJsonConversion()
            .IsRequired();
    }
}