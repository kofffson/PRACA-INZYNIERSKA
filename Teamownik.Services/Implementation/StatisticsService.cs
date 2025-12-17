using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Teamownik.Data;
using Teamownik.Services.Interfaces;

namespace Teamownik.Services.Implementation
{
    public class StatisticsService : IStatisticsService
    {
        private readonly TeamownikDbContext _context;
        private readonly ILogger<StatisticsService> _logger;

        public StatisticsService(TeamownikDbContext context, ILogger<StatisticsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task UpdateUserStatisticsAsync(string userId)
        {
            try
            {
                var now = DateTime.UtcNow;

                var stats = await _context.GroupMembers
                    .Where(gm => gm.UserId == userId)
                    .Select(gm => new
                    {
                        Member = gm,
                        GamesOrganized = _context.Games
                            .Count(g => g.OrganizerId == userId && 
                                       g.StartDateTime < now && 
                                       g.Status != "cancelled"),
                        GamesPlayed = _context.GameParticipants
                            .Count(gp => gp.UserId == userId && 
                                        gp.Game.StartDateTime < now && 
                                        gp.Game.Status != "cancelled" &&
                                        gp.Status == "confirmed")
                    })
                    .ToListAsync();

                foreach (var stat in stats)
                {
                    stat.Member.GamesPlayed = stat.GamesPlayed;
                    stat.Member.GamesOrganized = stat.GamesOrganized;

                    var membershipDuration = (now - stat.Member.JoinedAt).TotalDays;
                    if (!stat.Member.IsVIP && 
                        (stat.GamesPlayed >= 10 || stat.GamesOrganized >= 5) && 
                        membershipDuration >= 30)
                    {
                        stat.Member.IsVIP = true;
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas aktualizacji statystyk użytkownika {UserId}", userId);
            }
        }

        public async Task<bool> ShouldPromoteToVIPAsync(string userId, int groupId)
        {
            var member = await _context.GroupMembers
                .AsNoTracking()
                .FirstOrDefaultAsync(gm => gm.UserId == userId && gm.GroupId == groupId);

            if (member == null || member.IsVIP) return false;

            var membershipDuration = (DateTime.UtcNow - member.JoinedAt).TotalDays;
            return (member.GamesPlayed >= 10 || member.GamesOrganized >= 5) && membershipDuration >= 30;
        }

        public async Task PromoteToVIPAsync(string userId, int groupId)
        {
            var member = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.UserId == userId && gm.GroupId == groupId);

            if (member != null && !member.IsVIP)
            {
                member.IsVIP = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetGamesPlayedCountAsync(string userId)
        {
            var now = DateTime.UtcNow;
            return await _context.GameParticipants
                .CountAsync(gp => gp.UserId == userId && 
                           gp.Game.StartDateTime < now && 
                           gp.Game.Status != "cancelled" &&
                           gp.Status == "confirmed");
        }

        public async Task<int> GetGamesOrganizedCountAsync(string userId)
        {
            var now = DateTime.UtcNow;
            return await _context.Games
                .CountAsync(g => g.OrganizerId == userId && 
                           g.StartDateTime < now && 
                           g.Status != "cancelled");
        }
    }
}
