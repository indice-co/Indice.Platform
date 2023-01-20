using System.Text.Json;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Extensions;
using Indice.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Cases.Data.Config
{
    internal class DbCaseDataConfiguration : IEntityTypeConfiguration<DbCaseData>
    {
        public static readonly JsonSerializerOptions SerializerOptions = JsonSerializerOptionDefaults.GetDefaultSettings();

        public void Configure(EntityTypeBuilder<DbCaseData> builder) {
            builder
                .ToTable("CaseData");
            builder
                .HasKey(p => p.Id);
            builder
                .OwnsOneAudit(p => p.CreatedBy);
            builder
                .Property(c => c.Data)
                .HasJsonConversion();
        }
    }
}