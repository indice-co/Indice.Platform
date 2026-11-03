using Indice.Features.Messages.Core.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Messages.Core.Data;

/// <summary>Keeps the denormalized <see cref="DbDistributionList.ContactCount"/> in sync with the list memberships.</summary>
internal static class CampaignsDbContextExtensions
{
    /// <summary>Recounts <see cref="DbDistributionList.ContactCount"/> for the given lists from the committed memberships.</summary>
    /// <remarks>
    /// Call it after the membership change is saved. It is a single set-based UPDATE that writes the current row count,
    /// so concurrent or repeated calls are safe: the last one to run writes the up-to-date value.
    /// </remarks>
    public static async Task RefreshContactCountsAsync(this CampaignsDbContext dbContext, ICollection<Guid> distributionListIds, CancellationToken cancellationToken = default) {
        if (distributionListIds.Count == 0) {
            return;
        }
        await dbContext.DistributionLists
            .Where(list => distributionListIds.Contains(list.Id))
            .ExecuteUpdateAsync(setters => setters.SetProperty(list => list.ContactCount, list => list.ContactDistributionLists.Count), cancellationToken);
    }
}
