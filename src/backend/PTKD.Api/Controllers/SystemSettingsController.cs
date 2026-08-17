using System;
using System.IO;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using PTKD.Api.Security.Authorization;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Security.Authorization.Attributes;
using PTKD.Application.Security.Authorization.Models;

namespace PTKD.API.Controllers;

[ApiController]
[Route("api/v2/system/settings")]
[Authorize]
[RequirePermission(PermissionCodes.SystemSettingManage, PermissionScope.Global)]
public class SystemSettingsController : ControllerBase
{
    private readonly IAppSettingsService _settings;
    private readonly IConfiguration _configuration;

    public SystemSettingsController(IAppSettingsService settings, IConfiguration configuration)
    {
        _settings = settings;
        _configuration = configuration;
    }

    public sealed class StoragePathDto
    {
        /// <summary>Đường dẫn admin đã cấu hình (null = đang dùng mặc định).</summary>
        public string? ConfiguredPath { get; set; }
        /// <summary>Đường dẫn mặc định (appsettings) khi chưa cấu hình.</summary>
        public string DefaultPath { get; set; } = null!;
        /// <summary>Đường dẫn ĐANG dùng thực tế.</summary>
        public string EffectivePath { get; set; } = null!;
    }

    public sealed class UpdateStoragePathRequest
    {
        /// <summary>Đường dẫn gốc mới. Để trống = quay về mặc định (appsettings).</summary>
        public string? Path { get; set; }
    }

    [HttpGet("storage-path")]
    public async Task<IActionResult> GetStoragePath(CancellationToken ct)
    {
        var configured = await _settings.GetValueAsync(IAppSettingsService.FileStorageBasePathKey, ct);
        var defaultPath = _configuration["FileStorage:BasePath"]
            ?? System.IO.Path.Combine(Directory.GetCurrentDirectory(), "storage");
        return Ok(new StoragePathDto
        {
            ConfiguredPath = string.IsNullOrWhiteSpace(configured) ? null : configured,
            DefaultPath = defaultPath,
            EffectivePath = string.IsNullOrWhiteSpace(configured) ? defaultPath : configured!,
        });
    }

    [HttpPut("storage-path")]
    public async Task<IActionResult> SetStoragePath([FromBody] UpdateStoragePathRequest request, CancellationToken ct)
    {
        var path = request.Path?.Trim();

        if (!string.IsNullOrEmpty(path))
        {
            // Phải là đường dẫn tuyệt đối + tạo/ghi được thì mới nhận (tránh cấu hình đường chết).
            if (!System.IO.Path.IsPathRooted(path))
                return BadRequest(new ProblemDetails { Status = 400, Title = "Đường dẫn không hợp lệ", Detail = "Đường dẫn phải là tuyệt đối (vd D:\\ptkd-storage)." });
            try
            {
                Directory.CreateDirectory(path);
                var probe = System.IO.Path.Combine(path, ".ptkd_write_test");
                await System.IO.File.WriteAllTextAsync(probe, "ok", ct);
                System.IO.File.Delete(probe);
            }
            catch (Exception ex)
            {
                return BadRequest(new ProblemDetails { Status = 400, Title = "Không ghi được vào đường dẫn", Detail = ex.Message });
            }
        }

        await _settings.SetValueAsync(IAppSettingsService.FileStorageBasePathKey,
            string.IsNullOrEmpty(path) ? null : path, GetActorUserId(), ct);

        return await GetStoragePath(ct);
    }

    private long GetActorUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return long.Parse(claim!);
    }
}
