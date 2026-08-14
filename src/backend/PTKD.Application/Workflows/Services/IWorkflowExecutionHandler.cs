using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PTKD.Domain.Entities;

namespace PTKD.Application.Workflows.Services;

public interface IWorkflowExecutionHandler
{
    string ProcessCode { get; }

    /// <summary>Chạy nghiệp vụ sau khi hồ sơ được duyệt hết các bước.</summary>
    Task ExecuteAsync(WorkflowInstance instance, CancellationToken ct = default);

    /// <summary>
    /// Hoàn tác nghiệp vụ khi hồ sơ bị TỪ CHỐI (đưa bản ghi ra khỏi trạng thái "chờ duyệt").
    /// Mặc định không làm gì — module nào cần thì cài đè.
    /// Trước đây engine không có móc này, nên bản ghi bị từ chối kẹt "chờ duyệt" vĩnh viễn.
    /// </summary>
    Task OnRejectedAsync(WorkflowInstance instance, CancellationToken ct = default) => Task.CompletedTask;
}

public interface IWorkflowExecutionHandlerFactory
{
    IWorkflowExecutionHandler? GetHandler(string processCode);

    /// <summary>Có bộ xử lý cho mã quy trình này không (dùng để chặn sớm khi tạo hồ sơ).</summary>
    bool HasHandler(string processCode);

    /// <summary>Danh sách mã quy trình đã có bộ xử lý (dùng để kiểm tra lúc khởi động).</summary>
    IReadOnlyCollection<string> RegisteredProcessCodes { get; }
}
