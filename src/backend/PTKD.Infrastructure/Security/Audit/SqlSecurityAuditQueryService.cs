using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using PTKD.Application.Common.Exceptions;
using PTKD.Application.Common.Models;
using PTKD.Application.Security.Audit;
using PTKD.Application.Security.Audit.DTOs;

namespace PTKD.Infrastructure.Security.Audit;

public sealed class SqlSecurityAuditQueryService : ISecurityAuditQueryService
{
    private readonly string _connectionString;

    public SqlSecurityAuditQueryService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("DefaultConnection is missing.");
    }

    public async Task<PagedResult<SecurityAuditEventDto>> GetAuditEventsAsync(SecurityAuditQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        ValidateParameters(parameters);

        var items = new List<SecurityAuditEventDto>();
        long totalCount = 0;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // 1. Count query
        var countSql = new StringBuilder("SELECT COUNT_BIG(1) FROM dbo.Security_Audit_Events WHERE 1=1");
        
        await using var countCommand = new SqlCommand();
        countCommand.Connection = connection;
        AppendFilters(countCommand, countSql, parameters);
        countCommand.CommandText = countSql.ToString();

        totalCount = (long)(await countCommand.ExecuteScalarAsync(cancellationToken) ?? 0L);

        // 2. Data query
        var dataSql = new StringBuilder("""
            SELECT id, actor_user_id, acting_as_user_id, target_user_id, company_id,
                   event_code, entity_type, entity_id, reason, correlation_id,
                   outcome, policy_version, created_at
            FROM dbo.Security_Audit_Events
            WHERE 1=1
            """);

        await using var dataCommand = new SqlCommand();
        dataCommand.Connection = connection;
        AppendFilters(dataCommand, dataSql, parameters);
        
        // Pagination logic (Deterministic sort: created_at DESC, id DESC)
        dataSql.AppendLine(" ORDER BY created_at DESC, id DESC");
        dataSql.AppendLine(" OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY");
        
        dataCommand.Parameters.AddWithValue("@Offset", (parameters.Page - 1) * parameters.PageSize);
        dataCommand.Parameters.AddWithValue("@PageSize", parameters.PageSize);
        dataCommand.CommandText = dataSql.ToString();

        await using var reader = await dataCommand.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new SecurityAuditEventDto
            {
                Id = reader.GetInt64(0),
                ActorUserId = reader.IsDBNull(1) ? null : reader.GetInt64(1),
                ActingAsUserId = reader.IsDBNull(2) ? null : reader.GetInt64(2),
                TargetUserId = reader.IsDBNull(3) ? null : reader.GetInt64(3),
                CompanyId = reader.IsDBNull(4) ? null : reader.GetInt64(4),
                EventCode = reader.GetString(5),
                EntityType = reader.GetString(6),
                EntityId = reader.IsDBNull(7) ? null : reader.GetString(7),
                Reason = reader.IsDBNull(8) ? null : reader.GetString(8),
                CorrelationId = reader.GetGuid(9),
                Outcome = reader.GetString(10),
                PolicyVersion = reader.IsDBNull(11) ? null : reader.GetInt64(11),
                CreatedAt = reader.GetDateTime(12)
            });
        }

        return new PagedResult<SecurityAuditEventDto>
        {
            Page = parameters.Page,
            PageSize = parameters.PageSize,
            TotalCount = totalCount,
            Items = items
        };
    }

    private static void AppendFilters(SqlCommand command, StringBuilder sql, SecurityAuditQueryParameters parameters)
    {
        if (parameters.FromUtc.HasValue)
        {
            sql.Append(" AND created_at >= @FromUtc");
            command.Parameters.AddWithValue("@FromUtc", parameters.FromUtc.Value);
        }
        if (parameters.ToUtc.HasValue)
        {
            sql.Append(" AND created_at <= @ToUtc");
            command.Parameters.AddWithValue("@ToUtc", parameters.ToUtc.Value);
        }
        if (!string.IsNullOrWhiteSpace(parameters.EventType))
        {
            sql.Append(" AND event_code = @EventType");
            command.Parameters.AddWithValue("@EventType", parameters.EventType);
        }
        if (parameters.ActorUserId.HasValue)
        {
            sql.Append(" AND actor_user_id = @ActorUserId");
            command.Parameters.AddWithValue("@ActorUserId", parameters.ActorUserId.Value);
        }
        if (parameters.TargetUserId.HasValue)
        {
            sql.Append(" AND target_user_id = @TargetUserId");
            command.Parameters.AddWithValue("@TargetUserId", parameters.TargetUserId.Value);
        }
        if (!string.IsNullOrWhiteSpace(parameters.EntityType))
        {
            sql.Append(" AND entity_type = @EntityType");
            command.Parameters.AddWithValue("@EntityType", parameters.EntityType);
        }
        if (!string.IsNullOrWhiteSpace(parameters.EntityId))
        {
            sql.Append(" AND entity_id = @EntityId");
            command.Parameters.AddWithValue("@EntityId", parameters.EntityId);
        }
        if (parameters.CorrelationId.HasValue)
        {
            sql.Append(" AND correlation_id = @CorrelationId");
            command.Parameters.AddWithValue("@CorrelationId", parameters.CorrelationId.Value);
        }
    }

    private static void ValidateParameters(SecurityAuditQueryParameters parameters)
    {
        if (parameters.Page < 1)
            throw new BusinessRuleValidationException("INVALID_PAGE", "Page must be greater than or equal to 1.");
        if (parameters.PageSize < 1)
            throw new BusinessRuleValidationException("INVALID_PAGE_SIZE", "PageSize must be greater than or equal to 1.");
        if (parameters.PageSize > 1000)
            throw new BusinessRuleValidationException("PAGE_SIZE_EXCEEDED", "PageSize cannot exceed 1000.");

        if (parameters.FromUtc.HasValue && parameters.ToUtc.HasValue && parameters.FromUtc > parameters.ToUtc)
            throw new BusinessRuleValidationException("INVALID_DATE_RANGE", "FromUtc must be less than or equal to ToUtc.");
    }
}
