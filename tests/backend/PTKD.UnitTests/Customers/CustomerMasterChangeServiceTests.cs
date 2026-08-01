using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Moq.EntityFrameworkCore;
using PTKD.Application.Common.Exceptions;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Customers.DTOs;
using PTKD.Application.Customers.Services;
using PTKD.Application.Security.Audit;
using PTKD.Application.Workflows.DTOs;
using PTKD.Application.Workflows.Services;
using PTKD.Domain.Entities;
using Xunit;

namespace PTKD.UnitTests.Customers;

public class CustomerMasterChangeServiceTests
{
    private Mock<IOrganizationDbContextFactory> _dbFactoryMock;
    private Mock<IOrganizationDbContext> _dbContextMock;
    private Mock<IWorkflowRuntimeService> _workflowMock;
    private Mock<ITransactionalAuditWriter> _auditMock;
    private CustomerMasterChangeService _service;

    public CustomerMasterChangeServiceTests()
    {
        _dbFactoryMock = new Mock<IOrganizationDbContextFactory>();
        _dbContextMock = new Mock<IOrganizationDbContext>();
        _workflowMock = new Mock<IWorkflowRuntimeService>();
        _auditMock = new Mock<ITransactionalAuditWriter>();

        _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _dbContextMock.Setup(c => c.BeginTransactionAsync(It.IsAny<IsolationLevel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<IDbContextTransaction>());
            
        var connMock = new Mock<System.Data.Common.DbConnection>();
        var tranMock = new Mock<System.Data.Common.DbTransaction>();
        _dbContextMock.Setup(c => c.GetDbConnection()).Returns(connMock.Object);
        _dbContextMock.Setup(c => c.GetCurrentDbTransaction()).Returns(tranMock.Object);

        _dbContextMock.Setup(c => c.CreateExecutionStrategy()).Returns(new NoOpExecutionStrategy());
        _dbFactoryMock.Setup(f => f.CreateDbContext()).Returns(_dbContextMock.Object);

        _service = new CustomerMasterChangeService(_dbFactoryMock.Object, _workflowMock.Object, _auditMock.Object);
    }

    private Profile CreateProfile(long id, string fullName, string cccd)
    {
        var p = (Profile)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Profile));
        typeof(Profile).GetProperty("Id")?.SetValue(p, id);
        typeof(Profile).GetProperty("FullName")?.SetValue(p, fullName);
        typeof(Profile).GetProperty("Cccd")?.SetValue(p, cccd);
        
        var field = typeof(Profile).GetProperty("IsActive");
        if (field != null) field.SetValue(p, true);
        return p;
    }

    private Customer CreateCustomer(long id, string code, string status, Profile profile, byte[]? rowVersion = null)
    {
        var c = (Customer)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Customer));
        typeof(Customer).GetField("<Id>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(c, id);
        typeof(Customer).GetProperty("CustomerCode")?.SetValue(c, code);
        typeof(Customer).GetProperty("CustomerStatus")?.SetValue(c, status);
        typeof(Customer).GetProperty("Profile")?.SetValue(c, profile);
        if (rowVersion != null)
            typeof(Customer).GetField("<RowVersion>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(c, rowVersion);
        return c;
    }

    [Fact]
    public async Task CreateChangeRequest_Success()
    {
        // Arrange
        var customerId = 1L;
        var rowVersionBytes = new byte[] { 1, 2, 3 };
        var request = new CreateCustomerMasterChangeRequest
        {
            TargetCustomerId = customerId,
            TargetRowVersion = Convert.ToBase64String(rowVersionBytes),
            FullName = "New Name",
            Reason = "Typo"
        };

        var profile = CreateProfile(customerId, "New Name", "CCCD");
        var customer = CreateCustomer(customerId, "CUS001", "ACTIVE", profile, rowVersionBytes);
        
        _dbContextMock.Setup<DbSet<Customer>>(c => c.Customers).ReturnsDbSet(new List<Customer> { customer });
        _dbContextMock.Setup<DbSet<Profile>>(c => c.Profiles).ReturnsDbSet(new List<Profile> { profile });
        
        var dummyCcr = CustomerChangeRequest.CreateForUpdate("CUSTOMER_MASTER_CHANGE", 999, "{}", customerId, rowVersionBytes, null);
        typeof(CustomerChangeRequest).GetProperty("Id")?.GetSetMethod(true)?.Invoke(dummyCcr, new object[] { 0L });
        var changeRequests = new List<CustomerChangeRequest> { dummyCcr };
        _dbContextMock.Setup(c => c.CustomerChangeRequests).ReturnsDbSet(changeRequests);

        _workflowMock.Setup(w => w.CreateInstanceAsync(It.IsAny<CreateWorkflowInstanceRequest>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PTKD.Application.Workflows.DTOs.WorkflowInstanceDto { Id = 100 });

        // Act
        var result = await _service.CreateChangeRequestAsync(request, 999, 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(100, result.WorkflowInstanceId);
        _auditMock.Verify(a => a.WriteAsync(It.Is<SecurityAuditEventRecord>(r => r.EventCode == "CUSTOMER_MASTER_CHANGE_PROPOSED"), It.IsAny<System.Data.Common.DbConnection>(), It.IsAny<System.Data.Common.DbTransaction>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateChangeRequest_CustomerNotFound_Throws()
    {
        var request = new CreateCustomerMasterChangeRequest
        {
            TargetCustomerId = 99,
            TargetRowVersion = Convert.ToBase64String(new byte[] { 1 })
        };
        _dbContextMock.Setup<DbSet<Customer>>(c => c.Customers).ReturnsDbSet(new List<Customer>());

        var ex = await Assert.ThrowsAsync<BusinessRuleValidationException>(() => _service.CreateChangeRequestAsync(request, 1));
        Assert.Equal("CUS_NOT_FOUND", ex.ErrorCode);
    }

    [Fact]
    public async Task CreateChangeRequest_InactiveCustomer_Throws()
    {
        var customerId = 1L;
        var request = new CreateCustomerMasterChangeRequest
        {
            TargetCustomerId = customerId,
            TargetRowVersion = Convert.ToBase64String(new byte[] { 1 })
        };
        var profile = CreateProfile(customerId, "Name", "CCCD");
        var customer = CreateCustomer(customerId, "CUS001", "INACTIVE", profile);
        
        _dbContextMock.Setup<DbSet<Customer>>(c => c.Customers).ReturnsDbSet(new List<Customer> { customer });

        var ex = await Assert.ThrowsAsync<BusinessRuleValidationException>(() => _service.CreateChangeRequestAsync(request, 1));
        Assert.Equal("CUS_NOT_ACTIVE", ex.ErrorCode);
    }

    [Fact]
    public async Task CreateChangeRequest_DuplicateCCCD_Throws()
    {
        var customerId = 1L;
        var request = new CreateCustomerMasterChangeRequest
        {
            TargetCustomerId = customerId,
            TargetRowVersion = Convert.ToBase64String(new byte[] { 1 }),
            Cccd = "DUPLICATE"
        };
        
        var profile1 = CreateProfile(customerId, "Name", "OLD_CCCD");
        var customer1 = CreateCustomer(customerId, "CUS001", "ACTIVE", profile1);
        
        var profile2 = CreateProfile(2L, "Other", "DUPLICATE");
        var customer2 = CreateCustomer(2L, "CUS002", "ACTIVE", profile2);
        
        _dbContextMock.Setup<DbSet<Customer>>(c => c.Customers).ReturnsDbSet(new List<Customer> { customer1, customer2 });
        _dbContextMock.Setup<DbSet<Profile>>(c => c.Profiles).ReturnsDbSet(new List<Profile> { profile1, profile2 });

        var ex = await Assert.ThrowsAsync<BusinessRuleValidationException>(() => _service.CreateChangeRequestAsync(request, 1));
        Assert.Equal("CUS_DUPLICATE_CCCD", ex.ErrorCode);
    }

    [Fact]
    public async Task GetChangeRequestById_Success()
    {
        var ccr = CustomerChangeRequest.CreateForUpdate("CUSTOMER_MASTER_CHANGE", 1, "{}", 10, new byte[] { 1 }, null);
        typeof(CustomerChangeRequest).GetProperty("Id")?.GetSetMethod(true)?.Invoke(ccr, new object[] { 5L });
        typeof(CustomerChangeRequest).GetProperty("RowVersion")?.GetSetMethod(true)?.Invoke(ccr, new object[] { new byte[] { 1, 2, 3 } });
        _dbContextMock.Setup<DbSet<CustomerChangeRequest>>(c => c.CustomerChangeRequests).ReturnsDbSet(new List<CustomerChangeRequest> { ccr });

        var result = await _service.GetChangeRequestByIdAsync(5);
        Assert.NotNull(result);
        Assert.Equal(5, result.Id);
        Assert.Equal("CUSTOMER_MASTER_CHANGE", result.ProcessCode);
    }

    [Fact]
    public async Task GetMyChangeRequests_Success()
    {
        var ccr1 = CustomerChangeRequest.CreateForUpdate("CUSTOMER_MASTER_CHANGE", 1, "{}", 10, new byte[] { 1 }, null);
        var ccr2 = CustomerChangeRequest.CreateForUpdate("OTHER_PROCESS", 1, "{}", 10, new byte[] { 1 }, null);
        var ccr3 = CustomerChangeRequest.CreateForUpdate("CUSTOMER_MASTER_CHANGE", 2, "{}", 10, new byte[] { 1 }, null);
        
        typeof(CustomerChangeRequest).GetProperty("Id")?.GetSetMethod(true)?.Invoke(ccr1, new object[] { 1L });
        typeof(CustomerChangeRequest).GetProperty("RowVersion")?.GetSetMethod(true)?.Invoke(ccr1, new object[] { new byte[] { 1 } });
        typeof(CustomerChangeRequest).GetProperty("RowVersion")?.GetSetMethod(true)?.Invoke(ccr2, new object[] { new byte[] { 1 } });
        typeof(CustomerChangeRequest).GetProperty("RowVersion")?.GetSetMethod(true)?.Invoke(ccr3, new object[] { new byte[] { 1 } });
        
        _dbContextMock.Setup<DbSet<CustomerChangeRequest>>(c => c.CustomerChangeRequests).ReturnsDbSet(new List<CustomerChangeRequest> { ccr1, ccr2, ccr3 });

        var result = await _service.GetMyChangeRequestsAsync(1);
        Assert.Single(result);
        Assert.Equal(1L, result[0].Id);
    }

    private class NoOpExecutionStrategy : IExecutionStrategy
    {
        public bool RetriesOnFailure => false;
        public TResult Execute<TState, TResult>(TState state, Func<DbContext, TState, TResult> operation, Func<DbContext, TState, ExecutionResult<TResult>> verifySucceeded) => operation(null, state);
        public Task<TResult> ExecuteAsync<TState, TResult>(TState state, Func<DbContext, TState, CancellationToken, Task<TResult>> operation, Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>> verifySucceeded, CancellationToken cancellationToken) => operation(null, state, cancellationToken);
    }
}
