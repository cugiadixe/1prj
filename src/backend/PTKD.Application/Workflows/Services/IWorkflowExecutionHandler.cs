using System.Threading;
using System.Threading.Tasks;
using PTKD.Domain.Entities;

namespace PTKD.Application.Workflows.Services;

public interface IWorkflowExecutionHandler
{
    string ProcessCode { get; }
    Task ExecuteAsync(WorkflowInstance instance, CancellationToken ct = default);
}

public interface IWorkflowExecutionHandlerFactory
{
    IWorkflowExecutionHandler? GetHandler(string processCode);
}
