using System.Linq.Expressions;
using Indice.Configuration;
using Indice.Features.Cases.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Cases.Extensions;

/// <summary>
/// Entity builder extensions.
/// </summary>
public static class EntityTypeBuilderExtensions
{
    /// <summary>
    /// Map the <see cref="AuditMeta"/> property.
    /// </summary>
    public static EntityTypeBuilder<TEntity> OwnsOneAudit<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, AuditMeta>> navigationExpression)
        where TEntity : class {

        builder.OwnsOne(navigationExpression, actionBuilder => {
            var prefix = ((MemberExpression)navigationExpression.Body).Member.Name;
            actionBuilder
                .Property(p => p.Id)
                .HasColumnName($"{prefix}{nameof(AuditMeta.Id)}")
                .HasMaxLength(TextSizePresets.S64);
            actionBuilder
                .Property(p => p.Email)
                .HasColumnName($"{prefix}{nameof(AuditMeta.Email)}")
                .HasMaxLength(TextSizePresets.M128);
            actionBuilder
                .Property(p => p.Name)
                .HasColumnName($"{prefix}{nameof(AuditMeta.Name)}")
                .HasMaxLength(TextSizePresets.M128);
            actionBuilder
                .Property(p => p.When)
                .HasColumnName($"{prefix}{nameof(AuditMeta.When)}");

        });
        return builder;
    }
}