using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Customers.DTOs;
using PTKD.Application.Customers.Services;
using PTKD.Application.Security.Audit;
using PTKD.Domain.Entities;
using Xunit;

namespace PTKD.UnitTests.Customers;

public class CreateCustomerExecutionHandlerTests
{
    [Fact]
    public void ProcessCode_Is_CREATE_CUSTOMER()
    {
        var handler = new CreateCustomerExecutionHandler(
            Mock.Of<IOrganizationDbContextFactory>(),
            Mock.Of<ITransactionalAuditWriter>());

        Assert.Equal("CREATE_CUSTOMER", handler.ProcessCode);
    }
}
