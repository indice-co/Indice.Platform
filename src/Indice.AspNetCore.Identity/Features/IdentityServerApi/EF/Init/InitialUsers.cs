using System;
using System.Collections.Generic;
using System.Linq;
using Bogus;
using IdentityModel;
using Indice.AspNetCore.Identity.Models;
using Microsoft.AspNetCore.Identity;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Provides functionality to generate test users for development purposes.
    /// </summary>
    internal class InitialUsers
    {
        private const int DefaultNumberOfUsers = 100;

        private static readonly Faker<User> UserFaker = new Faker<User>()
            .RuleFor(x => x.Id, faker => $"{Guid.NewGuid()}")
            .RuleFor(x => x.Admin, faker => false)
            .RuleFor(x => x.ConcurrencyStamp, faker => $"{Guid.NewGuid()}")
            .RuleFor(x => x.SecurityStamp, faker => $"{Guid.NewGuid()}")
            .RuleFor(x => x.CreateDate, faker => faker.Date.BetweenOffset(DateTimeOffset.UtcNow.AddYears(-4), DateTimeOffset.UtcNow))
            .RuleFor(x => x.PhoneNumber, faker => faker.Phone.PhoneNumber())
            .RuleFor(x => x.PhoneNumberConfirmed, faker => faker.PickRandom(true, false))
            .RuleFor(x => x.EmailConfirmed, faker => faker.PickRandom(true, false))
            .RuleFor(x => x.Claims, (faker, user) => {
                user.Claims.Add(new IdentityUserClaim<string> {
                    ClaimType = JwtClaimTypes.GivenName,
                    ClaimValue = faker.Name.FirstName(),
                    UserId = user.Id
                });
                user.Claims.Add(new IdentityUserClaim<string> {
                    ClaimType = JwtClaimTypes.FamilyName,
                    ClaimValue = faker.Name.LastName(),
                    UserId = user.Id
                });
                return user.Claims;
            })
            .RuleFor(x => x.Email, (faker, user) => faker.Internet.Email(
                firstName: user.Claims.Single(x => x.ClaimType == JwtClaimTypes.GivenName).ClaimValue,
                lastName: user.Claims.Single(x => x.ClaimType == JwtClaimTypes.FamilyName).ClaimValue
            ))
            .RuleFor(x => x.UserName, (faker, user) => user.Email)
            .RuleFor(x => x.NormalizedEmail, (faker, user) => user.Email.ToUpper())
            .RuleFor(x => x.NormalizedUserName, (faker, user) => user.Email.ToUpper());

        /// <summary>
        /// Gets a collection of test users.
        /// </summary>
        /// <param name="numberOfUsers">The number of test users to generate. Default is 100.</param>
        public static IReadOnlyCollection<User> Get(int? numberOfUsers = null) {
            var random = new Random(1);
            Randomizer.Seed = random;
            return UserFaker.Generate(numberOfUsers ?? DefaultNumberOfUsers).ToList();
        }
    }
}
