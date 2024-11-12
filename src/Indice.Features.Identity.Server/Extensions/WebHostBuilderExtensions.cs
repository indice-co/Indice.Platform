﻿using Indice.Extensions.Configuration.Database;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.AspNetCore.Hosting;

/// <summary>Extension methods on <see cref="IWebHostBuilder"/> interface.</summary>
public static class WebHostBuilderExtensions
{
    /// <summary>Adds database settings feature using the <see cref="ExtendedIdentityDbContext{TUser, TRole}"/>.</summary>
    /// <param name="builder">Builds an <see cref="IWebHost"/> which hosts a web application.</param>
    public static IWebHostBuilder AddDatabaseSettings(this IWebHostBuilder builder) =>
        builder.AddDatabaseSettings<ExtendedIdentityDbContext<User, Role>>((options, configuration) => {
            options.ReloadOnInterval = TimeSpan.FromSeconds(30);
            options.ConfigureDbContext = dbBuilder => dbBuilder.UseSqlServer(configuration.GetConnectionString("IdentityDb"));
        });

    /// <summary>Adds database settings feature using the <see cref="ExtendedIdentityDbContext{TUser, TRole}"/>.</summary>
    /// <param name="builder">Builds an <see cref="IWebHost"/> which hosts a web application.</param>
    /// <param name="configureAction">Configuration action.</param>
    public static IWebHostBuilder AddDatabaseSettings(this IWebHostBuilder builder, Action<EntityConfigurationOptions, IConfiguration> configureAction) =>
        builder.AddDatabaseSettings<ExtendedIdentityDbContext<User, Role>>(configureAction);

    /// <summary>Adds seed information for the <see cref="ExtendedIdentityDbContext{TUser, TRole}"/>. Handy when used with database settings that initialization takes place sooner than the Identity Server is configured.</summary>
    /// <param name="builder">Builds an <see cref="IWebHost"/> which hosts a web application.</param>
    /// <param name="getInitialUsers">Function that gets the initial users.</param>
    /// <param name="getCustomRoles">Function that gets the custom roles needed</param>
    public static IWebHostBuilder AddInitialUsers(this IWebHostBuilder builder, Func<IEnumerable<User>> getInitialUsers, Func<IEnumerable<Role>>? getCustomRoles = null) =>
        builder.ConfigureServices(new Action<IServiceCollection>(services =>
            services.TryAddTransient(sp => new ExtendedIdentityDbContextSeedOptions<User, Role> { InitialUsers = getInitialUsers(), CustomRoles = getCustomRoles?.Invoke() ?? [] })
        ));
}
