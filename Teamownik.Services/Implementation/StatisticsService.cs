using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Teamownik.Data;
using Teamownik.Services.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

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
                var gamesOrganized = await _context.Games
                    .Where(g => g.OrganizerId == userId && g.StartDateTime < DateTime.UtcNow && g.Status != "cancelled")
                    .CountAsync();

                var gamesPlayed = await _context.GameParticipants
                    .Where(gp => gp.UserId == userId && 
                               gp.Game.StartDateTime < DateTime.UtcNow && 
                               gp.Game.Status != "cancelled" &&
                               gp.Status == "confirmed")
                    .CountAsync();

                var groupMemberships = await _context.GroupMembers
                    .Where(gm => gm.UserId == userId)
                    .ToListAsync();

                foreach (var membership in groupMemberships)
                {
                    membership.GamesPlayed = gamesPlayed;
                    membership.GamesOrganized = gamesOrganized;

                    if (await ShouldPromoteToVIPAsync(userId, membership.GroupId))
                    {
                        await PromoteToVIPAsync(userId, membership.GroupId);
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
            return await _context.GameParticipants
                .Where(gp => gp.UserId == userId && 
                           gp.Game.StartDateTime < DateTime.UtcNow && 
                           gp.Game.Status != "cancelled" &&
                           gp.Status == "confirmed")
                .CountAsync();
        }

        public async Task<int> GetGamesOrganizedCountAsync(string userId)
        {
            return await _context.Games
                .Where(g => g.OrganizerId == userId && g.StartDateTime < DateTime.UtcNow && g.Status != "cancelled")
                .CountAsync();
        }
    }
}