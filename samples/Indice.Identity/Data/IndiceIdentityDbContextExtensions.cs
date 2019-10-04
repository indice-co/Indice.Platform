using System;
using IdentityModel;
using Indice.AspNetCore.Identity.Models;
using Indice.Identity.Data.Init;
using Microsoft.AspNetCore.Identity;

namespace Indice.Identity.Data
{
    /// <summary>
    /// Extensions on type <see cref="ExtendedIdentityDbContext"/>.
    /// </summary>
    public static class IndiceIdentityDbContextExtensions
    {
        /// <summary>
        /// A method that seeds the database with initial realistic data.
        /// </summary>
        /// <param name="dbContext"></param>
        public static void Seed(this ExtendedIdentityDbContext dbContext) {
            // Create an admin account.
            const string adminEmail = "company@indice.gr";
            var admin = new User(adminEmail, "ab9769f1-d532-4b7d-9922-3da003157ebd") {
                Admin = true,
                ConcurrencyStamp = $"{Guid.NewGuid()}",
                CreateDate = DateTime.UtcNow,
                Email = adminEmail,
                EmailConfirmed = true,
                LockoutEnabled = false,
                NormalizedEmail = adminEmail.ToUpper(),
                NormalizedUserName = adminEmail.ToUpper(),
                PasswordHash = "AH6SA/wuxp9YEfLGROaj2CgjhxZhXDkMB1nD8V7lfQAI+WTM4lGMItjLhhV5ASsq+Q==",
                PhoneNumber = "+30 2106985955",
                PhoneNumberConfirmed = true,
                SecurityStamp = $"{Guid.NewGuid()}",
                UserName = adminEmail
            };
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
            dbContext.Users.AddRange(InitialUsers.Get(2000));
            dbContext.ClaimTypes.AddRange(InitialClaimTypes.Get());
            dbContext.Roles.AddRange(InitialRoles.Get());
            dbContext.SaveChanges();
        }
    }
}
