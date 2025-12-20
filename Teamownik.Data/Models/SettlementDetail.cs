namespace Teamownik.Data.Models;

public class SettlementDetail
{
    public int SettlementId { get; set; }
    public string PayerName { get; set; } = string.Empty;
    public string PayerEmail { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
}