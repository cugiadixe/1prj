using System;

namespace PTKD.Domain.Entities;

/// <summary>Cấu hình hệ thống dạng key/value, sửa được lúc chạy (không cần đổi appsettings).</summary>
public class AppSetting
{
    public long Id { get; private set; }
    public string SettingKey { get; private set; } = null!;
    public string? SettingValue { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public long? UpdatedByUserId { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    private AppSetting() { }

    public AppSetting(string settingKey, string? settingValue, long? updatedByUserId)
    {
        SettingKey = settingKey ?? throw new ArgumentNullException(nameof(settingKey));
        SettingValue = settingValue;
        UpdatedByUserId = updatedByUserId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetValue(string? settingValue, long? updatedByUserId)
    {
        SettingValue = settingValue;
        UpdatedByUserId = updatedByUserId;
        UpdatedAt = DateTime.UtcNow;
    }
}
