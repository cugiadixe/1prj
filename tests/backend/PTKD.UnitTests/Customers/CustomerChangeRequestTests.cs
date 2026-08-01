using System;
using PTKD.Domain.Entities;
using Xunit;

namespace PTKD.UnitTests.Customers;

public class CustomerChangeRequestTests
{
    [Fact]
    public void Constructor_Sets_Draft_Status()
    {
        var ccr = new CustomerChangeRequest("CREATE_CUSTOMER", 1, "{}");
        Assert.Equal("DRAFT", ccr.RequestStatus);
        Assert.Equal("CREATE_CUSTOMER", ccr.ProcessCode);
        Assert.Equal(1, ccr.RequesterId);
    }

    [Fact]
    public void SetSubmitted_Sets_Status_And_InstanceId()
    {
        var ccr = new CustomerChangeRequest("CREATE_CUSTOMER", 1, "{}");
        ccr.SetSubmitted(42);
        Assert.Equal("SUBMITTED", ccr.RequestStatus);
        Assert.Equal(42, ccr.WorkflowInstanceId);
    }

    [Fact]
    public void SetExecuted_Sets_Status_And_CustomerId()
    {
        var ccr = new CustomerChangeRequest("CREATE_CUSTOMER", 1, "{}");
        ccr.SetSubmitted(1);
        ccr.SetApproved();
        ccr.SetExecuted(99);
        Assert.Equal("EXECUTED", ccr.RequestStatus);
        Assert.Equal(99, ccr.CreatedCustomerId);
    }

    [Fact]
    public void SetFailed_Sets_Status()
    {
        var ccr = new CustomerChangeRequest("CREATE_CUSTOMER", 1, "{}");
        ccr.SetFailed();
        Assert.Equal("FAILED", ccr.RequestStatus);
    }

    [Fact]
    public void SetWithdrawn_Sets_Status()
    {
        var ccr = new CustomerChangeRequest("CREATE_CUSTOMER", 1, "{}");
        ccr.SetWithdrawn();
        Assert.Equal("WITHDRAWN", ccr.RequestStatus);
    }

    [Fact]
    public void Constructor_With_CompanyId()
    {
        var ccr = new CustomerChangeRequest("CREATE_CUSTOMER", 1, "{}", 5);
        Assert.Equal(5, ccr.CompanyId);
    }

    [Fact]
    public void Constructor_Throws_On_Null_ProcessCode()
    {
        Assert.Throws<ArgumentNullException>(() => new CustomerChangeRequest(null!, 1, "{}"));
    }

    [Fact]
    public void Constructor_Throws_On_Null_PayloadJson()
    {
        Assert.Throws<ArgumentNullException>(() => new CustomerChangeRequest("CREATE_CUSTOMER", 1, null!));
    }
}
