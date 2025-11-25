namespace Teamownik.Data.Models;

public class Settlement
{
    public int SettlementId { get; set; }
    public int GameId { get; set; }
    public string PayerId { get; set; } = string.Empty;
    public string RecipientId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? BankAccountNumber { get; set; }
    public bool IsPaid { get; set; } = false;
    public DateTime DueDate { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Game Game { get; set; } = null!;
    public ApplicationUser Payer { get; set; } = null!;
    public ApplicationUser Recipient { get; set; } = null!;
}