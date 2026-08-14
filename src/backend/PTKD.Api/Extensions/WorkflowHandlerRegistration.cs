using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Workflows.Services;

namespace PTKD.Api.Extensions;

/// <summary>
/// Đăng ký bộ xử lý thực thi quy trình (IWorkflowExecutionHandler) bằng cách TỰ QUÉT assembly,
/// thay cho việc liệt kê tay từng dòng trong Program.cs.
///
/// Lý do: đăng ký tay không có gì bảo vệ — quên một dòng là hồ sơ duyệt xong sẽ kẹt vĩnh viễn
/// mà không báo lỗi (đúng ca CardReprintExecutionHandler đã bị quên). Tự quét khiến việc
/// "viết class handler" là đủ, không cần nhớ sửa Program.cs.
/// </summary>
public static class WorkflowHandlerRegistration
{
    public static IServiceCollection AddWorkflowExecutionHandlers(this IServiceCollection services)
    {
        // Mọi handler đều nằm trong assembly PTKD.Application.
        var applicationAssembly = typeof(IWorkflowExecutionHandler).Assembly;

        var handlerTypes = SafeGetTypes(applicationAssembly)
            .Where(t => t is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false })
            .Where(t => typeof(IWorkflowExecutionHandler).IsAssignableFrom(t))
            .OrderBy(t => t.FullName, StringComparer.Ordinal)
            .ToList();

        foreach (var handlerType in handlerTypes)
        {
            services.AddScoped(typeof(IWorkflowExecutionHandler), handlerType);
        }

        services.AddScoped<IWorkflowExecutionHandlerFactory, WorkflowExecutionHandlerFactory>();
        return services;
    }

    /// <summary>
    /// Kiểm tra lúc khởi động: mọi quy trình trong danh mục có yêu cầu phê duyệt đều phải có
    /// bộ xử lý. Thiếu thì GHI CẢNH BÁO rõ ràng (không làm sập app — việc chặn thật sự nằm ở
    /// bước tạo hồ sơ trong WorkflowRuntimeService).
    /// </summary>
    public static void ValidateWorkflowExecutionHandlers(this IServiceProvider rootProvider)
    {
        using var scope = rootProvider.CreateScope();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("PTKD.Workflow.HandlerCheck");

        IReadOnlyCollection<string> registered;
        try
        {
            // Việc dựng factory cũng đồng thời phát hiện trùng mã quy trình (ném lỗi rõ ràng).
            registered = scope.ServiceProvider
                .GetRequiredService<IWorkflowExecutionHandlerFactory>()
                .RegisteredProcessCodes;
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Không dựng được danh sách bộ xử lý quy trình.");
            throw;
        }

        logger.LogInformation(
            "Quy trình có bộ xử lý ({Count}): {Codes}",
            registered.Count,
            string.Join(", ", registered.OrderBy(c => c, StringComparer.Ordinal)));

        try
        {
            var contextFactory = scope.ServiceProvider.GetRequiredService<IOrganizationDbContextFactory>();
            using var db = contextFactory.CreateDbContext();

            var needsHandler = db.BusinessProcessCatalogs.AsNoTracking()
                .Where(p => p.IsActive && p.IsApprovalRequired)
                .Select(p => p.ProcessCode)
                .ToList();

            var missing = needsHandler
                .Where(code => !registered.Contains(code, StringComparer.Ordinal))
                .OrderBy(c => c, StringComparer.Ordinal)
                .ToList();

            if (missing.Count > 0)
            {
                logger.LogWarning(
                    "CẢNH BÁO CẤU HÌNH: {Count} quy trình cần phê duyệt nhưng CHƯA có bộ xử lý: {Codes}. " +
                    "Hồ sơ của các quy trình này sẽ bị chặn ngay khi tạo (WF_NO_EXECUTION_HANDLER).",
                    missing.Count,
                    string.Join(", ", missing));
            }
        }
        catch (Exception ex)
        {
            // CSDL chưa sẵn sàng lúc khởi động không được làm sập ứng dụng.
            logger.LogWarning(ex, "Bỏ qua đối chiếu danh mục quy trình (không đọc được CSDL lúc khởi động).");
        }
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t != null)!;
        }
    }
}
