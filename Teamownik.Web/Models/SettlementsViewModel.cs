using Teamownik.Data.Models;

namespace Teamownik.Web.Models;

public class SettlementsViewModel
{
    public string CurrentMonthName { get; set; } = string.Empty;
    public decimal TotalToPay { get; set; }
    public decimal TotalPaidThisMonth { get; set; }

    public List<SettlementViewModel> PaymentsToMake { get; set; } = [];
    public List<SettlementViewModel> PaidHistory { get; set; } = [];

    public MonthlyBreakdown? MonthlyBreakdown { get; set; }
}

public class MonthlyBreakdown
{
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal UnpaidAmount { get; set; }
    public int PaymentsCount { get; set; }
}

public class ReceivablesViewModel
{
    public decimal TotalToReceive { get; set; }
    public List<SettlementViewModel> Receivables { get; set; } = [];
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
        Constants.SettlementStatus.Paid => Constants.CssClasses.BgSuccess,
        Constants.SettlementStatus.Overdue => Constants.CssClasses.BgDanger,
        Constants.SettlementStatus.Pending => Constants.CssClasses.BgWarning,
        _ => Constants.CssClasses.BgSecondary
    };
}

public class GameSettlementsViewModel
{
    public int GameId { get; set; }
    public string GameName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int TotalParticipants { get; set; }
    public int PaidCount { get; set; }
    public int UnpaidCount { get; set; }
    public decimal TotalCollected { get; set; }
    public decimal TotalOutstanding { get; set; }
    public List<GameSettlementDetailViewModel> Settlements { get; set; } = [];

    public decimal CollectionPercentage => TotalAmount * TotalParticipants > 0
        ? (TotalCollected / (TotalAmount * TotalParticipants)) * 100
        : 0;
}

public class GameSettlementDetailViewModel
{
    public int SettlementId { get; set; }
    public string PayerName { get; set; } = string.Empty;
    public string PayerEmail { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }

    public bool IsOverdue => !IsPaid && DueDate < DateTime.UtcNow;
}