using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using PTKD.Application.Customers.DTOs;
using PTKD.Application.Customers.Services;
using PTKD.Application.Common.Interfaces;
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
        var service = new CustomerMergeService(mockFactory.Object);

        var request = new CreateCustomerMergeRequestDto
        {
            SourceCustomerId = 1,
            TargetCustomerId = 1
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateMergeRequestAsync(request, 99));
    }
}
