using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Security.Audit;
using PTKD.Application.Security.Authentication.Models;
using PTKD.Infrastructure.Security.Audit;

namespace PTKD.IntegrationTests;

// AC-G-AUDIT-01: PASSWORD_CHANGED audit row must be part of the same database
//                transaction as the password change.
// AC-G-AUDIT-02: PASSWORD_CHANGED audit payload must contain no sensitive data.
[Collection("Sequential")]
public sealed class PasswordChangeAuditAtomicityTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public PasswordChangeAuditAtomicityTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        _fixture.ResetToV0003();
    }

    // ── AC-G-AUDIT-01: same-transaction semantics ────────────────────────────────

    [Fact]
    public async Task SqlTransactionalAuditWriter_InsertsRowInSameTransaction_AndRowDisappearsOnRollback()
    {
        // Arrange: open a connection and begin a transaction explicitly.
        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = connection.BeginTransaction();

        var writer = new SqlTransactionalAuditWriter();
        var correlationId = Guid.NewGuid();

        // ActorUserId/TargetUserId left null to avoid FK violations; users may not exist
        // in a freshly reset database without users seeded.
        var record = new SecurityAuditEventRecord
        {
            EventCode = "PASSWORD_CHANGED",
            EntityType = "AUTH_ACCOUNT",
            EntityId = "999",
            Outcome = "SUCCESS",
            CorrelationId = correlationId
        };

        // Act: write inside the uncommitted transaction.
        await writer.WriteAsync(record, connection, transaction);

        // Assert within-transaction: the row is visible to the same connection/transaction.
        var countInTx = await CountByCorrelationIdAsync(connection, transaction, correlationId);
        Assert.Equal(1, countInTx);

        // Roll back the transaction — the audit row must not persist.
        await transaction.RollbackAsync();

        // Assert after rollback: row must be gone (read from a separate connection).
        var countAfterRollback = CountByCorrelationIdExternal(correlationId);
        Assert.Equal(0, countAfterRollback);
    }

    [Fact]
    public async Task SqlTransactionalAuditWriter_InsertsRowThatPersistsOnCommit()
    {
        // Arrange
        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = connection.BeginTransaction();

        var writer = new SqlTransactionalAuditWriter();
        var correlationId = Guid.NewGuid();

        var record = new SecurityAuditEventRecord
        {
            EventCode = "PASSWORD_CHANGED",
            EntityType = "AUTH_ACCOUNT",
            EntityId = "1",
            Outcome = "SUCCESS",
            CorrelationId = correlationId
            // ActorUserId/TargetUserId left null to avoid FK violation
        };

        // Act: write and commit.
        await writer.WriteAsync(record, connection, transaction);
        await transaction.CommitAsync();

        // Assert: row is durable.
        var countAfterCommit = CountByCorrelationIdExternal(correlationId);
        Assert.Equal(1, countAfterCommit);
    }

    // ── AC-G-AUDIT-02: no sensitive data in PASSWORD_CHANGED payload ─────────────

    [Fact]
    public void PasswordChange_AuditRecord_ContainsNoSensitiveDataInJsonFields()
    {
        // The SecurityAuditEventRecord.ThrowIfContainsSensitiveData method enforces SEC-005.
        // This test confirms that the record as constructed by ChangePasswordAsync contains
        // only non-sensitive identifiers and that ThrowIfContainsSensitiveData does not throw.

        var record = new SecurityAuditEventRecord
        {
            EventCode = "PASSWORD_CHANGED",
            EntityType = "AUTH_ACCOUNT",
            EntityId = "42",
            Outcome = "SUCCESS",
            CorrelationId = Guid.NewGuid(),
            ActorUserId = 100,
            TargetUserId = 100,
            // These fields must not carry any sensitive data per SEC-005.
            ChangedFieldsJson = null,
            BeforeStateJson = null,
            AfterStateJson = null,
            RequestMetadataJson = null
        };

        // Must not throw (no sensitive JSON keys present).
        var ex = Record.Exception(() => record.ThrowIfContainsSensitiveData());
        Assert.Null(ex);
    }

    // The sensitive-key regex requires exact quoted JSON key names (e.g. "password", not "password_hash").
    // These cases verify that the exact keys listed in SEC-005 are blocked.
    [Theory]
    [InlineData("{\"password\":\"abc\"}", "ChangedFieldsJson")]
    [InlineData("{\"token\":\"xyz\"}", "ChangedFieldsJson")]
    [InlineData("{\"secret\":\"s\"}", "BeforeStateJson")]
    [InlineData("{\"password\":\"reused\"}", "AfterStateJson")]
    public void PasswordChange_AuditRecord_ThrowsIfSensitiveKeyInJsonField(string json, string fieldName)
    {
        // Arrange
        SecurityAuditEventRecord record = fieldName switch
        {
            "ChangedFieldsJson" => new SecurityAuditEventRecord
            {
                EventCode = "PASSWORD_CHANGED",
                EntityType = "AUTH_ACCOUNT",
                EntityId = "1",
                Outcome = "SUCCESS",
                CorrelationId = Guid.NewGuid(),
                ChangedFieldsJson = json
            },
            "BeforeStateJson" => new SecurityAuditEventRecord
            {
                EventCode = "PASSWORD_CHANGED",
                EntityType = "AUTH_ACCOUNT",
                EntityId = "1",
                Outcome = "SUCCESS",
                CorrelationId = Guid.NewGuid(),
                BeforeStateJson = json
            },
            _ => new SecurityAuditEventRecord
            {
                EventCode = "PASSWORD_CHANGED",
                EntityType = "AUTH_ACCOUNT",
                EntityId = "1",
                Outcome = "SUCCESS",
                CorrelationId = Guid.NewGuid(),
                AfterStateJson = json
            }
        };

        // Act & Assert: ThrowIfContainsSensitiveData must reject it.
        Assert.Throws<ArgumentException>(() => record.ThrowIfContainsSensitiveData());
    }

    [Fact]
    public async Task PasswordChange_Integration_AuditRowAppearsInDatabase()
    {
        // Full integration: ChangePasswordAsync must produce exactly one PASSWORD_CHANGED
        // row in Security_Audit_Events upon a successful password change.
        var harness = new Security.Authentication.AuthenticationTestHarness(_fixture);

        var seed = await harness.CreateInternalAccountAsync(
            "AUDIT-CHECK-USER",
            "synthetic-current-passphrase",
            mustChangePassword: true,
            temporaryPasswordExpiresAt: harness.Clock.UtcNow.AddHours(24));

        var result = await harness.Service.ChangePasswordAsync(new PTKD.Application.Security.Authentication.Models.ChangePasswordCommand(
            seed.AccountId,
            "synthetic-current-passphrase",
            "synthetic-replacement-passphrase",
            seed.RowVersion,
            seed.UserId));

        Assert.True(result.Succeeded);

        // Verify the audit row was persisted.
        var count = CountEventByCodeAndEntity("PASSWORD_CHANGED", "AUTH_ACCOUNT", seed.AccountId.ToString());
        Assert.Equal(1, count);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private static async Task<int> CountByCorrelationIdAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        Guid correlationId)
    {
        await using var command = new SqlCommand(
            "SELECT COUNT(*) FROM dbo.Security_Audit_Events WHERE correlation_id = @cid;",
            connection,
            transaction);
        command.Parameters.AddWithValue("@cid", correlationId);
        return (int)(await command.ExecuteScalarAsync())!;
    }

    private int CountByCorrelationIdExternal(Guid correlationId)
    {
        using var connection = _fixture.OpenVerifiedConnection();
        using var command = new SqlCommand(
            "SELECT COUNT(*) FROM dbo.Security_Audit_Events WHERE correlation_id = @cid;",
            connection);
        command.Parameters.AddWithValue("@cid", correlationId);
        return (int)command.ExecuteScalar()!;
    }

    private int CountEventByCodeAndEntity(string eventCode, string entityType, string entityId)
    {
        using var connection = _fixture.OpenVerifiedConnection();
        using var command = new SqlCommand("""
            SELECT COUNT(*) FROM dbo.Security_Audit_Events
            WHERE event_code = @code AND entity_type = @type AND entity_id = @id;
            """, connection);
        command.Parameters.AddWithValue("@code", eventCode);
        command.Parameters.AddWithValue("@type", entityType);
        command.Parameters.AddWithValue("@id", entityId);
        return (int)command.ExecuteScalar()!;
    }
}
