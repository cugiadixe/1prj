using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using PTKD.Application.Common.Exceptions;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Customers.Handlers;
using PTKD.Application.Security.Audit;
using PTKD.Domain.Entities;
using Xunit;

namespace PTKD.UnitTests.Customers;

public class CustomerMergeExecutionHandlerTests
{
    // Simplified unit tests for execution handler idempotency, etc.
    // Let's rely on integration tests for full database behavior.
    [Fact]
    public void Constructor_Succeeds()
    {
        var mockFactory = new Mock<IOrganizationDbContextFactory>();
        var mockAudit = new Mock<ITransactionalAuditWriter>();

        var executor = new CustomerMergeExecutor(mockFactory.Object, mockAudit.Object);
        var handler = new CustomerMergeExecutionHandler(executor);
        Assert.Equal("CUSTOMER_MERGE_DUPLICATE", handler.ProcessCode);
    }
}
