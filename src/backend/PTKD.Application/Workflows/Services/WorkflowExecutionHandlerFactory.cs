using System;
using System.Collections.Generic;
using System.Linq;

namespace PTKD.Application.Workflows.Services;

public class WorkflowExecutionHandlerFactory : IWorkflowExecutionHandlerFactory
{
    private readonly Dictionary<string, IWorkflowExecutionHandler> _handlers;

    public WorkflowExecutionHandlerFactory(IEnumerable<IWorkflowExecutionHandler> handlers)
    {
        // Trùng ProcessCode là lỗi lập trình: trước đây ToDictionary ném ArgumentException
        // với thông báo khó hiểu ("An item with the same key has already been added").
        // Nêu rõ mã quy trình nào bị trùng và do class nào.
        var duplicates = handlers
            .GroupBy(h => h.ProcessCode, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .ToList();

        if (duplicates.Count > 0)
        {
            var detail = string.Join("; ", duplicates.Select(g =>
                $"{g.Key} <- {string.Join(", ", g.Select(h => h.GetType().Name))}"));
            throw new InvalidOperationException(
                $"Có nhiều bộ xử lý (execution handler) cùng khai báo một mã quy trình: {detail}. " +
                "Mỗi mã quy trình chỉ được có đúng một bộ xử lý.");
        }

        _handlers = handlers.ToDictionary(h => h.ProcessCode, StringComparer.Ordinal);
    }

    public IWorkflowExecutionHandler? GetHandler(string processCode)
    {
        if (string.IsNullOrWhiteSpace(processCode)) return null;
        _handlers.TryGetValue(processCode, out var handler);
        return handler;
    }

    public bool HasHandler(string processCode) => GetHandler(processCode) != null;

    public IReadOnlyCollection<string> RegisteredProcessCodes => _handlers.Keys.ToList();
}
