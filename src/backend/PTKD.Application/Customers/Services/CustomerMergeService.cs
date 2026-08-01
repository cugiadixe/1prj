using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Customers.DTOs;
using PTKD.Domain.Entities;

namespace PTKD.Application.Customers.Services;

public class CustomerMergeService : ICustomerMergeService
{
    private readonly IOrganizationDbContextFactory _dbContextFactory;

    public CustomerMergeService(IOrganizationDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<CustomerMergeRequestDto> CreateMergeRequestAsync(CreateCustomerMergeRequestDto request, long actorUserId, CancellationToken ct = default)
    {
        await using var _dbContext = _dbContextFactory.CreateDbContext();
        if (request.SourceCustomerId == request.TargetCustomerId)
        {
            throw new InvalidOperationException("Source and target customer cannot be the same.");
        }

        var sourceCustomer = await _dbContext.Customers
            .Include(c => c.Profile)
            .FirstOrDefaultAsync(c => c.Id == request.SourceCustomerId, ct);

        var targetCustomer = await _dbContext.Customers
            .Include(c => c.Profile)
            .FirstOrDefaultAsync(c => c.Id == request.TargetCustomerId, ct);

        if (sourceCustomer == null || targetCustomer == null)
        {
            throw new InvalidOperationException("One or both customers not found.");
        }

        if (sourceCustomer.CustomerStatus == "MERGED" || targetCustomer.CustomerStatus == "MERGED")
        {
            throw new InvalidOperationException("Cannot merge a customer that is already merged.");
        }

        if (targetCustomer.CustomerStatus != "ACTIVE")
        {
            throw new InvalidOperationException("Target customer must be active.");
        }

        // Check for conflicting CustomerCompanyContext
        var sourceContexts = await _dbContext.CustomerCompanyContexts
            .Where(c => c.CustomerId == request.SourceCustomerId)
            .ToListAsync(ct);

        var targetContexts = await _dbContext.CustomerCompanyContexts
            .Where(c => c.CustomerId == request.TargetCustomerId)
            .ToListAsync(ct);

        var overlappingCompanyIds = sourceContexts.Select(s => s.CompanyId)
            .Intersect(targetContexts.Select(t => t.CompanyId))
            .ToList();

        if (overlappingCompanyIds.Any())
        {
            throw new InvalidOperationException("Cannot automatically merge overlapping company contexts. Manual resolution required.");
        }

        // Snapshots handling
        var sourceRowVersion = Convert.FromBase64String(request.SourceRowVersionSnapshot);
        var targetRowVersion = Convert.FromBase64String(request.TargetRowVersionSnapshot);

        var mergeRequest = new CustomerMergeRequest(
            request.SourceCustomerId,
            request.TargetCustomerId,
            actorUserId,
            request.SurvivorshipPayload,
            sourceRowVersion,
            targetRowVersion
        );

        foreach (var candidate in request.Candidates)
        {
            mergeRequest.AddCandidate(
                candidate.CandidateCustomerId,
                candidate.MatchType,
                candidate.MatchConfidence,
                candidate.SnapshotPayload
            );
        }

        _dbContext.CustomerMergeRequests.Add(mergeRequest);
        await _dbContext.SaveChangesAsync(ct);

        return await GetMergeRequestByIdAsync(mergeRequest.Id, ct) ?? throw new InvalidOperationException("Failed to load saved request");
    }

    public async Task<CustomerMergeRequestDto?> GetMergeRequestByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var _dbContext = _dbContextFactory.CreateDbContext();
        var request = await _dbContext.CustomerMergeRequests
            .Include(r => r.Candidates)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (request == null) return null;

        return new CustomerMergeRequestDto
        {
            Id = request.Id,
            SourceCustomerId = request.SourceCustomerId,
            TargetCustomerId = request.TargetCustomerId,
            RequesterId = request.RequesterId,
            RequestStatus = request.RequestStatus,
            SurvivorshipPayload = request.SurvivorshipPayload,
            SourceRowVersionSnapshot = Convert.ToBase64String(request.SourceRowVersionSnapshot),
            TargetRowVersionSnapshot = Convert.ToBase64String(request.TargetRowVersionSnapshot),
            WorkflowInstanceId = request.WorkflowInstanceId,
            CreatedAt = request.CreatedAt,
            UpdatedAt = request.UpdatedAt,
            RowVersion = Convert.ToBase64String(request.RowVersion),
            Candidates = request.Candidates.Select(c => new CustomerMergeCandidateDto
            {
                CandidateCustomerId = c.CandidateCustomerId,
                MatchType = c.MatchType,
                MatchConfidence = c.MatchConfidence,
                SnapshotPayload = c.SnapshotPayload
            }).ToList()
        };
    }

    public async Task<PagedResult<CustomerMergeRequestDto>> SearchMergeRequestsAsync(int page, int pageSize, CancellationToken ct = default)
    {
        await using var _dbContext = _dbContextFactory.CreateDbContext();
        var query = _dbContext.CustomerMergeRequests.AsNoTracking();

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(request => new CustomerMergeRequestDto
            {
                Id = request.Id,
                SourceCustomerId = request.SourceCustomerId,
                TargetCustomerId = request.TargetCustomerId,
                RequesterId = request.RequesterId,
                RequestStatus = request.RequestStatus,
                SurvivorshipPayload = request.SurvivorshipPayload,
                SourceRowVersionSnapshot = Convert.ToBase64String(request.SourceRowVersionSnapshot),
                TargetRowVersionSnapshot = Convert.ToBase64String(request.TargetRowVersionSnapshot),
                WorkflowInstanceId = request.WorkflowInstanceId,
                CreatedAt = request.CreatedAt,
                UpdatedAt = request.UpdatedAt,
                RowVersion = Convert.ToBase64String(request.RowVersion)
            })
            .ToListAsync(ct);

        return new PagedResult<CustomerMergeRequestDto>
        {
            Items = items.ToArray(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }
}
