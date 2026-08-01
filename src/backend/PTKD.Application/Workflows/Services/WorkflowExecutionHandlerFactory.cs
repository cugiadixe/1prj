using System.Collections.Generic;
using System.Linq;

namespace PTKD.Application.Workflows.Services;

public class WorkflowExecutionHandlerFactory : IWorkflowExecutionHandlerFactory
{
    private readonly Dictionary<string, IWorkflowExecutionHandler> _handlers;

    public WorkflowExecutionHandlerFactory(IEnumerable<IWorkflowExecutionHandler> handlers)
    {
        _handlers = handlers.ToDictionary(h => h.ProcessCode);
    }

    public IWorkflowExecutionHandler? GetHandler(string processCode)
    {
        _handlers.TryGetValue(processCode, out var handler);
        return handler;
    }
}
