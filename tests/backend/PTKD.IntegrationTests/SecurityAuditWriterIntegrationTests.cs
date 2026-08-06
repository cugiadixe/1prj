using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using PTKD.Application.Security.Audit;
using PTKD.Infrastructure.Security.Audit;

namespace PTKD.IntegrationTests;

[Collection("Sequential")]
public sealed class SecurityAuditWriterIntegrationTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;
    private readonly SqlSecurityAuditWriter _writer;

    public SecurityAuditWriterIntegrationTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        _fixture.ResetToV0003();
        _writer = new SqlSecurityAuditWriter(TestDatabaseSafety.DefaultConnectionString);
    }

    [Fact]
    public async Task WriteAsync_MinimalRequiredFields_InsertsExactlyOneRow()
    {
        var correlationId = Guid.NewGuid();
        var record = new SecurityAuditEventRecord
        {
            EventCode = "TEST_MINIMAL",
            EntityType = "USER",
            Outcome = "SUCCESS",
            CorrelationId = correlationId
        };

        await _writer.WriteAsync(record);

        Assert.Equal(1, CountByCorrelationId(correlationId));
    }

    [Fact]
    public async Task WriteAsync_WrittenRow_HasCorrectEventCode()
    {
        var correlationId = Guid.NewGuid();
        var record = new SecurityAuditEventRecord
        {
            EventCode = "BOOTSTRAP_ADMIN_CREATED",
            EntityType = "USER",
            Outcome = "SUCCESS",
            CorrelationId = correlationId
        };

        await _writer.WriteAsync(record);

        using var connection = _fixture.OpenVerifiedConnection();
        using var command = new SqlCommand(
            "SELECT event_code FROM dbo.Security_Audit_Events WHERE correlation_id = @cid;",
            connection);
        command.Parameters.AddWithValue("@cid", correlationId);
        var actual = (string?)command.ExecuteScalar();
        Assert.Equal("BOOTSTRAP_ADMIN_CREATED", actual);
    }

    [Fact]
    public async Task WriteAsync_WrittenRow_HasCorrectCorrelationId()
    {
        var correlationId = Guid.NewGuid();
        var record = new SecurityAuditEventRecord
        {
            EventCode = "TEST_EVENT",
            EntityType = "USER",
            Outcome = "SUCCESS",
            CorrelationId = correlationId
        };

        await _writer.WriteAsync(record);

        using var connection = _fixture.OpenVerifiedConnection();
        using var command = new SqlCommand(
            "SELECT correlation_id FROM dbo.Security_Audit_Events WHERE correlation_id = @cid;",
            connection);
        command.Parameters.AddWithValue("@cid", correlationId);
        var actual = (Guid?)command.ExecuteScalar();
        Assert.Equal(correlationId, actual);
    }

    [Fact]
    public async Task WriteAsync_WrittenRow_HasCorrectOutcome()
    {
        var correlationId = Guid.NewGuid();
        var record = new SecurityAuditEventRecord
        {
            EventCode = "TEST_EVENT",
            EntityType = "USER",
            Outcome = "FAILURE",
            CorrelationId = correlationId
        };

        await _writer.WriteAsync(record);

        using var connection = _fixture.OpenVerifiedConnection();
        using var command = new SqlCommand(
            "SELECT outcome FROM dbo.Security_Audit_Events WHERE correlation_id = @cid;",
            connection);
        command.Parameters.AddWithValue("@cid", correlationId);
        var actual = (string?)command.ExecuteScalar();
        Assert.Equal("FAILURE", actual);
    }

    [Fact]
    public async Task WriteAsync_WrittenRow_HasNonNullCreatedAtFromDatabase()
    {
        var correlationId = Guid.NewGuid();
        var record = new SecurityAuditEventRecord
        {
            EventCode = "TEST_EVENT",
            EntityType = "USER",
            Outcome = "SUCCESS",
            CorrelationId = correlationId
        };

        await _writer.WriteAsync(record);

        using var connection = _fixture.OpenVerifiedConnection();
        using var command = new SqlCommand(
            "SELECT created_at FROM dbo.Security_Audit_Events WHERE correlation_id = @cid;",
            connection);
        command.Parameters.AddWithValue("@cid", correlationId);
        var actual = command.ExecuteScalar();
        Assert.NotNull(actual);
        Assert.NotEqual(DBNull.Value, actual);
    }

    [Fact]
    public async Task WriteAsync_NullOptionalFields_AreStoredAsDbNull()
    {
        var correlationId = Guid.NewGuid();
        var record = new SecurityAuditEventRecord
        {
            EventCode = "TEST_EVENT",
            EntityType = "USER",
            Outcome = "SUCCESS",
            CorrelationId = correlationId
            // All nullable fields left as null
        };

        await _writer.WriteAsync(record);

        using var connection = _fixture.OpenVerifiedConnection();
        using var command = new SqlCommand("""
            SELECT actor_user_id, entity_id, changed_fields, reason, policy_version
            FROM dbo.Security_Audit_Events
            WHERE correlation_id = @cid;
            """, connection);
        command.Parameters.AddWithValue("@cid", correlationId);
        using var reader = command.ExecuteReader();
        Assert.True(reader.Read());
        Assert.True(reader.IsDBNull(0), "actor_user_id should be NULL");
        Assert.True(reader.IsDBNull(1), "entity_id should be NULL");
        Assert.True(reader.IsDBNull(2), "changed_fields should be NULL");
        Assert.True(reader.IsDBNull(3), "reason should be NULL");
        Assert.True(reader.IsDBNull(4), "policy_version should be NULL");
    }

    [Fact]
    public async Task WriteAsync_WithPreCancelledToken_ThrowsOperationCanceledException_NotAuditWriteException()
    {
        // Cancellation must propagate as OperationCanceledException, never wrapped in SecurityAuditWriteException.
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var record = new SecurityAuditEventRecord
        {
            EventCode = "TEST_EVENT",
            EntityType = "USER",
            Outcome = "SUCCESS",
            CorrelationId = Guid.NewGuid()
        };

        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _writer.WriteAsync(record, cts.Token));

        Assert.IsNotType<SecurityAuditWriteException>(ex);
    }

    // ── Failure-policy tests (OD-F-04) ──────────────────────────────────────────

    // RFC 5737 TEST-NET-1: 192.0.2.0/24 is documentation-reserved and not routable.
    // Using it as the server address guarantees a SqlException from the client within
    // the connect timeout rather than unpredictably depending on DNS or firewall state.
    private const string UnreachableConnectionString =
        "Server=192.0.2.1;Database=PTKD_TEST_PHASE1A2;" +
        "Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=3;";

    [Fact]
    public async Task WriteAsync_DatabaseUnreachable_ThrowsSecurityAuditWriteException()
    {
        var writer = new SqlSecurityAuditWriter(UnreachableConnectionString);
        var record = new SecurityAuditEventRecord
        {
            EventCode = "TEST_EVENT",
            EntityType = "USER",
            Outcome = "SUCCESS",
            CorrelationId = Guid.NewGuid()
        };

        await Assert.ThrowsAsync<SecurityAuditWriteException>(
            () => writer.WriteAsync(record));
    }

    [Fact]
    public async Task WriteAsync_DatabaseUnreachable_ExceptionMessageIsSanitized()
    {
        var writer = new SqlSecurityAuditWriter(UnreachableConnectionString);
        var record = new SecurityAuditEventRecord
        {
            EventCode = "SENSITIVE_CODE",
            EntityType = "USER",
            Outcome = "SUCCESS",
            CorrelationId = Guid.NewGuid()
        };

        var ex = await Assert.ThrowsAsync<SecurityAuditWriteException>(
            () => writer.WriteAsync(record));

        Assert.Equal("Security audit event could not be written.", ex.Message);

        // Public message must not leak SQL text, connection details, or payload values.
        Assert.DoesNotContain("INSERT", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Security_Audit_Events", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("192.0.2.1", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SENSITIVE_CODE", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WriteAsync_DatabaseUnreachable_InnerExceptionContainsOriginalCause()
    {
        var writer = new SqlSecurityAuditWriter(UnreachableConnectionString);
        var record = new SecurityAuditEventRecord
        {
            EventCode = "TEST_EVENT",
            EntityType = "USER",
            Outcome = "SUCCESS",
            CorrelationId = Guid.NewGuid()
        };

        var ex = await Assert.ThrowsAsync<SecurityAuditWriteException>(
            () => writer.WriteAsync(record));

        Assert.NotNull(ex.InnerException);
    }

    private int CountByCorrelationId(Guid correlationId)
    {
        using var connection = _fixture.OpenVerifiedConnection();
        using var command = new SqlCommand(
            "SELECT COUNT(*) FROM dbo.Security_Audit_Events WHERE correlation_id = @cid;",
            connection);
        command.Parameters.AddWithValue("@cid", correlationId);
        return (int)command.ExecuteScalar()!;
    }
}
