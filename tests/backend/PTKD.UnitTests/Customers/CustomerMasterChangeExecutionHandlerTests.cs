using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Moq.EntityFrameworkCore;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Customers.Handlers;
using PTKD.Application.Security.Audit;
using PTKD.Domain.Entities;
using Xunit;

namespace PTKD.UnitTests.Customers;

public class CustomerMasterChangeExecutionHandlerTests
{
    private Mock<IOrganizationDbContextFactory> _dbFactoryMock;
    private Mock<IOrganizationDbContext> _dbContextMock;
    private Mock<ITransactionalAuditWriter> _auditMock;
    private CustomerMasterChangeExecutionHandler _handler;

    public CustomerMasterChangeExecutionHandlerTests()
    {
        _dbFactoryMock = new Mock<IOrganizationDbContextFactory>();
        _dbContextMock = new Mock<IOrganizationDbContext>();
        _auditMock = new Mock<ITransactionalAuditWriter>();

        _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _dbContextMock.Setup(c => c.BeginTransactionAsync(It.IsAny<IsolationLevel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<IDbContextTransaction>());
            
        var connMock = new Mock<System.Data.Common.DbConnection>();
        var tranMock = new Mock<System.Data.Common.DbTransaction>();
        _dbContextMock.Setup(c => c.GetDbConnection()).Returns(connMock.Object);
        _dbContextMock.Setup(c => c.GetCurrentDbTransaction()).Returns(tranMock.Object);

        _dbFactoryMock.Setup(f => f.CreateDbContext()).Returns(_dbContextMock.Object);

        _handler = new CustomerMasterChangeExecutionHandler(_dbFactoryMock.Object, _auditMock.Object);
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
        typeof(Customer).GetProperty("Id")?.SetValue(c, id);
        typeof(Customer).GetProperty("CustomerCode")?.SetValue(c, code);
        typeof(Customer).GetProperty("CustomerStatus")?.SetValue(c, status);
        typeof(Customer).GetProperty("Profile")?.SetValue(c, profile);
        if (rowVersion != null)
            typeof(Customer).GetProperty("RowVersion")?.SetValue(c, rowVersion);
        return c;
    }

    private WorkflowInstance CreateWorkflowInstance(long ccrId)
    {
        var wi = (WorkflowInstance)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(WorkflowInstance));
        typeof(WorkflowInstance).GetProperty("BusinessEntityType")?.SetValue(wi, "CustomerChangeRequest");
        typeof(WorkflowInstance).GetProperty("BusinessEntityId")?.SetValue(wi, ccrId);
        return wi;
    }

    [Fact]
    public void ProcessCode_Is_CUSTOMER_MASTER_CHANGE()
    {
        Assert.Equal("CUSTOMER_MASTER_CHANGE", _handler.ProcessCode);
    }

    [Fact]
    public async Task ExecuteAsync_Success_MutatesProfile()
    {
        // Arrange
        var customerId = 1L;
        var rowVersion = new byte[] { 1, 2, 3 };
        
        var profile = CreateProfile(customerId, "Old Name", "OLD_CCCD");
        var customer = CreateCustomer(customerId, "CUS001", "ACTIVE", profile, rowVersion);
        
        var payload = "{\"FullName\":\"New Name\",\"Cccd\":\"NEW_CCCD\"}";
        var ccr = CustomerChangeRequest.CreateForUpdate("CUSTOMER_MASTER_CHANGE", 1, payload, customerId, rowVersion, null);
        typeof(CustomerChangeRequest).GetProperty("Id")?.SetValue(ccr, 10L);
        typeof(CustomerChangeRequest).GetProperty("RequestStatus")?.SetValue(ccr, "APPROVED");
        
        _dbContextMock.Setup<DbSet<CustomerChangeRequest>>(c => c.CustomerChangeRequests).ReturnsDbSet(new List<CustomerChangeRequest> { ccr });
        _dbContextMock.Setup<DbSet<Customer>>(c => c.Customers).ReturnsDbSet(new List<Customer> { customer });

        var instance = CreateWorkflowInstance(10L);
        
        // Act
        await _handler.ExecuteAsync(instance);

        // Assert
        Assert.Equal("New Name", profile.FullName);
        Assert.Equal("NEW_CCCD", profile.Cccd);
        Assert.Equal("EXECUTED", ccr.RequestStatus);
        
        _auditMock.Verify(a => a.WriteAsync(It.Is<SecurityAuditEventRecord>(r => r.EventCode == "CUSTOMER_MASTER_CHANGE_EXECUTED"), It.IsAny<System.Data.Common.DbConnection>(), It.IsAny<System.Data.Common.DbTransaction>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_Idempotency_SkipsIfExecuted()
    {
        var ccr = CustomerChangeRequest.CreateForUpdate("CUSTOMER_MASTER_CHANGE", 1, "{}", 10, new byte[] { 1 }, null);
        typeof(CustomerChangeRequest).GetProperty("Id")?.SetValue(ccr, 10L);
        typeof(CustomerChangeRequest).GetProperty("RequestStatus")?.SetValue(ccr, "EXECUTED");
        
        _dbContextMock.Setup<DbSet<CustomerChangeRequest>>(c => c.CustomerChangeRequests).ReturnsDbSet(new List<CustomerChangeRequest> { ccr });
        var instance = CreateWorkflowInstance(10L);

        await _handler.ExecuteAsync(instance);
        
        // Should return early, no customers fetched
        _dbContextMock.Verify(c => c.Customers, Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_StaleTargetRowVersion_ThrowsConflict()
    {
        var customerId = 1L;
        var rowVersion = new byte[] { 1, 2, 3 };
        var staleVersion = new byte[] { 9, 9, 9 };
        
        var profile = CreateProfile(customerId, "Old Name", "OLD_CCCD");
        var customer = CreateCustomer(customerId, "CUS001", "ACTIVE", profile, rowVersion);
        
        var ccr = CustomerChangeRequest.CreateForUpdate("CUSTOMER_MASTER_CHANGE", 1, "{}", customerId, staleVersion, null);
        typeof(CustomerChangeRequest).GetProperty("Id")?.SetValue(ccr, 10L);
        typeof(CustomerChangeRequest).GetProperty("RequestStatus")?.SetValue(ccr, "APPROVED");
        
        _dbContextMock.Setup<DbSet<CustomerChangeRequest>>(c => c.CustomerChangeRequests).ReturnsDbSet(new List<CustomerChangeRequest> { ccr });
        _dbContextMock.Setup<DbSet<Customer>>(c => c.Customers).ReturnsDbSet(new List<Customer> { customer });

        var instance = CreateWorkflowInstance(10L);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.ExecuteAsync(instance));
        Assert.Contains("Concurrency conflict", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_RejectedRequest_ThrowsInvalidOperation()
    {
        var customerId = 1L;
        var ccr = CustomerChangeRequest.CreateForUpdate("CUSTOMER_MASTER_CHANGE", 1, "{}", customerId, new byte[] { 1 }, null);
        typeof(CustomerChangeRequest).GetProperty("Id")?.SetValue(ccr, 10L);
        typeof(CustomerChangeRequest).GetProperty("RequestStatus")?.SetValue(ccr, "REJECTED");
        
        _dbContextMock.Setup<DbSet<CustomerChangeRequest>>(c => c.CustomerChangeRequests).ReturnsDbSet(new List<CustomerChangeRequest> { ccr });

        var instance = CreateWorkflowInstance(10L);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.ExecuteAsync(instance));
        Assert.Contains("Cannot execute request in state REJECTED", ex.Message);
    }
}
