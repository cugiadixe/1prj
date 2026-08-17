using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PTKD.Application.Common.Interfaces;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence;

/// <inheritdoc cref="IAppSettingsService"/>
public class AppSettingsService : IAppSettingsService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);
    private const string CachePrefix = "app-setting:";

    private readonly IOrganizationDbContextFactory _dbContextFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AppSettingsService> _logger;

    public AppSettingsService(IOrganizationDbContextFactory dbContextFactory, IMemoryCache cache, ILogger<AppSettingsService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _cache = cache;
        _logger = logger;
    }

    public string? GetValue(string key)
    {
        return _cache.GetOrCreate(CachePrefix + key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheTtl;
            try
            {
                using var ctx = _dbContextFactory.CreateDbContext();
                return ctx.AppSettings.AsNoTracking()
                    .Where(s => s.SettingKey == key)
                    .Select(s => s.SettingValue)
                    .FirstOrDefault();
            }
            catch (Exception ex)
            {
                // Phòng thủ: bảng chưa tồn tại (chưa migrate) hoặc lỗi đọc -> coi như chưa cấu hình,
                // nơi gọi tự lùi về mặc định (appsettings). Không để hỏng đường lưu file.
                _logger.LogWarning(ex, "Không đọc được cấu hình {Key}; dùng mặc định.", key);
                return null;
            }
        });
    }

    public async Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default)
    {
        await using var ctx = _dbContextFactory.CreateDbContext();
        return await ctx.AppSettings.AsNoTracking()
            .Where(s => s.SettingKey == key)
            .Select(s => s.SettingValue)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task SetValueAsync(string key, string? value, long actingUserId, CancellationToken cancellationToken = default)
    {
        await using var ctx = _dbContextFactory.CreateDbContext();
        var existing = await ctx.AppSettings.FirstOrDefaultAsync(s => s.SettingKey == key, cancellationToken);
        if (existing is null)
            ctx.AppSettings.Add(new AppSetting(key, value, actingUserId));
        else
            existing.SetValue(value, actingUserId);
        await ctx.SaveChangesAsync(cancellationToken);

        _cache.Remove(CachePrefix + key);
    }
}
