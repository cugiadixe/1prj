using System;

namespace PTKD.Domain.Entities;

public class Customer
{
    public long Id { get; private set; }
    public string CustomerCode { get; private set; } = null!;
    public long ProfileId { get; private set; }
    public string CustomerStatus { get; private set; } = null!;
    public long? SurvivorCustomerId { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }
    public long? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public long? UpdatedByUserId { get; private set; }

    public Profile Profile { get; private set; } = null!;

    private Customer() { }

    public Customer(string customerCode, long profileId)
    {
        CustomerCode = customerCode ?? throw new ArgumentNullException(nameof(customerCode));
        ProfileId = profileId;
        CustomerStatus = "ACTIVE";
        CreatedAt = DateTime.UtcNow;
    }

    public void SetCreatedBy(long userId)
    {
        CreatedByUserId = userId;
    }

    public void MarkUpdated(long userId)
    {
        UpdatedAt = DateTime.UtcNow;
        UpdatedByUserId = userId;
    }

    public void SetStatus(string status, long? updatedByUserId, long? survivorCustomerId = null)
    {
        CustomerStatus = status;
        SurvivorCustomerId = survivorCustomerId;
        UpdatedAt = DateTime.UtcNow;
        UpdatedByUserId = updatedByUserId;
    }
}
