using Teamownik.Data.Models;

namespace Teamownik.Web.Models;

public class SettlementsViewModel
{
    public string CurrentMonthName { get; set; } = string.Empty;
    public decimal TotalToPay { get; set; }
    public decimal TotalPaidThisMonth { get; set; }
    
    public List<SettlementViewModel> PaymentsToMake { get; set; } = new();
    public List<SettlementViewModel> PaidHistory { get; set; } = new();
}

public class ReceivablesViewModel
{
    public decimal TotalToReceive { get; set; }
    public List<SettlementViewModel> Receivables { get; set; } = new();
}

public class SettlementViewModel
{
    public int SettlementId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string RecipientName { get; set; } = string.Empty;
    public string? BankAccountNumber { get; set; }
    public int ParticipantsCount { get; set; }
    public bool IsPaid { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? PaymentMethod { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? PaidAt { get; set; } 
    
    public bool IsOverdue => !IsPaid && DueDate < DateTime.UtcNow;
    public string StatusBadgeClass => Status switch
    {
        "paid" => "bg-success",
        "overdue" => "bg-danger",
        "pending" => "bg-warning",
        _ => "bg-secondary"
    };
}