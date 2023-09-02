using IdentityModel;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Identity.Core.Data;

/// <summary>Extensions on type <see cref="ExtendedIdentityDbContext{TUser, TRole}"/>.</summary>
public static class ExtendedIdentityDbContextExtensions
{
    /// <summary>Sets up the ASP.NET Identity store and adds some required initial data.</summary>
    /// <param name="app">Defines a class that provides the mechanisms to configure an application's request pipeline.</param>
    public static IApplicationBuilder IdentityStoreSetup(this IApplicationBuilder app) {
        using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope()) {
            var dbContext = serviceScope.ServiceProvider.GetService<ExtendedIdentityDbContext<User, Role>>();
            if (dbContext is not null) {
                dbContext.Database.EnsureCreated();
                var seedOptions = serviceScope.ServiceProvider.GetService<ExtendedIdentityDbContextSeedOptions<User, Role>>();
                dbContext.SeedInitialData(seedOptions);
            }
        }
        return app;
    }

    /// <summary>A method that seeds the database with an administrator account.</summary>
    /// <typeparam name="TUser">The type of user.</typeparam>
    /// <typeparam name="TRole">The type of role.</typeparam>
    /// <param name="dbContext">An extended <see cref="DbContext"/> for the Identity framework.</param>
    /// <param name="seedOptions">Seed options to customize initial load of users and roles</param>
    public static void SeedInitialData<TUser, TRole>(this ExtendedIdentityDbContext<TUser, TRole> dbContext, ExtendedIdentityDbContextSeedOptions<TUser, TRole> seedOptions = null)
        where TUser : User, new()
        where TRole : Role, new() {
        if (!dbContext.Database.CanConnect()) {
            return;
        }

        var rolesExist = dbContext.Roles.Any();
        if (!rolesExist) {
            dbContext.Roles.AddRange(InitialRoles<TRole>.Get());
            if (seedOptions?.CustomRoles?.Any() == true) {
                dbContext.Roles.AddRange(seedOptions.CustomRoles);
            }
        }

        var usersExist = dbContext.Users.Any();
        if (!usersExist) {
            const string adminEmail = "company@indice.gr";
            const string adminId = "ab9769f1-d532-4b7d-9922-3da003157ebd";
            if (seedOptions?.InitialUsers?.Any() == true) {
                dbContext.Users.AddRange(seedOptions.InitialUsers);
                if (!seedOptions.InitialUsers.Any(x => x.Id == adminId || 
                                                      x.Email.Equals(adminEmail, StringComparison.OrdinalIgnoreCase))) {
                    // admin not seeded externaly through initial users!
                    // Create admin now
                    var admin = new TUser {
                        Admin = true,
                        ConcurrencyStamp = $"{Guid.NewGuid()}",
                        CreateDate = DateTime.UtcNow,
                        Email = adminEmail,
                        EmailConfirmed = true,
                        Id = "ab9769f1-d532-4b7d-9922-3da003157ebd",
                        LockoutEnabled = false,
                        NormalizedEmail = adminEmail.ToUpper(),
                        NormalizedUserName = adminEmail.ToUpper(),
                        PasswordHash = "AH6SA/wuxp9YEfLGROaj2CgjhxZhXDkMB1nD8V7lfQAI+WTM4lGMItjLhhV5ASsq+Q==",
                        PhoneNumber = "699XXXXXXX",
                        PhoneNumberConfirmed = true,
                        SecurityStamp = $"{Guid.NewGuid()}",
                        UserName = adminEmail,
                        Claims = {
                            new () { ClaimType = JwtClaimTypes.GivenName, ClaimValue = "Indice" },
                            new () { ClaimType = JwtClaimTypes.FamilyName, ClaimValue = "Company" },
                            new () { ClaimType = BasicClaimTypes.DeveloperTotp, ClaimValue = "123456" }
                        }
                    };
                    InitialRoles<TRole>.Get().ToList().ForEach(role => admin.Roles.Add(new() { RoleId = role.Id }));
                    dbContext.Users.Add(admin);
                }
            }
        }

        
        
      
        dbContext.SaveChanges();
    }
}
