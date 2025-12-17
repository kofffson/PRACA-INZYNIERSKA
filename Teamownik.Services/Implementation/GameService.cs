using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Teamownik.Data;
using Teamownik.Data.Models;
using Teamownik.Services.Interfaces;

namespace Teamownik.Services.Implementation;

public class GameService : IGameService
{
    private readonly TeamownikDbContext _context;
    private readonly IStatisticsService _statisticsService;
    private readonly ILogger<GameService> _logger;
    private readonly ISettlementService _settlementService;

    public GameService(
        TeamownikDbContext context, 
        IStatisticsService statisticsService, 
        ILogger<GameService> logger,
        ISettlementService settlementService)
    {
        _context = context;
        _statisticsService = statisticsService;
        _logger = logger;
        _settlementService = settlementService;
    }

    public async Task<Game?> GetGameByIdAsync(int gameId)
    {
        return await _context.Games
            .Include(g => g.Organizer)
            .Include(g => g.Group)
            .Include(g => g.Participants)
                .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(g => g.GameId == gameId);
    }

    public async Task<IEnumerable<Game>> GetUpcomingGamesAsync()
    {
        var now = DateTime.UtcNow;
        return await _context.Games
            .Where(g => g.StartDateTime > now && g.Status != "cancelled")
            .Include(g => g.Organizer)
            .Include(g => g.Group)
            .OrderBy(g => g.StartDateTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Game>> GetGamesByOrganizerAsync(string userId)
    {
        return await _context.Games
            .Where(g => g.OrganizerId == userId)
            .Include(g => g.Organizer)
            .Include(g => g.Group)
            .Include(g => g.Participants)
            .OrderByDescending(g => g.StartDateTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Game>> GetGamesByParticipantAsync(string userId)
    {
        return await _context.Games
            .Where(g => g.Participants.Any(p => p.UserId == userId))
            .Include(g => g.Organizer)
            .Include(g => g.Group)
            .OrderBy(g => g.StartDateTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Game>> GetGamesByGroupAsync(int groupId)
    {
        return await _context.Games
            .Where(g => g.GroupId == groupId && g.Status != "cancelled")
            .Include(g => g.Organizer)
            .Include(g => g.Participants)
            .OrderBy(g => g.StartDateTime)
            .ToListAsync();
    }

    public async Task<Game> CreateGameAsync(Game game)
    {
        try
        {
            game.CreatedAt = DateTime.UtcNow;
            game.Status = "open";
            
            _context.Games.Add(game);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Game created successfully with ID: {GameId}", game.GameId);
            return game;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating game");
            throw;
        }
    }

    public async Task<bool> UpdateGameAsync(Game game)
    {
        _context.Games.Update(game);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteGameAsync(int gameId)
    {
        var game = await _context.Games.FindAsync(gameId);
        if (game == null) return false;
        
        _context.Games.Remove(game);
        return await _context.SaveChangesAsync() > 0;
    }
    
    public async Task<bool> JoinGameAsync(int gameId, string userId, int guestsCount = 0)
    {
        var game = await _context.Games
            .Include(g => g.Participants.Where(p => p.Status == "confirmed" || p.UserId == userId))
            .FirstOrDefaultAsync(g => g.GameId == gameId);

        if (game == null) return false;

        var timeUntilStart = game.StartDateTime - DateTime.UtcNow;
        //if (timeUntilStart.TotalMinutes < 30) 
            if (timeUntilStart.TotalMinutes < 0)

        {
            _logger.LogWarning(
                "Próba zapisu na spotkanie {GameId} za mniej niż 30 minut przed startem. Pozostało: {Minutes:F1} minut",
                gameId, timeUntilStart.TotalMinutes);
            return false;
        }

        if (game.Participants.Any(p => p.UserId == userId))
            return false;

        var totalSlotsOccupied = game.Participants
            .Where(p => p.Status == "confirmed")
            .Sum(p => p.TotalSlotsOccupied);
        
        var totalSlotsNeeded = 1 + guestsCount;
        var availableSlots = game.MaxParticipants - totalSlotsOccupied;
        var isReserve = availableSlots < totalSlotsNeeded;

        int? waitlistPosition = null;
        if (isReserve)
        {
            var maxPosition = await _context.GameParticipants
                .Where(p => p.GameId == gameId && p.Status == "reserve")
                .MaxAsync(p => (int?)p.WaitlistPosition) ?? 0;
            
            waitlistPosition = maxPosition + 1;
        }

        var participant = new GameParticipant
        {
            GameId = gameId,
            UserId = userId,
            GuestsCount = guestsCount,
            Status = isReserve ? "reserve" : "confirmed",
            JoinedAt = DateTime.UtcNow,
            ConfirmedAt = isReserve ? null : DateTime.UtcNow,
            WaitlistPosition = waitlistPosition
        };

        _context.GameParticipants.Add(participant);

        if (!isReserve && totalSlotsOccupied + totalSlotsNeeded >= game.MaxParticipants)
        {
            game.Status = "full";
        }

        await _context.SaveChangesAsync();
        await _statisticsService.UpdateUserStatisticsAsync(userId);

        return true;
    }

    public async Task<bool> LeaveGameAsync(int gameId, string userId)
    {
        var participant = await _context.GameParticipants
            .FirstOrDefaultAsync(p => p.GameId == gameId && p.UserId == userId);

        if (participant == null) return false;

        var wasConfirmed = participant.Status == "confirmed";
        
        _context.GameParticipants.Remove(participant);
        await _context.SaveChangesAsync();

        await _statisticsService.UpdateUserStatisticsAsync(userId);

        if (wasConfirmed)
        {
            await PromoteFromWaitlistAsync(gameId);
        }

        return true;
    }

    public async Task<bool> IsUserParticipantAsync(int gameId, string userId)
    {
        return await _context.GameParticipants
            .AnyAsync(p => p.GameId == gameId && p.UserId == userId);
    }

    public async Task<int> GetAvailableSpotsAsync(int gameId)
    {
        var game = await _context.Games.FindAsync(gameId);
        if (game == null) return 0;

        var occupiedSlots = await GetTotalSlotsOccupiedAsync(gameId);
        return Math.Max(0, game.MaxParticipants - occupiedSlots);
    }

    public async Task<int> GetTotalSlotsOccupiedAsync(int gameId)
    {
        var participants = await _context.GameParticipants
            .Where(p => p.GameId == gameId && p.Status == "confirmed")
            .Select(p => new { p.GuestsCount }) 
            .ToListAsync(); 
    
        return participants.Sum(p => 1 + p.GuestsCount); 
    }
    
    public async Task<bool> UpdateGuestsCountAsync(int gameId, string userId, int newGuestsCount)
    {
        var game = await _context.Games
            .Include(g => g.Participants.Where(p => p.Status == "confirmed"))
            .FirstOrDefaultAsync(g => g.GameId == gameId);

        if (game == null) return false;

        var participant = game.Participants.FirstOrDefault(p => p.UserId == userId);
        if (participant == null) return false;
        
        var currentOccupied = game.Participants.Sum(p => p.TotalSlotsOccupied);
        var oldSlots = participant.TotalSlotsOccupied;
        var newSlots = 1 + newGuestsCount;
        var difference = newSlots - oldSlots;
        
        if (currentOccupied + difference > game.MaxParticipants)
        {
            return false;
        }
        
        participant.GuestsCount = newGuestsCount;
        await _context.SaveChangesAsync();
        
        return true;
    }

    public async Task<IEnumerable<GameParticipant>> GetWaitlistAsync(int gameId)
    {
        return await _context.GameParticipants
            .Where(p => p.GameId == gameId && p.Status == "reserve")
            .Include(p => p.User)
            .OrderBy(p => p.WaitlistPosition)
            .ToListAsync();
    }

    public async Task<bool> MoveFromWaitlistAsync(int gameId, string userId)
    {
        var participant = await _context.GameParticipants
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.GameId == gameId && p.UserId == userId && p.Status == "reserve");

        if (participant == null) return false;

        participant.Status = "confirmed";
        participant.ConfirmedAt = DateTime.UtcNow;
        participant.WaitlistPosition = null;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> PromoteFromWaitlistAsync(int gameId)
    {
        var game = await _context.Games
            .Include(g => g.Participants)
            .FirstOrDefaultAsync(g => g.GameId == gameId);

        if (game == null) return false;

        var confirmedSlots = game.Participants
            .Where(p => p.Status == "confirmed")
            .Sum(p => p.TotalSlotsOccupied);
        
        var availableSpots = game.MaxParticipants - confirmedSlots;
        if (availableSpots <= 0) return false;

        var waitlist = game.Participants
            .Where(p => p.Status == "reserve")
            .OrderBy(p => p.WaitlistPosition)
            .ToList();
        
        foreach (var waitingParticipant in waitlist)
        {
            var slotsNeeded = waitingParticipant.TotalSlotsOccupied;
            
            if (availableSpots >= slotsNeeded)
            {
                waitingParticipant.Status = "confirmed";
                waitingParticipant.ConfirmedAt = DateTime.UtcNow;
                waitingParticipant.WaitlistPosition = null;
                availableSpots -= slotsNeeded;
            }
            
            if (availableSpots <= 0) break;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateGameStatusAsync(int gameId, string status)
    {
        var game = await _context.Games.FindAsync(gameId);
        if (game == null) return false;

        game.Status = status;
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> CancelGameAsync(int gameId, string reason)
    {
        var game = await _context.Games.FindAsync(gameId);
        if (game == null) return false;

        game.Status = "cancelled";
        game.CancellationReason = reason;
        await _context.SaveChangesAsync();

        return true;
    }
    
    private DateTime CalculateNextDate(DateTime currentDate, string? pattern)
    {
        return pattern switch
        {
            "daily" => currentDate.AddDays(1),
            "weekly" => currentDate.AddDays(7),
            "biweekly" => currentDate.AddDays(14),
            "monthly" => currentDate.AddMonths(1),
            _ => currentDate
        };
    }

    public async Task<IEnumerable<Game>> CreateRecurringGamesAsync(Game template)
    {
        var games = new List<Game>();
        var currentDate = template.StartDateTime;
        var limit = template.RecurrencePattern == "daily" ? 1 : 4;
        var seriesId = Guid.NewGuid();
        var duration = template.EndDateTime - template.StartDateTime;

        for (int i = 0; i < limit; i++)
        {
            var game = new Game
            {
                GameName = template.GameName,
                OrganizerId = template.OrganizerId,
                GroupId = template.GroupId,
                Location = template.Location,
                StartDateTime = currentDate,
                EndDateTime = currentDate.Add(duration),
                Cost = template.Cost,
                IsPaid = template.IsPaid,
                MaxParticipants = template.MaxParticipants,
                IsRecurring = true,
                RecurrencePattern = template.RecurrencePattern,
                RecurrenceSeriesId = seriesId,
                Status = "open",
                IsPublic = template.IsPublic,
                CreatedAt = DateTime.UtcNow
            };
        
            _context.Games.Add(game);
            games.Add(game);

            currentDate = CalculateNextDate(currentDate, template.RecurrencePattern);
        }

        await _context.SaveChangesAsync();
        return games;
    }

    public async Task MaintainRecurringSeriesAsync()
    {
        var now = DateTime.UtcNow;

        var activeSeries = await _context.Games
            .Where(g => g.IsRecurring && 
                       g.RecurrenceSeriesId.HasValue && 
                       g.Status != "cancelled" &&
                       g.StartDateTime > now)
            .GroupBy(g => new { g.RecurrenceSeriesId, g.RecurrencePattern, g.OrganizerId })
            .Select(g => new
            {
                SeriesId = g.Key.RecurrenceSeriesId!.Value,
                Pattern = g.Key.RecurrencePattern,
                OrganizerId = g.Key.OrganizerId,
                LastUpcomingGame = g.OrderByDescending(game => game.StartDateTime).FirstOrDefault(),
                UpcomingCount = g.Count()
            })
            .Where(s => s.Pattern != null && s.LastUpcomingGame != null)
            .ToListAsync();

        foreach (var series in activeSeries)
        {
            var requiredLimit = series.Pattern == "daily" ? 1 : 4;

            if (series.UpcomingCount < requiredLimit)
            {
                var lastDateTime = series.LastUpcomingGame!.StartDateTime;
                var newStartDateTime = CalculateNextDate(lastDateTime, series.Pattern);
                var duration = series.LastUpcomingGame.EndDateTime - series.LastUpcomingGame.StartDateTime;
                var newEndDateTime = newStartDateTime.Add(duration);
                
                _logger.LogInformation(
                    "Uzupełnianie serii {SeriesId}. Obecnie {Count} nadchodzących gier. Tworzenie nowej na {NewDate}",
                    series.SeriesId, series.UpcomingCount, newStartDateTime);

                var newGame = new Game
                {
                    GameName = series.LastUpcomingGame.GameName,
                    OrganizerId = series.OrganizerId,
                    GroupId = series.LastUpcomingGame.GroupId,
                    Location = series.LastUpcomingGame.Location,
                    StartDateTime = newStartDateTime,
                    EndDateTime = newEndDateTime,
                    Cost = series.LastUpcomingGame.Cost,
                    IsPaid = series.LastUpcomingGame.IsPaid,
                    MaxParticipants = series.LastUpcomingGame.MaxParticipants,
                    IsRecurring = true,
                    RecurrencePattern = series.Pattern,
                    RecurrenceSeriesId = series.SeriesId,
                    Status = "open",
                    IsPublic = series.LastUpcomingGame.IsPublic,
                    CreatedAt = now
                };

                _context.Games.Add(newGame);
            }
        }
        
        await _context.SaveChangesAsync();
    }
    
    public async Task CleanupOldGamesAsync()
    {
        try
        {
            // var cutoffDate = DateTime.UtcNow.AddDays(-1);
            //
            // var oldGames = await _context.Games
            //     .Where(g => g.EndDateTime < cutoffDate && 
            //                g.Status != "cancelled" && 
            //                g.Status != "completed")
            //     .ToListAsync();
            var cutoffDate = DateTime.UtcNow.AddMinutes(-2); 
    
            var oldGames = await _context.Games
                .Where(g => g.StartDateTime < cutoffDate &&    
                            g.Status != "cancelled" && 
                            g.Status != "completed")
                .ToListAsync();

            if (!oldGames.Any())
            {
                _logger.LogInformation("Brak przeterminowanych gier do wyczyszczenia");
                return;
            }

            var gameIds = oldGames.Select(g => g.GameId).ToList();
            var allParticipants = await _context.GameParticipants
                .Where(gp => gameIds.Contains(gp.GameId) && gp.Status == "confirmed")
                .ToListAsync();

            var participantsByGame = allParticipants
                .GroupBy(p => p.GameId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var game in oldGames)
            {
                var participants = participantsByGame.GetValueOrDefault(game.GameId) ?? new List<GameParticipant>();

                foreach (var participant in participants)
                {
                    await _statisticsService.UpdateUserStatisticsAsync(participant.UserId);
                }

                game.Status = "completed";
            
                if (game.IsPaid && game.Cost > 0)
                {
                    await _settlementService.GenerateSettlementsForGameAsync(game.GameId);
                    _logger.LogInformation("Wygenerowano rozliczenia dla gry {GameId}", game.GameId);
                }
            }

            await _context.SaveChangesAsync();
            await MaintainRecurringSeriesAsync();
    
            _logger.LogInformation("Wyczyszczono {Count} przeterminowanych gier", oldGames.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas czyszczenia przeterminowanych gier");
        }
    }
}