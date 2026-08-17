using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.CarePackages.DTOs;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Common.Models;
using PTKD.Domain.Entities;
using PTKD.Application.Workflows.DTOs;
using PTKD.Application.Workflows.Services;
using PTKD.Application.PaymentManagement.DTOs;
using PTKD.Application.PaymentManagement.Services;
using PTKD.Application.Common.Exceptions;
using System.Text.Json;

namespace PTKD.Application.CarePackages.Services;

public interface ICarePackageRequestService
{
    Task<PagedResult<CarePackageRequestDto>> ListAsync(
        long companyId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<CarePackageRequestDto?> GetByIdAsync(
        long companyId,
        long id,
        CancellationToken ct = default);

    Task<CarePackageRequestDto> CreateAsync(
        long companyId,
        CreateCarePackageRequest request,
        long userId,
        CancellationToken ct = default);

    Task<CarePackageRequestDto> SubmitAsync(long id, long companyId, long actorUserId, CancellationToken ct = default);
    Task<CarePackageRequestDto> ApproveStepAsync(long id, long stepId, string targetVersion, string reason, string comment, long companyId, long actorUserId, CancellationToken ct = default);
    Task<CarePackageRequestDto> RejectStepAsync(long id, long stepId, string targetVersion, string reason, string comment, long companyId, long actorUserId, CancellationToken ct = default);
    Task<CarePackageRequestDto> CreatePaymentAsync(long id, string paymentMethod, long companyId, long actorUserId, CancellationToken ct = default);
    Task<object?> GetPaymentStatusAsync(long id, long companyId, CancellationToken ct = default);
    Task<CarePackageRequestDto> ActivateAsync(long id, long companyId, long actorUserId, CancellationToken ct = default);
}

public class CarePackageRequestService : ICarePackageRequestService
{
    private const string SellProcessCode = "SELL_CARE_PACKAGE";

    private readonly IOrganizationDbContextFactory _dbContextFactory;
    private readonly IWorkflowRuntimeService _workflowRuntimeService;
    private readonly IPaymentTransactionService _paymentTransactionService;

    /// <summary>
    /// Dữ liệu hồ sơ dùng để đánh giá điều kiện quy trình. Các khoá ở đây phải khớp field_code
    /// đã khai báo trong Workflow_Condition_Fields cho SELL_CARE_PACKAGE.
    /// </summary>
    private static string BuildApprovalPayload(CarePackageRequest request) =>
        JsonSerializer.Serialize(new
        {
            request.CustomerId,
            request.ServiceId,
            request.TotalAmount,
            request.DiscountAmount,
        });

    public CarePackageRequestService(
        IOrganizationDbContextFactory dbContextFactory,
        IWorkflowRuntimeService workflowRuntimeService,
        IPaymentTransactionService paymentTransactionService)
    {
        _dbContextFactory = dbContextFactory;
        _workflowRuntimeService = workflowRuntimeService;
        _paymentTransactionService = paymentTransactionService;
    }

    public async Task<PagedResult<CarePackageRequestDto>> ListAsync(
        long companyId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();

        var query = db.CarePackageRequests
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId);

        var totalCount = await query.LongCountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => MapToDto(x))
            .ToArrayAsync(ct);

        await EnrichNamesAsync(db, items, ct);

        return new PagedResult<CarePackageRequestDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<CarePackageRequestDto?> GetByIdAsync(
        long companyId,
        long id,
        CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();

        var entity = await db.CarePackageRequests
            .Include(x => x.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, ct);

        if (entity == null) return null;

        var dto = MapToDto(entity);
        await EnrichNamesAsync(db, new[] { dto }, ct);
        return dto;
    }

    public async Task<CarePackageRequestDto> CreateAsync(
        long companyId,
        CreateCarePackageRequest request,
        long userId,
        CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();

        // 1. Guards
        if (request.Item == null)
            throw new ArgumentException("At least one item is required.");

        var customerExists = await db.Customers.AnyAsync(x => x.Id == request.CustomerId, ct);
        if (!customerExists)
            throw new InvalidOperationException("Customer not found.");

        // 1a. Gói được chọn từ DANH MỤC dịch vụ (Service_Types có IsCarePackage = true).
        if (request.ServiceTypeId <= 0)
            throw new InvalidOperationException("ServiceTypeId is required.");

        var serviceType = await db.ServiceTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(st => st.Id == request.ServiceTypeId, ct);

        if (serviceType == null)
            throw new InvalidOperationException("Service type not found.");
        if (!serviceType.IsActive)
            throw new InvalidOperationException("Service type is not active.");
        if (!serviceType.IsCarePackage)
            throw new InvalidOperationException("Selected service type is not a care package.");

        var unitPriceSnapshot = serviceType.StandardPrice;
        var priceByCot = serviceType.PricingBasis != ServiceType.PricingBasisPerGrave;

        // 1b. Phần mộ BẮT BUỘC — số cốt lấy TỰ ĐỘNG từ phần mộ (server là nguồn sự thật, không tin client).
        //     Phạm vi công ty được kiểm qua nghĩa trang (mộ thuộc công ty QUA nghĩa trang).
        if (request.Item == null || request.Item.GraveId <= 0)
            throw new InvalidOperationException("GraveId is required.");

        var graveInfo = await db.Graves
            .AsNoTracking()
            .Where(g => g.Id == request.Item.GraveId)
            .Join(db.Cemeteries, g => g.CemeteryId, c => c.Id,
                (g, c) => new { g.GraveCode, g.CotCount, g.OwnerCustomerId, c.CompanyId })
            .FirstOrDefaultAsync(ct);

        if (graveInfo == null || graveInfo.CompanyId != companyId)
            throw new InvalidOperationException("Grave not found in this company.");

        // Chặn cứng: phần mộ phải DO ĐÚNG KHÁCH HÀNG NÀY SỞ HỮU (không cho mua hộ phần mộ người khác).
        if (graveInfo.OwnerCustomerId != request.CustomerId)
            throw new InvalidOperationException("Grave is not owned by this customer.");

        // 1c. Thanh toán cần một "dịch vụ của khách" (Service) để bấu vào. Dùng lại nếu đã có dịch vụ
        //     ĐANG hiệu lực cùng loại cho khách này, nếu chưa có thì tạo mới từ giá chuẩn danh mục.
        var service = await db.Services
            .FirstOrDefaultAsync(s => s.CustomerId == request.CustomerId
                && s.ServiceTypeId == serviceType.Id
                && s.CompanyId == companyId
                && s.Status == Service.StatusActive, ct);

        if (service == null)
        {
            service = Service.CreateStandard(
                serviceTypeId: serviceType.Id,
                customerId: request.CustomerId,
                companyId: companyId,
                standardPrice: serviceType.StandardPrice,
                validFrom: request.SaleDate,
                validTo: null,
                createdByUserId: userId);
            db.Services.Add(service);
            await db.SaveChangesAsync(ct);
        }

        // 2. Create Domain Entities
        var draft = CarePackageRequest.CreateDraft(
            companyId: companyId,
            customerId: request.CustomerId,
            serviceId: service.Id,
            saleDate: request.SaleDate,
            createdByUserId: userId
        );

        var item = CarePackageRequestItem.Create(
            graveId: graveInfo.GraveCode,
            cotCountSnapshot: graveInfo.CotCount,
            servicePeriodStartDate: request.Item.ServicePeriodStartDate,
            servicePeriodEndDate: request.Item.ServicePeriodStartDate.AddYears(1).AddDays(-1),
            unitPriceSnapshot: unitPriceSnapshot,
            priceByCot: priceByCot
        );

        draft.AddItem(item);

        if (request.DiscountAmount > 0)
        {
            draft.SetDiscount(request.DiscountAmount, request.DiscountReason);
        }

        // Luật "khi nào cần duyệt" nay lấy từ CẤU HÌNH quy trình (liên kết + điều kiện) thay vì
        // nằm cứng trong thực thể. Chưa cấu hình thì trả về null và thực thể lùi về luật cũ
        // (có giảm giá thì phải duyệt) làm lưới an toàn — không hồ sơ nào thoát duyệt âm thầm.
        var approvalDecision = await _workflowRuntimeService.IsApprovalRequiredAsync(
            SellProcessCode, companyId, BuildApprovalPayload(draft), ct);

        draft.EvaluateApprovalRequirement(approvalDecision);

        if (!draft.RequiresApproval)
        {
            draft.SetPaymentEligible();
        }

        // 3. Save
        db.CarePackageRequests.Add(draft);
        await db.SaveChangesAsync(ct);

        return MapToDto(draft);
    }

    public async Task<CarePackageRequestDto> SubmitAsync(long id, long companyId, long actorUserId, CancellationToken ct = default)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var request = await dbContext.CarePackageRequests.FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId, ct);
        if (request == null) throw new EntityNotFoundException("REQUEST_NOT_FOUND", "Care package request not found.");

        var workflowRequest = new CreateWorkflowInstanceRequest
        {
            ProcessCode = SellProcessCode,
            BusinessEntityType = "CarePackageRequest",
            BusinessEntityId = request.Id,
            CompanyId = companyId,
            // Dùng CHUNG một hàm dựng payload với lúc tạo, để điều kiện được đánh giá trên
            // đúng bộ dữ liệu ở cả hai thời điểm.
            PayloadJson = BuildApprovalPayload(request)
        };

        var instance = await _workflowRuntimeService.CreateInstanceAsync(workflowRequest, actorUserId, ct);

        request.SetSubmitted(instance.Id);
        await dbContext.SaveChangesAsync(ct);

        return MapToDto(request);
    }

    public async Task<CarePackageRequestDto> ApproveStepAsync(long id, long stepId, string targetVersion, string reason, string comment, long companyId, long actorUserId, CancellationToken ct = default)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var request = await dbContext.CarePackageRequests.FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId, ct);
        if (request == null || !request.WorkflowInstanceId.HasValue) throw new EntityNotFoundException("REQUEST_NOT_FOUND", "Care package request not found or not submitted.");

        var actionRequest = new ApprovalActionRequest
        {
            TargetVersion = targetVersion,
            Reason = reason,
            Comment = comment
        };

        var instance = await _workflowRuntimeService.ApproveStepAsync(request.WorkflowInstanceId.Value, stepId, actionRequest, actorUserId, ct);

        // Handlers will update domain status if execution happens
        await dbContext.Entry(request).ReloadAsync(ct);

        return MapToDto(request);
    }

    public async Task<CarePackageRequestDto> RejectStepAsync(long id, long stepId, string targetVersion, string reason, string comment, long companyId, long actorUserId, CancellationToken ct = default)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var request = await dbContext.CarePackageRequests.FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId, ct);
        if (request == null || !request.WorkflowInstanceId.HasValue) throw new EntityNotFoundException("REQUEST_NOT_FOUND", "Care package request not found or not submitted.");

        var actionRequest = new ApprovalActionRequest
        {
            TargetVersion = targetVersion,
            Reason = reason,
            Comment = comment
        };

        var instance = await _workflowRuntimeService.RejectStepAsync(request.WorkflowInstanceId.Value, stepId, actionRequest, actorUserId, ct);

        if (instance.InstanceStatus == "REJECTED")
        {
            request.SetRejected();
            await dbContext.SaveChangesAsync(ct);
        }

        return MapToDto(request);
    }

    public async Task<CarePackageRequestDto> CreatePaymentAsync(long id, string paymentMethod, long companyId, long actorUserId, CancellationToken ct = default)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var request = await dbContext.CarePackageRequests
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId, ct);
        if (request == null) throw new EntityNotFoundException("REQUEST_NOT_FOUND", "Care package request not found.");

        if (request.Status != CarePackageRequest.StatusPaymentEligible)
            throw new BusinessRuleValidationException("INVALID_STATUS", "Payment can only be created for PAYMENT_ELIGIBLE requests.");

        var paymentDraftRequest = new CreatePaymentDraftRequest
        {
            CustomerId = request.CustomerId,
            CompanyId = companyId,
            PaymentMethod = paymentMethod,
            PaymentDate = DateTime.UtcNow,
            Notes = $"Care Package Request {request.Id}",
            Items = new List<CreatePaymentItemRequest>()
        };

        if (request.ServiceId.HasValue)
        {
            paymentDraftRequest.Items.Add(new CreatePaymentItemRequest
            {
                ServiceId = request.ServiceId.Value,
                Amount = request.TotalAmount,
                Description = $"Care Package"
            });
        }
        else
        {
            throw new BusinessRuleValidationException("MISSING_SERVICE", "Service is required for payment.");
        }

        var transaction = await _paymentTransactionService.CreateDraftAsync(paymentDraftRequest, actorUserId, ct);

        request.SetPaymentDraft(transaction.Id);
        await dbContext.SaveChangesAsync(ct);

        return MapToDto(request);
    }

    public async Task<object?> GetPaymentStatusAsync(long id, long companyId, CancellationToken ct = default)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var request = await dbContext.CarePackageRequests.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId, ct);
        if (request == null || !request.PaymentTransactionId.HasValue) return null;

        return await _paymentTransactionService.GetByIdAsync(request.PaymentTransactionId.Value, ct);
    }

    public async Task<CarePackageRequestDto> ActivateAsync(long id, long companyId, long actorUserId, CancellationToken ct = default)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var request = await dbContext.CarePackageRequests.FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId, ct);
        if (request == null) throw new EntityNotFoundException("REQUEST_NOT_FOUND", "Care package request not found.");

        if (request.Status == CarePackageRequest.StatusPendingPayment)
        {
            if (!request.PaymentTransactionId.HasValue)
                throw new BusinessRuleValidationException("NO_PAYMENT", "No payment transaction linked.");

            var payment = await _paymentTransactionService.GetByIdAsync(request.PaymentTransactionId.Value, ct);
            if (payment == null || payment.Status != "CONFIRMED")
                throw new BusinessRuleValidationException("PAYMENT_NOT_CONFIRMED", "Payment must be CONFIRMED before activating.");

            request.SetPaid();
        }

        request.SetActive();
        await dbContext.SaveChangesAsync(ct);

        return MapToDto(request);
    }

    private static async Task EnrichNamesAsync(IOrganizationDbContext db, IReadOnlyCollection<CarePackageRequestDto> dtos, CancellationToken ct)
    {
        if (dtos.Count == 0) return;

        var customerIds = dtos.Select(d => d.CustomerId).Distinct().ToArray();
        var customerInfo = await db.Customers
            .AsNoTracking()
            .Where(c => customerIds.Contains(c.Id))
            .Select(c => new { c.Id, c.CustomerCode, FullName = c.Profile.FullName })
            .ToDictionaryAsync(c => c.Id, ct);

        var serviceIds = dtos.Where(d => d.ServiceId.HasValue).Select(d => d.ServiceId!.Value).Distinct().ToArray();
        var serviceInfo = serviceIds.Length == 0
            ? new Dictionary<long, string>()
            : await db.Services
                .AsNoTracking()
                .Where(s => serviceIds.Contains(s.Id))
                .Join(db.ServiceTypes, s => s.ServiceTypeId, st => st.Id, (s, st) => new { s.Id, st.Name })
                .ToDictionaryAsync(x => x.Id, x => x.Name, ct);

        foreach (var d in dtos)
        {
            if (customerInfo.TryGetValue(d.CustomerId, out var c))
            {
                d.CustomerName = c.FullName;
                d.CustomerCode = c.CustomerCode;
            }
            if (d.ServiceId.HasValue && serviceInfo.TryGetValue(d.ServiceId.Value, out var name))
            {
                d.ServiceName = name;
            }
        }
    }

    private static CarePackageRequestDto MapToDto(CarePackageRequest entity)
    {
        var dto = new CarePackageRequestDto
        {
            Id = entity.Id,
            CompanyId = entity.CompanyId,
            CustomerId = entity.CustomerId,
            Status = entity.Status,
            RequiresApproval = entity.RequiresApproval,
            WorkflowInstanceId = entity.WorkflowInstanceId,
            ServiceId = entity.ServiceId,
            SaleDate = entity.SaleDate,
            SubtotalAmount = entity.SubtotalAmount,
            DiscountAmount = entity.DiscountAmount,
            DiscountReason = entity.DiscountReason,
            TotalAmount = entity.TotalAmount,
            PaymentTransactionId = entity.PaymentTransactionId,
            PreviousRequestId = entity.PreviousRequestId,
            CreatedAt = entity.CreatedAt,
            CreatedByUserId = entity.CreatedByUserId,
            UpdatedAt = entity.UpdatedAt,
            UpdatedByUserId = entity.UpdatedByUserId
        };

        if (entity.Items != null && entity.Items.Any())
        {
            dto.Items = entity.Items.Select(i => new CarePackageRequestItemDto
            {
                Id = i.Id,
                CarePackageRequestId = i.CarePackageRequestId,
                GraveId = i.GraveId,
                CotCountSnapshot = i.CotCountSnapshot,
                ServicePeriodStartDate = i.ServicePeriodStartDate,
                ServicePeriodEndDate = i.ServicePeriodEndDate,
                UnitPriceSnapshot = i.UnitPriceSnapshot,
                LineSubtotal = i.LineSubtotal,
                Notes = i.Notes
            }).ToList();
        }

        return dto;
    }
}
