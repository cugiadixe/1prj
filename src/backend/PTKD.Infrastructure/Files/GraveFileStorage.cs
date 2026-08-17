using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PTKD.Application.Common.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace PTKD.Infrastructure.Files;

public class GraveFileStorage : IGraveFileStorage
{
    private const int ThumbMaxSize = 320;
    private readonly IConfiguration _configuration;
    private readonly PTKD.Application.Common.Interfaces.IAppSettingsService _settings;
    private readonly ILogger<GraveFileStorage> _logger;

    public GraveFileStorage(
        IConfiguration configuration,
        PTKD.Application.Common.Interfaces.IAppSettingsService settings,
        ILogger<GraveFileStorage> logger)
    {
        _configuration = configuration;
        _settings = settings;
        _logger = logger;
    }

    // Gốc HIỆN TẠI (cho lưu mới): App_Settings runtime → appsettings → mặc định.
    private string CurrentBasePath()
    {
        var configured = _settings.GetValue(PTKD.Application.Common.Interfaces.IAppSettingsService.FileStorageBasePathKey);
        if (!string.IsNullOrWhiteSpace(configured))
            return configured;
        return DefaultBasePath();
    }

    // Gốc MẶC ĐỊNH (appsettings → cwd/storage) — dùng cho file cũ chưa lưu gốc (StorageBasePath NULL).
    private string DefaultBasePath()
        => _configuration["FileStorage:BasePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "storage");

    // Gốc để đọc/xoá một file: gốc đã lưu của file đó; NULL → mặc định.
    private string ResolveForExisting(string? basePath)
        => string.IsNullOrWhiteSpace(basePath) ? DefaultBasePath() : basePath!;

    private string GraveDir(string basePath, string graveKey) => Path.Combine(basePath, "graves", SafeKey(graveKey));
    private string ThumbDir(string basePath, string graveKey) => Path.Combine(GraveDir(basePath, graveKey), "thumb");
    // Chỉ lấy tên file, chặn path traversal (storedName vốn là GUID do app sinh, đây là phòng thủ thêm)
    private static string Safe(string storedName) => Path.GetFileName(storedName);

    // Mã mộ dùng làm tên thư mục: bỏ ký tự lạ + chặn path traversal. Mã hợp lệ dạng "A-0001".
    // Rỗng/không hợp lệ thì lùi về "_" để không ghi ra ngoài gốc.
    private static string SafeKey(string graveKey)
    {
        var trimmed = Path.GetFileName(graveKey?.Trim() ?? string.Empty);
        var cleaned = new string((trimmed).Where(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.').ToArray());
        return string.IsNullOrEmpty(cleaned) ? "_" : cleaned;
    }

    public async Task<StoredFileResult> SaveAsync(string graveKey, string storedName, Stream content, string contentType, bool generateThumbnail, CancellationToken ct = default)
    {
        var name = Safe(storedName);
        var basePath = CurrentBasePath();
        var dir = GraveDir(basePath, graveKey);
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, name);

        long size;
        await using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await content.CopyToAsync(fs, ct);
            size = fs.Length;
        }

        var hasThumbnail = false;
        if (generateThumbnail && contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                Directory.CreateDirectory(ThumbDir(basePath, graveKey));
                var thumbPath = Path.Combine(ThumbDir(basePath, graveKey), name + ".jpg");
                using var image = await Image.LoadAsync(filePath, ct);
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(ThumbMaxSize, ThumbMaxSize)
                }));
                await image.SaveAsJpegAsync(thumbPath, ct);
                hasThumbnail = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không tạo được thumbnail cho file {StoredName} của mộ {GraveKey}", name, graveKey);
            }
        }

        return new StoredFileResult(size, hasThumbnail, basePath);
    }

    public Stream? OpenRead(string? basePath, string graveKey, string storedName)
    {
        var root = ResolveForExisting(basePath);
        var path = Path.Combine(GraveDir(root, graveKey), Safe(storedName));
        return File.Exists(path) ? File.OpenRead(path) : null;
    }

    public Stream? OpenReadThumbnail(string? basePath, string graveKey, string storedName)
    {
        var root = ResolveForExisting(basePath);
        var path = Path.Combine(ThumbDir(root, graveKey), Safe(storedName) + ".jpg");
        return File.Exists(path) ? File.OpenRead(path) : null;
    }

    public void Delete(string? basePath, string graveKey, string storedName)
    {
        var root = ResolveForExisting(basePath);
        var name = Safe(storedName);
        var filePath = Path.Combine(GraveDir(root, graveKey), name);
        if (File.Exists(filePath)) File.Delete(filePath);
        var thumbPath = Path.Combine(ThumbDir(root, graveKey), name + ".jpg");
        if (File.Exists(thumbPath)) File.Delete(thumbPath);
    }
}
