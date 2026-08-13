using System;
using System.IO;
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
    private readonly string _basePath;
    private readonly ILogger<GraveFileStorage> _logger;

    public GraveFileStorage(IConfiguration configuration, ILogger<GraveFileStorage> logger)
    {
        _logger = logger;
        _basePath = configuration["FileStorage:BasePath"]
            ?? Path.Combine(Directory.GetCurrentDirectory(), "storage");
    }

    private string GraveDir(long graveId) => Path.Combine(_basePath, "graves", graveId.ToString());
    private string ThumbDir(long graveId) => Path.Combine(GraveDir(graveId), "thumb");
    // Chỉ lấy tên file, chặn path traversal (storedName vốn là GUID do app sinh, đây là phòng thủ thêm)
    private static string Safe(string storedName) => Path.GetFileName(storedName);

    public async Task<StoredFileResult> SaveAsync(long graveId, string storedName, Stream content, string contentType, bool generateThumbnail, CancellationToken ct = default)
    {
        var name = Safe(storedName);
        var dir = GraveDir(graveId);
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
                Directory.CreateDirectory(ThumbDir(graveId));
                var thumbPath = Path.Combine(ThumbDir(graveId), name + ".jpg");
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
                _logger.LogWarning(ex, "Không tạo được thumbnail cho file {StoredName} của mộ {GraveId}", name, graveId);
            }
        }

        return new StoredFileResult(size, hasThumbnail);
    }

    public Stream? OpenRead(long graveId, string storedName)
    {
        var path = Path.Combine(GraveDir(graveId), Safe(storedName));
        return File.Exists(path) ? File.OpenRead(path) : null;
    }

    public Stream? OpenReadThumbnail(long graveId, string storedName)
    {
        var path = Path.Combine(ThumbDir(graveId), Safe(storedName) + ".jpg");
        return File.Exists(path) ? File.OpenRead(path) : null;
    }

    public void Delete(long graveId, string storedName)
    {
        var name = Safe(storedName);
        var filePath = Path.Combine(GraveDir(graveId), name);
        if (File.Exists(filePath)) File.Delete(filePath);
        var thumbPath = Path.Combine(ThumbDir(graveId), name + ".jpg");
        if (File.Exists(thumbPath)) File.Delete(thumbPath);
    }
}
