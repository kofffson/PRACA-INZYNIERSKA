using Teamownik.Data.Models;

namespace Teamownik.Services.Interfaces;

public interface ISettlementService
{
    Task<IEnumerable<Settlement>> GenerateSettlementsForGameAsync(int gameId);
    Task<bool> RegenerateSettlementsForGameAsync(int gameId);
    
    Task<bool> MarkAsPaidAsync(int settlementId, string paymentMethod, string? paymentReference = null);
    Task<bool> MarkAsPaidByOrganizerAsync(int settlementId, string organizerId);
    Task<bool> CancelSettlementAsync(int settlementId, string reason);
    
    Task<IEnumerable<Settlement>> GetUserPaymentsAsync(string userId, bool onlyUnpaid = false);
    Task<IEnumerable<Settlement>> GetUserReceivablesAsync(string userId, bool onlyUnpaid = false);
    Task<Settlement?> GetSettlementByIdAsync(int settlementId);
    
    // Rozliczenia zbiorcze użytkownika
    Task<decimal> GetTotalToPayAsync(string userId);
    Task<decimal> GetTotalToReceiveAsync(string userId);
    Task<decimal> GetPaidThisMonthAsync(string userId);
    Task<Dictionary<string, decimal>> GetMonthlyPaymentSummaryAsync(string userId, int year, int month);
    
    // Rozliczenia dla organizatora gry
    Task<IEnumerable<Settlement>> GetSettlementsForGameAsync(int gameId);
    Task<GameSettlementSummary> GetGameSettlementSummaryAsync(int gameId);
    
    Task SendPaymentRemindersAsync();
    Task<bool> SendReminderToUserAsync(int settlementId);
}

public class GameSettlementSummary
{
    public int GameId { get; set; }
    public string GameName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int TotalParticipants { get; set; }
    public int PaidCount { get; set; }
    public int UnpaidCount { get; set; }
    public decimal TotalCollected { get; set; }
    public decimal TotalOutstanding { get; set; }
    public List<SettlementDetail> Settlements { get; set; } = new();
}

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
