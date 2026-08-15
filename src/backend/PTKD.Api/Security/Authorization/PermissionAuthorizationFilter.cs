using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PTKD.Application.Security.Authorization.Attributes;
using PTKD.Application.Security.Authorization.Interfaces;
using PTKD.Application.Security.Authorization.Models;

namespace PTKD.Api.Security.Authorization;

/// <summary>
/// Cổng kiểm quyền cho mọi endpoint có <see cref="RequirePermissionAttribute"/>.
///
/// Điểm chốt an ninh quan trọng nhất nằm ở đây: header <c>X-Company-Id</c> do CLIENT tự khai.
/// Trước đây bộ lọc chỉ kiểm "có gửi không" và "có phải số không" rồi truyền thẳng xuống, không
/// hề đối chiếu người gọi có thuộc công ty đó hay không — nên bất kỳ ai có một lần cấp quyền ở
/// phạm vi toàn cục đều đọc/ghi được dữ liệu của công ty bất kỳ chỉ bằng cách đổi header. Không
/// chặn ở đây thì mọi bộ lọc công ty phía dưới đều là hàng rào giấy.
/// </summary>
public class PermissionAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly IPermissionEvaluator _permissionEvaluator;
    private readonly ICompanyContextService _companyContext;

    public PermissionAuthorizationFilter(
        IPermissionEvaluator permissionEvaluator,
        ICompanyContextService companyContext)
    {
        _permissionEvaluator = permissionEvaluator;
        _companyContext = companyContext;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var endpoint = context.HttpContext.GetEndpoint();
        if (endpoint == null)
        {
            return;
        }

        var attribute = endpoint.Metadata.GetMetadata<RequirePermissionAttribute>();
        if (attribute == null)
        {
            return;
        }

        // Chưa xác thực: để [Authorize] và pipeline xác thực trả 401. Fail-closed vẫn được đảm
        // bảo vì mọi controller có [RequirePermission] đều có [Authorize] ở cấp lớp.
        if (context.HttpContext.User.Identity is not { IsAuthenticated: true })
        {
            return;
        }

        var userIdString = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!long.TryParse(userIdString, out var userId))
        {
            // Không xác định được người gọi thì không cho qua. Đường xác thực đã chặn ca này từ
            // trước (OnTokenValidated), nhưng để 403 tường minh cho khỏi phụ thuộc vào nơi khác.
            Forbid(context, "Không xác định được người dùng.");
            return;
        }

        long? companyId = null;

        if (attribute.Scope == PermissionScope.Company)
        {
            var headerValue = context.HttpContext.Request.Headers["X-Company-Id"].ToString();

            if (string.IsNullOrWhiteSpace(headerValue))
            {
                context.Result = new BadRequestObjectResult(new ProblemDetails
                {
                    Status = 400,
                    Title = "Missing Company Context",
                    Detail = "The X-Company-Id header is required for this endpoint.",
                    Type = "https://ptkd-erp.example.com/errors/missing-company-context"
                });
                return;
            }

            if (!long.TryParse(headerValue, out var parsedCompanyId))
            {
                context.Result = new BadRequestObjectResult(new ProblemDetails
                {
                    Status = 400,
                    Title = "Malformed Company Context",
                    Detail = "The X-Company-Id header must be a valid integer.",
                    Type = "https://ptkd-erp.example.com/errors/malformed-company-context"
                });
                return;
            }

            companyId = parsedCompanyId;
        }

        try
        {
            var scope = await _permissionEvaluator.ResolveAsync(
                userId,
                attribute.PermissionCode,
                context.HttpContext.RequestAborted);

            if (!scope.Granted)
            {
                Forbid(context, "Bạn không có quyền thực hiện thao tác này.");
                return;
            }

            if (companyId is null)
            {
                // Endpoint không mang ngữ cảnh công ty: giữ nguyên mức chặt cũ — đòi lần cấp toàn
                // cục. Endpoint nào muốn phục vụ người chỉ có quyền theo công ty thì phải tự lọc
                // dữ liệu qua ResolveAsync, chứ không nới ở đây.
                if (!scope.IsGlobal)
                {
                    Forbid(context, "Quyền này cần được cấp ở phạm vi toàn cục.");
                }
                return;
            }

            // Hai chốt, thiếu một là thủng:
            // (1) người gọi phải THẬT SỰ thuộc công ty đang khai — chặn việc tự đổi header;
            // (2) quyền phải phủ được công ty đó.
            // Chốt (1) áp cả với người có quyền toàn cục: quyền toàn cục là để nhìn dữ liệu xuyên
            // công ty qua các endpoint có lọc, không phải để mượn danh nghĩa một công ty mình
            // không thuộc mà thao tác.
            var isMember = await _companyContext.IsMemberOfAsync(
                userId, companyId.Value, context.HttpContext.RequestAborted);

            if (!isMember)
            {
                Forbid(context, "Bạn không thuộc công ty này.");
                return;
            }

            if (!scope.Allows(companyId.Value))
            {
                Forbid(context, "Bạn không có quyền này ở công ty đang chọn.");
            }
        }
        catch
        {
            // Fail-closed khi engine lỗi, và không lộ chi tiết ra ngoài.
            Forbid(context, "Không kiểm tra được quyền.");
        }
    }

    private static void Forbid(AuthorizationFilterContext context, string detail)
    {
        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = 403,
            Title = "Forbidden",
            Detail = detail,
            Type = "https://ptkd-erp.example.com/errors/forbidden"
        })
        {
            StatusCode = 403
        };
    }
}
