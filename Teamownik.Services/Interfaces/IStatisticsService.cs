using System.Threading.Tasks;

namespace Teamownik.Services.Interfaces
{
    public interface IStatisticsService
    {
        Task UpdateUserStatisticsAsync(string userId);
        Task<bool> ShouldPromoteToVIPAsync(string userId, int groupId);
        Task PromoteToVIPAsync(string userId, int groupId);
        Task<int> GetGamesPlayedCountAsync(string userId);
        Task<int> GetGamesOrganizedCountAsync(string userId);
    }
}