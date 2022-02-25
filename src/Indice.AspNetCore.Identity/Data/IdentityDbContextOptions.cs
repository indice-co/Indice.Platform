using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using IdentityModel;
using Indice.AspNetCore.Identity.Api.Models;
using Indice.AspNetCore.Identity.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Indice.AspNetCore.Identity.Data
{
    /// <summary>
    /// Options for configuring <see cref="IdentityDbContext"/>.
    /// </summary>
    public class IdentityDbContextOptions
    {
        /// <summary>
        /// Callback to configure the EF DbContext.
        /// </summary>
        public Action<DbContextOptionsBuilder> ConfigureDbContext { get; set; }
        /// <summary>
        /// The expression used to filter the users list in the API.
        /// </summary>
        public static UserSearchFilterExpression<User, Role> UserSearchFilter { get; set; } = DefaultUserSearchFilterExpression;

        private static async Task<Expression<Func<UserInfo, bool>>> DefaultUserSearchFilterExpression<TUser, TRole>(IdentityDbContext<TUser, TRole> dbContext, string search) where TUser : User where TRole : Role {
            var searchTerm = search.ToLower();
            var idsFromClaims = await dbContext
                .UserClaims
                .Where(x => (x.ClaimType == JwtClaimTypes.GivenName || x.ClaimType == JwtClaimTypes.FamilyName) && EF.Functions.Like(x.ClaimValue.ToLower(), $"%{searchTerm}%"))
                .Select(x => x.UserId)
                .ToArrayAsync();
            return x => EF.Functions.Like(x.Email.ToLower(), $"%{searchTerm}%")
                     || EF.Functions.Like(x.PhoneNumber.ToLower(), $"%{searchTerm}%")
                     || EF.Functions.Like(x.UserName.ToLower(), $"%{searchTerm}%")
                     || searchTerm == x.Id.ToLower()
                     || idsFromClaims.Contains(x.Id);
        }
    }

    /// <summary>
    /// A delegate that refers to the expression that is used to filter the users list in the API.
    /// </summary>
    /// <typeparam name="TUser">The user type.</typeparam>
    /// <typeparam name="TRole">The role type.</typeparam>
    /// <param name="dbContext"><see cref="DbContext"/> for the Identity Framework.</param>
    /// <param name="search">The search term.</param>
    public delegate Task<Expression<Func<UserInfo, bool>>> UserSearchFilterExpression<TUser, TRole>(IdentityDbContext<TUser, TRole> dbContext, string search) where TUser : User where TRole : Role;
}
