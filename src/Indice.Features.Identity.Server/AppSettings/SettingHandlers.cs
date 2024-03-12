using Indice.Extensions.Configuration.Database;
using Indice.Extensions.Configuration.Database.Data.Models;
using Indice.Features.Identity.Server.AppSettings.Models;
using Indice.Types;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Hosting;

namespace Indice.Features.Identity.Server.AppSettings;

internal class SettingHandlers
{
    internal static async Task<Ok<ResultSet<AppSettingInfo>>> GetSettings(
        [FromServices] IAppSettingsDbContext dbContext,
        [AsParameters] ListOptions options
    ) {
        var query = dbContext.AppSettings.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(options.Search)) {
            var searchTerm = options.Search.ToLower();
            query = query.Where(x => x.Key.ToLower().Contains(searchTerm));
        }
        var settings = await query.Select(x => new AppSettingInfo {
            Key = x.Key,
            Value = x.Value
        })
        .ToResultSetAsync(options);
        return TypedResults.Ok(settings);
    }

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> LoadFromAppSettingsJson(
        [FromServices] IWebHostEnvironment webHostEnvironment,
        [FromServices] IAppSettingsDbContext dbContext,
        bool hardRefresh = false
    ) {
        if (!webHostEnvironment.IsDevelopment()) {
            return TypedResults.NotFound();
        }
        var fileInfo = webHostEnvironment.ContentRootFileProvider.GetFileInfo("appsettings.json");
        var settingsExist = await dbContext.AppSettings.AnyAsync();
        if (settingsExist && !hardRefresh) {
            return TypedResults.ValidationProblem(ValidationErrors.AddError(nameof(hardRefresh), "App settings are already loaded in the database."));
        }
        IDictionary<string, string> settings;
        using (var stream = fileInfo.CreateReadStream()) {
            settings = JsonConfigurationFileParser.Parse(stream);
        }
        if (settingsExist) {
            await dbContext.AppSettings.ExecuteDeleteAsync();
        }
        dbContext.AppSettings.AddRange(settings.Select(x => new DbAppSetting {
            Key = x.Key,
            Value = x.Value
        }));
        await dbContext.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    internal static async Task<Results<Ok<AppSettingInfo>, NotFound>> GetSettingByKey(
        [FromServices] IAppSettingsDbContext dbContext,
        string key
    ) {
        var setting = await dbContext
            .AppSettings
            .AsNoTracking()
            .Select(x => new AppSettingInfo {
                Key = x.Key,
                Value = x.Value
            })
            .SingleOrDefaultAsync(x => x.Key == key);
        if (setting == null) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(setting);
    }

    internal static async Task<CreatedAtRoute<AppSettingInfo>> CreateSetting(
        [FromServices] IAppSettingsDbContext dbContext,
        CreateAppSettingRequest request
    ) {
        var setting = new DbAppSetting {
            Key = request.Key,
            Value = request.Value
        };
        dbContext.AppSettings.Add(setting);
        await dbContext.SaveChangesAsync();
        return TypedResults.CreatedAtRoute(new AppSettingInfo {
            Key = setting.Key,
            Value = setting.Value
        }, nameof(GetSettingByKey), new { key = setting.Key });
    }

    internal static async Task<Results<Ok<AppSettingInfo>, NotFound>> UpdateSetting(
        [FromServices] IAppSettingsDbContext dbContext,
        string key,
        UpdateAppSettingRequest request
    ) {
        var setting = await dbContext.AppSettings.SingleOrDefaultAsync(x => x.Key == key);
        if (setting == null) {
            return TypedResults.NotFound();
        }
        setting.Value = request.Value;
        // Commit changes to database.
        await dbContext.SaveChangesAsync();
        // Send the response.
        return TypedResults.Ok(new AppSettingInfo {
            Key = setting.Key,
            Value = setting.Value
        });
    }

    internal static async Task<Results<NoContent, NotFound>> DeleteSetting(
        [FromServices] IAppSettingsDbContext dbContext,
        string key
    ) {
        var setting = await dbContext.AppSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Key == key);
        if (setting == null) {
            return TypedResults.NotFound();
        }
        dbContext.AppSettings.Remove(setting);
        await dbContext.SaveChangesAsync();
        return TypedResults.NoContent();
    }
}
