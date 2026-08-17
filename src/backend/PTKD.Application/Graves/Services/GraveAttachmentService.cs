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
using PTKD.Application.Security.Authorization;
using PTKD.Application.Security.Authorization.Interfaces;
using PTKD.Domain.Entities;

namespace PTKD.Application.Graves.Services;

public class GraveAttachmentService : IGraveAttachmentService
{
    private const string ViewPermission = "GRAVE_VIEW";
    private const string AttachmentManagePermission = "GRAVE_ATTACHMENT_MANAGE";

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
    private readonly IPermissionEvaluator _permissionEvaluator;

    public GraveAttachmentService(
        IOrganizationDbContextFactory dbContextFactory,
        IGraveFileStorage storage,
        IPermissionEvaluator permissionEvaluator)
    {
        _dbContextFactory = dbContextFactory;
        _storage = storage;
        _permissionEvaluator = permissionEvaluator;
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
        // Lấy MÃ MỘ để đặt tên thư mục lưu file (thay vì id số).
        var graveCode = await context.Graves.Where(g => g.Id == graveId).Select(g => g.GraveCode).FirstOrDefaultAsync(ct);
        if (graveCode == null)
            throw new EntityNotFoundException("GRAVE_NOT_FOUND", "Grave not found.");

        var manageScope = await _permissionEvaluator.ResolveAsync(actorUserId, AttachmentManagePermission, ct);
        await GraveCompanyScope.EnsureGraveAccessibleAsync(context, graveId, manageScope, "GRAVE_ATTACHMENT_FORBIDDEN_COMPANY", ct);

        if (ownershipHistoryId.HasValue &&
            !await context.GraveOwnershipHistories.AnyAsync(h => h.Id == ownershipHistoryId.Value && h.GraveId == graveId, ct))
            throw new EntityNotFoundException("GRAVE_OWNERSHIP_HISTORY_NOT_FOUND", "Ownership history not found.");

        var storedName = Guid.NewGuid().ToString("N") + ext;
        var result = await _storage.SaveAsync(graveCode, storedName, content, contentType, generateThumbnail: true, ct);

        var attachment = new GraveAttachment(
            graveId, category, ownershipHistoryId,
            SanitizeFileName(fileName), storedName, contentType,
            result.SizeBytes, result.HasThumbnail, description, actorUserId,
            result.BasePathUsed);
        context.GraveAttachments.Add(attachment);
        await context.SaveChangesAsync(ct);

        return MapToDto(attachment);
    }

    public async Task<IReadOnlyList<GraveAttachmentDto>> ListAsync(long graveId, long actorUserId, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();

        // Không xem được mộ (khác công ty) -> không liệt kê đính kèm của nó.
        var scope = await _permissionEvaluator.ResolveAsync(actorUserId, ViewPermission, ct);
        if (!await GraveCompanyScope.CanAccessGraveAsync(context, graveId, scope, ct))
            return Array.Empty<GraveAttachmentDto>();

        var items = await context.GraveAttachments.AsNoTracking()
            .Where(a => a.GraveId == graveId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
        return items.Select(MapToDto).ToList();
    }

    public async Task<AttachmentContent?> OpenContentAsync(long graveId, long attachmentId, long actorUserId, bool thumbnail, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();

        // Đính kèm PHẢI thuộc đúng mộ trên đường dẫn (trước đây bỏ qua graveId -> tải chéo được),
        // VÀ người gọi phải xem được mộ đó theo công ty.
        var a = await context.GraveAttachments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == attachmentId && x.GraveId == graveId, ct);
        if (a == null) return null;

        var scope = await _permissionEvaluator.ResolveAsync(actorUserId, ViewPermission, ct);
        if (!await GraveCompanyScope.CanAccessGraveAsync(context, graveId, scope, ct))
            return null;

        var graveCode = await context.Graves.Where(g => g.Id == graveId).Select(g => g.GraveCode).FirstOrDefaultAsync(ct);
        if (graveCode == null) return null;

        var stream = thumbnail && a.HasThumbnail
            ? _storage.OpenReadThumbnail(a.StorageBasePath, graveCode, a.StoredName)
            : _storage.OpenRead(a.StorageBasePath, graveCode, a.StoredName);
        if (stream == null) return null;

        var contentType = thumbnail && a.HasThumbnail ? "image/jpeg" : a.ContentType;
        return new AttachmentContent(stream, contentType, a.FileNameOriginal);
    }

    public async Task DeleteAsync(long graveId, long attachmentId, long actorUserId, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        var a = await context.GraveAttachments.FirstOrDefaultAsync(x => x.Id == attachmentId && x.GraveId == graveId, ct);
        if (a == null)
            throw new EntityNotFoundException("GRAVE_ATTACHMENT_NOT_FOUND", "Attachment not found.");

        var manageScope = await _permissionEvaluator.ResolveAsync(actorUserId, AttachmentManagePermission, ct);
        await GraveCompanyScope.EnsureGraveAccessibleAsync(context, graveId, manageScope, "GRAVE_ATTACHMENT_FORBIDDEN_COMPANY", ct);

        var graveCode = await context.Graves.Where(g => g.Id == graveId).Select(g => g.GraveCode).FirstOrDefaultAsync(ct);
        if (graveCode != null)
            _storage.Delete(a.StorageBasePath, graveCode, a.StoredName);
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
