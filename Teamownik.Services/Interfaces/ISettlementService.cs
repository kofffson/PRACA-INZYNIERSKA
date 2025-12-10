using Teamownik.Data.Models;

namespace Teamownik.Services.Interfaces;

public interface ISettlementService
{
    Task<IEnumerable<Settlement>> GenerateSettlementsForGameAsync(int gameId);
    Task<bool> RegenerateSettlementsForGameAsync(int gameId);
    
    Task<bool> MarkAsPaidAsync(int settlementId, string paymentMethod, string? paymentReference = null);
    Task<bool> CancelSettlementAsync(int settlementId, string reason);
    
    Task<IEnumerable<Settlement>> GetUserPaymentsAsync(string userId, bool onlyUnpaid = false);
    Task<IEnumerable<Settlement>> GetUserReceivablesAsync(string userId, bool onlyUnpaid = false);
    Task<Settlement?> GetSettlementByIdAsync(int settlementId);
    
    Task<decimal> GetTotalToPayAsync(string userId);
    Task<decimal> GetTotalToReceiveAsync(string userId);
    Task<decimal> GetPaidThisMonthAsync(string userId);
    
    Task SendPaymentRemindersAsync();
    Task<bool> SendReminderToUserAsync(int settlementId);
}