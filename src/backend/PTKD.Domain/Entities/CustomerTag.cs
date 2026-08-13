using System;

namespace PTKD.Domain.Entities;

/// <summary>Gắn thẻ (loại CUSTOMER) vào một khách hàng.</summary>
public class CustomerTag
{
    public long Id { get; private set; }
    public long CustomerId { get; private set; }
    public long TagId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public long? CreatedByUserId { get; private set; }

    public Tag? Tag { get; private set; }

    private CustomerTag() { }

    public CustomerTag(long customerId, long tagId, long? createdByUserId)
    {
        CustomerId = customerId;
        TagId = tagId;
        CreatedByUserId = createdByUserId;
        CreatedAt = DateTime.UtcNow;
    }
}
