using Teamownik.Data.Models;

namespace Teamownik.Services.Interfaces;

public interface IGameService
{
    Task<Game?> GetGameByIdAsync(int gameId);
    Task<IEnumerable<Game>> GetUpcomingGamesAsync();
    Task<IEnumerable<Game>> GetGamesByOrganizerAsync(string userId);
    Task<IEnumerable<Game>> GetGamesByParticipantAsync(string userId);
    Task<IEnumerable<Game>> GetGamesByGroupAsync(int groupId);
    Task<Game> CreateGameAsync(Game game);
    
    Task<bool> UpdateGameAsync(Game game);
    Task<bool> DeleteGameAsync(int gameId);
    Task<bool> JoinGameAsync(int gameId, string userId, int guestsCount = 0);
    Task<bool> AddParticipantByOrganizerAsync(int gameId, string organizerId, string targetUserId);
    
    Task<bool> LeaveGameAsync(int gameId, string userId);
    Task<(bool Success, string? ErrorMessage)> LeaveGameWithValidationAsync(int gameId, string userId);
    
    Task<bool> IsUserParticipantAsync(int gameId, string userId);
    Task<int> GetAvailableSpotsAsync(int gameId);
    Task<int> GetTotalSlotsOccupiedAsync(int gameId);
    Task<bool> UpdateGuestsCountAsync(int gameId, string userId, int newGuestsCount);
    Task<IEnumerable<GameParticipant>> GetWaitlistAsync(int gameId);
    Task<bool> MoveFromWaitlistAsync(int gameId, string userId);
    Task<bool> PromoteFromWaitlistAsync(int gameId);
    Task<bool> UpdateGameStatusAsync(int gameId, string status);
    Task<bool> CancelGameAsync(int gameId, string reason);
    Task<IEnumerable<Game>> CreateRecurringGamesAsync(Game template);
    Task MaintainRecurringSeriesAsync();
    Task CleanupOldGamesAsync();
}
