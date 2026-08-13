using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Common.Exceptions;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Graves.DTOs;
using PTKD.Domain.Entities;

namespace PTKD.Application.Graves.Services;

public class GraveAttachmentService : IGraveAttachmentService
{
    private const long MaxSizeBytes = 10 * 1024 * 1024; // 10MB
    private static readonly Dictionary<string, string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
        ["application/pdf"] = ".pdf",
    };
    private static readonly HashSet<string> AllowedCategories = new()
    {
        GraveAttachment.CategoryPhoto, GraveAttachment.CategoryTransferDoc, GraveAttachment.CategoryOther
    };

    private readonly IOrganizationDbContextFactory _dbContextFactory;
    private readonly IGraveFileStorage _storage;

    public GraveAttachmentService(IOrganizationDbContextFactory dbContextFactory, IGraveFileStorage storage)
    {
        _dbContextFactory = dbContextFactory;
        _storage = storage;
    }

    public async Task<GraveAttachmentDto> UploadAsync(long graveId, string category, long? ownershipHistoryId,
        string fileName, string contentType, long size, Stream content, string? description, long actorUserId, CancellationToken ct = default)
    {
        if (!AllowedCategories.Contains(category))
            throw new BusinessRuleValidationException("GRAVE_ATTACHMENT_INVALID_CATEGORY", "Loại đính kèm không hợp lệ.");
        if (!AllowedTypes.TryGetValue(contentType, out var ext))
            throw new BusinessRuleValidationException("GRAVE_ATTACHMENT_INVALID_TYPE", "Chỉ chấp nhận ảnh (JPG/PNG/WEBP) hoặc PDF.");
        if (size <= 0 || size > MaxSizeBytes)
            throw new BusinessRuleValidationException("GRAVE_ATTACHMENT_TOO_LARGE", "File rỗng hoặc vượt quá 10MB.");

        await using var context = _dbContextFactory.CreateDbContext();
        if (!await context.Graves.AnyAsync(g => g.Id == graveId, ct))
            throw new EntityNotFoundException("GRAVE_NOT_FOUND", "Grave not found.");
        if (ownershipHistoryId.HasValue &&
            !await context.GraveOwnershipHistories.AnyAsync(h => h.Id == ownershipHistoryId.Value && h.GraveId == graveId, ct))
            throw new EntityNotFoundException("GRAVE_OWNERSHIP_HISTORY_NOT_FOUND", "Ownership history not found.");

        var storedName = Guid.NewGuid().ToString("N") + ext;
        var result = await _storage.SaveAsync(graveId, storedName, content, contentType, generateThumbnail: true, ct);

        var attachment = new GraveAttachment(
            graveId, category, ownershipHistoryId,
            SanitizeFileName(fileName), storedName, contentType,
            result.SizeBytes, result.HasThumbnail, description, actorUserId);
        context.GraveAttachments.Add(attachment);
        await context.SaveChangesAsync(ct);

        return MapToDto(attachment);
    }

    public async Task<IReadOnlyList<GraveAttachmentDto>> ListAsync(long graveId, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        var items = await context.GraveAttachments.AsNoTracking()
            .Where(a => a.GraveId == graveId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
        return items.Select(MapToDto).ToList();
    }

    public async Task<AttachmentContent?> OpenContentAsync(long attachmentId, bool thumbnail, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        var a = await context.GraveAttachments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == attachmentId, ct);
        if (a == null) return null;

        var stream = thumbnail && a.HasThumbnail
            ? _storage.OpenReadThumbnail(a.GraveId, a.StoredName)
            : _storage.OpenRead(a.GraveId, a.StoredName);
        if (stream == null) return null;

        var contentType = thumbnail && a.HasThumbnail ? "image/jpeg" : a.ContentType;
        return new AttachmentContent(stream, contentType, a.FileNameOriginal);
    }

    public async Task DeleteAsync(long attachmentId, long actorUserId, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        var a = await context.GraveAttachments.FirstOrDefaultAsync(x => x.Id == attachmentId, ct);
        if (a == null)
            throw new EntityNotFoundException("GRAVE_ATTACHMENT_NOT_FOUND", "Attachment not found.");

        _storage.Delete(a.GraveId, a.StoredName);
        context.GraveAttachments.Remove(a);
        await context.SaveChangesAsync(ct);
    }

    private static string SanitizeFileName(string fileName) =>
        string.IsNullOrWhiteSpace(fileName) ? "file" : Path.GetFileName(fileName);

    private static GraveAttachmentDto MapToDto(GraveAttachment a) => new()
    {
        Id = a.Id,
        GraveId = a.GraveId,
        Category = a.Category,
        OwnershipHistoryId = a.OwnershipHistoryId,
        FileNameOriginal = a.FileNameOriginal,
        ContentType = a.ContentType,
        SizeBytes = a.SizeBytes,
        HasThumbnail = a.HasThumbnail,
        IsImage = a.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase),
        Description = a.Description,
        CreatedAt = a.CreatedAt
    };
}
