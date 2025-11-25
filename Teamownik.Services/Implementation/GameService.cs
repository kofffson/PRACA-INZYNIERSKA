using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Teamownik.Data;
using Teamownik.Data.Models;
using Teamownik.Services.Interfaces;

namespace Teamownik.Services.Implementation;

public class GameService : IGameService
{
    private readonly TeamownikDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IStatisticsService _statisticsService;
    private readonly ILogger<GameService> _logger;

    public GameService(TeamownikDbContext context, IEmailService emailService, 
        IStatisticsService statisticsService, ILogger<GameService> logger)
    {
        _context = context;
        _emailService = emailService;
        _statisticsService = statisticsService;
        _logger = logger;
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

    public async Task<IEnumerable<Game>> GetAllGamesAsync()
    {
        return await _context.Games
            .Include(g => g.Organizer)
            .Include(g => g.Group)
            .OrderBy(g => g.StartDateTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Game>> GetPublicGamesAsync()
    {
        return await _context.Games
            .Where(g => g.IsPublic && (g.Status == "open" || g.Status == "full"))
            .Include(g => g.Organizer)
            .OrderBy(g => g.StartDateTime)
            .ToListAsync();
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

    public async Task<bool> JoinGameAsync(int gameId, string userId)
    {
        var game = await GetGameByIdAsync(gameId);
        if (game == null) return false;

        if (await IsUserParticipantAsync(gameId, userId))
            return false;

        var confirmedCount = await GetConfirmedParticipantsCountAsync(gameId);
        var isReserve = confirmedCount >= game.MaxParticipants;

        int? waitlistPosition = null;
        if (isReserve)
        {
            var currentWaitlist = await GetWaitlistAsync(gameId);
            waitlistPosition = currentWaitlist.Count() + 1;
        }

        var participant = new GameParticipant
        {
            GameId = gameId,
            UserId = userId,
            Status = isReserve ? "reserve" : "confirmed",
            JoinedAt = DateTime.UtcNow,
            ConfirmedAt = isReserve ? null : DateTime.UtcNow,
            WaitlistPosition = waitlistPosition
        };

        _context.GameParticipants.Add(participant);
        await _context.SaveChangesAsync();

        await _statisticsService.UpdateUserStatisticsAsync(userId);

        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            if (isReserve)
            {
                await _emailService.SendWaitlistNotificationAsync(
                    user.Email!, 
                    user.FullName, 
                    game.GameName, 
                    participant.WaitlistPosition ?? 0
                );
            }
            else
            {
                await _emailService.SendGameConfirmationAsync(
                    user.Email!, 
                    user.FullName, 
                    game.GameName, 
                    game.StartDateTime, 
                    game.Location
                );
            }
        }

        if (confirmedCount + 1 >= game.MaxParticipants)
        {
            game.Status = "full";
            await UpdateGameAsync(game);
        }

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

        var confirmed = await GetConfirmedParticipantsCountAsync(gameId);
        return Math.Max(0, game.MaxParticipants - confirmed);
    }

    public async Task<int> GetConfirmedParticipantsCountAsync(int gameId)
    {
        return await _context.GameParticipants
            .CountAsync(p => p.GameId == gameId && p.Status == "confirmed");
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

        var game = await GetGameByIdAsync(gameId);
        if (game == null) return false;

        participant.Status = "confirmed";
        participant.ConfirmedAt = DateTime.UtcNow;
        participant.WaitlistPosition = null;

        await _context.SaveChangesAsync();

        await _emailService.SendPromotedFromWaitlistAsync(
            participant.User.Email!,
            participant.User.FullName,
            game.GameName,
            game.StartDateTime
        );

        return true;
    }

    public async Task<bool> PromoteFromWaitlistAsync(int gameId)
    {
        var game = await GetGameByIdAsync(gameId);
        if (game == null) return false;

        var availableSpots = await GetAvailableSpotsAsync(gameId);
        if (availableSpots <= 0) return false;

        var waitlist = await GetWaitlistAsync(gameId);
        var toPromote = waitlist.FirstOrDefault();

        if (toPromote != null)
        {
            return await MoveFromWaitlistAsync(gameId, toPromote.UserId);
        }

        return false;
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
        var game = await GetGameByIdAsync(gameId);
        if (game == null) return false;

        game.Status = "cancelled";
        game.CancellationReason = reason;
        await _context.SaveChangesAsync();

        var participants = game.Participants.Where(p => p.Status == "confirmed");
        foreach (var participant in participants)
        {
            await _emailService.SendGameCancellationAsync(
                participant.User.Email!,
                participant.User.FullName,
                game.GameName,
                game.StartDateTime,
                reason
            );
        }

        return true;
    }

    public async Task<bool> ReopenGameAsync(int gameId)
    {
        var game = await _context.Games.FindAsync(gameId);
        if (game == null) return false;

        var confirmed = await GetConfirmedParticipantsCountAsync(gameId);
        game.Status = confirmed >= game.MaxParticipants ? "full" : "open";
        
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<IEnumerable<Game>> CreateRecurringGamesAsync(Game template, int occurrences)
    {
        var games = new List<Game>();
        var currentDate = template.StartDateTime;

        for (int i = 0; i < occurrences; i++)
        {
            var game = new Game
            {
                GameName = template.GameName,
                OrganizerId = template.OrganizerId,
                GroupId = template.GroupId,
                Location = template.Location,
                StartDateTime = currentDate,
                EndDateTime = currentDate.Add(template.EndDateTime - template.StartDateTime),
                Cost = template.Cost,
                IsPaid = template.IsPaid,
                MaxParticipants = template.MaxParticipants,
                IsRecurring = true,
                RecurrencePattern = template.RecurrencePattern,
                Status = "open",
                IsPublic = template.IsPublic,
                CreatedAt = DateTime.UtcNow
            };

            _context.Games.Add(game);
            games.Add(game);

            currentDate = template.RecurrencePattern switch
            {
                "daily" => currentDate.AddDays(1),
                "weekly" => currentDate.AddDays(7),
                "biweekly" => currentDate.AddDays(14),
                "monthly" => currentDate.AddMonths(1),
                _ => currentDate
            };
        }

        await _context.SaveChangesAsync();
        return games;
    }

    public async Task CleanupOldGamesAsync()
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-1);
        
            var oldGames = await _context.Games
                .Where(g => g.EndDateTime < cutoffDate && g.Status != "cancelled")
                .ToListAsync();

            foreach (var game in oldGames)
            {
                var participants = await _context.GameParticipants
                    .Where(gp => gp.GameId == game.GameId && gp.Status == "confirmed")
                    .ToListAsync();

                foreach (var participant in participants)
                {
                    await _statisticsService.UpdateUserStatisticsAsync(participant.UserId);
                }

                game.Status = "completed";
            }

            await _context.SaveChangesAsync();
        
            _logger.LogInformation("Wyczyściono {Count} przeterminowanych gier", oldGames.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas czyszczenia przeterminowanych gier");
        }
    }
}