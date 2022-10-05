using System;
using System.Security.Claims;
using IdentityModel;
using Indice.Security;

namespace Indice.Features.Cases.Data.Models
{
    /// <summary>
    /// Audit metadata related with the user principal that "did" the action.
    /// </summary>
    public class AuditMeta
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime? When { get; set; }

        public void Clear() {
            Id = null;
            Name = null;
            Email = null;
            When = null;
        }

        public void Update(ClaimsPrincipal user, DateTime? now = null) {
            Populate(this, user, now);
        }

        public static AuditMeta Create(ClaimsPrincipal user, DateTime? now = null) {
            return Populate(null, user, now);
        }

        private static AuditMeta Populate(AuditMeta meta, ClaimsPrincipal user, DateTime? now = null) {
            meta = meta ?? new AuditMeta();
            meta.Id = user.FindFirstValue(JwtClaimTypes.Subject);
            meta.Email = user.FindFirstValue(JwtClaimTypes.Email);
            meta.Name = $"{user.FindFirstValue(BasicClaimTypes.GivenName)} {user.FindFirstValue(BasicClaimTypes.FamilyName)}".Trim();
            meta.When = now ?? DateTime.UtcNow;
            return meta;
        }
    }
}