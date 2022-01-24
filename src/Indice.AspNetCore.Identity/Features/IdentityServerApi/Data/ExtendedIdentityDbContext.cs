using System;
using System.Linq;
using Indice.AspNetCore.Identity.Api.Configuration;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Indice.AspNetCore.Identity.Data
{
    /// <summary>
    /// An extended <see cref="DbContext"/> for the Identity framework.
    /// </summary>
    public sealed class ExtendedIdentityDbContext<TUser, TRole> : IdentityDbContext<TUser, TRole>
        where TUser : User, new()
        where TRole : Role, new()
    {
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Creates a new instance of <see cref="ExtendedIdentityDbContext{TUser, TRole}"/>.
        /// </summary>
        /// <param name="dbContextOptions">The options to be used by a <see cref="DbContext"/>.</param>
        /// <param name="options">Options for configuring the IdentityServer API feature.</param>
        /// <param name="webHostEnvironment">Provides information about the web hosting environment an application is running in.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        public ExtendedIdentityDbContext(DbContextOptions<ExtendedIdentityDbContext<TUser, TRole>> dbContextOptions, IdentityServerApiEndpointsOptions options, IWebHostEnvironment webHostEnvironment, IConfiguration configuration) : base(dbContextOptions) {
            /* https://docs.microsoft.com/en-us/ef/core/logging-events-diagnostics/events */
            ChangeTracker.StateChanged += ChangeTrackerStateChanged;
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            if (webHostEnvironment.IsDevelopment() && Database.EnsureCreated()) {
                this.SeedAdminUser();
                if (options.SeedDummyUsers) {
                    this.SeedDummyUsers();
                }
                this.SeedCustomUsers(options.InitialUsers);
            }
        }

        /// <summary>
        /// Register extended configuration methods when the database is being created.
        /// </summary>
        /// <param name="modelBuilder">Provides a simple API surface for configuring a <see cref="DbContext"/>.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
        }

        private void ChangeTrackerStateChanged(object sender, EntityStateChangedEventArgs eventArgs) {
            var entity = eventArgs.Entry.Entity;
            if (entity is AppSetting && (eventArgs.OldState == EntityState.Added || eventArgs.NewState == EntityState.Deleted || eventArgs.NewState == EntityState.Modified)) {
                var entityConfigurationProvider = ((IConfigurationRoot)_configuration).Providers.SingleOrDefault(provider => provider is EntityConfigurationProvider);
                if (entityConfigurationProvider != null) {
                    ((EntityConfigurationProvider)entityConfigurationProvider).OnAppSettingsChanged();
                }
            }
        }
    }
}
