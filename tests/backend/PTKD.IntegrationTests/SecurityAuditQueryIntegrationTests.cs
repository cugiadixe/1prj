using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Moq;
using PTKD.Application.Security.Audit;
using PTKD.Application.Security.Audit.DTOs;
using PTKD.Infrastructure.Security.Audit;
using Xunit;

namespace PTKD.IntegrationTests.Security.Audit;

[Collection("Sequential")]
public sealed class SecurityAuditQueryIntegrationTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;
    private readonly SqlSecurityAuditQueryService _queryService;
    private readonly SqlSecurityAuditWriter _writer;

    public SecurityAuditQueryIntegrationTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        _fixture.ResetToV0003();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string?> {
                { "ConnectionStrings:DefaultConnection", TestDatabaseSafety.DefaultConnectionString }
            })
            .Build();

        _queryService = new SqlSecurityAuditQueryService(config);
        _writer = new SqlSecurityAuditWriter(TestDatabaseSafety.DefaultConnectionString);
    }

    [Fact]
    public async Task GetAuditEventsAsync_ReturnsPagedResult()
    {
        // Arrange
        await SeedEventsAsync(15, "TEST_EVENT");

        // Act
        var result = await _queryService.GetAuditEventsAsync(new SecurityAuditQueryParameters { Page = 1, PageSize = 10 });

        // Assert
        Assert.Equal(15, result.TotalCount);
        Assert.Equal(10, result.Items.Count());
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task GetAuditEventsAsync_FiltersByDateRange()
    {
        // Arrange
        var startTime = DateTime.UtcNow;
        await SeedEventsAsync(3, "TEST_DATE");
        var endTime = DateTime.UtcNow.AddMinutes(1);

        var parameters = new SecurityAuditQueryParameters
        {
            FromUtc = startTime.AddSeconds(-1),
            ToUtc = endTime,
            Page = 1,
            PageSize = 50
        };

        // Act
        var result = await _queryService.GetAuditEventsAsync(parameters);

        // Assert
        Assert.True(result.TotalCount >= 3);
        Assert.Contains(result.Items, i => i.EventCode == "TEST_DATE");
    }

    [Fact]
    public async Task GetAuditEventsAsync_FiltersByEventType()
    {
        // Arrange
        await SeedEventsAsync(2, "FILTER_EVENT_A");
        await SeedEventsAsync(1, "FILTER_EVENT_B");

        var parameters = new SecurityAuditQueryParameters { EventType = "FILTER_EVENT_A" };

        // Act
        var result = await _queryService.GetAuditEventsAsync(parameters);

        // Assert
        Assert.True(result.Items.All(i => i.EventCode == "FILTER_EVENT_A"));
    }

    [Fact]
    public async Task GetAuditEventsAsync_ResponseExcludesJsonFields()
    {
        // Arrange
        var record = new SecurityAuditEventRecord
        {
            EventCode = "JSON_TEST",
            EntityType = "USER",
            Outcome = "SUCCESS",
            CorrelationId = Guid.NewGuid(),
            BeforeStateJson = "{\"test\":1}",
            AfterStateJson = "{\"test\":2}",
            ChangedFieldsJson = "{\"test\":\"changed\"}"
        };
        await _writer.WriteAsync(record);

        // Act
        var result = await _queryService.GetAuditEventsAsync(new SecurityAuditQueryParameters { EventType = "JSON_TEST" });

        // Assert
        var item = result.Items.First();
        Assert.Equal("JSON_TEST", item.EventCode);
        
        // Assert that SecurityAuditEventDto does not even contain properties for BeforeStateJson, AfterStateJson, ChangedFieldsJson
        var properties = typeof(SecurityAuditEventDto).GetProperties().Select(p => p.Name).ToList();
        Assert.DoesNotContain("BeforeStateJson", properties);
        Assert.DoesNotContain("AfterStateJson", properties);
        Assert.DoesNotContain("ChangedFields", properties);
        Assert.DoesNotContain("ChangedFieldsJson", properties);
    }
    
    [Fact]
    public async Task GetAuditEventsAsync_ReturnsEmptyWhenNoMatches()
    {
        var parameters = new SecurityAuditQueryParameters { EventType = "NON_EXISTENT_EVENT_TYPE" };
        var result = await _queryService.GetAuditEventsAsync(parameters);
        
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    private async Task SeedEventsAsync(int count, string eventCode)
    {
        for (int i = 0; i < count; i++)
        {
            var record = new SecurityAuditEventRecord
            {
                EventCode = eventCode,
                EntityType = "TEST_ENTITY",
                Outcome = "SUCCESS",
                CorrelationId = Guid.NewGuid()
            };
            await _writer.WriteAsync(record);
        }
    }
}
