using System.Linq.Expressions;
using Indice.Features.Identity.Core.Logging.Models;

namespace Indice.Features.Identity.Core.Logging.EntityFrameworkCore;

internal static class ObjectMapping
{
    public static Expression<Func<DbSignInLogEntry, SignInLogEntry>> ToSignInLogEntry = (entry) => new() {
        ActionName = entry.ActionName,
        CreatedAt = entry.CreatedAt,
        Description = entry.Description,
        ExtraData = entry.ExtraData,
        Id = entry.Id,
        ResourceType = entry.ResourceType,
        ResourceId = entry.ResourceId,
        Source = entry.Source,
        Subject = entry.Subject,
        SubjectId = entry.SubjectId,
        SubjectType = entry.SubjectType,
        Succedded = entry.Succedded
    };

    public static DbSignInLogEntry ToDbSignInLogEntry(this SignInLogEntry entry) => new() {
        ActionName = entry.ActionName,
        CreatedAt = entry.CreatedAt,
        Description = entry.Description,
        ExtraData = entry.ExtraData,
        Id = entry.Id,
        ResourceType = entry.ResourceType,
        ResourceId = entry.ResourceId,
        Source = entry.Source,
        Subject = entry.Subject,
        SubjectId = entry.SubjectId,
        SubjectType = entry.SubjectType,
        Succedded = entry.Succedded
    };
}
