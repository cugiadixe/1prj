using System;

namespace PTKD.Domain.Entities;

/// <summary>Gắn thẻ (loại GRAVE) vào một phần mộ.</summary>
public class GraveTag
{
    public long Id { get; private set; }
    public long GraveId { get; private set; }
    public long TagId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public long? CreatedByUserId { get; private set; }

    public Tag? Tag { get; private set; }

    private GraveTag() { }

    public GraveTag(long graveId, long tagId, long? createdByUserId)
    {
        GraveId = graveId;
        TagId = tagId;
        CreatedByUserId = createdByUserId;
        CreatedAt = DateTime.UtcNow;
    }
}
