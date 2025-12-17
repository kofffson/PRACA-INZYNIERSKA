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
        try
        {
            var game = await _context.Games
                .Include(g => g.Participants)
                .ThenInclude(p => p.User)
                .Include(g => g.Organizer)
                .FirstOrDefaultAsync(g => g.GameId == gameId);

            if (game == null || !game.IsPaid || game.Cost <= 0)
                return Enumerable.Empty<Settlement>();

            // Sprawdź czy już istnieją rozliczenia
            var existingSettlements = await _context.Settlements
                .Where(s => s.GameId == gameId)
                .ToListAsync();

            if (existingSettlements.Any())
                return existingSettlements;

            var settlements = new List<Settlement>();
            var confirmedParticipants = game.Participants
                .Where(p => p.Status == "confirmed")
                .ToList();

            foreach (var participant in confirmedParticipants)
            {
                if (participant.UserId == game.OrganizerId)
                    continue; // Organizator nie płaci sam sobie

                var settlement = new Settlement
                {
                    GameId = gameId,
                    PayerId = participant.UserId,
                    RecipientId = game.OrganizerId,
                    Amount = game.Cost,
                    IsPaid = false,
                    Status = "pending",
                    DueDate = DateTime.SpecifyKind(game.StartDateTime.AddDays(-1), DateTimeKind.Utc),
                    CreatedAt = DateTime.UtcNow,
                    BankAccountNumber = game.Organizer.PhoneNumber
                };

                settlements.Add(settlement);
            }

            if (settlements.Any())
            {
                await _context.Settlements.AddRangeAsync(settlements);
                await _context.SaveChangesAsync();
            }

            return settlements;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas generowania rozliczeń dla gry {GameId}", gameId);
            return Enumerable.Empty<Settlement>();
        }
    }

    public async Task<bool> RegenerateSettlementsForGameAsync(int gameId)
    {
        try
        {
            var existingSettlements = await _context.Settlements
                .Where(s => s.GameId == gameId)
                .ToListAsync();

            _context.Settlements.RemoveRange(existingSettlements);
            await _context.SaveChangesAsync();

            await GenerateSettlementsForGameAsync(gameId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas regeneracji rozliczeń dla gry {GameId}", gameId);
            return false;
        }
    }

    public async Task<bool> MarkAsPaidAsync(int settlementId, string paymentMethod, string? paymentReference = null)
    {
        try
        {
            var settlement = await _context.Settlements.FindAsync(settlementId);
            if (settlement == null) return false;

            settlement.IsPaid = true;
            settlement.PaidAt = DateTime.UtcNow;
            settlement.Status = "paid";
            settlement.PaymentMethod = paymentMethod;
            settlement.PaymentReference = paymentReference;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas oznaczania płatności jako opłaconej {SettlementId}", settlementId);
            return false;
        }
    }

    public async Task<bool> MarkAsPaidByOrganizerAsync(int settlementId, string organizerId)
    {
        try
        {
            var settlement = await _context.Settlements
                .Include(s => s.Game)
                .FirstOrDefaultAsync(s => s.SettlementId == settlementId);

            if (settlement == null || settlement.Game.OrganizerId != organizerId)
                return false;

            settlement.IsPaid = true;
            settlement.PaidAt = DateTime.UtcNow;
            settlement.Status = "paid";
            settlement.PaymentMethod = "confirmed_by_organizer";
            settlement.Notes = $"Potwierdzono przez organizatora w dniu {DateTime.UtcNow:dd.MM.yyyy HH:mm}";

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas potwierdzania płatności przez organizatora {SettlementId}", settlementId);
            return false;
        }
    }

    public async Task<bool> CancelSettlementAsync(int settlementId, string reason)
    {
        try
        {
            var settlement = await _context.Settlements.FindAsync(settlementId);
            if (settlement == null) return false;

            settlement.Status = "cancelled";
            settlement.Notes = reason;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas anulowania rozliczenia {SettlementId}", settlementId);
            return false;
        }
    }

    public async Task<IEnumerable<Settlement>> GetUserPaymentsAsync(string userId, bool onlyUnpaid = false)
    {
        var query = _context.Settlements
            .Include(s => s.Game)
            .Include(s => s.Recipient)
            .Where(s => s.PayerId == userId);

        if (onlyUnpaid)
            query = query.Where(s => !s.IsPaid);

        return await query
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Settlement>> GetUserReceivablesAsync(string userId, bool onlyUnpaid = false)
    {
        var query = _context.Settlements
            .Include(s => s.Game)
            .Include(s => s.Payer)
            .Where(s => s.RecipientId == userId);

        if (onlyUnpaid)
            query = query.Where(s => !s.IsPaid);

        return await query
            .OrderByDescending(s => s.CreatedAt)
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
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOfMonth = startOfMonth.AddMonths(1);

        return await _context.Settlements
            .Where(s => s.PayerId == userId 
                && s.IsPaid 
                && s.PaidAt >= startOfMonth 
                && s.PaidAt < endOfMonth)
            .SumAsync(s => s.Amount);
    }

    public async Task<Dictionary<string, decimal>> GetMonthlyPaymentSummaryAsync(string userId, int year, int month)
    {
        var startOfMonth = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOfMonth = startOfMonth.AddMonths(1);

        var payments = await _context.Settlements
            .Include(s => s.Game)
            .Where(s => s.PayerId == userId 
                && s.Game.StartDateTime >= startOfMonth 
                && s.Game.StartDateTime < endOfMonth)
            .ToListAsync();

        var totalAmount = payments.Sum(s => s.Amount);
        var paidAmount = payments.Where(s => s.IsPaid).Sum(s => s.Amount);
        var unpaidAmount = payments.Where(s => !s.IsPaid).Sum(s => s.Amount);

        return new Dictionary<string, decimal>
        {
            { "Total", totalAmount },
            { "Paid", paidAmount },
            { "Unpaid", unpaidAmount },
            { "Count", payments.Count }
        };
    }

    public async Task<IEnumerable<Settlement>> GetSettlementsForGameAsync(int gameId)
    {
        return await _context.Settlements
            .Include(s => s.Payer)
            .Include(s => s.Game)
            .Where(s => s.GameId == gameId)
            .OrderBy(s => s.Payer.LastName)
            .ThenBy(s => s.Payer.FirstName)
            .ToListAsync();
    }

    public async Task<GameSettlementSummary> GetGameSettlementSummaryAsync(int gameId)
    {
        var game = await _context.Games
            .Include(g => g.Settlements)
            .ThenInclude(s => s.Payer)
            .FirstOrDefaultAsync(g => g.GameId == gameId);

        if (game == null)
            return new GameSettlementSummary();

        var settlements = game.Settlements.ToList();
        var paidCount = settlements.Count(s => s.IsPaid);
        var unpaidCount = settlements.Count(s => !s.IsPaid);

        return new GameSettlementSummary
        {
            GameId = game.GameId,
            GameName = game.GameName,
            TotalAmount = game.Cost,
            TotalParticipants = settlements.Count,
            PaidCount = paidCount,
            UnpaidCount = unpaidCount,
            TotalCollected = settlements.Where(s => s.IsPaid).Sum(s => s.Amount),
            TotalOutstanding = settlements.Where(s => !s.IsPaid).Sum(s => s.Amount),
            Settlements = settlements.Select(s => new SettlementDetail
            {
                SettlementId = s.SettlementId,
                PayerName = s.Payer.FullName,
                PayerEmail = s.Payer.Email ?? "",
                Amount = s.Amount,
                IsPaid = s.IsPaid,
                PaidAt = s.PaidAt,
                Status = s.Status,
                DueDate = s.DueDate
            }).ToList()
        };
    }

    public async Task SendPaymentRemindersAsync()
    {
        await Task.CompletedTask;
    }

    public async Task<bool> SendReminderToUserAsync(int settlementId)
    {
        await Task.CompletedTask;
        return true;
    }
}