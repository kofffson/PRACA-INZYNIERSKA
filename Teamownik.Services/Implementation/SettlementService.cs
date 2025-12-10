using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Teamownik.Data;
using Teamownik.Data.Models;
using Teamownik.Services.Interfaces;

namespace Teamownik.Services.Implementation;

public class SettlementService : ISettlementService
{
    private readonly TeamownikDbContext _context;
    private readonly ILogger<SettlementService> _logger;

    public SettlementService(TeamownikDbContext context, ILogger<SettlementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Settlement>> GenerateSettlementsForGameAsync(int gameId)
    {
        var game = await _context.Games
            .Include(g => g.Participants)
                .ThenInclude(p => p.User)
            .Include(g => g.Organizer)
            .FirstOrDefaultAsync(g => g.GameId == gameId);

        if (game == null || !game.IsPaid || game.Cost <= 0)
        {
            _logger.LogWarning($"Nie można wygenerować rozliczeń dla gry {gameId}");
            return new List<Settlement>();
        }

        // Sprawdź czy rozliczenia już istnieją
        var existingSettlements = await _context.Settlements
            .Where(s => s.GameId == gameId)
            .ToListAsync();

        if (existingSettlements.Any())
        {
            _logger.LogWarning($"Rozliczenia dla gry {gameId} już istnieją");
            return existingSettlements;
        }

        var settlements = new List<Settlement>();
        
        // Pobierz organizatora i jego dane bankowe
        var organizer = game.Organizer;
        var bankAccount = organizer.PhoneNumber; // Możesz dodać pole BankAccountNumber do ApplicationUser

        // Pobierz potwierdzonych uczestników (bez organizatora)
        var confirmedParticipants = game.Participants
            .Where(p => p.Status == "confirmed" && p.UserId != game.OrganizerId)
            .ToList();

        if (!confirmedParticipants.Any())
        {
            _logger.LogInformation($"Brak uczestników do rozliczenia dla gry {gameId}");
            return new List<Settlement>();
        }

        // Oblicz koszt na osobę (łącznie z gośćmi)
        var totalParticipants = confirmedParticipants.Sum(p => p.TotalSlotsOccupied);
        var costPerPerson = game.Cost / totalParticipants;

        _logger.LogInformation($"Gra {gameId}: Koszt całkowity: {game.Cost} zł, " +
                             $"Uczestników: {totalParticipants}, " +
                             $"Koszt na osobę: {costPerPerson:F2} zł");

        // Generuj rozliczenia dla każdego uczestnika
        foreach (var participant in confirmedParticipants)
        {
            var totalCost = costPerPerson * participant.TotalSlotsOccupied;
            
            var settlement = new Settlement
            {
                GameId = gameId,
                PayerId = participant.UserId,
                RecipientId = game.OrganizerId,
                Amount = Math.Round(totalCost, 2),
                BankAccountNumber = bankAccount,
                IsPaid = false,
                DueDate = game.StartDateTime.AddDays(7), // Termin płatności: 7 dni po grze
                Status = "pending",
                CreatedAt = DateTime.UtcNow,
                Notes = participant.GuestsCount > 0 
                    ? $"Płatność za {participant.TotalSlotsOccupied} miejsc (ty + {participant.GuestsCount} gości)"
                    : "Płatność za 1 miejsce"
            };

            settlements.Add(settlement);
            _context.Settlements.Add(settlement);
        }

        await _context.SaveChangesAsync();
        
        _logger.LogInformation($"Wygenerowano {settlements.Count} rozliczeń dla gry {gameId}");
        return settlements;
    }

    public async Task<bool> RegenerateSettlementsForGameAsync(int gameId)
    {
        try
        {
            // Usuń istniejące rozliczenia (tylko te nieopłacone)
            var existingSettlements = await _context.Settlements
                .Where(s => s.GameId == gameId && !s.IsPaid)
                .ToListAsync();

            _context.Settlements.RemoveRange(existingSettlements);
            await _context.SaveChangesAsync();

            // Wygeneruj nowe
            await GenerateSettlementsForGameAsync(gameId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Błąd podczas regeneracji rozliczeń dla gry {gameId}");
            return false;
        }
    }

    public async Task<bool> MarkAsPaidAsync(int settlementId, string paymentMethod, string? paymentReference = null)
    {
        var settlement = await _context.Settlements.FindAsync(settlementId);
        
        if (settlement == null || settlement.IsPaid)
            return false;

        settlement.IsPaid = true;
        settlement.PaidAt = DateTime.UtcNow;
        settlement.Status = "paid";
        settlement.PaymentMethod = paymentMethod;
        settlement.PaymentReference = paymentReference;

        await _context.SaveChangesAsync();
        
        _logger.LogInformation($"Płatność {settlementId} oznaczona jako zapłacona. Metoda: {paymentMethod}");
        return true;
    }

    public async Task<bool> CancelSettlementAsync(int settlementId, string reason)
    {
        var settlement = await _context.Settlements.FindAsync(settlementId);
        
        if (settlement == null || settlement.IsPaid)
            return false;

        settlement.Status = "cancelled";
        settlement.Notes = $"Anulowano: {reason}";

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<Settlement>> GetUserPaymentsAsync(string userId, bool onlyUnpaid = false)
    {
        var query = _context.Settlements
            .Include(s => s.Game)
            .Include(s => s.Recipient)
            .Where(s => s.PayerId == userId);

        if (onlyUnpaid)
            query = query.Where(s => !s.IsPaid && s.Status == "pending");

        return await query
            .OrderByDescending(s => s.DueDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Settlement>> GetUserReceivablesAsync(string userId, bool onlyUnpaid = false)
    {
        var query = _context.Settlements
            .Include(s => s.Game)
            .Include(s => s.Payer)
            .Where(s => s.RecipientId == userId);

        if (onlyUnpaid)
            query = query.Where(s => !s.IsPaid && s.Status == "pending");

        return await query
            .OrderByDescending(s => s.DueDate)
            .ToListAsync();
    }

    public async Task<Settlement?> GetSettlementByIdAsync(int settlementId)
    {
        return await _context.Settlements
            .Include(s => s.Game)
            .Include(s => s.Payer)
            .Include(s => s.Recipient)
            .FirstOrDefaultAsync(s => s.SettlementId == settlementId);
    }

    public async Task<decimal> GetTotalToPayAsync(string userId)
    {
        return await _context.Settlements
            .Where(s => s.PayerId == userId && !s.IsPaid && s.Status == "pending")
            .SumAsync(s => s.Amount);
    }

    public async Task<decimal> GetTotalToReceiveAsync(string userId)
    {
        return await _context.Settlements
            .Where(s => s.RecipientId == userId && !s.IsPaid && s.Status == "pending")
            .SumAsync(s => s.Amount);
    }

    public async Task<decimal> GetPaidThisMonthAsync(string userId)
    {
        var now = DateTime.UtcNow;
        var firstDayOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
    
        return await _context.Settlements
            .Where(s => s.PayerId == userId && 
                        s.IsPaid && 
                        s.PaidAt >= firstDayOfMonth)
            .SumAsync(s => s.Amount);
    }

    public async Task SendPaymentRemindersAsync()
    {
        var overdueSettlements = await _context.Settlements
            .Include(s => s.Payer)
            .Include(s => s.Game)
            .Where(s => !s.IsPaid && 
                       s.Status == "pending" && 
                       s.DueDate < DateTime.UtcNow)
            .ToListAsync();

        foreach (var settlement in overdueSettlements)
        {
            settlement.Status = "overdue";
            // TODO: Wyślij email/SMS przypomnienie
            _logger.LogInformation($"Przypomnienie o płatności wysłane do {settlement.Payer.Email}");
        }

        await _context.SaveChangesAsync();
    }

    public async Task<bool> SendReminderToUserAsync(int settlementId)
    {
        var settlement = await GetSettlementByIdAsync(settlementId);
        
        if (settlement == null || settlement.IsPaid)
            return false;

        // TODO: Implementacja wysyłki emaila/SMS
        _logger.LogInformation($"Przypomnienie wysłane dla płatności {settlementId}");
        return true;
    }
}