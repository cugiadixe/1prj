using System.Collections.Generic;
using Moq;
using PTKD.Application.Workflows.Services;
using Xunit;

namespace PTKD.UnitTests.Workflows;

public class WorkflowExecutionHandlerFactoryTests
{
    [Fact]
    public void GetHandler_Returns_Handler_For_Known_ProcessCode()
    {
        var handler = new Mock<IWorkflowExecutionHandler>();
        handler.Setup(h => h.ProcessCode).Returns("CREATE_CUSTOMER");

        var factory = new WorkflowExecutionHandlerFactory(new[] { handler.Object });
        var result = factory.GetHandler("CREATE_CUSTOMER");

        Assert.NotNull(result);
        Assert.Same(handler.Object, result);
    }

    [Fact]
    public void GetHandler_Returns_Null_For_Unknown_ProcessCode()
    {
        var factory = new WorkflowExecutionHandlerFactory(new List<IWorkflowExecutionHandler>());
        var result = factory.GetHandler("UNKNOWN");
        Assert.Null(result);
    }

    [Fact]
    public void GetHandler_Supports_Multiple_Handlers()
    {
        var h1 = new Mock<IWorkflowExecutionHandler>();
        h1.Setup(h => h.ProcessCode).Returns("CREATE_CUSTOMER");
        var h2 = new Mock<IWorkflowExecutionHandler>();
        h2.Setup(h => h.ProcessCode).Returns("CUSTOMER_MASTER_CHANGE");

        var factory = new WorkflowExecutionHandlerFactory(new[] { h1.Object, h2.Object });

        Assert.Same(h1.Object, factory.GetHandler("CREATE_CUSTOMER"));
        Assert.Same(h2.Object, factory.GetHandler("CUSTOMER_MASTER_CHANGE"));
    }
}
