using System.Security.Claims;
using System.ServiceModel.Syndication;
using System.Xml;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Server.Manager.Models;
using Indice.Security;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

namespace Indice.Features.Identity.Server.Manager;

internal static class DashboardHandlers
{
    internal static Ok<ResultSet<BlogItemInfo>> GetNews(int? page, int? size) {
        const string url = "https://www.identityserver.com/rss";
        var feedItems = new List<BlogItemInfo>();
        using (var reader = XmlReader.Create(url)) {
            var feed = SyndicationFeed.Load(reader);
            feedItems.AddRange(
                feed.Items.Select(post => new BlogItemInfo {
                    Title = post.Title?.Text,
                    Link = post.Links[0].Uri.AbsoluteUri,
                    PublishDate = post.PublishDate.DateTime,
                    Description = post.Summary?.Text
                })
            );
        }
        var p = page.GetValueOrDefault(1);
        var s = size.GetValueOrDefault(100);
        var response = feedItems.Skip((p - 1) * s).Take(s).ToArray();
        return TypedResults.Ok(new ResultSet<BlogItemInfo>(response, feedItems.Count));
    }

    internal static async Task<Ok<SummaryInfo>> GetSystemSummary(
        ExtendedConfigurationDbContext configurationDbContext,
        ExtendedUserManager<User> userManager,
        RoleManager<Role> roleManager,
        IFeatureManager featureManager,
        ClaimsPrincipal currentUser) {
        // Get total number of users in the system.
        var numberOfUsers = currentUser.CanReadUsers() ? await userManager.Users.CountAsync() : 0;
        // Get total number of roles in the system.
        var numberOfRoles = currentUser.CanReadUsers() ? await roleManager.Roles.CountAsync() : 0;
        // Get total number of clients in the system.
        var numberOfClients = currentUser.CanReadClients() ? await configurationDbContext.Clients.CountAsync() : 0;
        var metrics = new SummaryInfo {
            LastUpdatedAt = DateTime.UtcNow,
            TotalUsers = numberOfUsers,
            TotalRoles = numberOfRoles,
            TotalClients = numberOfClients
        };
        var metricsFeatureEnabled = await featureManager.IsEnabledAsync(IdentityEndpoints.Features.DashboardMetrics);
        if (!metricsFeatureEnabled || !currentUser.CanReadUsers()) {
            return TypedResults.Ok(metrics);
        }
        // Get percentage of active users (users that have logged into the system) on a daily/weekly/monthly basis.
        var dailyActiveUsers = await userManager.Users.CountAsync(x => x.LastSignInDate >= DateTime.UtcNow.Date);
        var weeklyActiveUsers = await userManager.Users.CountAsync(x => x.LastSignInDate >= DateTime.UtcNow.Date.AddDays(-7));
        var monthlyActiveUsers = await userManager.Users.CountAsync(x => x.LastSignInDate >= DateTime.UtcNow.Date.AddDays(-30));
        metrics.Activity = new UsersActivityInfo {
            Day = new SummaryStatistic(count: dailyActiveUsers, percent: Math.Round(dailyActiveUsers / (double)numberOfUsers * 100, 2)),
            Week = new SummaryStatistic(count: weeklyActiveUsers, percent: Math.Round(weeklyActiveUsers / (double)numberOfUsers * 100, 2)),
            Month = new SummaryStatistic(count: monthlyActiveUsers, percent: Math.Round(monthlyActiveUsers / (double)numberOfUsers * 100, 2))
        };
        var userWithVerifiedEmail = await userManager.Users.CountAsync(x => x.EmailConfirmed);
        var userWithVerifiedPhoneNumber = await userManager.Users.CountAsync(x => x.PhoneNumberConfirmed);
        metrics.Stats = new UsersStatisticsInfo {
            EmailsVerified = new SummaryStatistic(count: userWithVerifiedEmail, percent: Math.Round(userWithVerifiedEmail / (double)numberOfUsers * 100, 2)),
            PhoneNumbersVerified = new SummaryStatistic(count: userWithVerifiedPhoneNumber, percent: Math.Round(userWithVerifiedPhoneNumber / (double)numberOfUsers * 100, 2))
        };
        return TypedResults.Ok(metrics);
    }

    internal static async Task<Ok<UiFeaturesInfo>> GetUiFeatures(IFeatureManager featureManager) =>
        TypedResults.Ok(new UiFeaturesInfo {
            MetricsEnabled = await featureManager.IsEnabledAsync(IdentityServerFeatures.DashboardMetrics),
            SignInLogsEnabled = await featureManager.IsEnabledAsync(IdentityServerFeatures.SignInLogs)
        });
}
