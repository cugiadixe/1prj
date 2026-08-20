using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Common.Exceptions;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Customers.DTOs;
using PTKD.Application.Customers.Handlers;
using PTKD.Application.Security.Authorization;
using PTKD.Application.Security.Authorization.Interfaces;
using PTKD.Application.Workflows.DTOs;
using PTKD.Application.Workflows.Services;
using PTKD.Domain.Entities;

namespace PTKD.Application.Customers.Services;

public class CustomerMergeService : ICustomerMergeService
{
    private const string ViewPermission = "CUSTOMER_MERGE_REQUEST_VIEW";
    private const string AdminViewPermission = "CUSTOMER_MERGE_REQUEST_ADMIN_VIEW";
    private const string CreatePermission = "CUSTOMER_MERGE_REQUEST_CREATE";
    // Quyền "thực thi gộp": người có quyền này ở phạm vi TOÀN CỤC (admin full quyền) được tự duyệt
    // + thực thi ngay khi gửi (không cần cấp duyệt thứ hai). Người không có → đi qua luồng duyệt.
    private const string ExecutePermission = "CUSTOMER_MERGE_EXECUTE";
    private const string MergeProcessCode = "CUSTOMER_MERGE_DUPLICATE";

    // Trạng thái yêu cầu gộp còn "mở" (chưa kết thúc): dùng để chặn tạo trùng cặp nguồn→đích.
    private static readonly string[] OpenStatuses = { "DRAFT", "SUBMITTED", "APPROVED" };

    private readonly IOrganizationDbContextFactory _dbContextFactory;
    private readonly IPermissionEvaluator _permissionEvaluator;
    private readonly IWorkflowRuntimeService _workflowRuntimeService;
    private readonly CustomerMergeExecutor _mergeExecutor;

    public CustomerMergeService(
        IOrganizationDbContextFactory dbContextFactory,
        IPermissionEvaluator permissionEvaluator,
        IWorkflowRuntimeService workflowRuntimeService,
        CustomerMergeExecutor mergeExecutor)
    {
        _dbContextFactory = dbContextFactory;
        _permissionEvaluator = permissionEvaluator;
        _workflowRuntimeService = workflowRuntimeService;
        _mergeExecutor = mergeExecutor;
    }

    public async Task<CustomerMergeRequestDto> CreateMergeRequestAsync(CreateCustomerMergeRequestDto request, long actorUserId, CancellationToken ct = default)
    {
        await using var _dbContext = _dbContextFactory.CreateDbContext();
        if (request.SourceCustomerId == request.TargetCustomerId)
        {
            throw new InvalidOperationException("Source and target customer cannot be the same.");
        }

        var sourceCustomer = await _dbContext.Customers
            .Include(c => c.Profile)
            .FirstOrDefaultAsync(c => c.Id == request.SourceCustomerId, ct);

        var targetCustomer = await _dbContext.Customers
            .Include(c => c.Profile)
            .FirstOrDefaultAsync(c => c.Id == request.TargetCustomerId, ct);

        if (sourceCustomer == null || targetCustomer == null)
        {
            throw new InvalidOperationException("One or both customers not found.");
        }

        if (sourceCustomer.CustomerStatus == "MERGED" || targetCustomer.CustomerStatus == "MERGED")
        {
            throw new InvalidOperationException("Cannot merge a customer that is already merged.");
        }

        if (targetCustomer.CustomerStatus != "ACTIVE")
        {
            throw new InvalidOperationException("Target customer must be active.");
        }

        // Lọc theo công ty: người tạo phải được phép thao tác trên CẢ hai khách (nguồn + đích) trong
        // phạm vi quyền merge của họ. Ném 403 nếu chạm khách thuộc công ty ngoài phạm vi.
        var createScope = await _permissionEvaluator.ResolveAsync(actorUserId, CreatePermission, ct);
        await CustomerCompanyScope.EnsureCustomerAccessibleAsync(_dbContext, request.SourceCustomerId, createScope, "CUS_MERGE_FORBIDDEN", ct);
        await CustomerCompanyScope.EnsureCustomerAccessibleAsync(_dbContext, request.TargetCustomerId, createScope, "CUS_MERGE_FORBIDDEN", ct);

        // CHO PHÉP trùng công ty: ca chống trùng phổ biến nhất là hai bản ghi CÙNG một công ty (một chi
        // nhánh nhập trùng một người) — chặn ca này thì tính năng gộp vô dụng. Việc hoà giải context
        // công ty trùng nhau (giữ context đích, bỏ context nguồn thừa) do BƯỚC THỰC THI gộp xử lý.

        // Chặn tạo TRÙNG: đã có một yêu cầu gộp còn "mở" (DRAFT/SUBMITTED/APPROVED) cho đúng cặp
        // nguồn→đích thì không tạo thêm. Trước đây mỗi lần bấm "Gộp" lại đẻ một DRAFT mới (anh Bách
        // thấy 2 dòng DRAFT y hệt). Không xét yêu cầu đã EXECUTED/REJECTED/WITHDRAWN (đã kết thúc).
        var hasOpenRequest = await _dbContext.CustomerMergeRequests.AnyAsync(
            r => r.SourceCustomerId == request.SourceCustomerId
              && r.TargetCustomerId == request.TargetCustomerId
              && OpenStatuses.Contains(r.RequestStatus), ct);
        if (hasOpenRequest)
            throw new InvalidOperationException("A pending merge request already exists for this source and target customer.");

        // Snapshots handling
        var sourceRowVersion = Convert.FromBase64String(request.SourceRowVersionSnapshot);
        var targetRowVersion = Convert.FromBase64String(request.TargetRowVersionSnapshot);

        var mergeRequest = new CustomerMergeRequest(
            request.SourceCustomerId,
            request.TargetCustomerId,
            actorUserId,
            request.SurvivorshipPayload,
            sourceRowVersion,
            targetRowVersion
        );

        foreach (var candidate in request.Candidates)
        {
            mergeRequest.AddCandidate(
                candidate.CandidateCustomerId,
                candidate.MatchType,
                candidate.MatchConfidence,
                candidate.SnapshotPayload
            );
        }

        _dbContext.CustomerMergeRequests.Add(mergeRequest);
        await _dbContext.SaveChangesAsync(ct);

        // Người tạo vừa qua kiểm tra quyền ở trên → nạp lại KHÔNG cần lọc quyền nữa.
        return await LoadDtoAsync(_dbContext, mergeRequest.Id, ct) ?? throw new InvalidOperationException("Failed to load saved request");
    }

    public async Task<CustomerMergeRequestDto> SubmitMergeRequestAsync(Guid id, long actorUserId, long? companyId, CancellationToken ct = default)
    {
        await using var _dbContext = _dbContextFactory.CreateDbContext();

        var mergeRequest = await _dbContext.CustomerMergeRequests
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new EntityNotFoundException("CUS_MERGE_NOT_FOUND", "Merge request not found.");

        if (mergeRequest.RequestStatus != "DRAFT")
            throw new InvalidOperationException("Only DRAFT merge requests can be submitted for approval.");

        // Quyền: người gửi duyệt phải được thao tác trên CẢ hai khách (nguồn + đích) trong phạm vi
        // quyền merge của họ — giống lúc tạo. Ném 403 nếu chạm khách ngoài phạm vi.
        var createScope = await _permissionEvaluator.ResolveAsync(actorUserId, CreatePermission, ct);
        await CustomerCompanyScope.EnsureCustomerAccessibleAsync(_dbContext, mergeRequest.SourceCustomerId, createScope, "CUS_MERGE_FORBIDDEN", ct);
        await CustomerCompanyScope.EnsureCustomerAccessibleAsync(_dbContext, mergeRequest.TargetCustomerId, createScope, "CUS_MERGE_FORBIDDEN", ct);

        // Kiểm tra lại tình trạng khách ngay trước khi mở luồng duyệt (tránh gửi duyệt hồ sơ đã hỏng).
        var sourceCustomer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == mergeRequest.SourceCustomerId, ct);
        var targetCustomer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == mergeRequest.TargetCustomerId, ct);
        if (sourceCustomer == null || targetCustomer == null)
            throw new InvalidOperationException("One or both customers not found.");
        if (sourceCustomer.CustomerStatus == "MERGED" || targetCustomer.CustomerStatus == "MERGED")
            throw new InvalidOperationException("Cannot merge a customer that is already merged.");
        if (targetCustomer.CustomerStatus != "ACTIVE")
            throw new InvalidOperationException("Target customer must be active.");

        // TỰ DUYỆT: người gửi có quyền CUSTOMER_MERGE_EXECUTE phủ CẢ công ty của khách nguồn + đích
        // (toàn cục, hoặc theo công ty bao trùm cả hai) → tự tạo + tự duyệt + thực thi NGAY, không cần
        // cấp duyệt thứ hai. Đây là "admin full quyền" theo các công ty họ quản. Vẫn ghi audit + lịch
        // sử gộp. Người KHÔNG đủ quyền thực thi trên cả hai khách → rơi xuống luồng duyệt (trưởng phòng).
        var executeScope = await _permissionEvaluator.ResolveAsync(actorUserId, ExecutePermission, ct);
        var canSelfApprove = executeScope.Granted && (
            executeScope.IsUnrestricted ||
            (await CustomerCompanyScope.CanAccessCustomerAsync(_dbContext, mergeRequest.SourceCustomerId, executeScope, ct)
             && await CustomerCompanyScope.CanAccessCustomerAsync(_dbContext, mergeRequest.TargetCustomerId, executeScope, ct)));
        if (canSelfApprove)
        {
            await _mergeExecutor.ExecuteAsync(id, actorUserId, null, ct);
            return await LoadDtoAsync(_dbContext, id, ct) ?? throw new InvalidOperationException("Failed to load executed request");
        }

        // PayloadJson mang MergeRequestId (Guid) để handler nạp đúng yêu cầu — vì
        // WorkflowInstance.BusinessEntityId là long không chứa được Guid. SourceCustomerId/
        // TargetCustomerId khớp các trường điều kiện đã seed (V0035) cho quy trình này.
        var payloadJson = JsonSerializer.Serialize(new
        {
            MergeRequestId = mergeRequest.Id.ToString(),
            mergeRequest.SourceCustomerId,
            mergeRequest.TargetCustomerId
        });

        var workflowInstanceRequest = new CreateWorkflowInstanceRequest
        {
            ProcessCode = MergeProcessCode,
            BusinessEntityType = "CustomerMergeRequest",
            BusinessEntityId = mergeRequest.TargetCustomerId, // long "mỏ neo" hiển thị; định danh thật ở payload
            CompanyId = companyId,
            PayloadJson = payloadJson
        };

        var workflowInstance = await _workflowRuntimeService.CreateInstanceAsync(workflowInstanceRequest, actorUserId, ct);

        await using var linkContext = _dbContextFactory.CreateDbContext();
        var linkStrategy = linkContext.CreateExecutionStrategy();
        await linkStrategy.ExecuteAsync(async () =>
        {
            await using var ctx2 = _dbContextFactory.CreateDbContext();
            var mr = await ctx2.CustomerMergeRequests.FirstAsync(r => r.Id == id, ct);
            mr.SetSubmitted(workflowInstance.Id);
            await ctx2.SaveChangesAsync(ct);
        });

        return await LoadDtoAsync(_dbContext, id, ct) ?? throw new InvalidOperationException("Failed to load submitted request");
    }

    public async Task<CustomerMergeRequestDto?> GetMergeRequestByIdAsync(Guid id, long actorUserId, CancellationToken ct = default)
    {
        await using var _dbContext = _dbContextFactory.CreateDbContext();
        var dto = await LoadDtoAsync(_dbContext, id, ct);
        if (dto == null) return null;

        var viewScope = await _permissionEvaluator.ResolveAsync(actorUserId, ViewPermission, ct);
        var adminScope = await _permissionEvaluator.ResolveAsync(actorUserId, AdminViewPermission, ct);

        // Admin-view toàn cục hoặc view toàn cục → xem mọi yêu cầu.
        if (adminScope.IsUnrestricted || viewScope.IsUnrestricted)
            return dto;

        // Theo công ty: chỉ thấy yêu cầu chạm tới khách hàng trong phạm vi (nguồn HOẶC đích).
        var canSource = await CustomerCompanyScope.CanAccessCustomerAsync(_dbContext, dto.SourceCustomerId, viewScope, ct)
                     || await CustomerCompanyScope.CanAccessCustomerAsync(_dbContext, dto.SourceCustomerId, adminScope, ct);
        var canTarget = await CustomerCompanyScope.CanAccessCustomerAsync(_dbContext, dto.TargetCustomerId, viewScope, ct)
                     || await CustomerCompanyScope.CanAccessCustomerAsync(_dbContext, dto.TargetCustomerId, adminScope, ct);

        // Không thuộc phạm vi → coi như không tồn tại (che sự tồn tại).
        return (canSource || canTarget) ? dto : null;
    }

    public async Task<PagedResult<CustomerMergeRequestDto>> SearchMergeRequestsAsync(int page, int pageSize, long actorUserId, CancellationToken ct = default)
    {
        await using var _dbContext = _dbContextFactory.CreateDbContext();

        var viewScope = await _permissionEvaluator.ResolveAsync(actorUserId, ViewPermission, ct);
        var adminScope = await _permissionEvaluator.ResolveAsync(actorUserId, AdminViewPermission, ct);

        var query = _dbContext.CustomerMergeRequests.AsNoTracking();

        // Nếu KHÔNG có quyền toàn cục → lọc chỉ những yêu cầu chạm tới khách hàng trong phạm vi công ty
        // (khớp CustomerCompanyScope). Người quyền-công-ty không thấy yêu cầu gộp của công ty khác.
        if (!adminScope.IsUnrestricted && !viewScope.IsUnrestricted)
        {
            var viewIds = CustomerCompanyScope
                .ApplyScope(_dbContext.Customers.AsNoTracking(), _dbContext, viewScope)
                .Select(c => c.Id);
            var adminIds = CustomerCompanyScope
                .ApplyScope(_dbContext.Customers.AsNoTracking(), _dbContext, adminScope)
                .Select(c => c.Id);

            query = query.Where(r =>
                viewIds.Contains(r.SourceCustomerId) || viewIds.Contains(r.TargetCustomerId) ||
                adminIds.Contains(r.SourceCustomerId) || adminIds.Contains(r.TargetCustomerId));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(request => new CustomerMergeRequestDto
            {
                Id = request.Id,
                SourceCustomerId = request.SourceCustomerId,
                TargetCustomerId = request.TargetCustomerId,
                RequesterId = request.RequesterId,
                RequestStatus = request.RequestStatus,
                SurvivorshipPayload = request.SurvivorshipPayload,
                SourceRowVersionSnapshot = Convert.ToBase64String(request.SourceRowVersionSnapshot),
                TargetRowVersionSnapshot = Convert.ToBase64String(request.TargetRowVersionSnapshot),
                WorkflowInstanceId = request.WorkflowInstanceId,
                CreatedAt = request.CreatedAt,
                UpdatedAt = request.UpdatedAt,
                RowVersion = Convert.ToBase64String(request.RowVersion)
            })
            .ToListAsync(ct);

        return new PagedResult<CustomerMergeRequestDto>
        {
            Items = items.ToArray(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    // Nạp + ánh xạ DTO không kèm kiểm tra quyền. Đường công khai (Get/Search) phải tự lọc phạm vi.
    private static async Task<CustomerMergeRequestDto?> LoadDtoAsync(IOrganizationDbContext context, Guid id, CancellationToken ct)
    {
        var request = await context.CustomerMergeRequests
            .Include(r => r.Candidates)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (request == null) return null;

        return new CustomerMergeRequestDto
        {
            Id = request.Id,
            SourceCustomerId = request.SourceCustomerId,
            TargetCustomerId = request.TargetCustomerId,
            RequesterId = request.RequesterId,
            RequestStatus = request.RequestStatus,
            SurvivorshipPayload = request.SurvivorshipPayload,
            SourceRowVersionSnapshot = Convert.ToBase64String(request.SourceRowVersionSnapshot),
            TargetRowVersionSnapshot = Convert.ToBase64String(request.TargetRowVersionSnapshot),
            WorkflowInstanceId = request.WorkflowInstanceId,
            CreatedAt = request.CreatedAt,
            UpdatedAt = request.UpdatedAt,
            RowVersion = Convert.ToBase64String(request.RowVersion),
            Candidates = request.Candidates.Select(c => new CustomerMergeCandidateDto
            {
                CandidateCustomerId = c.CandidateCustomerId,
                MatchType = c.MatchType,
                MatchConfidence = c.MatchConfidence,
                SnapshotPayload = c.SnapshotPayload
            }).ToList()
        };
    }
}
