using System.Threading;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Models;

namespace Indice.AspNetCore.Identity.Extensions
{
    public interface IExtendedUserStore<TUser> where TUser : User
    {
        int? PasswordHistoryLimit { get; }
        double? PasswordHistoryRetentionDays { get; }
        PasswordExpirationPolicy? PasswordExpirationPolicy { get; }
        Task SetPasswordExpirationPolicyAsync(TUser user, PasswordExpirationPolicy? policy, CancellationToken cancellationToken);
        Task SetMustRevalidateAsync(TUser user, bool mustRevalidate, CancellationToken cancellationToken);
    }
}
