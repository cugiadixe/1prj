using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using PTKD.Application.Customers.DTOs;
using PTKD.Application.Customers.Services;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Customers.Handlers;
using PTKD.Application.Security.Audit;
using PTKD.Application.Security.Authorization.Interfaces;
using PTKD.Application.Workflows.Services;
using PTKD.Domain.Entities;
using Xunit;

namespace PTKD.UnitTests.Customers;

public class CustomerMergeServiceTests
{
    [Fact]
    public async Task CreateMergeRequest_SourceTargetSame_Throws()
    {
        var mockDb = new Mock<IOrganizationDbContext>();
        var mockFactory = new Mock<IOrganizationDbContextFactory>();
        mockFactory.Setup(f => f.CreateDbContext()).Returns(mockDb.Object);
        var mockEvaluator = new Mock<IPermissionEvaluator>();
        var mockWorkflowRuntime = new Mock<IWorkflowRuntimeService>();
        var executor = new CustomerMergeExecutor(mockFactory.Object, Mock.Of<ITransactionalAuditWriter>());
        var service = new CustomerMergeService(mockFactory.Object, mockEvaluator.Object, mockWorkflowRuntime.Object, executor);

        var request = new CreateCustomerMergeRequestDto
        {
            SourceCustomerId = 1,
            TargetCustomerId = 1
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateMergeRequestAsync(request, 99));
    }
}
