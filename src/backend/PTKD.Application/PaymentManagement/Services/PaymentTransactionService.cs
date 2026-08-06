using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Common.Models;
using PTKD.Application.PaymentManagement.DTOs;
using PTKD.Domain.Entities;

namespace PTKD.Application.PaymentManagement.Services;

public class PaymentTransactionService : IPaymentTransactionService
{
    private readonly IOrganizationDbContextFactory _dbContextFactory;

    public PaymentTransactionService(IOrganizationDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<PaymentTransactionDto> CreateDraftAsync(CreatePaymentDraftRequest request, long actorUserId, CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();

        var companyExists = await db.Companies.AnyAsync(c => c.Id == request.CompanyId, ct);
        if (!companyExists)
            throw new InvalidOperationException("Company not found.");

        var customerExists = await db.Customers.AnyAsync(c => c.Id == request.CustomerId, ct);
        if (!customerExists)
            throw new InvalidOperationException("Customer not found.");

        var contextExists = await db.CustomerCompanyContexts.AnyAsync(
            ccc => ccc.CustomerId == request.CustomerId && ccc.CompanyId == request.CompanyId, ct);
        if (!contextExists)
            throw new InvalidOperationException("Customer does not have a company context for the specified company.");

        if (request.Items == null || request.Items.Count == 0)
            throw new InvalidOperationException("Payment must contain at least one item.");

        var serviceIds = request.Items.Select(i => i.ServiceId).Distinct().ToArray();
        var services = await db.Services.AsNoTracking()
            .Where(s => serviceIds.Contains(s.Id))
            .ToArrayAsync(ct);

        foreach (var item in request.Items)
        {
            var service = services.FirstOrDefault(s => s.Id == item.ServiceId)
                ?? throw new InvalidOperationException($"Service {item.ServiceId} not found.");
            if (service.CompanyId != request.CompanyId)
                throw new InvalidOperationException($"Service {item.ServiceId} does not belong to the specified company.");
            if (service.CustomerId != request.CustomerId)
                throw new InvalidOperationException($"Service {item.ServiceId} does not belong to the specified customer.");
        }

        var duplicateServiceCycles = request.Items
            .GroupBy(i => new { i.ServiceId })
            .Where(g => g.Count() > 1)
            .ToList();
        if (duplicateServiceCycles.Count > 0)
            throw new InvalidOperationException("Duplicate service entries in payment items.");

        var billCode = await GenerateBillCodeAsync(db, request.CompanyId, request.PaymentDate, ct);

        var entity = PaymentTransaction.CreateDraft(
            billCode,
            request.CompanyId,
            request.CustomerId,
            request.PaymentMethod,
            request.PaymentDate,
            request.Notes,
            actorUserId);

        db.PaymentTransactions.Add(entity);
        await db.SaveChangesAsync(ct);

        var serviceTypeCodes = await db.ServiceTypes.AsNoTracking()
            .Where(st => services.Select(s => s.ServiceTypeId).Contains(st.Id))
            .ToDictionaryAsync(st => st.Id, st => st.Code, ct);

        decimal total = 0;
        foreach (var itemReq in request.Items)
        {
            var service = services.First(s => s.Id == itemReq.ServiceId);
            var typeCode = serviceTypeCodes.GetValueOrDefault(service.ServiceTypeId, "UNKNOWN");

            var item = new PaymentTransactionItem(
                entity.Id,
                itemReq.ServiceId,
                typeCode,
                service.CycleNumber,
                itemReq.Amount,
                itemReq.Description);

            db.PaymentTransactionItems.Add(item);
            total += itemReq.Amount;
        }

        entity.SetTotalAmount(total);
        await db.SaveChangesAsync(ct);

        return await LoadAndMapAsync(db, entity.Id, ct);
    }

    public async Task<PaymentTransactionDto> ConfirmAsync(long id, ConfirmPaymentRequest request, long actorUserId, CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();

        var entity = await db.PaymentTransactions
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct)
            ?? throw new InvalidOperationException("Payment not found.");

        db.Entry(entity).Property(e => e.RowVersion).OriginalValue = Convert.FromBase64String(request.RowVersion);

        if (entity.Items.Count == 0)
            throw new InvalidOperationException("Payment must contain at least one item.");
        if (entity.TotalAmount <= 0)
            throw new InvalidOperationException("Payment total must be greater than zero.");

        var itemServiceIds = entity.Items.Select(i => i.ServiceId).ToArray();
        var itemCycleNumbers = entity.Items.Select(i => i.ServiceCycleNumber).ToArray();

        var existingConfirmedItems = await db.PaymentTransactionItems
            .Where(pti => itemServiceIds.Contains(pti.ServiceId))
            .Join(db.PaymentTransactions.Where(pt => pt.Status == PaymentTransaction.StatusConfirmed && !pt.IsDeleted),
                pti => pti.PaymentTransactionId,
                pt => pt.Id,
                (pti, pt) => pti)
            .ToArrayAsync(ct);

        foreach (var item in entity.Items)
        {
            var duplicate = existingConfirmedItems.FirstOrDefault(
                e => e.ServiceId == item.ServiceId && e.ServiceCycleNumber == item.ServiceCycleNumber);
            if (duplicate != null)
                throw new InvalidOperationException($"Service {item.ServiceId} cycle {item.ServiceCycleNumber} already has a confirmed payment.");
        }

        entity.Confirm(actorUserId);

        var dailyDate = entity.PaymentDate.Date;
        var monthlyDate = new DateTime(dailyDate.Year, dailyDate.Month, 1);

        await EnsureReconciliationPeriodAsync(db, entity.CompanyId, "DAILY", dailyDate, ct);
        await EnsureReconciliationPeriodAsync(db, entity.CompanyId, "MONTHLY", monthlyDate, ct);

        await db.SaveChangesAsync(ct);
        return await LoadAndMapAsync(db, entity.Id, ct);
    }

    public async Task<PaymentTransactionDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();

        var entity = await db.PaymentTransactions
            .AsNoTracking()
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);

        if (entity == null) return null;
        return MapToDto(entity);
    }

    public async Task<PagedResult<PaymentTransactionListDto>> ListAsync(
        long companyId, long? customerId, string? status,
        DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize, CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();

        var query = db.PaymentTransactions.AsNoTracking()
            .Where(p => p.CompanyId == companyId && !p.IsDeleted);

        if (customerId.HasValue)
            query = query.Where(p => p.CustomerId == customerId.Value);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(p => p.Status == status);
        if (dateFrom.HasValue)
            query = query.Where(p => p.PaymentDate >= dateFrom.Value);
        if (dateTo.HasValue)
            query = query.Where(p => p.PaymentDate <= dateTo.Value);

        var totalCount = await query.LongCountAsync(ct);
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(ct);

        return new PagedResult<PaymentTransactionListDto>
        {
            Items = items.Select(MapToListDto).ToArray(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PaymentTransactionDto> CorrectConfirmedAsync(long id, CorrectPaymentRequest request, long actorUserId, CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();

        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new InvalidOperationException("Correction reason is required.");

        var entity = await db.PaymentTransactions
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct)
            ?? throw new InvalidOperationException("Payment not found.");

        if (entity.Status != PaymentTransaction.StatusConfirmed)
            throw new InvalidOperationException("Only confirmed payments can be corrected.");

        db.Entry(entity).Property(e => e.RowVersion).OriginalValue = Convert.FromBase64String(request.RowVersion);

        var correlationId = Guid.NewGuid();
        var beforeData = JsonSerializer.Serialize(new
        {
            entity.CompanyId, entity.CustomerId, entity.PaymentMethod,
            entity.PaymentDate, entity.Notes, entity.TotalAmount,
            Items = entity.Items.Select(i => new { i.ServiceId, i.ServiceCycleNumber, i.Amount, i.Description })
        });

        var correctedFields = new List<string>();
        var oldCompanyId = entity.CompanyId;
        var oldPaymentDate = entity.PaymentDate;

        if (request.CompanyId.HasValue && request.CompanyId.Value != entity.CompanyId)
        {
            var companyExists = await db.Companies.AnyAsync(c => c.Id == request.CompanyId.Value, ct);
            if (!companyExists)
                throw new InvalidOperationException("Company not found.");
            correctedFields.Add(entity.CorrectField(nameof(PaymentTransaction.CompanyId), request.CompanyId.Value));
        }

        if (request.CustomerId.HasValue && request.CustomerId.Value != entity.CustomerId)
        {
            var customerExists = await db.Customers.AnyAsync(c => c.Id == request.CustomerId.Value, ct);
            if (!customerExists)
                throw new InvalidOperationException("Customer not found.");
            correctedFields.Add(entity.CorrectField(nameof(PaymentTransaction.CustomerId), request.CustomerId.Value));
        }

        if (request.PaymentMethod != null && request.PaymentMethod != entity.PaymentMethod)
            correctedFields.Add(entity.CorrectField(nameof(PaymentTransaction.PaymentMethod), request.PaymentMethod));

        if (request.PaymentDate.HasValue && request.PaymentDate.Value != entity.PaymentDate)
            correctedFields.Add(entity.CorrectField(nameof(PaymentTransaction.PaymentDate), request.PaymentDate.Value));

        if (request.Notes != entity.Notes)
            correctedFields.Add(entity.CorrectField(nameof(PaymentTransaction.Notes), request.Notes));

        if (request.Items != null)
        {
            var existingItems = await db.PaymentTransactionItems
                .Where(i => i.PaymentTransactionId == entity.Id)
                .ToArrayAsync(ct);
            db.PaymentTransactionItems.RemoveRange(existingItems);

            var serviceIds = request.Items.Select(i => i.ServiceId).Distinct().ToArray();
            var services = await db.Services.AsNoTracking()
                .Where(s => serviceIds.Contains(s.Id))
                .ToArrayAsync(ct);

            var serviceTypeCodes = await db.ServiceTypes.AsNoTracking()
                .Where(st => services.Select(s => s.ServiceTypeId).Contains(st.Id))
                .ToDictionaryAsync(st => st.Id, st => st.Code, ct);

            decimal total = 0;
            foreach (var itemReq in request.Items)
            {
                var service = services.FirstOrDefault(s => s.Id == itemReq.ServiceId)
                    ?? throw new InvalidOperationException($"Service {itemReq.ServiceId} not found.");

                var typeCode = serviceTypeCodes.GetValueOrDefault(service.ServiceTypeId, "UNKNOWN");
                var item = new PaymentTransactionItem(
                    entity.Id, itemReq.ServiceId, typeCode,
                    service.CycleNumber, itemReq.Amount, itemReq.Description);
                db.PaymentTransactionItems.Add(item);
                total += itemReq.Amount;
            }

            entity.SetTotalAmountForCorrection(total);
            correctedFields.Add("Items");
            correctedFields.Add("TotalAmount");
        }

        if (correctedFields.Count == 0)
            throw new InvalidOperationException("No fields were changed in the correction.");

        var affectedPeriodIds = new List<long>();
        bool companyOrDateChanged = entity.CompanyId != oldCompanyId || entity.PaymentDate != oldPaymentDate;
        if (companyOrDateChanged)
        {
            await MarkReconciliationPeriodsDirtyAsync(db, oldCompanyId, oldPaymentDate, affectedPeriodIds, ct);
            await MarkReconciliationPeriodsDirtyAsync(db, entity.CompanyId, entity.PaymentDate, affectedPeriodIds, ct);
        }

        var afterData = JsonSerializer.Serialize(new
        {
            entity.CompanyId, entity.CustomerId, entity.PaymentMethod,
            entity.PaymentDate, entity.Notes, entity.TotalAmount
        });

        var history = new PaymentCorrectionHistory(
            entity.Id, actorUserId, request.Reason,
            beforeData, afterData,
            string.Join(",", correctedFields),
            correlationId,
            affectedPeriodIds.Count > 0 ? JsonSerializer.Serialize(affectedPeriodIds) : null);

        db.PaymentCorrectionHistories.Add(history);
        await db.SaveChangesAsync(ct);

        return await LoadAndMapAsync(db, entity.Id, ct);
    }

    public async Task SoftDeleteDraftAsync(long id, SoftDeletePaymentRequest request, long actorUserId, CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();

        var entity = await db.PaymentTransactions
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct)
            ?? throw new InvalidOperationException("Payment not found.");

        db.Entry(entity).Property(e => e.RowVersion).OriginalValue = Convert.FromBase64String(request.RowVersion);

        entity.SoftDelete();
        await db.SaveChangesAsync(ct);
    }

    private async Task<string> GenerateBillCodeAsync(IOrganizationDbContext db, long companyId, DateTime paymentDate, CancellationToken ct)
    {
        var dateStr = paymentDate.ToString("yyyyMMdd");
        var prefix = $"PAY-{dateStr}-";

        var maxCode = await db.PaymentTransactions
            .Where(p => p.CompanyId == companyId && p.BillCode.StartsWith(prefix))
            .OrderByDescending(p => p.BillCode)
            .Select(p => p.BillCode)
            .FirstOrDefaultAsync(ct);

        int nextSeq = 1;
        if (maxCode != null)
        {
            var seqPart = maxCode.Substring(prefix.Length);
            if (int.TryParse(seqPart, out var parsed))
                nextSeq = parsed + 1;
        }

        return $"{prefix}{nextSeq:D4}";
    }

    private static async Task EnsureReconciliationPeriodAsync(
        IOrganizationDbContext db, long companyId, string periodType, DateTime periodDate, CancellationToken ct)
    {
        var exists = await db.ReconciliationPeriods.AnyAsync(
            rp => rp.CompanyId == companyId && rp.PeriodType == periodType && rp.PeriodDate == periodDate, ct);

        if (!exists)
        {
            var period = ReconciliationPeriod.Create(companyId, periodType, periodDate);
            db.ReconciliationPeriods.Add(period);
        }
    }

    private static async Task MarkReconciliationPeriodsDirtyAsync(
        IOrganizationDbContext db, long companyId, DateTime paymentDate,
        List<long> affectedPeriodIds, CancellationToken ct)
    {
        var dailyDate = paymentDate.Date;
        var monthlyDate = new DateTime(dailyDate.Year, dailyDate.Month, 1);

        var dailyPeriod = await db.ReconciliationPeriods
            .FirstOrDefaultAsync(rp => rp.CompanyId == companyId && rp.PeriodType == "DAILY" && rp.PeriodDate == dailyDate, ct);
        if (dailyPeriod != null)
        {
            dailyPeriod.MarkDirty();
            affectedPeriodIds.Add(dailyPeriod.Id);
        }
        else
        {
            var newPeriod = ReconciliationPeriod.Create(companyId, "DAILY", dailyDate);
            db.ReconciliationPeriods.Add(newPeriod);
        }

        var monthlyPeriod = await db.ReconciliationPeriods
            .FirstOrDefaultAsync(rp => rp.CompanyId == companyId && rp.PeriodType == "MONTHLY" && rp.PeriodDate == monthlyDate, ct);
        if (monthlyPeriod != null)
        {
            monthlyPeriod.MarkDirty();
            affectedPeriodIds.Add(monthlyPeriod.Id);
        }
        else
        {
            var newPeriod = ReconciliationPeriod.Create(companyId, "MONTHLY", monthlyDate);
            db.ReconciliationPeriods.Add(newPeriod);
        }
    }

    private async Task<PaymentTransactionDto> LoadAndMapAsync(IOrganizationDbContext db, long id, CancellationToken ct)
    {
        var entity = await db.PaymentTransactions
            .Include(p => p.Items)
            .FirstAsync(p => p.Id == id, ct);
        return MapToDto(entity);
    }

    private static PaymentTransactionDto MapToDto(PaymentTransaction entity)
    {
        return new PaymentTransactionDto
        {
            Id = entity.Id,
            BillCode = entity.BillCode,
            CompanyId = entity.CompanyId,
            CustomerId = entity.CustomerId,
            PaymentMethod = entity.PaymentMethod,
            PaymentDate = entity.PaymentDate,
            TotalAmount = entity.TotalAmount,
            CurrencyCode = entity.CurrencyCode,
            Status = entity.Status,
            Notes = entity.Notes,
            ConfirmedAt = entity.ConfirmedAt,
            ConfirmedByUserId = entity.ConfirmedByUserId,
            CreatedByUserId = entity.CreatedByUserId,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            RowVersion = Convert.ToBase64String(entity.RowVersion),
            Items = entity.Items.Select(i => new PaymentTransactionItemDto
            {
                Id = i.Id,
                PaymentTransactionId = i.PaymentTransactionId,
                ServiceId = i.ServiceId,
                ServiceTypeCode = i.ServiceTypeCode,
                ServiceCycleNumber = i.ServiceCycleNumber,
                Amount = i.Amount,
                Description = i.Description,
                CreatedAt = i.CreatedAt
            }).ToList()
        };
    }

    private static PaymentTransactionListDto MapToListDto(PaymentTransaction entity)
    {
        return new PaymentTransactionListDto
        {
            Id = entity.Id,
            BillCode = entity.BillCode,
            CompanyId = entity.CompanyId,
            CustomerId = entity.CustomerId,
            PaymentMethod = entity.PaymentMethod,
            PaymentDate = entity.PaymentDate,
            TotalAmount = entity.TotalAmount,
            Status = entity.Status,
            CreatedAt = entity.CreatedAt
        };
    }
}
