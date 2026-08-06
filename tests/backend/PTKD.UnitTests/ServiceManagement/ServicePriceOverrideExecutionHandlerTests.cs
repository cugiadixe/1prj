using Moq;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Security.Audit;
using PTKD.Application.ServiceManagement.Handlers;
using Xunit;

namespace PTKD.UnitTests.ServiceManagement;

public class ServicePriceOverrideExecutionHandlerTests
{
    [Fact]
    public void ProcessCode_Returns_SERVICE_PRICE_OVERRIDE()
    {
        var mockFactory = new Mock<IOrganizationDbContextFactory>();
        var mockAudit = new Mock<ITransactionalAuditWriter>();
        var handler = new ServicePriceOverrideExecutionHandler(mockFactory.Object, mockAudit.Object);
        Assert.Equal("SERVICE_PRICE_OVERRIDE", handler.ProcessCode);
    }
}
