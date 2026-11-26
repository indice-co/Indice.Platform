using System.Linq.Expressions;
using Indice.Configuration;
using Indice.Features.Cases.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Cases.Core.Data;

/// <summary>Entity builder extensions.</summary>
internal static class EntityTypeBuilderExtensions
{
    /// <summary>Map the <see cref="AuditMeta"/> property.</summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="builder">The EntityTypeBuilder.</param>
    /// <param name="navigationExpression">The navigation expression.</param>
    /// <param name="required">Define the property required or not. Default is false.</param>
    /// <returns></returns>
    public static EntityTypeBuilder<TEntity> OwnsOneAudit<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, AuditMeta?>> navigationExpression,
        bool required = false)
        where TEntity : class {

        builder.OwnsOne(navigationExpression, actionBuilder => {
            var prefix = ((MemberExpression)navigationExpression.Body).Member.Name;
            actionBuilder
                .Property(p => p.Id)
                .HasColumnName($"{prefix}{nameof(AuditMeta.Id)}")
                .HasMaxLength(TextSizePresets.S64)
                .IsRequired(required);
            actionBuilder
                .Property(p => p.Email)
                .HasColumnName($"{prefix}{nameof(AuditMeta.Email)}")
                .HasMaxLength(TextSizePresets.M128)
                .IsRequired(required);
            actionBuilder
                .Property(p => p.Name)
                .HasColumnName($"{prefix}{nameof(AuditMeta.Name)}")
                .HasMaxLength(TextSizePresets.M128)
                .IsRequired(required);
            actionBuilder
                    .Property(p => p.When)
                .HasColumnName($"{prefix}{nameof(AuditMeta.When)}")
                .IsRequired(required);
        });
        return builder;
    }

    /// <summary>Map the <see cref="AuditMeta"/> property of an owned type.</summary>
    /// <param name="builder">The builder of the owned type.</param>
    /// <param name="navigationExpression">The navigation expression.</param>
    /// <param name="required">Define the property required or not. Default is false.</param>
    /// <param name="prefix">The prefix of the column names. Defaults to the name of the owned type followed by the name of the navigation.</param>
    public static OwnedNavigationBuilder<TOwnerEntity, TEntity> OwnsOneAudit<TOwnerEntity, TEntity>(
    this OwnedNavigationBuilder<TOwnerEntity, TEntity> builder,
    Expression<Func<TEntity, AuditMeta?>> navigationExpression,
    bool required = false,
    string? prefix = null)
    where TOwnerEntity : class
    where TEntity : class {

        builder.OwnsOne(navigationExpression, actionBuilder => {
            var navigationName = ((MemberExpression)navigationExpression.Body).Member.Name;
            prefix ??= $"{typeof(TEntity).Name}{navigationName}";
            actionBuilder
                .Property(p => p.Id)
                .HasColumnName($"{prefix}{nameof(AuditMeta.Id)}")
                .HasMaxLength(TextSizePresets.S64)
                .IsRequired(required);
            actionBuilder
                .Property(p => p.Email)
                .HasColumnName($"{prefix}{nameof(AuditMeta.Email)}")
                .HasMaxLength(TextSizePresets.M128)
                .IsRequired(required);
            actionBuilder
                .Property(p => p.Name)
                .HasColumnName($"{prefix}{nameof(AuditMeta.Name)}")
                .HasMaxLength(TextSizePresets.M128)
                .IsRequired(required);
            actionBuilder
                .Property(p => p.When)
                .HasColumnName($"{prefix}{nameof(AuditMeta.When)}")
                .IsRequired(required);
        });
        return builder;
    }
}