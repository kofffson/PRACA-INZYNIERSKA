namespace Teamownik.Data.Models;

public class Settlement
{
    public int SettlementId { get; set; }
    public int GameId { get; set; }
    public string PayerId { get; set; } = string.Empty;
    public string RecipientId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? BankAccountNumber { get; set; }
    public bool IsPaid { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public string? PaymentMethod { get; set; }
    public string? PaymentReference { get; set; } 
    public string? PaymentProviderId { get; set; } 
    public string Status { get; set; } = "pending"; 
    public string? Notes { get; set; } 
    
    public Game Game { get; set; } = null!;
    public ApplicationUser Payer { get; set; } = null!;
    public ApplicationUser Recipient { get; set; } = null!;
}