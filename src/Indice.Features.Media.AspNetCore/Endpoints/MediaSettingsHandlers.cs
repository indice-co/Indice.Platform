using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http;
using Indice.Features.Media.AspNetCore.Models;
using Indice.Features.Media.AspNetCore.Models.Requests;
using Indice.Features.Media.AspNetCore.Services;

namespace Indice.Features.Media.AspNetCore.Endpoints;
internal static class MediaSettingsHandlers
{
    internal static async Task<Results<Ok<MediaSetting>, NotFound>> GetMediaSetting(string key, IMediaSettingService mediaSettingService) {
        var setting = await mediaSettingService.GetSetting(key);
        if (setting is null) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(setting);
    }

    internal static async Task<Ok<List<MediaSetting>>> ListMediaSettings(IMediaSettingService mediaSettingService) {
        var settings = await mediaSettingService.ListSettings();
        return TypedResults.Ok(settings);
    }

    internal static async Task<Results<Ok, BadRequest>> UpdateMediaSetting(string key, UpdateMediaSettingRequest request, IMediaSettingService mediaSettingService) {
        if (!MediaSetting.GetAll().Any(s => s.Key == key)) {
            return TypedResults.BadRequest();
        }
        await mediaSettingService.UpdateSetting(key, request.Value);
        return TypedResults.Ok();
    }
}
