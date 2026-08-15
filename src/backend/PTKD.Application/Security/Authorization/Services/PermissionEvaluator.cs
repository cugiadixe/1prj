using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PTKD.Application.Security.Authorization.Interfaces;
using PTKD.Application.Security.Authorization.Models;

namespace PTKD.Application.Security.Authorization.Services;

/// <summary>
/// Bộ đánh giá quyền — MỘT thuật toán duy nhất cho mọi câu hỏi về quyền.
///
/// Trước đây hệ có BA bản "quyền hiệu dụng" khác nhau (một để chặn API, một để dựng menu, một
/// cho màn hình quản trị) và chúng cho kết quả lệch nhau, nên menu hiện mà bấm vào thì 403, còn
/// màn hình dùng để KIỂM TRA ma trận lại báo cáo rộng hơn thực tế. Nay mọi đường đều đi qua
/// <see cref="GatherGrantsAsync"/>.
///
/// Mô hình phạm vi: phạm vi là thuộc tính của LẦN CẤP (ô trong ma trận), không phải thuộc tính
/// cứng của mã quyền. Cột <c>data_scope</c> của danh mục nay chỉ còn là nhãn phân loại, không
/// tham gia quyết định — trước đây nó chặn cứng, khiến quyền khai GLOBAL không bao giờ được
/// đánh giá kèm công ty, tức chỉ có hai nấc: mất sạch quyền, hoặc xem được mọi công ty.
/// </summary>
public class PermissionEvaluator : IPermissionEvaluator
{
    private readonly IAuthorizationDbContext _dbContext;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PermissionEvaluator> _logger;
    private readonly TimeProvider _timeProvider;

    private const string ScopeGlobal = "GLOBAL";
    private const string GrantDeny = "DENY";

    public PermissionEvaluator(
        IAuthorizationDbContext dbContext,
        IMemoryCache cache,
        ILogger<PermissionEvaluator> logger,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _cache = cache;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Câu hỏi đầy đủ: "người này có quyền đó không, VÀ ở những công ty nào".
    /// Đây là đường chính; mọi hàm còn lại chỉ là lớp bọc quanh nó.
    /// </summary>
    public async Task<PermissionScopeResult> ResolveAsync(
        long userId,
        string permissionCode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var grants = await GetGrantMapAsync(userId, cancellationToken);
            return grants.TryGetValue(permissionCode, out var acc)
                ? acc.ToResult()
                : PermissionScopeResult.Denied;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fail-closed: Error resolving permission {PermissionCode} for User {UserId}", permissionCode, userId);
            return PermissionScopeResult.Denied;
        }
    }

    /// <summary>
    /// Lớp bọc tương thích cho mã nguồn cũ.
    ///
    /// LƯU Ý VỀ NGỮ NGHĨA <paramref name="companyId"/> = null: cố ý giữ NGUYÊN mức chặt của mô
    /// hình cũ — đòi phải có lần cấp TOÀN CỤC. Nếu nới thành "có quyền ở đâu đó là được" thì mọi
    /// endpoint đang khai <c>PermissionScope.Global</c> mà CHƯA lọc dữ liệu theo công ty sẽ mở
    /// rộng ra âm thầm. Endpoint nào muốn theo mô hình mới thì gọi thẳng
    /// <see cref="ResolveAsync"/> rồi tự lọc dữ liệu theo <c>AllowedCompanyIds</c>.
    /// </summary>
    public async Task<bool> EvaluateAsync(
        long userId,
        string permissionCode,
        long? companyId,
        CancellationToken cancellationToken = default)
    {
        var result = await ResolveAsync(userId, permissionCode, cancellationToken);

        if (!result.Granted)
            return false;

        return companyId is null
            ? result.IsGlobal
            : result.Allows(companyId.Value);
    }

    /// <summary>
    /// Bộ quyền hiệu dụng KÈM PHẠM VI THẬT của từng mã. Dùng cho <c>/me/permissions</c> và cho
    /// màn hình chẩn đoán quyền của người khác.
    ///
    /// Thay cho việc trả <c>data_scope</c> của danh mục như trước — đó là nhãn tĩnh của mã quyền
    /// chứ không phải phạm vi người dùng được cấp, nên giao diện không thể phân biệt người có
    /// quyền toàn cục với người chỉ có quyền một công ty.
    /// </summary>
    public async Task<IReadOnlyList<EffectivePermissionEntry>> GetEffectivePermissionEntriesAsync(
        long userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var grants = await GetGrantMapAsync(userId, cancellationToken);

            return grants
                .Select(kv => new { kv.Key, Result = kv.Value.ToResult() })
                .Where(x => x.Result.Granted)
                .Select(x => new EffectivePermissionEntry(x.Key, x.Result.IsGlobal, x.Result.AllowedCompanyIds))
                .OrderBy(e => e.PermissionCode, StringComparer.Ordinal)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fail-closed: Error getting effective permissions for User {UserId}", userId);
            return Array.Empty<EffectivePermissionEntry>();
        }
    }

    /// <summary>
    /// Lớp bọc tương thích: chỉ trả danh sách mã. <paramref name="companyId"/> null nghĩa là
    /// "mọi mã người này có ở bất kỳ phạm vi nào" (dùng để dựng menu khi chưa chọn công ty).
    /// </summary>
    public async Task<IReadOnlyList<string>> GetEffectivePermissionsAsync(
        long userId,
        long? companyId,
        CancellationToken cancellationToken = default)
    {
        var entries = await GetEffectivePermissionEntriesAsync(userId, cancellationToken);

        return entries
            .Where(e => companyId is null || e.IsGlobal || e.CompanyIds.Contains(companyId.Value))
            .Select(e => e.PermissionCode)
            .ToList();
    }

    // ---------------------------------------------------------------------------------------

    private async Task<IReadOnlyDictionary<string, GrantAccumulator>> GetGrantMapAsync(
        long userId,
        CancellationToken cancellationToken)
    {
        var policyState = await _dbContext.AuthorizationPolicyStates
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == 1, cancellationToken);

        long policyVersion = policyState?.PolicyVersion ?? 1;

        var cacheKey = $"grants:{userId}:{policyVersion}";

        if (_cache.TryGetValue(cacheKey, out IReadOnlyDictionary<string, GrantAccumulator>? cached) && cached != null)
            return cached;

        var grants = await GatherGrantsAsync(userId, cancellationToken);

        _cache.Set(cacheKey, grants, TimeSpan.FromMinutes(5));

        return grants;
    }

    /// <summary>
    /// Gom mọi lần cấp quyền của một người từ cả 4 nguồn, kèm phạm vi của từng lần cấp.
    /// Đây là chỗ DUY NHẤT đọc dữ liệu phân quyền.
    /// </summary>
    private async Task<IReadOnlyDictionary<string, GrantAccumulator>> GatherGrantsAsync(
        long userId,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var map = new Dictionary<string, GrantAccumulator>(StringComparer.Ordinal);

        GrantAccumulator Acc(string code)
        {
            if (!map.TryGetValue(code, out var acc))
            {
                acc = new GrantAccumulator();
                map[code] = acc;
            }
            return acc;
        }

        // 1. Nhóm quản trị
        var adminGroupGrants = await _dbContext.UserAdminGroupAssignments
            .AsNoTracking()
            .Where(a => a.UserId == userId
                        && a.AssignmentStatus == "ACTIVE"
                        && a.EffectiveFrom <= now
                        && (a.EffectiveTo == null || a.EffectiveTo > now)
                        && a.AdminGroup.IsActive)
            .SelectMany(a => a.AdminGroup.Permissions.Select(p => new ScopedGrantRow(
                p.PermissionCode, a.AdminGroup.ScopeType, a.AdminGroup.CompanyId)))
            .ToListAsync(cancellationToken);

        // 2. Vai trò
        var roleGrants = await _dbContext.UserRoleAssignments
            .AsNoTracking()
            .Where(a => a.UserId == userId
                        && a.AssignmentStatus == "ACTIVE"
                        && a.EffectiveFrom <= now
                        && (a.EffectiveTo == null || a.EffectiveTo > now)
                        && a.Role.IsActive)
            .SelectMany(a => a.Role.Permissions.Select(p => new ScopedGrantRow(
                p.PermissionCode, a.Role.ScopeType, a.Role.CompanyId)))
            .ToListAsync(cancellationToken);

        // 3. Quyền chuẩn theo phòng ban.
        //    Phòng ban LUÔN thuộc đúng một công ty, nên quyền từ nguồn này LUÔN là phạm vi công ty
        //    của phòng đó — không bao giờ toàn cục. Mô hình cũ bỏ lọc công ty ở nhánh này khi
        //    không có ngữ cảnh công ty, biến "gán vào phòng ban = có quyền" thành "gán vào phòng
        //    ban bất kỳ = có quyền ở mọi công ty".
        //
        //    Tách làm hai truy vấn thay vì join: người không thuộc phòng ban nào thì không chạm
        //    vào bảng quyền chuẩn.
        var myDepartments = await _dbContext.UserDepartmentAssignments
            .AsNoTracking()
            .Where(a => a.UserId == userId
                        && a.AssignmentStatus == "ACTIVE"
                        && a.EffectiveFrom <= now
                        && (a.EffectiveTo == null || a.EffectiveTo > now)
                        && a.Department.IsActive)
            .Select(a => new { a.DepartmentId, a.CompanyId })
            .ToListAsync(cancellationToken);

        var departmentGrants = new List<DepartmentGrantRow>();

        if (myDepartments.Count > 0)
        {
            var departmentIds = myDepartments.Select(d => d.DepartmentId).Distinct().ToList();

            var departmentPermissions = await _dbContext.DepartmentPermissions
                .AsNoTracking()
                .Where(dp => departmentIds.Contains(dp.DepartmentId))
                .Select(dp => new { dp.DepartmentId, dp.PermissionCode })
                .ToListAsync(cancellationToken);

            departmentGrants.AddRange(
                from dp in departmentPermissions
                join d in myDepartments on dp.DepartmentId equals d.DepartmentId
                select new DepartmentGrantRow(dp.PermissionCode, d.CompanyId));
        }

        // 4. Quyền cấp riêng cho cá nhân (cả ALLOW lẫn DENY)
        var individualGrants = await _dbContext.UserIndividualPermissions
            .AsNoTracking()
            .Where(p => p.UserId == userId
                        && p.AssignmentStatus == "ACTIVE"
                        && p.EffectiveFrom <= now
                        && (p.EffectiveTo == null || p.EffectiveTo > now))
            .Select(p => new IndividualGrantRow(p.PermissionCode, p.ScopeType, p.CompanyId, p.GrantType))
            .ToListAsync(cancellationToken);

        foreach (var g in adminGroupGrants) Acc(g.PermissionCode).AddAllow(g.ScopeType, g.CompanyId);
        foreach (var g in roleGrants) Acc(g.PermissionCode).AddAllow(g.ScopeType, g.CompanyId);
        foreach (var g in departmentGrants) Acc(g.PermissionCode).AddCompanyAllow(g.CompanyId);

        foreach (var g in individualGrants)
        {
            var acc = Acc(g.PermissionCode);
            if (string.Equals(g.GrantType, GrantDeny, StringComparison.Ordinal))
                acc.AddDeny(g.ScopeType, g.CompanyId);
            else
                acc.AddAllow(g.ScopeType, g.CompanyId);
        }

        // Loại các mã đã bị vô hiệu hoá trong danh mục. Làm ở bước cuối để tránh kéo cả bảng
        // danh mục về khi người dùng không có quyền nào.
        if (map.Count > 0)
        {
            var codes = map.Keys.ToList();
            var activeCodes = await _dbContext.Permissions
                .AsNoTracking()
                .Where(p => p.IsActive && codes.Contains(p.PermissionCode))
                .Select(p => p.PermissionCode)
                .ToListAsync(cancellationToken);

            var activeSet = new HashSet<string>(activeCodes, StringComparer.Ordinal);
            foreach (var code in codes.Where(c => !activeSet.Contains(c)))
                map.Remove(code);
        }

        return map;
    }

    private sealed record ScopedGrantRow(string PermissionCode, string ScopeType, long? CompanyId);
    private sealed record DepartmentGrantRow(string PermissionCode, long CompanyId);
    private sealed record IndividualGrantRow(string PermissionCode, string ScopeType, long? CompanyId, string GrantType);

    /// <summary>Gom các lần cấp của cùng một mã quyền lại thành một phạm vi hiệu dụng.</summary>
    private sealed class GrantAccumulator
    {
        private bool _allowGlobal;
        private bool _denyGlobal;
        private readonly HashSet<long> _allowCompanies = new();
        private readonly HashSet<long> _denyCompanies = new();

        public void AddAllow(string scopeType, long? companyId)
        {
            if (string.Equals(scopeType, ScopeGlobal, StringComparison.Ordinal))
                _allowGlobal = true;
            else if (companyId.HasValue)
                _allowCompanies.Add(companyId.Value);
            // scope COMPANY mà thiếu company_id là dữ liệu hỏng — bỏ qua, không đoán.
        }

        public void AddCompanyAllow(long companyId) => _allowCompanies.Add(companyId);

        public void AddDeny(string scopeType, long? companyId)
        {
            if (string.Equals(scopeType, ScopeGlobal, StringComparison.Ordinal))
                _denyGlobal = true;
            else if (companyId.HasValue)
                _denyCompanies.Add(companyId.Value);
        }

        public PermissionScopeResult ToResult()
        {
            // Cấm toàn cục phủ mọi thứ.
            if (_denyGlobal)
                return PermissionScopeResult.Denied;

            var granted = _allowGlobal || _allowCompanies.Count > 0;
            if (!granted)
                return PermissionScopeResult.Denied;

            return new PermissionScopeResult(
                Granted: true,
                IsGlobal: _allowGlobal,
                CompanyIds: _allowCompanies.OrderBy(x => x).ToList(),
                DeniedCompanyIds: _denyCompanies.OrderBy(x => x).ToList());
        }
    }
}
