﻿using Indice.Features.Cases.Core.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Cases.Core.Data.Mappings;

internal class DbAttachmentConfiguration : IEntityTypeConfiguration<DbAttachment>
{
    public void Configure(EntityTypeBuilder<DbAttachment> builder) {
        builder
            .ToTable("Attachment");
        builder
            .HasKey(p => p.Id);
        builder
            .Property(p => p.Name)
            .IsRequired();
        builder
            .Property(p => p.FileExtension)
            .IsRequired();
        builder
            .Property(p => p.ContentType)
            .IsRequired();
        builder
            .Property(p => p.Data)
            .IsRequired();
    }
}