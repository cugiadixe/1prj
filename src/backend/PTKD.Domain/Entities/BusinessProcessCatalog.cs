using System;

namespace PTKD.Domain.Entities;

public class BusinessProcessCatalog
{
    public string ProcessCode { get; private set; } = null!;
    public string ProcessName { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsApprovalRequired { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private BusinessProcessCatalog() { }
}
