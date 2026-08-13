using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Common.Exceptions;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Customers.DTOs;
using PTKD.Application.Security.Audit;
using PTKD.Domain.Entities;
using PTKD.Domain.ValueObjects;

namespace PTKD.Application.Customers.Services;

public class CustomerService : ICustomerService
{
    private readonly IOrganizationDbContextFactory _dbContextFactory;
    private readonly ITransactionalAuditWriter _auditWriter;

    public CustomerService(IOrganizationDbContextFactory dbContextFactory, ITransactionalAuditWriter auditWriter)
    {
        _dbContextFactory = dbContextFactory;
        _auditWriter = auditWriter;
    }

    public async Task<CustomerDetailDto> CreateCustomerAsync(CreateCustomerRequest request, long actorUserId, CancellationToken ct = default)
    {
        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            if (await context.Customers.AnyAsync(c => c.CustomerCode == request.CustomerCode, ct))
                throw new BusinessRuleValidationException("CUS_DUPLICATE_CUSTOMER_CODE", "Customer code already exists.");

            if (!string.IsNullOrWhiteSpace(request.Cccd))
            {
                var hasDuplicate = await context.Profiles
                    .AnyAsync(p => p.Cccd == request.Cccd && p.IsActive, ct);
                if (hasDuplicate)
                    throw new BusinessRuleValidationException("CUS_DUPLICATE_CCCD", "An active customer with this CCCD already exists.");
            }

            var profile = new Profile(
                request.FullName, request.Cccd, request.Dob, request.DobPartial, request.DobPrecision,
                request.Gender, request.PermanentAddress, request.CccdIssueDate, request.CccdIssuePlace,
                request.TaxCode, request.Phone, request.ContactAddress,
                request.DeathDateSolar, request.DeathDateLunar, request.DeathPlace, request.Hometown);
            profile.SetCreatedBy(actorUserId);
            context.Profiles.Add(profile);
            await context.SaveChangesAsync(ct);

            var customer = new Customer(request.CustomerCode, profile.Id);
            customer.SetCreatedBy(actorUserId);
            context.Customers.Add(customer);
            await context.SaveChangesAsync(ct);

            if (request.InitialCompanyId.HasValue)
            {
                if (!await context.Companies.AnyAsync(c => c.Id == request.InitialCompanyId.Value && c.IsActive, ct))
                    throw new EntityNotFoundException("CUS_COMPANY_NOT_FOUND", "Company not found or inactive.");

                var companyContext = new CustomerCompanyContext(
                    customer.Id, request.InitialCompanyId.Value, request.AssignedStaffId,
                    request.InternalNotes, DateTime.UtcNow);
                companyContext.SetCreatedBy(actorUserId);
                context.CustomerCompanyContexts.Add(companyContext);
                await context.SaveChangesAsync(ct);
            }

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "CUSTOMER_CREATE",
                EntityType = "Customer",
                EntityId = customer.Id.ToString(),
                Outcome = "SUCCESS",
                CorrelationId = Guid.NewGuid(),
                ActorUserId = actorUserId,
                AfterStateJson = JsonSerializer.Serialize(new { customer.CustomerCode, profile.FullName, profile.Cccd, request.InitialCompanyId })
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);

            return MapToDetailDto(customer, profile, false);
        });
    }

    public async Task<CustomerDetailDto> UpdateCustomerAsync(long id, UpdateCustomerRequest request, long actorUserId, CancellationToken ct = default)
    {
        var rowVersion = RowVersion.FromBase64(request.TargetVersion).Value;

        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var customer = await context.Customers
                .Include(c => c.Profile)
                .FirstOrDefaultAsync(c => c.Id == id, ct);

            if (customer == null)
                throw new EntityNotFoundException("CUS_CUSTOMER_NOT_FOUND", "Customer not found.");

            if (!customer.RowVersion.SequenceEqual(rowVersion))
                throw new ConcurrencyException("CUS_INVALID_ROW_VERSION", "The customer has been modified by another process.");

            if (!string.IsNullOrWhiteSpace(request.Cccd) && request.Cccd != customer.Profile.Cccd)
            {
                var hasDuplicate = await context.Profiles
                    .AnyAsync(p => p.Cccd == request.Cccd && p.IsActive && p.Id != customer.ProfileId, ct);
                if (hasDuplicate)
                    throw new BusinessRuleValidationException("CUS_DUPLICATE_CCCD", "An active customer with this CCCD already exists.");
            }

            var beforeState = JsonSerializer.Serialize(new
            {
                customer.Profile.FullName, customer.Profile.Cccd, customer.Profile.Phone,
                customer.Profile.PermanentAddress, customer.Profile.ContactAddress
            });

            customer.Profile.Update(
                request.FullName, request.Cccd, request.Dob, request.DobPartial, request.DobPrecision,
                request.Gender, request.PermanentAddress, request.CccdIssueDate, request.CccdIssuePlace,
                request.TaxCode, request.Phone, request.ContactAddress,
                request.DeathDateSolar, request.DeathDateLunar, request.DeathPlace, request.Hometown,
                actorUserId);

            customer.MarkUpdated(actorUserId);

            var afterState = JsonSerializer.Serialize(new
            {
                request.FullName, request.Cccd, request.Phone,
                request.PermanentAddress, request.ContactAddress
            });

            try
            {
                await context.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConcurrencyException("CUS_INVALID_ROW_VERSION", "The customer has been modified by another process.");
            }

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "CUSTOMER_UPDATE",
                EntityType = "Customer",
                EntityId = customer.Id.ToString(),
                Outcome = "SUCCESS",
                CorrelationId = Guid.NewGuid(),
                ActorUserId = actorUserId,
                Reason = request.Reason,
                BeforeStateJson = beforeState,
                AfterStateJson = afterState
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);

            return MapToDetailDto(customer, customer.Profile, false);
        });
    }

    public async Task<CustomerDetailDto?> GetCustomerByIdAsync(long id, bool canViewSensitive, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        var customer = await context.Customers
            .AsNoTracking()
            .Include(c => c.Profile)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (customer == null) return null;

        var dto = MapToDetailDto(customer, customer.Profile, !canViewSensitive);
        dto.Tags = await context.CustomerTags.AsNoTracking()
            .Where(x => x.CustomerId == id)
            .OrderBy(x => x.Tag!.Name)
            .Select(x => new PTKD.Application.Tags.DTOs.TagDto
            {
                Id = x.Tag!.Id, TagType = x.Tag.TagType, Name = x.Tag.Name,
                Color = x.Tag.Color, IsActive = x.Tag.IsActive
            })
            .ToArrayAsync(ct);
        return dto;
    }

    public async Task<PagedResult<CustomerListItemDto>> SearchCustomersAsync(CustomerSearchRequest request, bool canViewSensitive, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        var query = context.Customers
            .AsNoTracking()
            .Include(c => c.Profile)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.CustomerStatus))
            query = query.Where(c => c.CustomerStatus == request.CustomerStatus);

        var hasSearch = !string.IsNullOrWhiteSpace(request.Search);
        if (hasSearch)
        {
            // Nhận diện ý định để TRÁNH LIKE '%x%' 4 cột (full scan ~2s trên 300K KH).
            //  • Mã KH (chữ + số, vd KH0098943) → prefix trên customer_code → SEEK index, ~tức thì.
            //  • Toàn số → SĐT/CCCD (contains, 1 cột).
            //  • Chữ → họ tên (contains, 1 cột).
            var term = request.Search!.Trim();
            var allDigits = term.Length > 0 && term.All(char.IsDigit);
            var looksLikeCode = !allDigits && term.Length >= 2 && char.IsLetter(term[0]) && term.Any(char.IsDigit);

            if (looksLikeCode)
            {
                // LIKE 'KH0098943%' → SEEK trên IX_Customers_customer_code_search (dùng EF.Functions.Like
                // để chắc chắn ra dạng prefix sargable, escape wildcard bằng [].)
                var prefix = LikePrefix(term);
                query = query.Where(c => EF.Functions.Like(c.CustomerCode, prefix));
            }
            else if (allDigits)
                query = query.Where(c =>
                    (c.Profile.Phone != null && c.Profile.Phone.Contains(term)) ||
                    (c.Profile.Cccd != null && c.Profile.Cccd.Contains(term)));
            else
                query = query.Where(c => c.Profile.FullName.Contains(term));
        }

        var hasContextFilter = request.CompanyId.HasValue || request.AssignedStaffId.HasValue || request.UnassignedStaff == true;
        if (hasContextFilter)
        {
            query = query.Where(c => context.CustomerCompanyContexts.Any(ctx =>
                ctx.CustomerId == c.Id
                && (request.CompanyId == null || ctx.CompanyId == request.CompanyId)
                && (request.AssignedStaffId == null || ctx.AssignedStaffId == request.AssignedStaffId)
                && (request.UnassignedStaff != true || ctx.AssignedStaffId == null)));
        }

        var hasTagFilter = request.TagIds != null && request.TagIds.Length > 0;
        if (hasTagFilter)
        {
            var tagIds = request.TagIds!;
            query = query.Where(c => context.CustomerTags.Any(x => x.CustomerId == c.Id && tagIds.Contains(x.TagId)));
        }

        var mask = !canViewSensitive;

        var projectedQuery = query
            .OrderBy(c => c.Id)
            .Select(c => new CustomerListItemDto
            {
                Id = c.Id,
                CustomerCode = c.CustomerCode,
                FullName = c.Profile.FullName,
                Cccd = mask ? MaskCccd(c.Profile.Cccd) : c.Profile.Cccd,
                Phone = mask ? MaskPhone(c.Profile.Phone) : c.Profile.Phone,
                CustomerStatus = c.CustomerStatus,
                CreatedAt = c.CreatedAt,
                Tags = context.CustomerTags
                    .Where(x => x.CustomerId == c.Id)
                    .OrderBy(x => x.Tag!.Name)
                    .Select(x => new PTKD.Application.Tags.DTOs.TagDto
                    {
                        Id = x.Tag!.Id, TagType = x.Tag.TagType, Name = x.Tag.Name,
                        Color = x.Tag.Color, IsActive = x.Tag.IsActive
                    }).ToArray()
            });

        var anyFilter = hasSearch || hasContextFilter || hasTagFilter || !string.IsNullOrWhiteSpace(request.CustomerStatus);

        int totalCount;
        CustomerListItemDto[] items;

        if (!anyFilter)
        {
            totalCount = await context.Customers.CountAsync(ct);
            items = await projectedQuery
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToArrayAsync(ct);
        }
        else
        {
            totalCount = await query.CountAsync(ct);
            items = await projectedQuery
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToArrayAsync(ct);
        }

        return new PagedResult<CustomerListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<CompanyLookupDto[]> GetAssignedCompanyLookupsAsync(CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        var companyIds = await context.CustomerCompanyContexts
            .AsNoTracking()
            .Select(c => c.CompanyId)
            .Distinct()
            .ToListAsync(ct);
        if (companyIds.Count == 0) return Array.Empty<CompanyLookupDto>();

        return await context.Companies
            .AsNoTracking()
            .Where(co => companyIds.Contains(co.Id))
            .OrderBy(co => co.Name)
            .Select(co => new CompanyLookupDto { Id = co.Id, Name = co.Name })
            .ToArrayAsync(ct);
    }

    public async Task<StaffLookupDto[]> GetAssignedStaffLookupsAsync(CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        var staffIds = await context.CustomerCompanyContexts
            .AsNoTracking()
            .Where(c => c.AssignedStaffId != null)
            .Select(c => c.AssignedStaffId!.Value)
            .Distinct()
            .ToListAsync(ct);
        if (staffIds.Count == 0) return Array.Empty<StaffLookupDto>();

        return await context.Users
            .AsNoTracking()
            .Where(u => staffIds.Contains(u.Id))
            .OrderBy(u => u.FullName)
            .Select(u => new StaffLookupDto { Id = u.Id, FullName = u.FullName })
            .ToArrayAsync(ct);
    }

    public async Task<DuplicateCheckResult> CheckDuplicatesAsync(DuplicateCheckRequest request, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();

        var query = context.Customers
            .AsNoTracking()
            .Include(c => c.Profile)
            .Where(c => c.Profile.IsActive);

        if (!string.IsNullOrWhiteSpace(request.Cccd))
            query = query.Where(c => c.Profile.Cccd == request.Cccd);
        else if (!string.IsNullOrWhiteSpace(request.Phone))
            query = query.Where(c => c.Profile.Phone == request.Phone);
        else
            return new DuplicateCheckResult();

        var matches = await query
            .Select(c => new CustomerListItemDto
            {
                Id = c.Id,
                CustomerCode = c.CustomerCode,
                FullName = c.Profile.FullName,
                Cccd = MaskCccd(c.Profile.Cccd),
                Phone = MaskPhone(c.Profile.Phone),
                CustomerStatus = c.CustomerStatus,
                CreatedAt = c.CreatedAt
            })
            .ToArrayAsync(ct);

        return new DuplicateCheckResult { HasDuplicates = matches.Length > 0, Matches = matches };
    }

    public async Task<CustomerCompanyContextDto[]> GetCompanyContextsAsync(long customerId, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        var contexts = await context.CustomerCompanyContexts
            .AsNoTracking()
            .Where(c => c.CustomerId == customerId)
            .OrderBy(c => c.CompanyId)
            .ToListAsync(ct);
        if (contexts.Count == 0) return System.Array.Empty<CustomerCompanyContextDto>();

        var companyIds = contexts.Select(c => c.CompanyId).Distinct().ToList();
        var companyNames = await context.Companies.AsNoTracking()
            .Where(co => companyIds.Contains(co.Id))
            .Select(co => new { co.Id, co.Name })
            .ToDictionaryAsync(x => x.Id, x => x.Name, ct);

        var staffIds = contexts.Where(c => c.AssignedStaffId.HasValue)
            .Select(c => c.AssignedStaffId!.Value).Distinct().ToList();
        var staffNames = staffIds.Count == 0
            ? new Dictionary<long, string>()
            : await context.Users.AsNoTracking()
                .Where(u => staffIds.Contains(u.Id))
                .Select(u => new { u.Id, u.FullName })
                .ToDictionaryAsync(x => x.Id, x => x.FullName, ct);

        return contexts.Select(c =>
        {
            var dto = MapToContextDto(c);
            dto.CompanyName = companyNames.TryGetValue(c.CompanyId, out var cn) ? cn : null;
            dto.AssignedStaffName = c.AssignedStaffId.HasValue && staffNames.TryGetValue(c.AssignedStaffId.Value, out var sn) ? sn : null;
            return dto;
        }).ToArray();
    }

    public async Task<CustomerCompanyContextDto> CreateCompanyContextAsync(long customerId, CreateCustomerCompanyContextRequest request, long actorUserId, CancellationToken ct = default)
    {
        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            if (!await context.Customers.AnyAsync(c => c.Id == customerId, ct))
                throw new EntityNotFoundException("CUS_CUSTOMER_NOT_FOUND", "Customer not found.");

            if (!await context.Companies.AnyAsync(c => c.Id == request.CompanyId && c.IsActive, ct))
                throw new EntityNotFoundException("CUS_COMPANY_NOT_FOUND", "Company not found or inactive.");

            if (await context.CustomerCompanyContexts.AnyAsync(c => c.CustomerId == customerId && c.CompanyId == request.CompanyId, ct))
                throw new BusinessRuleValidationException("CUS_DUPLICATE_COMPANY_CONTEXT", "Customer already has a context for this company.");

            var companyContext = new CustomerCompanyContext(
                customerId, request.CompanyId, request.AssignedStaffId,
                request.InternalNotes, request.FirstInteractionAt);
            companyContext.SetCreatedBy(actorUserId);
            context.CustomerCompanyContexts.Add(companyContext);
            await context.SaveChangesAsync(ct);

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "CONTEXT_CREATE",
                EntityType = "CustomerCompanyContext",
                EntityId = companyContext.Id.ToString(),
                CompanyId = request.CompanyId,
                Outcome = "SUCCESS",
                CorrelationId = Guid.NewGuid(),
                ActorUserId = actorUserId,
                AfterStateJson = JsonSerializer.Serialize(new { customerId, request.CompanyId, request.AssignedStaffId })
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);

            return MapToContextDto(companyContext);
        });
    }

    public async Task<CustomerCompanyContextDto> UpdateCompanyContextAsync(long customerId, long contextId, UpdateCustomerCompanyContextRequest request, long actorUserId, CancellationToken ct = default)
    {
        var rowVersion = RowVersion.FromBase64(request.TargetVersion).Value;

        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var companyContext = await context.CustomerCompanyContexts
                .FirstOrDefaultAsync(c => c.Id == contextId && c.CustomerId == customerId, ct);

            if (companyContext == null)
                throw new EntityNotFoundException("CUS_CONTEXT_NOT_FOUND", "Company context not found.");

            if (!companyContext.RowVersion.SequenceEqual(rowVersion))
                throw new ConcurrencyException("CUS_INVALID_ROW_VERSION", "The company context has been modified by another process.");

            companyContext.Update(request.AssignedStaffId, request.RelationshipStatus,
                request.InternalNotes, request.LastInteractionAt, actorUserId);

            try
            {
                await context.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConcurrencyException("CUS_INVALID_ROW_VERSION", "The company context has been modified by another process.");
            }

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "CONTEXT_UPDATE",
                EntityType = "CustomerCompanyContext",
                EntityId = companyContext.Id.ToString(),
                CompanyId = companyContext.CompanyId,
                Outcome = "SUCCESS",
                CorrelationId = Guid.NewGuid(),
                ActorUserId = actorUserId,
                AfterStateJson = JsonSerializer.Serialize(new { request.RelationshipStatus, request.AssignedStaffId })
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);

            return MapToContextDto(companyContext);
        });
    }

    private static CustomerDetailDto MapToDetailDto(Customer customer, Profile profile, bool maskSensitive)
    {
        return new CustomerDetailDto
        {
            Id = customer.Id,
            CustomerCode = customer.CustomerCode,
            CustomerStatus = customer.CustomerStatus,
            RowVersion = Convert.ToBase64String(customer.RowVersion ?? Array.Empty<byte>()),
            CreatedAt = customer.CreatedAt,
            UpdatedAt = customer.UpdatedAt,
            Profile = new ProfileDto
            {
                Id = profile.Id,
                FullName = profile.FullName,
                Cccd = maskSensitive ? MaskCccd(profile.Cccd) : profile.Cccd,
                Dob = profile.Dob,
                DobPartial = profile.DobPartial,
                DobPrecision = profile.DobPrecision,
                Gender = profile.Gender,
                PermanentAddress = maskSensitive ? MaskAddress(profile.PermanentAddress) : profile.PermanentAddress,
                CccdIssueDate = profile.CccdIssueDate,
                CccdIssuePlace = profile.CccdIssuePlace,
                TaxCode = profile.TaxCode,
                Phone = maskSensitive ? MaskPhone(profile.Phone) : profile.Phone,
                ContactAddress = maskSensitive ? MaskAddress(profile.ContactAddress) : profile.ContactAddress,
                DeathDateSolar = profile.DeathDateSolar,
                DeathDateLunar = profile.DeathDateLunar,
                DeathPlace = profile.DeathPlace,
                Hometown = profile.Hometown,
                IsActive = profile.IsActive,
                RowVersion = Convert.ToBase64String(profile.RowVersion ?? Array.Empty<byte>())
            }
        };
    }

    private static CustomerCompanyContextDto MapToContextDto(CustomerCompanyContext ctx)
    {
        return new CustomerCompanyContextDto
        {
            Id = ctx.Id,
            CustomerId = ctx.CustomerId,
            CompanyId = ctx.CompanyId,
            AssignedStaffId = ctx.AssignedStaffId,
            RelationshipStatus = ctx.RelationshipStatus,
            InternalNotes = ctx.InternalNotes,
            FirstInteractionAt = ctx.FirstInteractionAt,
            LastInteractionAt = ctx.LastInteractionAt,
            RowVersion = Convert.ToBase64String(ctx.RowVersion ?? Array.Empty<byte>()),
            CreatedAt = ctx.CreatedAt,
            UpdatedAt = ctx.UpdatedAt
        };
    }

    private static string? MaskCccd(string? cccd)
    {
        if (string.IsNullOrEmpty(cccd) || cccd.Length <= 4) return cccd == null ? null : "****";
        return new string('*', cccd.Length - 4) + cccd[^4..];
    }

    private static string? MaskPhone(string? phone)
    {
        if (string.IsNullOrEmpty(phone) || phone.Length <= 3) return phone == null ? null : "***";
        return new string('*', phone.Length - 3) + phone[^3..];
    }

    private static string? MaskAddress(string? address)
    {
        if (string.IsNullOrEmpty(address)) return address;
        return "***";
    }

    // Tạo pattern LIKE 'term%' an toàn: escape %, _, [ bằng cú pháp [] (không cần ESCAPE clause).
    private static string LikePrefix(string term)
        => term.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]") + "%";
}
