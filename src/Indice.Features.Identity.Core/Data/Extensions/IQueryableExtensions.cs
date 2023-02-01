using System;
using System.Linq;
using Indice.Features.Identity.Core.Models;

namespace Indice.Features.Identity.Core.Data.Extensions
{
    /// <summary>Extension methods on various <see cref="IQueryable{T}"/> instances.</summary>
    public static class IQueryableExtensions
    {
        /// <summary>Applies a filter of type <see cref="UserDeviceListFilter"/> on <seealso cref="IQueryable{UserDevice}"/>.</summary>
        /// <param name="query">The query.</param>
        /// <param name="filter">The filter to apply.</param>
        public static IQueryable<UserDevice> ApplyFilter(this IQueryable<UserDevice> query, UserDeviceListFilter filter) {
            if (filter is not null) {
                query = query.Where(device =>
                    (filter.IsPushNotificationEnabled == null || device.IsPushNotificationsEnabled == filter.IsPushNotificationEnabled) &&
                    ((filter.IsTrusted == null || device.IsTrusted == filter.IsTrusted) || 
                    (
                        filter.IsPendingTrustActivation == null || 
                        (filter.IsPendingTrustActivation.Value && device.TrustActivationDate.HasValue && device.TrustActivationDate.Value > DateTimeOffset.UtcNow) || 
                        (!filter.IsPendingTrustActivation.Value && device.TrustActivationDate.HasValue && device.TrustActivationDate.Value < DateTimeOffset.UtcNow)
                    )) &&
                    (filter.Blocked == null || device.Blocked == filter.Blocked) &&
                    (filter.ClientType == null || device.ClientType == filter.ClientType)
                );
            }
            return query;
        }
    }
}
