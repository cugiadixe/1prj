using System.ComponentModel.DataAnnotations;

namespace PTKD.Application.Cards.DTOs;

public class CreateCardReprintRequest
{
    [Required]
    public long CompanyId { get; set; }
    
    [Required]
    public long CardId { get; set; }
    
    public string? ReasonCode { get; set; }
    
    public string? Notes { get; set; }
}
