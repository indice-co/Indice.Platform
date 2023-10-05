using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Indice.Features.Media.AspNetCore.Data.Models;
using Indice.Features.Media.AspNetCore.Models;
using Indice.Features.Media.AspNetCore.Stores;
using Indice.Features.Media.Data;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Media.AspNetCore.Services;
/// <summary>An implementation of <see cref="IMediaSettingService"/>.</summary>
internal class MediaSettingService : IMediaSettingService
{
    private readonly MediaDbContext _dbContext;

    /// <summary>Creates a new instance of <see cref="MediaFolderStore"/>.</summary>
    /// <param name="dbContext">The <see cref="DbContext"/> for Media API feature.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public MediaSettingService(MediaDbContext dbContext) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc/>
    public async Task<MediaSetting?> GetSetting(string key) {
        var setting = MediaSetting.GetAll().FirstOrDefault(x => x.Key == key);
        if (setting is null) {
            return default;
        }
        var dbSetting = await _dbContext.Settings.FirstOrDefaultAsync(s => s.Key == key);
        if (dbSetting is not null) {
            setting.Value = dbSetting.Value;
            setting.CreatedBy = dbSetting.CreatedBy;
            setting.CreatedAt = dbSetting.CreatedAt;
            setting.UpdatedAt = dbSetting.UpdatedAt;
            setting.UpdatedBy = dbSetting.UpdatedBy;
        }
        return setting;
    }
    /// <inheritdoc/>
    public async Task<List<MediaSetting>> ListSettings() {
        var settings = MediaSetting.GetAll();
        if (settings is null) {
            return new List<MediaSetting>();
        }
        var dbSettings = await _dbContext.Settings
            .Where(s => settings.Select(i => i.Key).Contains(s.Key))
            .ToListAsync();
        if (dbSettings is not null) {
            foreach (var setting in settings) {
                var dbSetting = dbSettings.FirstOrDefault(s => s.Key == setting.Key);
                if (dbSetting is not null) {
                    setting.Value = dbSetting.Value;
                    setting.CreatedBy = dbSetting.CreatedBy;
                    setting.CreatedAt = dbSetting.CreatedAt;
                    setting.UpdatedAt = dbSetting.UpdatedAt;
                    setting.UpdatedBy = dbSetting.UpdatedBy;
                }
            }
        }
        return settings;
    }
    /// <inheritdoc/>
    public async Task UpdateSetting(string key, string value) {
        var dbSetting = await _dbContext.Settings.FirstOrDefaultAsync(s => s.Key == key);
        if (dbSetting is not null) {
            dbSetting.Value = value;
        }
        else {
            dbSetting = new DbMediaSetting {
                Key = key,
                Value = value
            };
            _dbContext.Settings.Add(dbSetting);
        }
        
        await _dbContext.SaveChangesAsync();
    }
}
