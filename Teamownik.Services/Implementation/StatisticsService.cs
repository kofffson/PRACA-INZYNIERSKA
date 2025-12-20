using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Teamownik.Data;
using Teamownik.Data.Models;
using Teamownik.Services.Interfaces;

namespace Teamownik.Services.Implementation;

public class StatisticsService(TeamownikDbContext context, ILogger<StatisticsService> logger) : IStatisticsService
{
    public async Task UpdateUserStatisticsAsync(string userId)
    {
        try
        {
            var now = DateTime.UtcNow;

            var stats = await context.GroupMembers
                .Where(gm => gm.UserId == userId)
                .Select(gm => new
                {
                    Member = gm,
                    GamesOrganized = context.Games
                        .Count(g => g.OrganizerId == userId &&
                                   g.StartDateTime < now &&
                                   g.Status != Constants.GameStatus.Cancelled),
                    GamesPlayed = context.GameParticipants
                        .Count(gp => gp.UserId == userId &&
                                    gp.Game.StartDateTime < now &&
                                    gp.Game.Status != Constants.GameStatus.Cancelled &&
                                    gp.Status == Constants.ParticipantStatus.Confirmed)
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

            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Błąd podczas aktualizacji statystyk użytkownika {UserId}", userId);
        }
    }

    public async Task<bool> ShouldPromoteToVIPAsync(string userId, int groupId)
    {
        var member = await context.GroupMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(gm => gm.UserId == userId && gm.GroupId == groupId);

        if (member == null || member.IsVIP) return false;

        var membershipDuration = (DateTime.UtcNow - member.JoinedAt).TotalDays;
        return (member.GamesPlayed >= 10 || member.GamesOrganized >= 5) && membershipDuration >= 30;
    }

    public async Task PromoteToVIPAsync(string userId, int groupId)
    {
        var member = await context.GroupMembers
            .FirstOrDefaultAsync(gm => gm.UserId == userId && gm.GroupId == groupId);

        if (member?.IsVIP == false)
        {
            member.IsVIP = true;
            await context.SaveChangesAsync();
        }
    }

    public async Task<int> GetGamesPlayedCountAsync(string userId)
    {
        var now = DateTime.UtcNow;
        return await context.GameParticipants
            .CountAsync(gp => gp.UserId == userId &&
                       gp.Game.StartDateTime < now &&
                       gp.Game.Status != Constants.GameStatus.Cancelled &&
                       gp.Status == Constants.ParticipantStatus.Confirmed);
    }

    public async Task<int> GetGamesOrganizedCountAsync(string userId)
    {
        var now = DateTime.UtcNow;
        return await context.Games
            .CountAsync(g => g.OrganizerId == userId &&
                       g.StartDateTime < now &&
                       g.Status != Constants.GameStatus.Cancelled);
    }
}