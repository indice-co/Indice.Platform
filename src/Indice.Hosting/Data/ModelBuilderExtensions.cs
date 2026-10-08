using Indice.Hosting.Data;
using Indice.Hosting.Data.Models;

namespace Microsoft.EntityFrameworkCore;

/// <summary>Extension methods on <see cref="ModelBuilder"/> that configure the worker store entities.</summary>
public static class ModelBuilderExtensions
{
    /// <summary>Applies the entity configuration for <see cref="DbQMessage"/>, <see cref="DbScheduledTask"/> and <see cref="DbLock"/>.</summary>
    public static void ApplyWorkerConfiguration(this ModelBuilder builder, string? providerName = null) {
        builder.ApplyConfiguration(new DbQMessageMap());
        if (providerName == "Npgsql.EntityFrameworkCore.PostgreSQL") {
            builder.ApplyConfiguration(new DbQMessagePostgreSQLMap());
        }
        builder.ApplyConfiguration(new DbScheduledTaskMap());
        builder.ApplyConfiguration(new DbLockMap());
    }
}
