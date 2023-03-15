using System.Linq.Expressions;
using Indice.Features.Identity.Core.Logging.Models;

namespace Indice.Features.Identity.Core.Logging.EntityFrameworkCore;

internal static class ObjectMapping
{
    public static Expression<Func<DbSignInLogEntry, SignInLogEntry>> ToSignInLogEntry = (entry) => new() {
        ActionName = entry.ActionName,
        ApplicationId = entry.ApplicationId,
        ApplicationName = entry.ApplicationName,
        CreatedAt = entry.CreatedAt,
        Description = entry.Description,
        ExtraData = entry.ExtraData,
        Id = entry.Id,
        IpAddress = entry.IpAddress,
        Location = entry.Location,
        RequestId = entry.RequestId,
        ResourceId = entry.ResourceId,
        ResourceType = entry.ResourceType,
        SessionId = entry.SessionId,
        SignInType = entry.SignInType,
        SubjectId = entry.SubjectId,
        SubjectName = entry.SubjectName,
        Succedded = entry.Succedded
    };

    public static DbSignInLogEntry ToDbSignInLogEntry(this SignInLogEntry entry) => new() {
        ActionName = entry.ActionName,
        ApplicationId = entry.ApplicationId,
        ApplicationName = entry.ApplicationName,
        CreatedAt = entry.CreatedAt,
        Description = entry.Description,
        ExtraData = entry.ExtraData,
        Id = entry.Id,
        IpAddress = entry.IpAddress,
        Location = entry.Location,
        RequestId = entry.RequestId,
        ResourceId = entry.ResourceId,
        ResourceType = entry.ResourceType,
        SessionId = entry.SessionId,
        SignInType = entry.SignInType,
        SubjectId = entry.SubjectId,
        SubjectName = entry.SubjectName,
        Succedded = entry.Succedded
    };
}
