using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Moq;
using PTKD.Application.Common.Exceptions;
using PTKD.Application.Security.Audit.DTOs;
using PTKD.Infrastructure.Security.Audit;
using Xunit;

namespace PTKD.UnitTests.Security.Audit;

public class SecurityAuditQueryValidationTests
{
    private readonly SqlSecurityAuditQueryService _service;

    public SecurityAuditQueryValidationTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string?> {
                { "ConnectionStrings:DefaultConnection", "Server=dummy;Database=dummy;" }
            })
            .Build();

        _service = new SqlSecurityAuditQueryService(config);
    }

    [Fact]
    public async Task GetAuditEventsAsync_PageLessThan1_ThrowsValidationException()
    {
        var parameters = new SecurityAuditQueryParameters { Page = 0 };

        var ex = await Assert.ThrowsAsync<BusinessRuleValidationException>(() => _service.GetAuditEventsAsync(parameters));
        Assert.Equal("INVALID_PAGE", ex.ErrorCode);
    }

    [Fact]
    public async Task GetAuditEventsAsync_PageSizeLessThan1_ThrowsValidationException()
    {
        var parameters = new SecurityAuditQueryParameters { PageSize = 0 };

        var ex = await Assert.ThrowsAsync<BusinessRuleValidationException>(() => _service.GetAuditEventsAsync(parameters));
        Assert.Equal("INVALID_PAGE_SIZE", ex.ErrorCode);
    }

    [Fact]
    public async Task GetAuditEventsAsync_PageSizeExceedsCap_ThrowsValidationException()
    {
        var parameters = new SecurityAuditQueryParameters { PageSize = 1001 };

        var ex = await Assert.ThrowsAsync<BusinessRuleValidationException>(() => _service.GetAuditEventsAsync(parameters));
        Assert.Equal("PAGE_SIZE_EXCEEDED", ex.ErrorCode);
    }

    [Fact]
    public async Task GetAuditEventsAsync_FromUtcGreaterThanToUtc_ThrowsValidationException()
    {
        var parameters = new SecurityAuditQueryParameters
        {
            FromUtc = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            ToUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var ex = await Assert.ThrowsAsync<BusinessRuleValidationException>(() => _service.GetAuditEventsAsync(parameters));
        Assert.Equal("INVALID_DATE_RANGE", ex.ErrorCode);
    }
}
