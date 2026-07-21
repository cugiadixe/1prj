using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using PTKD.Application.Security.Audit;

namespace PTKD.Infrastructure.Security.Audit;

// Direct parameterized INSERT into Security_Audit_Events (OD-F-03).
// Does not use EF tracked-entity flow. created_at is DB-generated (SYSUTCDATETIME default).
// Fail-closed (OD-F-04): exceptions from the INSERT propagate to the caller.
public sealed class SqlSecurityAuditWriter : IAuditWriter
{
    private readonly string _connectionString;

    public SqlSecurityAuditWriter(IConfiguration configuration)
        : this(configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection connection string is not configured."))
    {
    }

    public SqlSecurityAuditWriter(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string must not be empty.", nameof(connectionString));
        _connectionString = connectionString;
    }

    public async Task WriteAsync(SecurityAuditEventRecord record, CancellationToken cancellationToken = default)
    {
        // Sensitive data validation (SEC-005) — propagates as ArgumentException; not a DB failure.
        record.ThrowIfContainsSensitiveData();

        const string sql = """
            INSERT INTO dbo.Security_Audit_Events
                (actor_user_id, acting_as_user_id, target_user_id, company_id,
                 event_code, entity_type, entity_id,
                 changed_fields, before_state_json, after_state_json,
                 reason, correlation_id, request_metadata, outcome, policy_version)
            VALUES
                (@actor_user_id, @acting_as_user_id, @target_user_id, @company_id,
                 @event_code, @entity_type, @entity_id,
                 @changed_fields, @before_state_json, @after_state_json,
                 @reason, @correlation_id, @request_metadata, @outcome, @policy_version);
            """;

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@actor_user_id", (object?)record.ActorUserId ?? DBNull.Value);
            command.Parameters.AddWithValue("@acting_as_user_id", (object?)record.ActingAsUserId ?? DBNull.Value);
            command.Parameters.AddWithValue("@target_user_id", (object?)record.TargetUserId ?? DBNull.Value);
            command.Parameters.AddWithValue("@company_id", (object?)record.CompanyId ?? DBNull.Value);
            command.Parameters.AddWithValue("@event_code", record.EventCode);
            command.Parameters.AddWithValue("@entity_type", record.EntityType);
            command.Parameters.AddWithValue("@entity_id", (object?)record.EntityId ?? DBNull.Value);
            command.Parameters.AddWithValue("@changed_fields", (object?)record.ChangedFieldsJson ?? DBNull.Value);
            command.Parameters.AddWithValue("@before_state_json", (object?)record.BeforeStateJson ?? DBNull.Value);
            command.Parameters.AddWithValue("@after_state_json", (object?)record.AfterStateJson ?? DBNull.Value);
            command.Parameters.AddWithValue("@reason", (object?)record.Reason ?? DBNull.Value);
            command.Parameters.AddWithValue("@correlation_id", record.CorrelationId);
            command.Parameters.AddWithValue("@request_metadata", (object?)record.RequestMetadataJson ?? DBNull.Value);
            command.Parameters.AddWithValue("@outcome", record.Outcome);
            command.Parameters.AddWithValue("@policy_version", (object?)record.PolicyVersion ?? DBNull.Value);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Cancellation is not an audit write failure — propagate as-is.
            throw;
        }
        catch (Exception ex)
        {
            // Wrap any DB/connection failure in a sanitized typed exception (OD-F-04).
            // The original exception is preserved in InnerException for log sinks only;
            // it must never be surfaced in an API response.
            throw new SecurityAuditWriteException(ex);
        }
    }
}
