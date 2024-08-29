using Indice.Configuration;
using Indice.Features.Cases.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Cases.Data.Config;

internal class DbCaseMemberConfiguration : IEntityTypeConfiguration<DbCaseMember>
{
    public void Configure(EntityTypeBuilder<DbCaseMember> builder) {
        builder
            .ToTable("CaseMember");
        builder
            .HasKey(p => p.Id);
        builder
            .Property(p => p.MemberId)
            .IsRequired();
        builder
            .Property(p => p.Type)
            .IsRequired();
        builder
           .Property(p => p.Accesslevel)
           .IsRequired();
    }
}