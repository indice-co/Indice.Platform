using System;
using System.Collections.Generic;
using System.Linq;
using IdentityModel;
using Indice.AspNetCore.Identity.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Indice.AspNetCore.Identity.Data
{
    /// <summary>
    /// Extensions on type <see cref="ExtendedIdentityDbContext{TUser, TRole}"/>.
    /// </summary>
    internal static class ExtendedIdentityDbContextExtensions
    {
        /// <summary>
        /// A method that seeds the database with an admin account.
        /// </summary>
        /// <typeparam name="TUser">The type of user.</typeparam>
        /// <typeparam name="TRole">The type of role.</typeparam>
        /// <param name="dbContext">An extended <see cref="DbContext"/> for the Identity framework.</param>
        public static void SeedAdminUser<TUser, TRole>(this ExtendedIdentityDbContext<TUser, TRole> dbContext)
            where TUser : User, new()
            where TRole : Role, new() {
            if (!dbContext.Database.CanConnect()) {
                return;
            }
            const string adminEmail = "company@indice.gr";
            var hasAdminAccount = dbContext.Users.Any(x => x.Email == adminEmail);
            if (hasAdminAccount) {
                return;
            }
            var admin = new TUser {
                Id = "ab9769f1-d532-4b7d-9922-3da003157ebd",
                Admin = true,
                ConcurrencyStamp = $"{Guid.NewGuid()}",
                CreateDate = DateTime.UtcNow,
                Email = adminEmail,
                EmailConfirmed = true,
                LockoutEnabled = false,
                NormalizedEmail = adminEmail.ToUpper(),
                NormalizedUserName = adminEmail.ToUpper(),
                PasswordHash = "AH6SA/wuxp9YEfLGROaj2CgjhxZhXDkMB1nD8V7lfQAI+WTM4lGMItjLhhV5ASsq+Q==",
                PhoneNumber = "699XXXXXXX",
                PhoneNumberConfirmed = true,
                SecurityStamp = $"{Guid.NewGuid()}",
                UserName = adminEmail
            };
            admin.GenerateDeveloperTotp();
            dbContext.Users.Add(admin);
            dbContext.UserClaims.Add(new IdentityUserClaim<string> {
                ClaimType = JwtClaimTypes.GivenName,
                ClaimValue = "Indice",
                UserId = admin.Id
            });
            dbContext.UserClaims.Add(new IdentityUserClaim<string> {
                ClaimType = JwtClaimTypes.FamilyName,
                ClaimValue = "Company",
                UserId = admin.Id
            });
            var initialRoles = InitialRoles<TRole>.Get();
            dbContext.Roles.AddRange(InitialRoles<TRole>.Get());
            foreach (var role in initialRoles) {
                dbContext.UserRoles.Add(new IdentityUserRole<string> {
                    UserId = admin.Id,
                    RoleId = role.Id
                });
            }
            dbContext.SaveChanges();
        }

        /// <summary>
        /// A method that seeds the database with initial realistic data.
        /// </summary>
        /// <typeparam name="TUser">The type of user.</typeparam>
        /// <typeparam name="TRole">The type of role.</typeparam>
        /// <param name="dbContext">An extended <see cref="DbContext"/> for the Identity framework.</param>
        public static void SeedDummyUsers<TUser, TRole>(this ExtendedIdentityDbContext<TUser, TRole> dbContext)
            where TUser : User, new()
            where TRole : Role, new() {
            dbContext.Users.AddRange(InitialUsers<TUser>.Get(2000));
            dbContext.SaveChanges();
        }

        /// <summary>
        /// A method that seeds the database with initial realistic data.
        /// </summary>
        /// <typeparam name="TUser">The type of user.</typeparam>
        /// <typeparam name="TRole">The type of role.</typeparam>
        /// <param name="dbContext">An extended <see cref="DbContext"/> for the Identity framework.</param>
        /// <param name="initialUsers">A list of initial users provided by the consumer in order to be inserted in the application startup.</param>
        public static void SeedCustomUsers<TUser, TRole>(this ExtendedIdentityDbContext<TUser, TRole> dbContext, IEnumerable<User> initialUsers = null)
            where TUser : User, new()
            where TRole : Role, new() {
            if (initialUsers.Any()) {
                dbContext.Users.AddRange(initialUsers.Cast<TUser>());
                dbContext.SaveChanges();
            }
        }
    }
}
