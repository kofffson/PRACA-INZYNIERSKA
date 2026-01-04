using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Teamownik.Data;
using Teamownik.Data.Models;
using Teamownik.Services.Interfaces;

namespace Teamownik.Services.Implementation;

public class GameService(
    TeamownikDbContext context,
    IStatisticsService statisticsService,
    ILogger<GameService> logger,
    ISettlementService settlementService) : IGameService
{
    public async Task<Game?> GetGameByIdAsync(int gameId)
    {
        return await context.Games
            .Include(g => g.Organizer)
            .Include(g => g.Group)
            .Include(g => g.Participants)
                .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(g => g.GameId == gameId);
    }

    public async Task<IEnumerable<Game>> GetUpcomingGamesAsync()
    {
        var now = DateTime.UtcNow;
        return await context.Games
            .Where(g => g.StartDateTime > now && g.Status != Constants.GameStatus.Cancelled)
            .Include(g => g.Organizer)
            .Include(g => g.Group)
            .OrderBy(g => g.StartDateTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Game>> GetGamesByOrganizerAsync(string userId)
    {
        return await context.Games
            .Where(g => g.OrganizerId == userId)
            .Include(g => g.Organizer)
            .Include(g => g.Group)
            .Include(g => g.Participants)
            .OrderByDescending(g => g.StartDateTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Game>> GetGamesByParticipantAsync(string userId)
    {
        return await context.Games
            .Where(g => g.Participants.Any(p => p.UserId == userId))
            .Include(g => g.Organizer)
            .Include(g => g.Group)
            .OrderBy(g => g.StartDateTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Game>> GetGamesByGroupAsync(int groupId)
    {
        return await context.Games
            .Where(g => g.GroupId == groupId && g.Status != Constants.GameStatus.Cancelled)
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
            game.Status = Constants.GameStatus.Default;

            context.Games.Add(game);
            await context.SaveChangesAsync();

            var organizerParticipant = new GameParticipant
            {
                GameId = game.GameId,
                UserId = game.OrganizerId,
                GuestsCount = 0,
                Status = Constants.ParticipantStatus.Confirmed,
                JoinedAt = DateTime.UtcNow,
                ConfirmedAt = DateTime.UtcNow,
                WaitlistPosition = null
            };

            context.GameParticipants.Add(organizerParticipant);
            await context.SaveChangesAsync();

            logger.LogInformation("Game created with ID: {GameId}, organizer auto-enrolled: {OrganizerId}", 
                game.GameId, game.OrganizerId);
            
            return game;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating game");
            throw;
        }
    }

    public async Task<bool> UpdateGameAsync(Game game)
    {
        context.Games.Update(game);
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteGameAsync(int gameId)
    {
        var game = await context.Games.FindAsync(gameId);
        if (game == null) return false;

        context.Games.Remove(game);
        return await context.SaveChangesAsync() > 0;
    }
    
    public async Task<(bool Success, string? ErrorMessage)> LeaveGameWithValidationAsync(int gameId, string userId)
    {
        var game = await context.Games
            .Include(g => g.Participants)
            .FirstOrDefaultAsync(g => g.GameId == gameId);

        if (game == null)
            return (false, "Nie znaleziono spotkania.");

        if (game.OrganizerId == userId)
            return (false, "Organizator nie może wypisać się ze swojego spotkania. Możesz je odwołać lub usunąć.");

        var participant = game.Participants.FirstOrDefault(p => p.UserId == userId);
        if (participant == null)
            return (false, "Nie jesteś zapisany na to spotkanie.");

        var wasConfirmed = participant.Status == Constants.ParticipantStatus.Confirmed;

        context.GameParticipants.Remove(participant);
        await context.SaveChangesAsync();

        await statisticsService.UpdateUserStatisticsAsync(userId);

        if (wasConfirmed)
        {
            await PromoteFromWaitlistAsync(gameId);
        }

        return (true, null);
    }
    
    public async Task<bool> JoinGameAsync(int gameId, string userId, int guestsCount = 0)
    {
        var game = await context.Games
            .Include(g => g.Participants.Where(p => p.Status == Constants.ParticipantStatus.Confirmed || p.UserId == userId))
            .FirstOrDefaultAsync(g => g.GameId == gameId);

        if (game == null) return false;

        var timeUntilStart = game.StartDateTime - DateTime.UtcNow;
        if (timeUntilStart.TotalMinutes < Constants.Defaults.RegistrationCloseMinutes)
        {
            logger.LogWarning(
                "Attempt to join game {GameId} less than {Minutes} minutes before start",
                gameId, Constants.Defaults.RegistrationCloseMinutes);
            return false;
        }

        if (game.Participants.Any(p => p.UserId == userId))
            return false;

        var totalSlotsOccupied = game.Participants
            .Where(p => p.Status == Constants.ParticipantStatus.Confirmed)
            .Sum(p => p.TotalSlotsOccupied);

        var totalSlotsNeeded = 1 + guestsCount;
        var availableSlots = game.MaxParticipants - totalSlotsOccupied;
        var isReserve = availableSlots < totalSlotsNeeded;

        int? waitlistPosition = null;
        if (isReserve)
        {
            var maxPosition = await context.GameParticipants
                .Where(p => p.GameId == gameId && p.Status == Constants.ParticipantStatus.Reserve)
                .MaxAsync(p => p.WaitlistPosition) ?? 0;

            waitlistPosition = maxPosition + 1;
        }

        var participant = new GameParticipant
        {
            GameId = gameId,
            UserId = userId,
            GuestsCount = guestsCount,
            Status = isReserve ? Constants.ParticipantStatus.Reserve : Constants.ParticipantStatus.Confirmed,
            JoinedAt = DateTime.UtcNow,
            ConfirmedAt = isReserve ? null : DateTime.UtcNow,
            WaitlistPosition = waitlistPosition
        };

        context.GameParticipants.Add(participant);

        if (!isReserve && totalSlotsOccupied + totalSlotsNeeded >= game.MaxParticipants)
        {
            game.Status = Constants.GameStatus.Full;
        }

        await context.SaveChangesAsync();
        await statisticsService.UpdateUserStatisticsAsync(userId);

        return true;
    }
    
    public Task<bool> AddParticipantByOrganizerAsync(int gameId, string organizerId, string targetUserId)
    {
        logger.LogWarning("Attempt to use deprecated AddParticipantByOrganizerAsync method");
        return Task.FromResult(false);
    }

    public async Task<bool> LeaveGameAsync(int gameId, string userId)
    {
        var participant = await context.GameParticipants
            .FirstOrDefaultAsync(p => p.GameId == gameId && p.UserId == userId);

        if (participant == null) return false;

        var wasConfirmed = participant.Status == Constants.ParticipantStatus.Confirmed;

        context.GameParticipants.Remove(participant);
        await context.SaveChangesAsync();

        await statisticsService.UpdateUserStatisticsAsync(userId);

        if (wasConfirmed)
        {
            await PromoteFromWaitlistAsync(gameId);
        }

        return true;
    }

    public async Task<bool> IsUserParticipantAsync(int gameId, string userId)
    {
        return await context.GameParticipants
            .AnyAsync(p => p.GameId == gameId && p.UserId == userId);
    }

    public async Task<int> GetAvailableSpotsAsync(int gameId)
    {
        var game = await context.Games.FindAsync(gameId);
        if (game == null) return 0;

        var occupiedSlots = await GetTotalSlotsOccupiedAsync(gameId);
        return Math.Max(0, game.MaxParticipants - occupiedSlots);
    }

    public async Task<int> GetTotalSlotsOccupiedAsync(int gameId)
    {
        var participants = await context.GameParticipants
            .Where(p => p.GameId == gameId && p.Status == Constants.ParticipantStatus.Confirmed)
            .Select(p => new { p.GuestsCount })
            .ToListAsync();

        return participants.Sum(p => 1 + p.GuestsCount);
    }

    public async Task<bool> UpdateGuestsCountAsync(int gameId, string userId, int newGuestsCount)
    {
        var game = await context.Games
            .Include(g => g.Participants.Where(p => p.Status == Constants.ParticipantStatus.Confirmed))
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
        await context.SaveChangesAsync();

        return true;
    }

    public async Task<IEnumerable<GameParticipant>> GetWaitlistAsync(int gameId)
    {
        return await context.GameParticipants
            .Where(p => p.GameId == gameId && p.Status == Constants.ParticipantStatus.Reserve)
            .Include(p => p.User)
            .OrderBy(p => p.WaitlistPosition)
            .ToListAsync();
    }

    public async Task<bool> MoveFromWaitlistAsync(int gameId, string userId)
    {
        var participant = await context.GameParticipants
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.GameId == gameId && p.UserId == userId && p.Status == Constants.ParticipantStatus.Reserve);

        if (participant == null) return false;

        participant.Status = Constants.ParticipantStatus.Confirmed;
        participant.ConfirmedAt = DateTime.UtcNow;
        participant.WaitlistPosition = null;

        await context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> PromoteFromWaitlistAsync(int gameId)
    {
        var game = await context.Games
            .Include(g => g.Participants)
            .FirstOrDefaultAsync(g => g.GameId == gameId);

        if (game == null) return false;

        var confirmedSlots = game.Participants
            .Where(p => p.Status == Constants.ParticipantStatus.Confirmed)
            .Sum(p => p.TotalSlotsOccupied);

        var availableSpots = game.MaxParticipants - confirmedSlots;
        if (availableSpots <= 0) return false;

        var waitlist = game.Participants
            .Where(p => p.Status == Constants.ParticipantStatus.Reserve)
            .OrderBy(p => p.WaitlistPosition)
            .ToList();

        foreach (var waitingParticipant in waitlist)
        {
            var slotsNeeded = waitingParticipant.TotalSlotsOccupied;

            if (availableSpots >= slotsNeeded)
            {
                waitingParticipant.Status = Constants.ParticipantStatus.Confirmed;
                waitingParticipant.ConfirmedAt = DateTime.UtcNow;
                waitingParticipant.WaitlistPosition = null;
                availableSpots -= slotsNeeded;
            }

            if (availableSpots <= 0) break;
        }

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateGameStatusAsync(int gameId, string status)
    {
        var game = await context.Games.FindAsync(gameId);
        if (game == null) return false;

        game.Status = status;
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<bool> CancelGameAsync(int gameId, string reason)
    {
        var game = await context.Games.FindAsync(gameId);
        if (game == null) return false;

        game.Status = Constants.GameStatus.Cancelled;
        game.CancellationReason = reason;
        await context.SaveChangesAsync();

        return true;
    }

    private static DateTime CalculateNextDate(DateTime currentDate, string? pattern) => pattern switch
    {
        Constants.RecurrencePattern.Daily => currentDate.AddDays(1),
        Constants.RecurrencePattern.Weekly => currentDate.AddDays(7),
        Constants.RecurrencePattern.Biweekly => currentDate.AddDays(14),
        Constants.RecurrencePattern.Monthly => currentDate.AddMonths(1),
        _ => currentDate
    };

    public async Task<IEnumerable<Game>> CreateRecurringGamesAsync(Game template)
    {
        var games = new List<Game>();
        var currentDate = template.StartDateTime;
        var limit = template.RecurrencePattern == Constants.RecurrencePattern.Daily ? 1 : 4;
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
                Status = Constants.GameStatus.Default,
                IsPublic = template.IsPublic,
                CreatedAt = DateTime.UtcNow
            };

            context.Games.Add(game);
            games.Add(game);

            currentDate = CalculateNextDate(currentDate, template.RecurrencePattern);
        }

        await context.SaveChangesAsync();
        return games;
    }

    public async Task MaintainRecurringSeriesAsync()
    {
        var now = DateTime.UtcNow;

        var activeSeries = await context.Games
            .Where(g => g.IsRecurring &&
                       g.RecurrenceSeriesId.HasValue &&
                       g.Status != Constants.GameStatus.Cancelled &&
                       g.StartDateTime > now)
            .GroupBy(g => new { g.RecurrenceSeriesId, g.RecurrencePattern, g.OrganizerId })
            .Select(g => new
            {
                SeriesId = g.Key.RecurrenceSeriesId!.Value,
                Pattern = g.Key.RecurrencePattern,
                g.Key.OrganizerId,
                LastUpcomingGame = g.OrderByDescending(game => game.StartDateTime).FirstOrDefault(),
                UpcomingCount = g.Count()
            })
            .Where(s => s.Pattern != null && s.LastUpcomingGame != null)
            .ToListAsync();

        foreach (var series in activeSeries)
        {
            var requiredLimit = series.Pattern == Constants.RecurrencePattern.Daily ? 1 : 4;

            if (series.UpcomingCount < requiredLimit)
            {
                var lastDateTime = series.LastUpcomingGame!.StartDateTime;
                var newStartDateTime = CalculateNextDate(lastDateTime, series.Pattern);
                var duration = series.LastUpcomingGame.EndDateTime - series.LastUpcomingGame.StartDateTime;
                var newEndDateTime = newStartDateTime.Add(duration);

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
                    Status = Constants.GameStatus.Default,
                    IsPublic = series.LastUpcomingGame.IsPublic,
                    CreatedAt = now
                };

                context.Games.Add(newGame);
            }
        }

        await context.SaveChangesAsync();
    }

    public async Task CleanupOldGamesAsync()
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddMinutes(-2);

            var oldGames = await context.Games
                .Where(g => g.StartDateTime < cutoffDate &&
                            g.Status != Constants.GameStatus.Cancelled &&
                            g.Status != Constants.GameStatus.Completed)
                .ToListAsync();

            if (oldGames.Count == 0)
            {
                logger.LogInformation("No expired games to cleanup");
                return;
            }

            var gameIds = oldGames.ConvertAll(g => g.GameId);
            var allParticipants = await context.GameParticipants
                .Where(gp => gameIds.Contains(gp.GameId) && gp.Status == Constants.ParticipantStatus.Confirmed)
                .ToListAsync();

            var participantsByGame = allParticipants
                .GroupBy(p => p.GameId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var game in oldGames)
            {
                foreach (var participant in participantsByGame.GetValueOrDefault(game.GameId) ?? [])
                {
                    await statisticsService.UpdateUserStatisticsAsync(participant.UserId);
                }

                game.Status = Constants.GameStatus.Completed;

                if (game.IsPaid && game.Cost > 0)
                {
                    await settlementService.GenerateSettlementsForGameAsync(game.GameId);
                }
            }

            await context.SaveChangesAsync();
            await MaintainRecurringSeriesAsync();

            logger.LogInformation("Cleaned up {Count} expired games", oldGames.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during game cleanup");
        }
    }
}
