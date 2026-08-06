using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using PTKD.Application.Security.Audit;

namespace PTKD.Infrastructure.Security.Audit;

// Transaction-aware INSERT into Security_Audit_Events.
// Uses the DbConnection and DbTransaction provided by the caller; does NOT open a new
// connection or begin a new transaction.  This guarantees that the audit row is part of
// the same database transaction as the protected operation (e.g. ChangePasswordAsync).
// If the INSERT fails the exception propagates, the caller's transaction will roll back,
// and the protected operation will not be durably committed (fail-closed, OD-F-04).
public sealed class SqlTransactionalAuditWriter : ITransactionalAuditWriter
{
    private const string Sql = """
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

    public async Task WriteAsync(
        SecurityAuditEventRecord record,
        DbConnection connection,
        DbTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(transaction);

        // Sensitive data validation (SEC-005).
        record.ThrowIfContainsSensitiveData();

        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = Sql;

        AddParam(command, "@actor_user_id", (object?)record.ActorUserId ?? DBNull.Value);
        AddParam(command, "@acting_as_user_id", (object?)record.ActingAsUserId ?? DBNull.Value);
        AddParam(command, "@target_user_id", (object?)record.TargetUserId ?? DBNull.Value);
        AddParam(command, "@company_id", (object?)record.CompanyId ?? DBNull.Value);
        AddParam(command, "@event_code", record.EventCode);
        AddParam(command, "@entity_type", record.EntityType);
        AddParam(command, "@entity_id", (object?)record.EntityId ?? DBNull.Value);
        AddParam(command, "@changed_fields", (object?)record.ChangedFieldsJson ?? DBNull.Value);
        AddParam(command, "@before_state_json", (object?)record.BeforeStateJson ?? DBNull.Value);
        AddParam(command, "@after_state_json", (object?)record.AfterStateJson ?? DBNull.Value);
        AddParam(command, "@reason", (object?)record.Reason ?? DBNull.Value);
        AddParam(command, "@correlation_id", record.CorrelationId);
        AddParam(command, "@request_metadata", (object?)record.RequestMetadataJson ?? DBNull.Value);
        AddParam(command, "@outcome", record.Outcome);
        AddParam(command, "@policy_version", (object?)record.PolicyVersion ?? DBNull.Value);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddParam(DbCommand command, string name, object value)
    {
        var p = command.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        command.Parameters.Add(p);
    }
}
