using Indice.Configuration;
using Indice.Features.Cases.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Cases.Data.Config;

internal class DbStakeHolderConfiguration : IEntityTypeConfiguration<DbStakeHolder>
{
    public void Configure(EntityTypeBuilder<DbStakeHolder> builder) {
        builder
            .ToTable("StakeHolder");
        builder
            .HasKey(p => p.Id);
        builder
            .Property(p => p.StakeHolderId)
            .IsRequired();
        builder
            .Property(p => p.Type)
            .IsRequired();
        builder
           .Property(p => p.Accesslevel)
           .IsRequired();
    }
}